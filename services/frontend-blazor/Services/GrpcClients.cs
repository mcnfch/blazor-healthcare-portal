using Grpc.Core;
using Grpc.Net.Client;
using ClaimsProcessor.Protos;
using DocumentService.Protos;

namespace BlazorApp.Services;

public interface IClaimsGrpcClient
{
    Task<ClaimsProcessor.Protos.SubmitClaimResponse> SubmitClaimAsync(ClaimsProcessor.Protos.SubmitClaimRequest request);
    Task<ClaimsProcessor.Protos.GetClaimResponse> GetClaimAsync(ClaimsProcessor.Protos.GetClaimRequest request);
    Task<ClaimsProcessor.Protos.UpdateClaimStatusResponse> UpdateClaimStatusAsync(ClaimsProcessor.Protos.UpdateClaimStatusRequest request);
    Task<ClaimsProcessor.Protos.ListClaimsResponse> ListClaimsAsync(ClaimsProcessor.Protos.ListClaimsRequest request);
    Task<ClaimsProcessor.Protos.ProcessPaymentResponse> ProcessClaimPaymentAsync(ClaimsProcessor.Protos.ProcessPaymentRequest request);
}

public interface IDocumentGrpcClient
{
    Task<ProcessDocumentResponse> ProcessDocumentAsync(ProcessDocumentRequest request);
    Task<GetDocumentMetadataResponse> GetDocumentMetadataAsync(GetDocumentMetadataRequest request);
    Task<ListClaimDocumentsResponse> ListClaimDocumentsAsync(ListClaimDocumentsRequest request);
    Task<DeleteDocumentResponse> DeleteDocumentAsync(DeleteDocumentRequest request);
}

public class ClaimsGrpcClient : IClaimsGrpcClient
{
    private readonly ClaimsProcessor.Protos.ClaimsService.ClaimsServiceClient _client;
    private readonly ILogger<ClaimsGrpcClient> _logger;

    public ClaimsGrpcClient(IConfiguration configuration, ILogger<ClaimsGrpcClient> logger)
    {
        _logger = logger;
        
        var claimsServiceUrl = configuration.GetValue<string>("GrpcServices:ClaimsProcessor") ?? 
                              Environment.GetEnvironmentVariable("CLAIMS_PROCESSOR_URL") ?? 
                              "http://localhost:50051";

        var channel = GrpcChannel.ForAddress(claimsServiceUrl);
        _client = new ClaimsProcessor.Protos.ClaimsService.ClaimsServiceClient(channel);
        
        _logger.LogInformation("Claims gRPC client initialized for: {Url}", claimsServiceUrl);
    }

    public async Task<ClaimsProcessor.Protos.SubmitClaimResponse> SubmitClaimAsync(ClaimsProcessor.Protos.SubmitClaimRequest request)
    {
        try
        {
            return await _client.SubmitClaimAsync(request, deadline: DateTime.UtcNow.AddSeconds(30));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error submitting claim");
            return new ClaimsProcessor.Protos.SubmitClaimResponse
            {
                Success = false,
                Message = $"Service error: {ex.Status.Detail}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting claim");
            return new ClaimsProcessor.Protos.SubmitClaimResponse
            {
                Success = false,
                Message = "An error occurred while submitting the claim"
            };
        }
    }

    public async Task<ClaimsProcessor.Protos.GetClaimResponse> GetClaimAsync(ClaimsProcessor.Protos.GetClaimRequest request)
    {
        try
        {
            return await _client.GetClaimAsync(request, deadline: DateTime.UtcNow.AddSeconds(30));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error getting claim");
            return new ClaimsProcessor.Protos.GetClaimResponse
            {
                Success = false,
                Message = $"Service error: {ex.Status.Detail}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claim");
            return new ClaimsProcessor.Protos.GetClaimResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the claim"
            };
        }
    }

    public async Task<ClaimsProcessor.Protos.UpdateClaimStatusResponse> UpdateClaimStatusAsync(ClaimsProcessor.Protos.UpdateClaimStatusRequest request)
    {
        try
        {
            return await _client.UpdateClaimStatusAsync(request, deadline: DateTime.UtcNow.AddSeconds(30));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error updating claim status");
            return new ClaimsProcessor.Protos.UpdateClaimStatusResponse
            {
                Success = false,
                Message = $"Service error: {ex.Status.Detail}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating claim status");
            return new ClaimsProcessor.Protos.UpdateClaimStatusResponse
            {
                Success = false,
                Message = "An error occurred while updating the claim status"
            };
        }
    }

    public async Task<ClaimsProcessor.Protos.ListClaimsResponse> ListClaimsAsync(ClaimsProcessor.Protos.ListClaimsRequest request)
    {
        try
        {
            return await _client.ListClaimsAsync(request, deadline: DateTime.UtcNow.AddSeconds(30));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error listing claims");
            return new ClaimsProcessor.Protos.ListClaimsResponse
            {
                Success = false,
                Message = $"Service error: {ex.Status.Detail}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing claims");
            return new ClaimsProcessor.Protos.ListClaimsResponse
            {
                Success = false,
                Message = "An error occurred while listing claims"
            };
        }
    }

    public async Task<ClaimsProcessor.Protos.ProcessPaymentResponse> ProcessClaimPaymentAsync(ClaimsProcessor.Protos.ProcessPaymentRequest request)
    {
        try
        {
            return await _client.ProcessClaimPaymentAsync(request, deadline: DateTime.UtcNow.AddSeconds(30));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error processing claim payment");
            return new ClaimsProcessor.Protos.ProcessPaymentResponse
            {
                Success = false,
                Message = $"Service error: {ex.Status.Detail}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing claim payment");
            return new ClaimsProcessor.Protos.ProcessPaymentResponse
            {
                Success = false,
                Message = "An error occurred while processing the payment"
            };
        }
    }
}

public class DocumentGrpcClient : IDocumentGrpcClient
{
    private readonly DocumentService.Protos.DocumentService.DocumentServiceClient _client;
    private readonly ILogger<DocumentGrpcClient> _logger;

    public DocumentGrpcClient(IConfiguration configuration, ILogger<DocumentGrpcClient> logger)
    {
        _logger = logger;
        
        var documentServiceUrl = configuration.GetValue<string>("GrpcServices:DocumentService") ?? 
                                Environment.GetEnvironmentVariable("DOC_SERVICE_URL") ?? 
                                "http://localhost:50052";

        var channel = GrpcChannel.ForAddress(documentServiceUrl);
        _client = new DocumentService.Protos.DocumentService.DocumentServiceClient(channel);
        
        _logger.LogInformation("Document gRPC client initialized for: {Url}", documentServiceUrl);
    }

    public async Task<ProcessDocumentResponse> ProcessDocumentAsync(ProcessDocumentRequest request)
    {
        try
        {
            return await _client.ProcessDocumentAsync(request, deadline: DateTime.UtcNow.AddMinutes(2));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error processing document");
            return new ProcessDocumentResponse
            {
                Success = false,
                Message = $"Service error: {ex.Status.Detail}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document");
            return new ProcessDocumentResponse
            {
                Success = false,
                Message = "An error occurred while processing the document"
            };
        }
    }

    public async Task<GetDocumentMetadataResponse> GetDocumentMetadataAsync(GetDocumentMetadataRequest request)
    {
        try
        {
            return await _client.GetDocumentMetadataAsync(request, deadline: DateTime.UtcNow.AddSeconds(30));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error getting document metadata");
            return new GetDocumentMetadataResponse
            {
                Success = false,
                Message = $"Service error: {ex.Status.Detail}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document metadata");
            return new GetDocumentMetadataResponse
            {
                Success = false,
                Message = "An error occurred while retrieving document metadata"
            };
        }
    }

    public async Task<ListClaimDocumentsResponse> ListClaimDocumentsAsync(ListClaimDocumentsRequest request)
    {
        try
        {
            return await _client.ListClaimDocumentsAsync(request, deadline: DateTime.UtcNow.AddSeconds(30));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error listing claim documents");
            return new ListClaimDocumentsResponse
            {
                Success = false,
                Message = $"Service error: {ex.Status.Detail}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing claim documents");
            return new ListClaimDocumentsResponse
            {
                Success = false,
                Message = "An error occurred while listing claim documents"
            };
        }
    }

    public async Task<DeleteDocumentResponse> DeleteDocumentAsync(DeleteDocumentRequest request)
    {
        try
        {
            return await _client.DeleteDocumentAsync(request, deadline: DateTime.UtcNow.AddSeconds(30));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error deleting document");
            return new DeleteDocumentResponse
            {
                Success = false,
                Message = $"Service error: {ex.Status.Detail}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document");
            return new DeleteDocumentResponse
            {
                Success = false,
                Message = "An error occurred while deleting the document"
            };
        }
    }
}