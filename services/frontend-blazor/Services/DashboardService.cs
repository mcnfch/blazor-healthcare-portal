using Microsoft.EntityFrameworkCore;
using BlazorApp.Models;
using BlazorApp.Models.DTOs;

namespace BlazorApp.Services;

public interface IDashboardService
{
    Task<AdminDashboardResponse> GetDashboardDataAsync();
    Task<DashboardStats> GetStatsAsync();
}

public class DashboardService : IDashboardService
{
    private readonly HealthcareDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(HealthcareDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AdminDashboardResponse> GetDashboardDataAsync()
    {
        try
        {
            var stats = await GetStatsAsync();
            var recentClaims = await GetRecentClaimsAsync();
            var recentActivity = await GetRecentActivityAsync();

            return new AdminDashboardResponse(stats, recentClaims, recentActivity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            return new AdminDashboardResponse(
                new DashboardStats(0, 0, 0, 0, 0, 0, 0, 0),
                new List<ClaimResponse>(),
                new List<UserActivityDto>()
            );
        }
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        try
        {
            var totalClaims = await _context.Claims.CountAsync();
            var pendingClaims = await _context.Claims.CountAsync(c => c.Status == "submitted" || c.Status == "under_review");
            var approvedClaims = await _context.Claims.CountAsync(c => c.Status == "approved" || c.Status == "paid");
            var deniedClaims = await _context.Claims.CountAsync(c => c.Status == "denied");

            var totalClaimValue = await _context.Claims.SumAsync(c => c.TotalAmount);
            var averageClaimValue = totalClaims > 0 ? totalClaimValue / totalClaims : 0;

            var activePatients = await _context.Patients
                .Join(_context.Users, p => p.UserId, u => u.Id, (p, u) => new { Patient = p, User = u })
                .CountAsync(pu => pu.User.IsActive);

            var activeProviders = await _context.HealthcareProviders.CountAsync(p => p.IsActive);

            return new DashboardStats(
                totalClaims,
                pendingClaims,
                approvedClaims,
                deniedClaims,
                totalClaimValue,
                averageClaimValue,
                activePatients,
                activeProviders
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dashboard stats");
            return new DashboardStats(0, 0, 0, 0, 0, 0, 0, 0);
        }
    }

    private async Task<List<ClaimResponse>> GetRecentClaimsAsync()
    {
        try
        {
            var recentClaims = await _context.Claims
                .Include(c => c.Patient)
                .ThenInclude(p => p.User)
                .Include(c => c.Provider)
                .Include(c => c.InsurancePlan)
                .ThenInclude(ip => ip.InsuranceCompany)
                .OrderByDescending(c => c.SubmittedDate)
                .Take(5)
                .ToListAsync();

            return recentClaims.Select(c => new ClaimResponse(
                c.Id,
                c.ClaimNumber,
                c.PatientId,
                c.InsurancePlanId,
                c.ProviderId,
                c.ClaimType.ToString(),
                c.Status.ToString(),
                c.PriorityLevel,
                c.TotalAmount,
                c.ApprovedAmount,
                null, // PatientResponsibility
                null, // InsurancePayment
                c.ServiceDate.ToString("yyyy-MM-dd"),
                c.DiagnosisCodes.ToList(),
                c.ProcedureCodes.ToList(),
                c.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                c.ProcessedDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                null, // PaidDate
                c.AssignedAdjusterId,
                null, // ReviewNotes
                null, // DenialReason
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading recent claims");
            return new List<ClaimResponse>();
        }
    }

    private async Task<List<UserActivityDto>> GetRecentActivityAsync()
    {
        // This would typically come from an audit log table
        // For demo purposes, we'll generate some sample activities
        return await Task.FromResult(new List<UserActivityDto>
        {
            new("Claim Submitted", "John Smith", "Submitted claim CLM-2024-000001", DateTime.Now.AddMinutes(-5).ToString("yyyy-MM-ddTHH:mm:ssZ")),
            new("User Login", "Jane Doe", "Logged into patient portal", DateTime.Now.AddMinutes(-12).ToString("yyyy-MM-ddTHH:mm:ssZ")),
            new("Claim Approved", "Mike Johnson", "Approved claim CLM-2024-000002", DateTime.Now.AddMinutes(-25).ToString("yyyy-MM-ddTHH:mm:ssZ")),
            new("Document Uploaded", "John Smith", "Uploaded medical receipt", DateTime.Now.AddMinutes(-45).ToString("yyyy-MM-ddTHH:mm:ssZ")),
            new("Provider Registered", "Dr. Wilson", "New provider registration", DateTime.Now.AddHours(-2).ToString("yyyy-MM-ddTHH:mm:ssZ"))
        });
    }
}