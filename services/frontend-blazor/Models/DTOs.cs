namespace BlazorApp.Models.DTOs;

// Authentication DTOs - Using class for Blazor two-way binding support
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
public record LoginResponse(bool Success, string? Token, UserInfo? User, string? Message);
public record UserInfo(int Id, string Email, string FirstName, string LastName, string Role);

// Claim DTOs
public record SubmitClaimRequest(
    int PatientId,
    int InsurancePlanId,
    int ProviderId,
    string ClaimType,
    decimal TotalAmount,
    string ServiceDate,
    List<string> DiagnosisCodes,
    List<string> ProcedureCodes,
    int PriorityLevel = 3,
    List<ClaimLineItemDto>? LineItems = null);

public record ClaimLineItemDto(
    int LineNumber,
    string ProcedureCode,
    string ProcedureDescription,
    string DiagnosisCode,
    string ServiceDate,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount);

public record UpdateClaimStatusRequest(
    string Status,
    string? ReviewNotes = null,
    string? DenialReason = null,
    int? AssignedAdjusterId = null);

public record ClaimResponse(
    int Id,
    string ClaimNumber,
    int PatientId,
    int InsurancePlanId,
    int ProviderId,
    string ClaimType,
    string Status,
    int PriorityLevel,
    decimal TotalAmount,
    decimal? ApprovedAmount,
    decimal? PatientResponsibility,
    decimal? InsurancePayment,
    string ServiceDate,
    List<string> DiagnosisCodes,
    List<string> ProcedureCodes,
    string SubmittedDate,
    string? ProcessedDate,
    string? PaidDate,
    int? AssignedAdjusterId,
    string? ReviewNotes,
    string? DenialReason,
    PatientInfoDto? PatientInfo = null,
    ProviderInfoDto? ProviderInfo = null,
    InsurancePlanInfoDto? InsurancePlanInfo = null);

public record PatientInfoDto(
    int Id,
    string PatientId,
    string FirstName,
    string LastName,
    string DateOfBirth,
    string? Gender,
    string? PhoneNumber);

public record ProviderInfoDto(
    int Id,
    string ProviderId,
    string ProviderName,
    string ProviderType,
    List<string> Specialties,
    string? PhoneNumber);

public record InsurancePlanInfoDto(
    int Id,
    string PlanName,
    string PlanCode,
    string PlanType,
    string CompanyName);


// Admin DTOs
public record AdminDashboardResponse(
    DashboardStats Stats,
    List<ClaimResponse> RecentClaims,
    List<UserActivityDto> RecentActivity);

public record DashboardStats(
    int TotalClaims,
    int PendingClaims,
    int ApprovedClaims,
    int DeniedClaims,
    decimal TotalClaimValue,
    decimal AverageClaimValue,
    int ActivePatients,
    int ActiveProviders);

public record UserActivityDto(
    string Action,
    string UserName,
    string Details,
    string Timestamp);

public record ProcessPaymentRequest(
    decimal ApprovedAmount,
    decimal PatientResponsibility,
    decimal InsurancePayment);

// Document DTOs
public record DocumentUploadResponse(
    bool Success,
    string Message,
    int? DocumentId,
    DocumentMetadataDto? Metadata);

public record DocumentMetadataDto(
    int Id,
    int ClaimId,
    string DocumentType,
    string Filename,
    long FileSize,
    string MimeType,
    int UploadedBy,
    string UploadDate,
    bool IsProcessed,
    string ProcessingStatus,
    Dictionary<string, string> ExtractedData);

// Error response DTO
public record ErrorResponse(bool Success, string Message, string? Details = null);

// Pagination DTO
public record PagedResponse<T>(
    bool Success,
    List<T> Data,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    string? Message = null);

// Healthcare specific DTOs
public record PatientSearchRequest(
    string? PatientId = null,
    string? FirstName = null,
    string? LastName = null,
    string? DateOfBirth = null,
    int Page = 1,
    int PageSize = 10);

public record ProviderSearchRequest(
    string? ProviderId = null,
    string? ProviderName = null,
    string? ProviderType = null,
    string? Specialty = null,
    int Page = 1,
    int PageSize = 10);

public record ClaimSearchRequest(
    int? PatientId = null,
    int? ProviderId = null,
    string? Status = null,
    string? ClaimType = null,
    string? StartDate = null,
    string? EndDate = null,
    int Page = 1,
    int PageSize = 10);

public record InsurancePlanDto(
    int Id,
    string PlanName,
    string PlanCode,
    string PlanType,
    string CoverageType,
    decimal MonthlyPremium,
    decimal AnnualDeductible,
    decimal? PrimaryCareCopay,
    decimal? SpecialistCopay,
    string CompanyName,
    bool IsActive);

public record PatientDto(
    int Id,
    string PatientId,
    string FirstName,
    string LastName,
    string DateOfBirth,
    string? Gender,
    string? PhoneNumber,
    string Email,
    bool IsActive);

public record ProviderDto(
    int Id,
    string ProviderId,
    string ProviderName,
    string ProviderType,
    List<string> Specialties,
    string? PhoneNumber,
    string? Email,
    bool IsActive,
    bool IsAcceptingPatients);