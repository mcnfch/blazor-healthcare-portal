using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimsProcessor.Models;

public class HealthcareDbContext : DbContext
{
    public HealthcareDbContext(DbContextOptions<HealthcareDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<HealthcareProvider> HealthcareProviders { get; set; }
    public DbSet<ProviderNetwork> ProviderNetworks { get; set; }
    public DbSet<ProviderNetworkParticipation> ProviderNetworkParticipations { get; set; }
    public DbSet<InsuranceCompany> InsuranceCompanies { get; set; }
    public DbSet<InsurancePlan> InsurancePlans { get; set; }
    public DbSet<PatientInsurancePlan> PatientInsurancePlans { get; set; }
    public DbSet<Claim> Claims { get; set; }
    public DbSet<ClaimLineItem> ClaimLineItems { get; set; }
    public DbSet<ClaimDocument> ClaimDocuments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure PostgreSQL enums
        modelBuilder.HasPostgresEnum<UserRole>();
        modelBuilder.HasPostgresEnum<ClaimStatus>();
        modelBuilder.HasPostgresEnum<ClaimType>();
        modelBuilder.HasPostgresEnum<CoverageType>();
        modelBuilder.HasPostgresEnum<PlanType>();
        modelBuilder.HasPostgresEnum<ProviderType>();
        modelBuilder.HasPostgresEnum<GenderType>();

        // Configure table names to match PostgreSQL schema
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Address>().ToTable("addresses");
        modelBuilder.Entity<Patient>().ToTable("patients");
        modelBuilder.Entity<HealthcareProvider>().ToTable("healthcare_providers");
        modelBuilder.Entity<ProviderNetwork>().ToTable("provider_networks");
        modelBuilder.Entity<ProviderNetworkParticipation>().ToTable("provider_network_participation");
        modelBuilder.Entity<InsuranceCompany>().ToTable("insurance_companies");
        modelBuilder.Entity<InsurancePlan>().ToTable("insurance_plans");
        modelBuilder.Entity<PatientInsurancePlan>().ToTable("patient_insurance_plans");
        modelBuilder.Entity<Claim>().ToTable("claims");
        modelBuilder.Entity<ClaimLineItem>().ToTable("claim_line_items");
        modelBuilder.Entity<ClaimDocument>().ToTable("claim_documents");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");
        modelBuilder.Entity<SystemConfiguration>().ToTable("system_configurations");

        // Configure array properties for PostgreSQL
        modelBuilder.Entity<HealthcareProvider>()
            .Property(e => e.Specialties)
            .HasColumnType("text[]");

        modelBuilder.Entity<Claim>()
            .Property(e => e.DiagnosisCodes)
            .HasColumnType("text[]");

        modelBuilder.Entity<Claim>()
            .Property(e => e.ProcedureCodes)
            .HasColumnType("text[]");

        // Configure relationships
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId);

        modelBuilder.Entity<Patient>()
            .HasOne(p => p.Address)
            .WithMany()
            .HasForeignKey(p => p.AddressId);

        modelBuilder.Entity<Claim>()
            .HasOne(c => c.Patient)
            .WithMany(p => p.Claims)
            .HasForeignKey(c => c.PatientId);

        modelBuilder.Entity<Claim>()
            .HasOne(c => c.Provider)
            .WithMany()
            .HasForeignKey(c => c.ProviderId);

        modelBuilder.Entity<Claim>()
            .HasOne(c => c.InsurancePlan)
            .WithMany()
            .HasForeignKey(c => c.InsurancePlanId);

        modelBuilder.Entity<ClaimLineItem>()
            .HasOne(cli => cli.Claim)
            .WithMany(c => c.LineItems)
            .HasForeignKey(cli => cli.ClaimId);

        // Configure decimal precision
        modelBuilder.Entity<Claim>()
            .Property(c => c.TotalAmount)
            .HasPrecision(12, 2);

        modelBuilder.Entity<Claim>()
            .Property(c => c.ApprovedAmount)
            .HasPrecision(12, 2);

        modelBuilder.Entity<ClaimLineItem>()
            .Property(cli => cli.UnitPrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ClaimLineItem>()
            .Property(cli => cli.TotalAmount)
            .HasPrecision(12, 2);

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

// Entity models
[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("first_name")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Column("role")]
    public UserRole Role { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("addresses")]
public class Address
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("street_address")]
    [MaxLength(255)]
    public string StreetAddress { get; set; } = string.Empty;

    [Column("apartment_unit")]
    [MaxLength(50)]
    public string? ApartmentUnit { get; set; }

    [Column("city")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Column("state_province")]
    [MaxLength(50)]
    public string StateProvince { get; set; } = string.Empty;

    [Column("postal_code")]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Column("country")]
    [MaxLength(50)]
    public string Country { get; set; } = "United States";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("patients")]
public class Patient
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("patient_id")]
    [MaxLength(50)]
    public string PatientId { get; set; } = string.Empty;

    [Column("date_of_birth")]
    public DateOnly DateOfBirth { get; set; }

    [Column("gender")]
    public GenderType? Gender { get; set; }

    [Column("phone_number")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Column("address_id")]
    public int? AddressId { get; set; }

    [Column("emergency_contact_name")]
    [MaxLength(200)]
    public string? EmergencyContactName { get; set; }

    [Column("emergency_contact_phone")]
    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }

    [Column("medical_record_number")]
    [MaxLength(50)]
    public string? MedicalRecordNumber { get; set; }

    [Column("social_security_number")]
    [MaxLength(11)]
    public string? SocialSecurityNumber { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Address? Address { get; set; }
    public List<Claim> Claims { get; set; } = new();
}

[Table("healthcare_providers")]
public class HealthcareProvider
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("provider_id")]
    [MaxLength(50)]
    public string ProviderId { get; set; } = string.Empty;

    [Column("provider_name")]
    [MaxLength(200)]
    public string ProviderName { get; set; } = string.Empty;

    [Column("provider_type")]
    public ProviderType ProviderType { get; set; }

    [Column("license_number")]
    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    [Column("specialties")]
    public string[] Specialties { get; set; } = Array.Empty<string>();

    [Column("phone_number")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Column("fax_number")]
    [MaxLength(20)]
    public string? FaxNumber { get; set; }

    [Column("address_id")]
    public int? AddressId { get; set; }

    [Column("is_accepting_patients")]
    public bool IsAcceptingPatients { get; set; } = true;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Address? Address { get; set; }
}

[Table("provider_networks")]
public class ProviderNetwork
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("network_name")]
    [MaxLength(200)]
    public string NetworkName { get; set; } = string.Empty;

    [Column("network_code")]
    [MaxLength(50)]
    public string NetworkCode { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("provider_network_participation")]
public class ProviderNetworkParticipation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("network_id")]
    public int NetworkId { get; set; }

    [Column("effective_date")]
    public DateOnly EffectiveDate { get; set; }

    [Column("termination_date")]
    public DateOnly? TerminationDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("insurance_companies")]
public class InsuranceCompany
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("company_name")]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Column("company_code")]
    [MaxLength(50)]
    public string CompanyCode { get; set; } = string.Empty;

    [Column("phone_number")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Column("website")]
    [MaxLength(255)]
    public string? Website { get; set; }

    [Column("address_id")]
    public int? AddressId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Address? Address { get; set; }
}

[Table("insurance_plans")]
public class InsurancePlan
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("insurance_company_id")]
    public int InsuranceCompanyId { get; set; }

    [Column("plan_name")]
    [MaxLength(200)]
    public string PlanName { get; set; } = string.Empty;

    [Column("plan_code")]
    [MaxLength(100)]
    public string PlanCode { get; set; } = string.Empty;

    [Column("plan_type")]
    public PlanType PlanType { get; set; }

    [Column("coverage_type")]
    public CoverageType CoverageType { get; set; }

    [Column("network_id")]
    public int? NetworkId { get; set; }

    [Column("monthly_premium")]
    [Precision(10, 2)]
    public decimal MonthlyPremium { get; set; }

    [Column("annual_deductible")]
    [Precision(10, 2)]
    public decimal AnnualDeductible { get; set; }

    [Column("out_of_pocket_maximum")]
    [Precision(10, 2)]
    public decimal OutOfPocketMaximum { get; set; }

    [Column("in_network_coverage_pct")]
    [Precision(5, 2)]
    public decimal InNetworkCoveragePercentage { get; set; } = 80.00m;

    [Column("out_of_network_coverage_pct")]
    [Precision(5, 2)]
    public decimal OutOfNetworkCoveragePercentage { get; set; } = 60.00m;

    [Column("primary_care_copay")]
    [Precision(8, 2)]
    public decimal? PrimaryCareCopay { get; set; }

    [Column("specialist_copay")]
    [Precision(8, 2)]
    public decimal? SpecialistCopay { get; set; }

    [Column("emergency_room_copay")]
    [Precision(8, 2)]
    public decimal? EmergencyRoomCopay { get; set; }

    [Column("urgent_care_copay")]
    [Precision(8, 2)]
    public decimal? UrgentCareCopay { get; set; }

    [Column("prescription_copay")]
    [Precision(8, 2)]
    public decimal? PrescriptionCopay { get; set; }

    [Column("effective_date")]
    public DateOnly EffectiveDate { get; set; }

    [Column("termination_date")]
    public DateOnly? TerminationDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public InsuranceCompany InsuranceCompany { get; set; } = null!;
    public ProviderNetwork? Network { get; set; }
}

[Table("patient_insurance_plans")]
public class PatientInsurancePlan
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("patient_id")]
    public int PatientId { get; set; }

    [Column("insurance_plan_id")]
    public int InsurancePlanId { get; set; }

    [Column("policy_number")]
    [MaxLength(100)]
    public string PolicyNumber { get; set; } = string.Empty;

    [Column("group_number")]
    [MaxLength(100)]
    public string? GroupNumber { get; set; }

    [Column("effective_date")]
    public DateOnly EffectiveDate { get; set; }

    [Column("termination_date")]
    public DateOnly? TerminationDate { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; } = true;

    [Column("annual_deductible_met")]
    [Precision(10, 2)]
    public decimal AnnualDeductibleMet { get; set; } = 0.00m;

    [Column("out_of_pocket_met")]
    [Precision(10, 2)]
    public decimal OutOfPocketMet { get; set; } = 0.00m;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public InsurancePlan InsurancePlan { get; set; } = null!;
}

[Table("claims")]
public class Claim
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("claim_number")]
    [MaxLength(50)]
    public string ClaimNumber { get; set; } = string.Empty;

    [Column("patient_id")]
    public int PatientId { get; set; }

    [Column("insurance_plan_id")]
    public int InsurancePlanId { get; set; }

    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("claim_type")]
    public ClaimType ClaimType { get; set; }

    [Column("status")]
    public ClaimStatus Status { get; set; } = ClaimStatus.submitted;

    [Column("priority_level")]
    public int PriorityLevel { get; set; } = 3;

    [Column("total_amount")]
    [Precision(12, 2)]
    public decimal TotalAmount { get; set; }

    [Column("approved_amount")]
    [Precision(12, 2)]
    public decimal? ApprovedAmount { get; set; }

    [Column("patient_responsibility")]
    [Precision(12, 2)]
    public decimal? PatientResponsibility { get; set; }

    [Column("insurance_payment")]
    [Precision(12, 2)]
    public decimal? InsurancePayment { get; set; }

    [Column("service_date")]
    public DateOnly ServiceDate { get; set; }

    [Column("diagnosis_codes")]
    public string[] DiagnosisCodes { get; set; } = Array.Empty<string>();

    [Column("procedure_codes")]
    public string[] ProcedureCodes { get; set; } = Array.Empty<string>();

    [Column("submitted_date")]
    public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

    [Column("processed_date")]
    public DateTime? ProcessedDate { get; set; }

    [Column("paid_date")]
    public DateTime? PaidDate { get; set; }

    [Column("assigned_adjuster_id")]
    public int? AssignedAdjusterId { get; set; }

    [Column("review_notes")]
    public string? ReviewNotes { get; set; }

    [Column("denial_reason")]
    public string? DenialReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public InsurancePlan InsurancePlan { get; set; } = null!;
    public HealthcareProvider Provider { get; set; } = null!;
    public List<ClaimLineItem> LineItems { get; set; } = new();
}

[Table("claim_line_items")]
public class ClaimLineItem
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("claim_id")]
    public int ClaimId { get; set; }

    [Column("line_number")]
    public int LineNumber { get; set; }

    [Column("procedure_code")]
    [MaxLength(20)]
    public string ProcedureCode { get; set; } = string.Empty;

    [Column("procedure_description")]
    public string? ProcedureDescription { get; set; }

    [Column("diagnosis_code")]
    [MaxLength(20)]
    public string? DiagnosisCode { get; set; }

    [Column("service_date")]
    public DateOnly ServiceDate { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("unit_price")]
    [Precision(10, 2)]
    public decimal UnitPrice { get; set; }

    [Column("total_amount")]
    [Precision(12, 2)]
    public decimal TotalAmount { get; set; }

    [Column("allowed_amount")]
    [Precision(12, 2)]
    public decimal? AllowedAmount { get; set; }

    [Column("deductible_amount")]
    [Precision(10, 2)]
    public decimal DeductibleAmount { get; set; } = 0.00m;

    [Column("copay_amount")]
    [Precision(10, 2)]
    public decimal CopayAmount { get; set; } = 0.00m;

    [Column("coinsurance_amount")]
    [Precision(10, 2)]
    public decimal CoinsuranceAmount { get; set; } = 0.00m;

    [Column("not_covered_amount")]
    [Precision(10, 2)]
    public decimal NotCoveredAmount { get; set; } = 0.00m;

    [Column("status")]
    public ClaimStatus Status { get; set; } = ClaimStatus.submitted;

    [Column("denial_reason")]
    public string? DenialReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Claim Claim { get; set; } = null!;
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

    // Navigation properties
    public Claim Claim { get; set; } = null!;
}

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("table_name")]
    [MaxLength(100)]
    public string TableName { get; set; } = string.Empty;

    [Column("record_id")]
    public int RecordId { get; set; }

    [Column("action")]
    [MaxLength(20)]
    public string Action { get; set; } = string.Empty;

    [Column("old_values")]
    public string? OldValues { get; set; } // JSON data

    [Column("new_values")]
    public string? NewValues { get; set; } // JSON data

    [Column("changed_by")]
    public int? ChangedBy { get; set; }

    [Column("changed_at")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }
}

[Table("system_configurations")]
public class SystemConfiguration
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("config_key")]
    [MaxLength(100)]
    public string ConfigKey { get; set; } = string.Empty;

    [Column("config_value")]
    public string ConfigValue { get; set; } = string.Empty;

    [Column("config_description")]
    public string? ConfigDescription { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}