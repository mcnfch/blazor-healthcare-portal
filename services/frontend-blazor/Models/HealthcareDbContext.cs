using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorApp.Models;

// Shared models for API Gateway database access
public class HealthcareDbContext : DbContext
{
    public HealthcareDbContext(DbContextOptions<HealthcareDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<HealthcareProvider> HealthcareProviders { get; set; }
    public DbSet<InsurancePlan> InsurancePlans { get; set; }
    public DbSet<InsuranceCompany> InsuranceCompanies { get; set; }
    public DbSet<Claim> Claims { get; set; }
    public DbSet<ClaimDocument> ClaimDocuments { get; set; }
    public DbSet<DiagnosisCode> DiagnosisCodes { get; set; }
    public DbSet<ProcedureCode> ProcedureCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Don't register PostgreSQL enums - we'll handle them as strings

        // Configure table names to match PostgreSQL schema
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Patient>().ToTable("patients");
        modelBuilder.Entity<HealthcareProvider>().ToTable("healthcare_providers");
        modelBuilder.Entity<InsurancePlan>().ToTable("insurance_plans");
        modelBuilder.Entity<InsuranceCompany>().ToTable("insurance_companies");
        modelBuilder.Entity<Claim>().ToTable("claims");
        modelBuilder.Entity<ClaimDocument>().ToTable("claim_documents");

        // Configure array properties
        modelBuilder.Entity<HealthcareProvider>()
            .Property(e => e.Specialties)
            .HasColumnType("text[]");

        modelBuilder.Entity<Claim>()
            .Property(e => e.DiagnosisCodes)
            .HasColumnType("text[]");

        modelBuilder.Entity<Claim>()
            .Property(e => e.ProcedureCodes)
            .HasColumnType("text[]");

        base.OnModelCreating(modelBuilder);
    }
}

// Enums
public enum UserRole { patient, provider, admin, claims_adjuster, customer_service }
public enum ClaimStatus { submitted, under_review, pending_info, approved, denied, paid, appealed }
public enum ClaimType { medical, dental, vision, prescription, emergency, preventive, specialist }
public enum CoverageType { individual, family, employee, employee_spouse, employee_children, employee_family }
public enum PlanType { hmo, ppo, epo, pos, hdhp, catastrophic }
public enum ProviderType { primary_care, specialist, hospital, clinic, lab, pharmacy, mental_health, emergency }
public enum GenderType { male, female, other, prefer_not_to_say }

// Entity models (simplified for API Gateway use)
[Table("users")]
public class User
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("email")] public string Email { get; set; } = string.Empty;
    [Column("password_hash")] public string PasswordHash { get; set; } = string.Empty;
    [Column("first_name")] public string FirstName { get; set; } = string.Empty;
    [Column("last_name")] public string LastName { get; set; } = string.Empty;
    [Column("role")] public string Role { get; set; } = string.Empty;
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("last_login")] public DateTime? LastLogin { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}

[Table("patients")]
public class Patient
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("patient_id")] public string PatientId { get; set; } = string.Empty;
    [Column("date_of_birth")] public DateOnly DateOfBirth { get; set; }
    [Column("gender")] public string? Gender { get; set; }
    [Column("phone_number")] public string? PhoneNumber { get; set; }
    [Column("address_id")] public int? AddressId { get; set; }
    [Column("emergency_contact_name")] public string? EmergencyContactName { get; set; }
    [Column("emergency_contact_phone")] public string? EmergencyContactPhone { get; set; }
    [Column("medical_record_number")] public string? MedicalRecordNumber { get; set; }
    [Column("social_security_number")] public string? SocialSecurityNumber { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    
    public User User { get; set; } = null!;
}

[Table("healthcare_providers")]
public class HealthcareProvider
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int? UserId { get; set; }
    [Column("provider_id")] public string ProviderId { get; set; } = string.Empty;
    [Column("provider_name")] public string ProviderName { get; set; } = string.Empty;
    [Column("provider_type")] public string ProviderType { get; set; } = string.Empty;
    [Column("specialties")] public string[] Specialties { get; set; } = Array.Empty<string>();
    [Column("phone_number")] public string? PhoneNumber { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    
    public User? User { get; set; }
}

[Table("insurance_companies")]
public class InsuranceCompany
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("company_name")] public string CompanyName { get; set; } = string.Empty;
    [Column("company_code")] public string CompanyCode { get; set; } = string.Empty;
    [Column("phone_number")] public string? PhoneNumber { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}

[Table("insurance_plans")]
public class InsurancePlan
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("insurance_company_id")] public int InsuranceCompanyId { get; set; }
    [Column("plan_name")] public string PlanName { get; set; } = string.Empty;
    [Column("plan_code")] public string PlanCode { get; set; } = string.Empty;
    [Column("plan_type")] public string PlanType { get; set; } = string.Empty;
    [Column("coverage_type")] public string CoverageType { get; set; } = string.Empty;
    [Column("monthly_premium")] [Precision(10, 2)] public decimal MonthlyPremium { get; set; }
    [Column("annual_deductible")] [Precision(10, 2)] public decimal AnnualDeductible { get; set; }
    [Column("primary_care_copay")] [Precision(8, 2)] public decimal? PrimaryCareCopay { get; set; }
    [Column("specialist_copay")] [Precision(8, 2)] public decimal? SpecialistCopay { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    
    public InsuranceCompany InsuranceCompany { get; set; } = null!;
}

[Table("claims")]
public class Claim
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("claim_number")] public string ClaimNumber { get; set; } = string.Empty;
    [Column("patient_id")] public int PatientId { get; set; }
    [Column("insurance_plan_id")] public int InsurancePlanId { get; set; }
    [Column("provider_id")] public int ProviderId { get; set; }
    [Column("claim_type")] public string ClaimType { get; set; } = string.Empty;
    [Column("status")] public string Status { get; set; } = string.Empty;
    [Column("priority_level")] public int PriorityLevel { get; set; }
    [Column("total_amount")] [Precision(12, 2)] public decimal TotalAmount { get; set; }
    [Column("approved_amount")] [Precision(12, 2)] public decimal? ApprovedAmount { get; set; }
    [Column("service_date")] public DateOnly ServiceDate { get; set; }
    [Column("diagnosis_codes")] public string[] DiagnosisCodes { get; set; } = Array.Empty<string>();
    [Column("procedure_codes")] public string[] ProcedureCodes { get; set; } = Array.Empty<string>();
    [Column("submitted_date")] public DateTime SubmittedDate { get; set; }
    [Column("processed_date")] public DateTime? ProcessedDate { get; set; }
    [Column("assigned_adjuster_id")] public int? AssignedAdjusterId { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    
    public Patient Patient { get; set; } = null!;
    public HealthcareProvider Provider { get; set; } = null!;
    public InsurancePlan InsurancePlan { get; set; } = null!;
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
    public DateTime? UploadDate { get; set; }
    
    [Column("is_processed")]
    public bool IsProcessed { get; set; } = false;
    
    [Column("processing_status")]
    [MaxLength(50)]
    public string ProcessingStatus { get; set; } = "pending";
    
    [Column("extracted_data", TypeName = "jsonb")]
    public string? ExtractedDataJson { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Claim Claim { get; set; } = null!;
}

[Table("icd10_diagnosis_codes")]
public class DiagnosisCode
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("code")] public string Code { get; set; } = string.Empty;
    [Column("description")] public string Description { get; set; } = string.Empty;
    [Column("category")] public string? Category { get; set; }
}

[Table("cpt_procedure_codes")]
public class ProcedureCode
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("code")] public string Code { get; set; } = string.Empty;
    [Column("description")] public string Description { get; set; } = string.Empty;
    [Column("category")] public string? Category { get; set; }
}