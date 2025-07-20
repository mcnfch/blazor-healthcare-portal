using Microsoft.EntityFrameworkCore;
using BlazorApp.Models;
using BlazorApp.Models.DTOs;

namespace BlazorApp.Services;

public class FileDocumentService : IDocumentService
{
    private readonly HealthcareDbContext _context;
    private readonly ILogger<FileDocumentService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _uploadPath;

    public FileDocumentService(HealthcareDbContext context, ILogger<FileDocumentService> logger, IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
        
        // Create uploads directory
        _uploadPath = Path.Combine(_environment.ContentRootPath, "uploads", "documents");
        Directory.CreateDirectory(_uploadPath);
        _logger.LogInformation($"Document upload path: {_uploadPath}");
    }

    public async Task<List<DocumentDto>> GetClaimDocumentsAsync(int claimId)
    {
        try
        {
            var documents = await _context.ClaimDocuments
                .Where(d => d.ClaimId == claimId)
                .OrderByDescending(d => d.UploadDate)
                .Select(d => new DocumentDto(
                    d.Id,
                    d.ClaimId,
                    d.DocumentType,
                    d.Filename,
                    d.FilePath,
                    d.FileSize,
                    d.MimeType ?? "",
                    d.UploadedBy.HasValue ? d.UploadedBy.Value : 0,
                    d.UploadDate != null ? d.UploadDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    d.IsProcessed,
                    d.ProcessingStatus,
                    new Dictionary<string, string>() // Empty for now
                ))
                .ToListAsync();

            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for claim {ClaimId}", claimId);
            return new List<DocumentDto>();
        }
    }

    public async Task<DocumentDto?> GetDocumentMetadataAsync(int documentId)
    {
        try
        {
            var document = await _context.ClaimDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return null;

            return new DocumentDto(
                document.Id,
                document.ClaimId,
                document.DocumentType,
                document.Filename,
                document.FilePath,
                document.FileSize,
                document.MimeType ?? "",
                document.UploadedBy ?? 0,
                document.UploadDate != null ? document.UploadDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                document.IsProcessed,
                document.ProcessingStatus,
                new Dictionary<string, string>() // Empty for now
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document metadata for document {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<DocumentUploadResponse> UploadDocumentAsync(int claimId, string filename, byte[] fileData, string fileType, string documentType, int uploadedBy)
    {
        try
        {
            // Validate inputs
            if (claimId <= 0 || string.IsNullOrEmpty(filename) || fileData == null || fileData.Length == 0)
            {
                return new DocumentUploadResponse(false, "Invalid input parameters", null);
            }

            // Verify claim exists
            var claimExists = await _context.Claims.AnyAsync(c => c.Id == claimId);
            if (!claimExists)
            {
                return new DocumentUploadResponse(false, "Claim not found", null);
            }

            // Generate safe filename
            var sanitizedFilename = SanitizeFilename(filename);
            var fileExtension = Path.GetExtension(sanitizedFilename);
            var uniqueFilename = $"{Guid.NewGuid()}{fileExtension}";
            var fullPath = Path.Combine(_uploadPath, uniqueFilename);

            // Save file to disk
            await File.WriteAllBytesAsync(fullPath, fileData);
            _logger.LogInformation($"File saved to: {fullPath}");

            // Save metadata to database
            var document = new ClaimDocument
            {
                ClaimId = claimId,
                DocumentType = documentType,
                Filename = sanitizedFilename,
                FilePath = fullPath,
                FileSize = fileData.Length,
                MimeType = fileType,
                UploadedBy = uploadedBy,
                UploadDate = DateTime.UtcNow,
                IsProcessed = false,
                ProcessingStatus = "uploaded"
            };

            _context.ClaimDocuments.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Document uploaded successfully: {document.Id}");

            return new DocumentUploadResponse(true, "Document uploaded successfully", document.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for claim {ClaimId}: {Error}", claimId, ex.Message);
            return new DocumentUploadResponse(false, $"Upload failed: {ex.Message}", null);
        }
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int deletedBy)
    {
        try
        {
            var document = await _context.ClaimDocuments.FindAsync(documentId);
            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found", documentId);
                return false;
            }

            // Delete file from disk
            if (File.Exists(document.FilePath))
            {
                File.Delete(document.FilePath);
                _logger.LogInformation("Deleted file: {FilePath}", document.FilePath);
            }

            // Remove from database
            _context.ClaimDocuments.Remove(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} deleted by user {DeletedBy}", documentId, deletedBy);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}: {Error}", documentId, ex.Message);
            return false;
        }
    }

    private static string SanitizeFilename(string filename)
    {
        // Remove invalid characters and limit length
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(filename.Where(c => !invalidChars.Contains(c)).ToArray());
        
        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            sanitized = sanitized.Substring(0, 200 - extension.Length) + extension;
        }
        
        return sanitized;
    }
}

