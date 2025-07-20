using DocumentService.Models;
using DocumentService.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/document-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddGrpc();

// Configure Entity Framework with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                      Environment.GetEnvironmentVariable("DATABASE_URL") ??
                      "Host=localhost;Database=claims_db;Username=claims_user;Password=claims_password";

builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null))
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));

// Register document processor
builder.Services.AddScoped<IDocumentProcessor, DocumentProcessor>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContext<DocumentDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Health check endpoint
app.MapHealthChecks("/health");

// Configure gRPC services
app.MapGrpcService<DocumentServiceImpl>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
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

// Ensure storage directory exists
var storageDir = builder.Configuration.GetValue<string>("StorageDirectory") ?? "/app/storage";
Directory.CreateDirectory(storageDir);
Log.Information("Storage directory ensured at: {StorageDirectory}", storageDir);

Log.Information("Document Service starting on port 50052");

app.Run("http://0.0.0.0:50052");