using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using DocumentService.Models;
using DocumentService.Protos;
using System.Text.Json;

namespace DocumentService.Services;

public class DocumentServiceImpl : Protos.DocumentService.DocumentServiceBase
{
    private readonly DocumentDbContext _context;
    private readonly ILogger<DocumentServiceImpl> _logger;
    private readonly IDocumentProcessor _documentProcessor;
    private readonly string _storageDirectory;

    public DocumentServiceImpl(
        DocumentDbContext context,
        ILogger<DocumentServiceImpl> logger,
        IDocumentProcessor documentProcessor,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _documentProcessor = documentProcessor;
        _storageDirectory = configuration.GetValue<string>("StorageDirectory") ?? "/app/storage";
        
        // Ensure storage directory exists
        Directory.CreateDirectory(_storageDirectory);
    }

    public override async Task<ProcessDocumentResponse> ProcessDocument(ProcessDocumentRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Processing document {Filename} for claim {ClaimId}", request.Filename, request.ClaimId);

            // Validate request
            if (request.ClaimId <= 0)
            {
                return new ProcessDocumentResponse
                {
                    Success = false,
                    Message = "Invalid claim ID"
                };
            }

            if (request.FileData.IsEmpty)
            {
                return new ProcessDocumentResponse
                {
                    Success = false,
                    Message = "No file data provided"
                };
            }

            // Create claim-specific directory
            var claimDir = Path.Combine(_storageDirectory, $"claim_{request.ClaimId}");
            Directory.CreateDirectory(claimDir);

            // Generate unique filename
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var fileExtension = Path.GetExtension(request.Filename);
            var baseName = Path.GetFileNameWithoutExtension(request.Filename);
            var uniqueFilename = $"{baseName}_{timestamp}{fileExtension}";
            var filePath = Path.Combine(claimDir, uniqueFilename);

            // Save file to disk
            await File.WriteAllBytesAsync(filePath, request.FileData.ToByteArray());

            // Process the document
            var extractedMetadata = await _documentProcessor.ProcessDocumentAsync(
                filePath, 
                request.FileType, 
                request.FileData.ToByteArray());

            // Create document record
            var document = new ClaimDocument
            {
                ClaimId = request.ClaimId,
                DocumentType = request.DocumentType,
                Filename = request.Filename,
                FilePath = filePath,
                FileSize = request.FileData.Length,
                MimeType = request.FileType,
                UploadedBy = request.UploadedBy > 0 ? request.UploadedBy : null,
                UploadDate = DateTime.UtcNow,
                IsProcessed = true,
                ProcessingStatus = extractedMetadata.ContainsKey("status") ? extractedMetadata["status"] : "completed",
                ExtractedData = JsonSerializer.Serialize(extractedMetadata),
                CreatedAt = DateTime.UtcNow
            };

            _context.ClaimDocuments.Add(document);
            await _context.SaveChangesAsync();

            // Prepare response metadata
            var responseMetadata = new DocumentMetadata
            {
                Id = document.Id,
                ClaimId = document.ClaimId,
                DocumentType = document.DocumentType,
                Filename = document.Filename,
                FilePath = document.FilePath,
                FileSize = document.FileSize,
                MimeType = document.MimeType ?? "",
                UploadedBy = document.UploadedBy ?? 0,
                UploadDate = document.UploadDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                IsProcessed = document.IsProcessed,
                ProcessingStatus = document.ProcessingStatus
            };

            // Add extracted data to response
            foreach (var kvp in extractedMetadata)
            {
                responseMetadata.ExtractedData[kvp.Key] = kvp.Value;
            }

            _logger.LogInformation("Successfully processed document {DocumentId} for claim {ClaimId}", 
                document.Id, request.ClaimId);

            return new ProcessDocumentResponse
            {
                Success = true,
                Message = "Document processed successfully",
                DocumentId = document.Id,
                Metadata = responseMetadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {Filename} for claim {ClaimId}", 
                request.Filename, request.ClaimId);
            
            return new ProcessDocumentResponse
            {
                Success = false,
                Message = "An error occurred while processing the document"
            };
        }
    }

    public override async Task<GetDocumentMetadataResponse> GetDocumentMetadata(GetDocumentMetadataRequest request, ServerCallContext context)
    {
        try
        {
            var document = await _context.ClaimDocuments
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId);

            if (document == null)
            {
                return new GetDocumentMetadataResponse
                {
                    Success = false,
                    Message = "Document not found"
                };
            }

            var metadata = new DocumentMetadata
            {
                Id = document.Id,
                ClaimId = document.ClaimId,
                DocumentType = document.DocumentType,
                Filename = document.Filename,
                FilePath = document.FilePath,
                FileSize = document.FileSize,
                MimeType = document.MimeType ?? "",
                UploadedBy = document.UploadedBy ?? 0,
                UploadDate = document.UploadDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                IsProcessed = document.IsProcessed,
                ProcessingStatus = document.ProcessingStatus
            };

            // Parse and add extracted data
            if (!string.IsNullOrEmpty(document.ExtractedData))
            {
                try
                {
                    var extractedData = JsonSerializer.Deserialize<Dictionary<string, string>>(document.ExtractedData);
                    if (extractedData != null)
                    {
                        foreach (var kvp in extractedData)
                        {
                            metadata.ExtractedData[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse extracted data for document {DocumentId}", request.DocumentId);
                }
            }

            return new GetDocumentMetadataResponse
            {
                Success = true,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document metadata for document {DocumentId}", request.DocumentId);
            
            return new GetDocumentMetadataResponse
            {
                Success = false,
                Message = "An error occurred while retrieving document metadata"
            };
        }
    }

    public override async Task<ListClaimDocumentsResponse> ListClaimDocuments(ListClaimDocumentsRequest request, ServerCallContext context)
    {
        try
        {
            var documents = await _context.ClaimDocuments
                .Where(d => d.ClaimId == request.ClaimId)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();

            var responseDocuments = documents.Select(document =>
            {
                var metadata = new DocumentMetadata
                {
                    Id = document.Id,
                    ClaimId = document.ClaimId,
                    DocumentType = document.DocumentType,
                    Filename = document.Filename,
                    FilePath = document.FilePath,
                    FileSize = document.FileSize,
                    MimeType = document.MimeType ?? "",
                    UploadedBy = document.UploadedBy ?? 0,
                    UploadDate = document.UploadDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    IsProcessed = document.IsProcessed,
                    ProcessingStatus = document.ProcessingStatus
                };

                // Parse extracted data
                if (!string.IsNullOrEmpty(document.ExtractedData))
                {
                    try
                    {
                        var extractedData = JsonSerializer.Deserialize<Dictionary<string, string>>(document.ExtractedData);
                        if (extractedData != null)
                        {
                            foreach (var kvp in extractedData)
                            {
                                metadata.ExtractedData[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse extracted data for document {DocumentId}", document.Id);
                    }
                }

                return metadata;
            }).ToList();

            return new ListClaimDocumentsResponse
            {
                Success = true,
                Documents = { responseDocuments },
                Message = $"Found {responseDocuments.Count} documents for claim {request.ClaimId}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing documents for claim {ClaimId}", request.ClaimId);
            
            return new ListClaimDocumentsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving claim documents"
            };
        }
    }

    public override async Task<DeleteDocumentResponse> DeleteDocument(DeleteDocumentRequest request, ServerCallContext context)
    {
        try
        {
            var document = await _context.ClaimDocuments
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId);

            if (document == null)
            {
                return new DeleteDocumentResponse
                {
                    Success = false,
                    Message = "Document not found"
                };
            }

            // Delete file from disk
            try
            {
                if (File.Exists(document.FilePath))
                {
                    File.Delete(document.FilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file {FilePath} for document {DocumentId}", 
                    document.FilePath, request.DocumentId);
            }

            // Remove from database
            _context.ClaimDocuments.Remove(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted document {DocumentId} by user {DeletedBy}", 
                request.DocumentId, request.DeletedBy);

            return new DeleteDocumentResponse
            {
                Success = true,
                Message = "Document deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", request.DocumentId);
            
            return new DeleteDocumentResponse
            {
                Success = false,
                Message = "An error occurred while deleting the document"
            };
        }
    }
}