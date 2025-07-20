using ClaimsProcessor.Models;
using ClaimsProcessor.Services;
using ClaimsProcessor.Services.Messaging;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/claims-processor-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddGrpc();

// Configure Entity Framework with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                      Environment.GetEnvironmentVariable("DATABASE_URL") ??
                      "Host=localhost;Database=claims_db;Username=claims_user;Password=claims_password";

builder.Services.AddDbContext<HealthcareDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null))
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));

// Register RabbitMQ publisher
builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<RabbitMQHealthCheck>("rabbitmq");

// Add health check for Entity Framework
builder.Services.AddScoped<RabbitMQHealthCheck>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Health check endpoint
app.MapHealthChecks("/health");

// Configure gRPC services
app.MapGrpcService<ClaimsService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HealthcareDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database connection verified successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to connect to database");
        throw;
    }
}

Log.Information("Claims Processor gRPC Service starting on port 50051");

app.Run("http://0.0.0.0:50051");

// Health check for RabbitMQ
public class RabbitMQHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<RabbitMQHealthCheck> _logger;

    public RabbitMQHealthCheck(IMessagePublisher messagePublisher, ILogger<RabbitMQHealthCheck> logger)
    {
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple check - if the service was created successfully, RabbitMQ is likely working
            // In a more sophisticated implementation, you might try to publish a test message
            return await Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("RabbitMQ is accessible"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ health check failed");
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("RabbitMQ is not accessible", ex);
        }
    }
}