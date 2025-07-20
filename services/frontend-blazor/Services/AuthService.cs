using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BlazorApp.Models;
using BlazorApp.Models.DTOs;
using BCrypt.Net;
using SecurityClaim = System.Security.Claims.Claim;

namespace BlazorApp.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<bool> ValidateTokenAsync(string token);
    Task<UserInfo?> GetUserFromTokenAsync(string token);
}

public class AuthService : IAuthService
{
    private readonly HealthcareDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthService(
        HealthcareDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        
        _jwtSecret = _configuration["Jwt:Secret"] ?? "demo-secret-key-change-in-production";
        _jwtIssuer = _configuration["Jwt:Issuer"] ?? "ClaimsProcessingSystem";
        _jwtAudience = _configuration["Jwt:Audience"] ?? "ClaimsProcessingSystem";
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("Login attempt failed - user not found: {Email}", request.Email);
                return new LoginResponse(false, null, null, "Invalid credentials");
            }

            // Verify password - Demo mode: accept "password" for all demo accounts
            bool isValidPassword = false;
            
            if (request.Password == "password" && 
                (user.Email.Contains("patient@example.com") || 
                 user.Email.Contains("admin@example.com") || 
                 user.Email.Contains("adjuster@example.com")))
            {
                isValidPassword = true;
            }
            else
            {
                isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            
            if (!isValidPassword)
            {
                _logger.LogWarning("Login attempt failed - invalid password for user: {Email}", request.Email);
                return new LoginResponse(false, null, null, "Invalid credentials");
            }

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = GenerateJwtToken(user);
            
            var userInfo = new UserInfo(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role.ToString());

            _logger.LogInformation("User logged in successfully: {Email}, Role: {Role}", user.Email, user.Role);

            return new LoginResponse(true, token, userInfo, "Login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Email}", request.Email);
            return new LoginResponse(false, null, null, "An error occurred during login");
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserInfo?> GetUserFromTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            return new UserInfo(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user from token");
            return null;
        }
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new SecurityClaim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new SecurityClaim(ClaimTypes.Email, user.Email),
                new SecurityClaim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new SecurityClaim(ClaimTypes.Role, user.Role.ToString()),
                new SecurityClaim("firstName", user.FirstName),
                new SecurityClaim("lastName", user.LastName)
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}