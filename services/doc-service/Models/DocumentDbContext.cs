using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocumentService.Models;

public class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options) { }

    public DbSet<ClaimDocument> ClaimDocuments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClaimDocument>().ToTable("claim_documents");

        // Configure JSON column for extracted data
        modelBuilder.Entity<ClaimDocument>()
            .Property(e => e.ExtractedData)
            .HasColumnType("jsonb");

        base.OnModelCreating(modelBuilder);
    }
}

[Table("claim_documents")]
public class ClaimDocument
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("claim_id")]
    public int ClaimId { get; set; }

    [Column("document_type")]
    [MaxLength(100)]
    public string DocumentType { get; set; } = string.Empty;

    [Column("filename")]
    [MaxLength(255)]
    public string Filename { get; set; } = string.Empty;

    [Column("file_path")]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Column("file_size")]
    public int FileSize { get; set; }

    [Column("mime_type")]
    [MaxLength(100)]
    public string? MimeType { get; set; }

    [Column("uploaded_by")]
    public int? UploadedBy { get; set; }

    [Column("upload_date")]
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    [Column("is_processed")]
    public bool IsProcessed { get; set; } = false;

    [Column("processing_status")]
    [MaxLength(50)]
    public string ProcessingStatus { get; set; } = "pending";

    [Column("extracted_data")]
    public string? ExtractedData { get; set; } // JSON data

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}