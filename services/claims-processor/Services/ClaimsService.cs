using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ClaimsProcessor.Protos;
using ClaimsProcessor.Models;
using ClaimsProcessor.Services.Messaging;
using System.Globalization;

namespace ClaimsProcessor.Services;

public class ClaimsService : Protos.ClaimsService.ClaimsServiceBase
{
    private readonly HealthcareDbContext _context;
    private readonly ILogger<ClaimsService> _logger;
    private readonly IMessagePublisher _messagePublisher;

    public ClaimsService(
        HealthcareDbContext context,
        ILogger<ClaimsService> logger,
        IMessagePublisher messagePublisher)
    {
        _context = context;
        _logger = logger;
        _messagePublisher = messagePublisher;
    }

    public override async Task<SubmitClaimResponse> SubmitClaim(SubmitClaimRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Processing claim submission for patient {PatientId}", request.PatientId);

            // Validate the request
            var validationResult = await ValidateClaimRequest(request);
            if (!validationResult.IsValid)
            {
                return new SubmitClaimResponse
                {
                    Success = false,
                    Message = validationResult.ErrorMessage
                };
            }

            // Generate unique claim number
            var claimNumber = await GenerateClaimNumber();

            // Parse amounts
            if (!decimal.TryParse(request.TotalAmount, out var totalAmount))
            {
                return new SubmitClaimResponse
                {
                    Success = false,
                    Message = "Invalid total amount format"
                };
            }

            // Create the claim
            var claim = new Claim
            {
                ClaimNumber = claimNumber,
                PatientId = request.PatientId,
                InsurancePlanId = request.InsurancePlanId,
                ProviderId = request.ProviderId,
                ClaimType = Enum.Parse<ClaimType>(request.ClaimType, ignoreCase: true),
                TotalAmount = totalAmount,
                ServiceDate = DateOnly.Parse(request.ServiceDate),
                DiagnosisCodes = request.DiagnosisCodes.ToArray(),
                ProcedureCodes = request.ProcedureCodes.ToArray(),
                PriorityLevel = request.PriorityLevel > 0 ? request.PriorityLevel : 3,
                Status = ClaimStatus.submitted,
                SubmittedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            // Add line items if provided
            if (request.LineItems.Count > 0)
            {
                await AddLineItems(claim.Id, request.LineItems);
            }

            // Publish claim submitted event
            await _messagePublisher.PublishClaimEvent("claim.submitted", claim.Id, claim.PatientId);

            _logger.LogInformation("Successfully created claim {ClaimNumber} with ID {ClaimId}", claimNumber, claim.Id);

            return new SubmitClaimResponse
            {
                Success = true,
                Message = "Claim submitted successfully",
                ClaimNumber = claimNumber,
                ClaimId = claim.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting claim for patient {PatientId}", request.PatientId);
            return new SubmitClaimResponse
            {
                Success = false,
                Message = "An error occurred while processing the claim submission"
            };
        }
    }

    public override async Task<GetClaimResponse> GetClaim(GetClaimRequest request, ServerCallContext context)
    {
        try
        {
            IQueryable<Claim> query = _context.Claims
                .Include(c => c.Patient)
                .ThenInclude(p => p.User)
                .Include(c => c.Provider)
                .Include(c => c.InsurancePlan)
                .ThenInclude(ip => ip.InsuranceCompany)
                .Include(c => c.LineItems);

            Claim? claim = null;

            if (request.ClaimId > 0)
            {
                claim = await query.FirstOrDefaultAsync(c => c.Id == request.ClaimId);
            }
            else if (!string.IsNullOrEmpty(request.ClaimNumber))
            {
                claim = await query.FirstOrDefaultAsync(c => c.ClaimNumber == request.ClaimNumber);
            }

            if (claim == null)
            {
                return new GetClaimResponse
                {
                    Success = false,
                    Message = "Claim not found"
                };
            }

            var healthcareClaim = MapToHealthcareClaim(claim);

            return new GetClaimResponse
            {
                Success = true,
                Claim = healthcareClaim
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving claim with ID {ClaimId} or number {ClaimNumber}", 
                request.ClaimId, request.ClaimNumber);
            
            return new GetClaimResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the claim"
            };
        }
    }

    public override async Task<UpdateClaimStatusResponse> UpdateClaimStatus(UpdateClaimStatusRequest request, ServerCallContext context)
    {
        try
        {
            var claim = await _context.Claims.FindAsync(request.ClaimId);
            if (claim == null)
            {
                return new UpdateClaimStatusResponse
                {
                    Success = false,
                    Message = "Claim not found"
                };
            }

            var oldStatus = claim.Status;
            
            // Update claim status
            if (Enum.TryParse<ClaimStatus>(request.Status, ignoreCase: true, out var newStatus))
            {
                claim.Status = newStatus;
            }
            else
            {
                return new UpdateClaimStatusResponse
                {
                    Success = false,
                    Message = "Invalid claim status"
                };
            }

            if (!string.IsNullOrEmpty(request.ReviewNotes))
            {
                claim.ReviewNotes = request.ReviewNotes;
            }

            if (!string.IsNullOrEmpty(request.DenialReason))
            {
                claim.DenialReason = request.DenialReason;
            }

            if (request.AssignedAdjusterId > 0)
            {
                claim.AssignedAdjusterId = request.AssignedAdjusterId;
            }

            // Set processing timestamp for status changes
            if (newStatus == ClaimStatus.approved || newStatus == ClaimStatus.denied)
            {
                claim.ProcessedDate = DateTime.UtcNow;
            }

            if (newStatus == ClaimStatus.paid)
            {
                claim.PaidDate = DateTime.UtcNow;
            }

            claim.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Publish status change event
            await _messagePublisher.PublishClaimEvent("claim.status_changed", claim.Id, claim.PatientId);

            _logger.LogInformation("Updated claim {ClaimNumber} status from {OldStatus} to {NewStatus}", 
                claim.ClaimNumber, oldStatus, newStatus);

            return new UpdateClaimStatusResponse
            {
                Success = true,
                Message = "Claim status updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for claim {ClaimId}", request.ClaimId);
            return new UpdateClaimStatusResponse
            {
                Success = false,
                Message = "An error occurred while updating the claim status"
            };
        }
    }

    public override async Task<ListClaimsResponse> ListClaims(ListClaimsRequest request, ServerCallContext context)
    {
        try
        {
            var query = _context.Claims
                .Include(c => c.Patient)
                .ThenInclude(p => p.User)
                .Include(c => c.Provider)
                .Include(c => c.InsurancePlan)
                .ThenInclude(ip => ip.InsuranceCompany)
                .Include(c => c.LineItems)
                .AsQueryable();

            // Apply filters
            if (request.PatientId > 0)
            {
                query = query.Where(c => c.PatientId == request.PatientId);
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                if (Enum.TryParse<ClaimStatus>(request.Status, ignoreCase: true, out var statusFilter))
                {
                    query = query.Where(c => c.Status == statusFilter);
                }
            }

            if (!string.IsNullOrEmpty(request.ClaimType))
            {
                if (Enum.TryParse<ClaimType>(request.ClaimType, ignoreCase: true, out var typeFilter))
                {
                    query = query.Where(c => c.ClaimType == typeFilter);
                }
            }

            if (!string.IsNullOrEmpty(request.StartDate) && DateOnly.TryParse(request.StartDate, out var startDate))
            {
                query = query.Where(c => c.ServiceDate >= startDate);
            }

            if (!string.IsNullOrEmpty(request.EndDate) && DateOnly.TryParse(request.EndDate, out var endDate))
            {
                query = query.Where(c => c.ServiceDate <= endDate);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var limit = request.Limit > 0 ? request.Limit : 10;
            var offset = request.Offset >= 0 ? request.Offset : 0;

            var claims = await query
                .OrderByDescending(c => c.SubmittedDate)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();

            var healthcareClaims = claims.Select(MapToHealthcareClaim).ToList();

            return new ListClaimsResponse
            {
                Success = true,
                Claims = { healthcareClaims },
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing claims for patient {PatientId}", request.PatientId);
            return new ListClaimsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving claims"
            };
        }
    }

    public override async Task<ListClaimsResponse> GetClaimsByProvider(GetClaimsByProviderRequest request, ServerCallContext context)
    {
        try
        {
            var query = _context.Claims
                .Include(c => c.Patient)
                .ThenInclude(p => p.User)
                .Include(c => c.Provider)
                .Include(c => c.InsurancePlan)
                .ThenInclude(ip => ip.InsuranceCompany)
                .Include(c => c.LineItems)
                .Where(c => c.ProviderId == request.ProviderId);

            if (!string.IsNullOrEmpty(request.Status))
            {
                if (Enum.TryParse<ClaimStatus>(request.Status, ignoreCase: true, out var statusFilter))
                {
                    query = query.Where(c => c.Status == statusFilter);
                }
            }

            var totalCount = await query.CountAsync();

            var limit = request.Limit > 0 ? request.Limit : 10;
            var offset = request.Offset >= 0 ? request.Offset : 0;

            var claims = await query
                .OrderByDescending(c => c.SubmittedDate)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();

            var healthcareClaims = claims.Select(MapToHealthcareClaim).ToList();

            return new ListClaimsResponse
            {
                Success = true,
                Claims = { healthcareClaims },
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving claims for provider {ProviderId}", request.ProviderId);
            return new ListClaimsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving provider claims"
            };
        }
    }

    public override async Task<ProcessPaymentResponse> ProcessClaimPayment(ProcessPaymentRequest request, ServerCallContext context)
    {
        try
        {
            var claim = await _context.Claims.FindAsync(request.ClaimId);
            if (claim == null)
            {
                return new ProcessPaymentResponse
                {
                    Success = false,
                    Message = "Claim not found"
                };
            }

            // Parse payment amounts
            if (decimal.TryParse(request.ApprovedAmount, out var approvedAmount))
            {
                claim.ApprovedAmount = approvedAmount;
            }

            if (decimal.TryParse(request.PatientResponsibility, out var patientResponsibility))
            {
                claim.PatientResponsibility = patientResponsibility;
            }

            if (decimal.TryParse(request.InsurancePayment, out var insurancePayment))
            {
                claim.InsurancePayment = insurancePayment;
            }

            claim.Status = ClaimStatus.paid;
            claim.ProcessedDate = DateTime.UtcNow;
            claim.PaidDate = DateTime.UtcNow;
            claim.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Publish payment event
            await _messagePublisher.PublishClaimEvent("claim.payment_processed", claim.Id, claim.PatientId);

            _logger.LogInformation("Processed payment for claim {ClaimNumber}", claim.ClaimNumber);

            return new ProcessPaymentResponse
            {
                Success = true,
                Message = "Payment processed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for claim {ClaimId}", request.ClaimId);
            return new ProcessPaymentResponse
            {
                Success = false,
                Message = "An error occurred while processing payment"
            };
        }
    }

    // Helper methods
    private async Task<(bool IsValid, string ErrorMessage)> ValidateClaimRequest(SubmitClaimRequest request)
    {
        // Validate patient exists
        var patientExists = await _context.Patients.AnyAsync(p => p.Id == request.PatientId);
        if (!patientExists)
        {
            return (false, "Patient not found");
        }

        // Validate provider exists
        var providerExists = await _context.HealthcareProviders.AnyAsync(p => p.Id == request.ProviderId);
        if (!providerExists)
        {
            return (false, "Healthcare provider not found");
        }

        // Validate insurance plan exists
        var planExists = await _context.InsurancePlans.AnyAsync(p => p.Id == request.InsurancePlanId);
        if (!planExists)
        {
            return (false, "Insurance plan not found");
        }

        // Validate claim type
        if (!Enum.TryParse<ClaimType>(request.ClaimType, ignoreCase: true, out _))
        {
            return (false, "Invalid claim type");
        }

        // Validate service date
        if (!DateOnly.TryParse(request.ServiceDate, out var serviceDate))
        {
            return (false, "Invalid service date format");
        }

        if (serviceDate > DateOnly.FromDateTime(DateTime.Today))
        {
            return (false, "Service date cannot be in the future");
        }

        return (true, string.Empty);
    }

    private async Task<string> GenerateClaimNumber()
    {
        var year = DateTime.UtcNow.Year;
        var sequence = await _context.Claims
            .Where(c => c.ClaimNumber.StartsWith($"CLM-{year}-"))
            .CountAsync() + 1;

        return $"CLM-{year}-{sequence:D6}";
    }

    private async Task AddLineItems(int claimId, IEnumerable<ClaimsProcessor.Protos.ClaimLineItem> requestLineItems)
    {
        var lineItems = requestLineItems.Select((item, index) => new Models.ClaimLineItem
        {
            ClaimId = claimId,
            LineNumber = index + 1,
            ProcedureCode = item.ProcedureCode,
            ProcedureDescription = item.ProcedureDescription,
            DiagnosisCode = item.DiagnosisCode,
            ServiceDate = DateOnly.Parse(item.ServiceDate),
            Quantity = item.Quantity > 0 ? item.Quantity : 1,
            UnitPrice = decimal.TryParse(item.UnitPrice, out var unitPrice) ? unitPrice : 0,
            TotalAmount = decimal.TryParse(item.TotalAmount, out var totalAmount) ? totalAmount : 0,
            Status = ClaimStatus.submitted,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.ClaimLineItems.AddRange(lineItems);
        await _context.SaveChangesAsync();
    }

    private HealthcareClaim MapToHealthcareClaim(Claim claim)
    {
        var healthcareClaim = new HealthcareClaim
        {
            Id = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            PatientId = claim.PatientId,
            InsurancePlanId = claim.InsurancePlanId,
            ProviderId = claim.ProviderId,
            ClaimType = claim.ClaimType.ToString(),
            Status = claim.Status.ToString(),
            PriorityLevel = claim.PriorityLevel,
            TotalAmount = claim.TotalAmount.ToString("F2", CultureInfo.InvariantCulture),
            ApprovedAmount = claim.ApprovedAmount?.ToString("F2", CultureInfo.InvariantCulture) ?? "0.00",
            PatientResponsibility = claim.PatientResponsibility?.ToString("F2", CultureInfo.InvariantCulture) ?? "0.00",
            InsurancePayment = claim.InsurancePayment?.ToString("F2", CultureInfo.InvariantCulture) ?? "0.00",
            ServiceDate = claim.ServiceDate.ToString("yyyy-MM-dd"),
            SubmittedDate = claim.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ProcessedDate = claim.ProcessedDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "",
            PaidDate = claim.PaidDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "",
            AssignedAdjusterId = claim.AssignedAdjusterId ?? 0,
            ReviewNotes = claim.ReviewNotes ?? "",
            DenialReason = claim.DenialReason ?? ""
        };

        healthcareClaim.DiagnosisCodes.AddRange(claim.DiagnosisCodes);
        healthcareClaim.ProcedureCodes.AddRange(claim.ProcedureCodes);

        // Map line items
        foreach (var lineItem in claim.LineItems)
        {
            healthcareClaim.LineItems.Add(new ClaimsProcessor.Protos.ClaimLineItem
            {
                Id = lineItem.Id,
                LineNumber = lineItem.LineNumber,
                ProcedureCode = lineItem.ProcedureCode,
                ProcedureDescription = lineItem.ProcedureDescription ?? "",
                DiagnosisCode = lineItem.DiagnosisCode ?? "",
                ServiceDate = lineItem.ServiceDate.ToString("yyyy-MM-dd"),
                Quantity = lineItem.Quantity,
                UnitPrice = lineItem.UnitPrice.ToString("F2", CultureInfo.InvariantCulture),
                TotalAmount = lineItem.TotalAmount.ToString("F2", CultureInfo.InvariantCulture),
                AllowedAmount = lineItem.AllowedAmount?.ToString("F2", CultureInfo.InvariantCulture) ?? "0.00",
                DeductibleAmount = lineItem.DeductibleAmount.ToString("F2", CultureInfo.InvariantCulture),
                CopayAmount = lineItem.CopayAmount.ToString("F2", CultureInfo.InvariantCulture),
                CoinsuranceAmount = lineItem.CoinsuranceAmount.ToString("F2", CultureInfo.InvariantCulture),
                NotCoveredAmount = lineItem.NotCoveredAmount.ToString("F2", CultureInfo.InvariantCulture),
                Status = lineItem.Status.ToString(),
                DenialReason = lineItem.DenialReason ?? ""
            });
        }

        // Map patient info
        if (claim.Patient != null)
        {
            healthcareClaim.PatientInfo = new PatientInfo
            {
                Id = claim.Patient.Id,
                PatientId = claim.Patient.PatientId,
                FirstName = claim.Patient.User.FirstName,
                LastName = claim.Patient.User.LastName,
                DateOfBirth = claim.Patient.DateOfBirth.ToString("yyyy-MM-dd"),
                Gender = claim.Patient.Gender?.ToString() ?? "",
                PhoneNumber = claim.Patient.PhoneNumber ?? ""
            };
        }

        // Map provider info
        if (claim.Provider != null)
        {
            healthcareClaim.ProviderInfo = new ProviderInfo
            {
                Id = claim.Provider.Id,
                ProviderId = claim.Provider.ProviderId,
                ProviderName = claim.Provider.ProviderName,
                ProviderType = claim.Provider.ProviderType.ToString(),
                PhoneNumber = claim.Provider.PhoneNumber ?? ""
            };
            healthcareClaim.ProviderInfo.Specialties.AddRange(claim.Provider.Specialties);
        }

        // Map insurance plan info
        if (claim.InsurancePlan != null)
        {
            healthcareClaim.InsurancePlanInfo = new InsurancePlanInfo
            {
                Id = claim.InsurancePlan.Id,
                PlanName = claim.InsurancePlan.PlanName,
                PlanCode = claim.InsurancePlan.PlanCode,
                PlanType = claim.InsurancePlan.PlanType.ToString(),
                CompanyName = claim.InsurancePlan.InsuranceCompany.CompanyName
            };
        }

        return healthcareClaim;
    }
}