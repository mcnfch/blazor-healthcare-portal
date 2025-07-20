using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BlazorApp.Models;
using BlazorApp.Services;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Server services
builder.Services.AddRazorPages();
builder.Services.AddControllers(); // Add MVC controllers support
builder.Services.AddServerSideBlazor(options =>
{
    options.MaxBufferedUnacknowledgedRenderBatches = 10;
})
.AddHubOptions(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
});

// Configure file upload limits
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
    options.ValueLengthLimit = 10 * 1024 * 1024; // 10MB
});

// Configure Kestrel server limits
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

// Add Blazorise
builder.Services
    .AddBlazorise(options =>
    {
        options.Immediate = true;
    })
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons();

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
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information));

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "demo-secret-key-change-in-production";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ClaimsProcessingSystem",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ClaimsProcessingSystem",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register custom authentication state provider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Register application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClaimsGrpcClient, ClaimsGrpcClient>();
builder.Services.AddScoped<IDocumentGrpcClient, DocumentGrpcClient>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<IInsurancePlanService, InsurancePlanService>();
builder.Services.AddScoped<IMedicalCodesService, MedicalCodesService>();
builder.Services.AddTransient<IDocumentService, FileDocumentService>();

// Add HttpClient for API calls
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


// Map controllers FIRST to handle API routes
app.MapControllers();
app.MapRazorPages();
app.MapBlazorHub();
// Simple fallback for Blazor pages only
app.MapFallbackToPage("/_Host");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HealthcareDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("Database connection verified successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to connect to database: {ex.Message}");
    }
}

Console.WriteLine("Blazor Healthcare Insurance Portal starting on port 3000");

app.Run("http://0.0.0.0:3000");