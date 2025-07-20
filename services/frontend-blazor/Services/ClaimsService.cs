using Microsoft.EntityFrameworkCore;
using BlazorApp.Models;
using BlazorApp.Models.DTOs;
using DocumentService.Protos;

namespace BlazorApp.Services;

public interface IClaimsService
{
    Task<List<ClaimResponse>> GetClaimsByPatientAsync(int patientId);
    Task<ClaimResponse?> GetClaimByIdAsync(int claimId);
    Task<List<ClaimResponse>> GetAllClaimsAsync();
    Task<SubmitClaimResponse> SubmitClaimAsync(SubmitClaimRequest request);
}

public record SubmitClaimResponse(bool Success, string? ClaimNumber, string? Message, int? ClaimId);

public interface IPatientService
{
    Task<List<PatientDto>> GetAllPatientsAsync();
    Task<PatientDto?> GetPatientByIdAsync(int patientId);
    Task<PatientDto?> GetPatientByUserIdAsync(int userId);
}

public interface IProviderService
{
    Task<List<ProviderDto>> GetAllProvidersAsync();
    Task<ProviderDto?> GetProviderByIdAsync(int providerId);
}

public interface IInsurancePlanService
{
    Task<List<InsurancePlanDto>> GetAllInsurancePlansAsync();
    Task<InsurancePlanDto?> GetInsurancePlanByIdAsync(int planId);
}

public interface IMedicalCodesService
{
    Task<List<DiagnosisCodeDto>> SearchDiagnosisCodesAsync(string searchTerm, int limit = 10);
    Task<List<ProcedureCodeDto>> SearchProcedureCodesAsync(string searchTerm, int limit = 10);
    Task<DiagnosisCodeDto?> GetDiagnosisCodeAsync(string code);
    Task<ProcedureCodeDto?> GetProcedureCodeAsync(string code);
}

public record DiagnosisCodeDto(string Code, string Description, string? Category);
public record ProcedureCodeDto(string Code, string Description, string? Category);


public interface IDocumentService
{
    Task<List<DocumentDto>> GetClaimDocumentsAsync(int claimId);
    Task<DocumentDto?> GetDocumentMetadataAsync(int documentId);
    Task<DocumentUploadResponse> UploadDocumentAsync(int claimId, string filename, byte[] fileData, string fileType, string documentType, int uploadedBy);
    Task<bool> DeleteDocumentAsync(int documentId, int deletedBy);
}

public record DocumentDto(
    int Id,
    int ClaimId,
    string DocumentType,
    string Filename,
    string FilePath,
    long FileSize,
    string MimeType,
    int UploadedBy,
    string UploadDate,
    bool IsProcessed,
    string ProcessingStatus,
    Dictionary<string, string> ExtractedData);

public record DocumentUploadResponse(bool Success, string Message, int? DocumentId);

public class ClaimsService : IClaimsService
{
    private readonly HealthcareDbContext _context;
    private readonly ILogger<ClaimsService> _logger;

    public ClaimsService(HealthcareDbContext context, ILogger<ClaimsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ClaimResponse>> GetClaimsByPatientAsync(int patientId)
    {
        try
        {
            var claims = await _context.Claims
                .Include(c => c.Patient).ThenInclude(p => p.User)
                .Include(c => c.Provider)
                .Include(c => c.InsurancePlan).ThenInclude(ip => ip.InsuranceCompany)
                .Where(c => c.PatientId == patientId)
                .OrderByDescending(c => c.SubmittedDate)
                .ToListAsync();

            return MapToClaimResponses(claims);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claims for patient {PatientId}", patientId);
            return new List<ClaimResponse>();
        }
    }

    public async Task<ClaimResponse?> GetClaimByIdAsync(int claimId)
    {
        try
        {
            var claim = await _context.Claims
                .Include(c => c.Patient).ThenInclude(p => p.User)
                .Include(c => c.Provider)
                .Include(c => c.InsurancePlan).ThenInclude(ip => ip.InsuranceCompany)
                .FirstOrDefaultAsync(c => c.Id == claimId);

            return claim != null ? MapToClaimResponses(new[] { claim }).FirstOrDefault() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claim {ClaimId}", claimId);
            return null;
        }
    }

    public async Task<List<ClaimResponse>> GetAllClaimsAsync()
    {
        try
        {
            var claims = await _context.Claims
                .Include(c => c.Patient).ThenInclude(p => p.User)
                .Include(c => c.Provider)
                .Include(c => c.InsurancePlan).ThenInclude(ip => ip.InsuranceCompany)
                .OrderByDescending(c => c.SubmittedDate)
                .ToListAsync();

            return MapToClaimResponses(claims);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all claims");
            return new List<ClaimResponse>();
        }
    }

    public async Task<SubmitClaimResponse> SubmitClaimAsync(SubmitClaimRequest request)
    {
        try
        {
            // Generate a unique claim number
            var claimNumber = GenerateClaimNumber();
            
            // Use raw SQL to insert with proper enum casting and return the ID
            var claimIds = await _context.Database.SqlQueryRaw<int>(@"
                INSERT INTO claims (claim_number, patient_id, insurance_plan_id, provider_id, claim_type, status, 
                                  priority_level, total_amount, service_date, diagnosis_codes, procedure_codes,
                                  submitted_date, created_at, updated_at)
                VALUES ({0}, {1}, {2}, {3}, {4}::claim_type, {5}::claim_status, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13})
                RETURNING id",
                claimNumber, request.PatientId, request.InsurancePlanId, request.ProviderId, 
                request.ClaimType.ToLower(), "submitted", request.PriorityLevel, request.TotalAmount,
                DateOnly.Parse(request.ServiceDate), request.DiagnosisCodes.ToArray(), 
                request.ProcedureCodes.ToArray(), DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow)
                .ToListAsync();
            
            var claimId = claimIds.FirstOrDefault();

            _logger.LogInformation("Claim {ClaimNumber} submitted successfully for patient {PatientId}", 
                claimNumber, request.PatientId);

            return new SubmitClaimResponse(true, claimNumber, "Claim submitted successfully", claimId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting claim for patient {PatientId}", request.PatientId);
            return new SubmitClaimResponse(false, null, $"Failed to submit claim: {ex.Message}", null);
        }
    }

    private string GenerateClaimNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(100, 999);
        return $"CLM-{timestamp}-{random}";
    }

    private List<ClaimResponse> MapToClaimResponses(IEnumerable<Claim> claims)
    {
        return claims.Select(c => new ClaimResponse(
            c.Id,
            c.ClaimNumber,
            c.PatientId,
            c.InsurancePlanId,
            c.ProviderId,
            c.ClaimType,
            c.Status,
            c.PriorityLevel,
            c.TotalAmount,
            c.ApprovedAmount,
            null,
            null,
            c.ServiceDate.ToString("yyyy-MM-dd"),
            c.DiagnosisCodes.ToList(),
            c.ProcedureCodes.ToList(),
            c.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            c.ProcessedDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            null,
            c.AssignedAdjusterId,
            null,
            null,
            new PatientInfoDto(
                c.Patient.Id,
                c.Patient.PatientId,
                c.Patient.User.FirstName,
                c.Patient.User.LastName,
                c.Patient.DateOfBirth.ToString("yyyy-MM-dd"),
                c.Patient.Gender?.ToString(),
                c.Patient.PhoneNumber
            ),
            new ProviderInfoDto(
                c.Provider.Id,
                c.Provider.ProviderId,
                c.Provider.ProviderName,
                c.Provider.ProviderType.ToString(),
                c.Provider.Specialties.ToList(),
                c.Provider.PhoneNumber
            ),
            new InsurancePlanInfoDto(
                c.InsurancePlan.Id,
                c.InsurancePlan.PlanName,
                c.InsurancePlan.PlanCode,
                c.InsurancePlan.PlanType.ToString(),
                c.InsurancePlan.InsuranceCompany.CompanyName
            )
        )).ToList();
    }
}

public class PatientService : IPatientService
{
    private readonly HealthcareDbContext _context;
    private readonly ILogger<PatientService> _logger;

    public PatientService(HealthcareDbContext context, ILogger<PatientService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PatientDto>> GetAllPatientsAsync()
    {
        try
        {
            var patients = await _context.Patients
                .Include(p => p.User)
                .Where(p => p.User.IsActive)
                .OrderBy(p => p.User.LastName)
                .ToListAsync();

            return patients.Select(p => new PatientDto(
                p.Id,
                p.PatientId,
                p.User.FirstName,
                p.User.LastName,
                p.DateOfBirth.ToString("yyyy-MM-dd"),
                p.Gender?.ToString(),
                p.PhoneNumber,
                p.User.Email,
                p.User.IsActive
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all patients");
            return new List<PatientDto>();
        }
    }

    public async Task<PatientDto?> GetPatientByIdAsync(int patientId)
    {
        try
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null) return null;

            return new PatientDto(
                patient.Id,
                patient.PatientId,
                patient.User.FirstName,
                patient.User.LastName,
                patient.DateOfBirth.ToString("yyyy-MM-dd"),
                patient.Gender,
                patient.PhoneNumber,
                patient.User.Email,
                patient.User.IsActive
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient {PatientId}", patientId);
            return null;
        }
    }

    public async Task<PatientDto?> GetPatientByUserIdAsync(int userId)
    {
        try
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null) return null;

            return new PatientDto(
                patient.Id,
                patient.PatientId,
                patient.User.FirstName,
                patient.User.LastName,
                patient.DateOfBirth.ToString("yyyy-MM-dd"),
                patient.Gender,
                patient.PhoneNumber,
                patient.User.Email,
                patient.User.IsActive
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient by user ID {UserId}", userId);
            return null;
        }
    }
}

public class ProviderService : IProviderService
{
    private readonly HealthcareDbContext _context;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(HealthcareDbContext context, ILogger<ProviderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProviderDto>> GetAllProvidersAsync()
    {
        try
        {
            var providers = await _context.HealthcareProviders
                .Include(p => p.User)
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProviderName)
                .ToListAsync();

            return providers.Select(p => new ProviderDto(
                p.Id,
                p.ProviderId,
                p.ProviderName,
                p.ProviderType,
                p.Specialties.ToList(),
                p.PhoneNumber,
                p.User?.Email,
                p.IsActive,
                true // IsAcceptingPatients - would come from database
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all providers");
            return new List<ProviderDto>();
        }
    }

    public async Task<ProviderDto?> GetProviderByIdAsync(int providerId)
    {
        try
        {
            var provider = await _context.HealthcareProviders
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null) return null;

            return new ProviderDto(
                provider.Id,
                provider.ProviderId,
                provider.ProviderName,
                provider.ProviderType,
                provider.Specialties.ToList(),
                provider.PhoneNumber,
                provider.User?.Email,
                provider.IsActive,
                true
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider {ProviderId}", providerId);
            return null;
        }
    }
}

public class InsurancePlanService : IInsurancePlanService
{
    private readonly HealthcareDbContext _context;
    private readonly ILogger<InsurancePlanService> _logger;

    public InsurancePlanService(HealthcareDbContext context, ILogger<InsurancePlanService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<InsurancePlanDto>> GetAllInsurancePlansAsync()
    {
        try
        {
            var plans = await _context.InsurancePlans
                .Include(p => p.InsuranceCompany)
                .Where(p => p.IsActive)
                .OrderBy(p => p.InsuranceCompany.CompanyName)
                .ThenBy(p => p.PlanName)
                .ToListAsync();

            return plans.Select(p => new InsurancePlanDto(
                p.Id,
                p.PlanName,
                p.PlanCode,
                p.PlanType,
                p.CoverageType,
                p.MonthlyPremium,
                p.AnnualDeductible,
                p.PrimaryCareCopay,
                p.SpecialistCopay,
                p.InsuranceCompany.CompanyName,
                p.IsActive
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all insurance plans");
            return new List<InsurancePlanDto>();
        }
    }

    public async Task<InsurancePlanDto?> GetInsurancePlanByIdAsync(int planId)
    {
        try
        {
            var plan = await _context.InsurancePlans
                .Include(p => p.InsuranceCompany)
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan == null) return null;

            return new InsurancePlanDto(
                plan.Id,
                plan.PlanName,
                plan.PlanCode,
                plan.PlanType,
                plan.CoverageType,
                plan.MonthlyPremium,
                plan.AnnualDeductible,
                plan.PrimaryCareCopay,
                plan.SpecialistCopay,
                plan.InsuranceCompany.CompanyName,
                plan.IsActive
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting insurance plan {PlanId}", planId);
            return null;
        }
    }
}

public class BlazorDocumentService : IDocumentService
{
    private readonly IDocumentGrpcClient _documentGrpcClient;
    private readonly ILogger<BlazorDocumentService> _logger;

    public BlazorDocumentService(IDocumentGrpcClient documentGrpcClient, ILogger<BlazorDocumentService> logger)
    {
        _documentGrpcClient = documentGrpcClient;
        _logger = logger;
    }

    public async Task<List<DocumentDto>> GetClaimDocumentsAsync(int claimId)
    {
        try
        {
            var request = new ListClaimDocumentsRequest
            {
                ClaimId = claimId
            };

            var response = await _documentGrpcClient.ListClaimDocumentsAsync(request);
            
            if (!response.Success)
            {
                _logger.LogWarning("Failed to get documents for claim {ClaimId}: {Message}", claimId, response.Message);
                return new List<DocumentDto>();
            }

            return response.Documents.Select(d => new DocumentDto(
                d.Id,
                d.ClaimId,
                d.DocumentType,
                d.Filename,
                d.FilePath,
                d.FileSize,
                d.MimeType,
                d.UploadedBy,
                d.UploadDate,
                d.IsProcessed,
                d.ProcessingStatus,
                d.ExtractedData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents for claim {ClaimId}", claimId);
            return new List<DocumentDto>();
        }
    }

    public async Task<DocumentDto?> GetDocumentMetadataAsync(int documentId)
    {
        try
        {
            var request = new GetDocumentMetadataRequest
            {
                DocumentId = documentId
            };

            var response = await _documentGrpcClient.GetDocumentMetadataAsync(request);
            
            if (!response.Success || response.Metadata == null)
            {
                return null;
            }

            var d = response.Metadata;
            return new DocumentDto(
                d.Id,
                d.ClaimId,
                d.DocumentType,
                d.Filename,
                d.FilePath,
                d.FileSize,
                d.MimeType,
                d.UploadedBy,
                d.UploadDate,
                d.IsProcessed,
                d.ProcessingStatus,
                d.ExtractedData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document metadata {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<DocumentUploadResponse> UploadDocumentAsync(int claimId, string filename, byte[] fileData, string fileType, string documentType, int uploadedBy)
    {
        try
        {
            var request = new ProcessDocumentRequest
            {
                ClaimId = claimId,
                Filename = filename,
                FileData = Google.Protobuf.ByteString.CopyFrom(fileData),
                FileType = fileType,
                DocumentType = documentType,
                UploadedBy = uploadedBy
            };

            var response = await _documentGrpcClient.ProcessDocumentAsync(request);
            
            return new DocumentUploadResponse(response.Success, response.Message, response.DocumentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for claim {ClaimId}", claimId);
            return new DocumentUploadResponse(false, $"Upload failed: {ex.Message}", null);
        }
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int deletedBy)
    {
        try
        {
            var request = new DeleteDocumentRequest
            {
                DocumentId = documentId,
                DeletedBy = deletedBy
            };

            var response = await _documentGrpcClient.DeleteDocumentAsync(request);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return false;
        }
    }
}

public class MedicalCodesService : IMedicalCodesService
{
    private readonly HealthcareDbContext _context;
    private readonly ILogger<MedicalCodesService> _logger;

    public MedicalCodesService(HealthcareDbContext context, ILogger<MedicalCodesService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<DiagnosisCodeDto>> SearchDiagnosisCodesAsync(string searchTerm, int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<DiagnosisCodeDto>();

            var codes = await _context.DiagnosisCodes
                .Where(d => d.Code.ToLower().Contains(searchTerm.ToLower()) || 
                            d.Description.ToLower().Contains(searchTerm.ToLower()))
                .OrderBy(d => d.Code)
                .Take(limit)
                .ToListAsync();

            return codes.Select(d => new DiagnosisCodeDto(d.Code, d.Description, d.Category)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching diagnosis codes with term: {SearchTerm}", searchTerm);
            return new List<DiagnosisCodeDto>();
        }
    }

    public async Task<List<ProcedureCodeDto>> SearchProcedureCodesAsync(string searchTerm, int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<ProcedureCodeDto>();

            var codes = await _context.ProcedureCodes
                .Where(p => p.Code.ToLower().Contains(searchTerm.ToLower()) || 
                            p.Description.ToLower().Contains(searchTerm.ToLower()))
                .OrderBy(p => p.Code)
                .Take(limit)
                .ToListAsync();

            return codes.Select(p => new ProcedureCodeDto(p.Code, p.Description, p.Category)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching procedure codes with term: {SearchTerm}", searchTerm);
            return new List<ProcedureCodeDto>();
        }
    }

    public async Task<DiagnosisCodeDto?> GetDiagnosisCodeAsync(string code)
    {
        try
        {
            var diagnosisCode = await _context.DiagnosisCodes
                .FirstOrDefaultAsync(d => d.Code == code);

            return diagnosisCode != null ? 
                new DiagnosisCodeDto(diagnosisCode.Code, diagnosisCode.Description, diagnosisCode.Category) : 
                null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting diagnosis code: {Code}", code);
            return null;
        }
    }

    public async Task<ProcedureCodeDto?> GetProcedureCodeAsync(string code)
    {
        try
        {
            var procedureCode = await _context.ProcedureCodes
                .FirstOrDefaultAsync(p => p.Code == code);

            return procedureCode != null ? 
                new ProcedureCodeDto(procedureCode.Code, procedureCode.Description, procedureCode.Category) : 
                null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting procedure code: {Code}", code);
            return null;
        }
    }
}