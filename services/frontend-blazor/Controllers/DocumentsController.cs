using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BlazorApp.Services;
using BlazorApp.Models;

namespace BlazorApp.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly HealthcareDbContext _context;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentService documentService, HealthcareDbContext context, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("{id}/view")]
    public async Task<IActionResult> ViewDocument(int id)
    {
        try
        {
            var document = await _context.ClaimDocuments.FindAsync(id);
            if (document == null)
            {
                return NotFound("Document not found");
            }

            // Check if file exists
            if (!System.IO.File.Exists(document.FilePath))
            {
                return NotFound("File not found on disk");
            }

            var mimeType = document.MimeType ?? "application/octet-stream";

            // For viewing, use PhysicalFile with inline disposition
            return PhysicalFile(document.FilePath, mimeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing document {DocumentId}", id);
            return StatusCode(500, "Error retrieving document");
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        try
        {
            var document = await _context.ClaimDocuments.FindAsync(id);
            if (document == null)
            {
                return NotFound("Document not found");
            }

            // Check if file exists
            if (!System.IO.File.Exists(document.FilePath))
            {
                return NotFound("File not found on disk");
            }

            var mimeType = document.MimeType ?? "application/octet-stream";

            // For downloading, use PhysicalFile with attachment disposition
            return PhysicalFile(document.FilePath, mimeType, document.Filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", id);
            return StatusCode(500, "Error downloading document");
        }
    }
}