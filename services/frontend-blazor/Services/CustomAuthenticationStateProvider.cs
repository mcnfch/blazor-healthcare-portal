using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using BlazorApp.Models.DTOs;

namespace BlazorApp.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // If we already have a current user, return it (for subsequent calls)
            if (_currentUser.Identity?.IsAuthenticated == true)
            {
                return new AuthenticationState(_currentUser);
            }

            // Try to get authentication data from localStorage
            string? token = null;
            string? userJson = null;
            
            try
            {
                token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
                userJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "currentUser");
            }
            catch (InvalidOperationException)
            {
                // JavaScript not available yet (pre-rendering), return current user
                return new AuthenticationState(_currentUser);
            }

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userJson))
            {
                return new AuthenticationState(_currentUser);
            }

            // Validate token expiration
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                await LogoutAsync();
                return new AuthenticationState(_currentUser);
            }

            // Parse user info
            var user = JsonSerializer.Deserialize<UserInfo>(userJson);
            if (user != null)
            {
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("sub", user.Id.ToString()),
                    new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                    new Claim("given_name", user.FirstName),
                    new Claim("family_name", user.LastName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("email", user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("role", user.Role)
                }, "jwt");

                _currentUser = new ClaimsPrincipal(identity);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting authentication state: {ex.Message}");
            await LogoutAsync();
        }

        return new AuthenticationState(_currentUser);
    }

    public async Task LoginAsync(string token, UserInfo user)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", token);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", JsonSerializer.Serialize(user));

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("sub", user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim("given_name", user.FirstName),
            new Claim("family_name", user.LastName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("email", user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("role", user.Role)
        }, "jwt");

        _currentUser = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
        }
        catch
        {
            return null;
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            var userJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "currentUser");

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userJson))
            {
                // Validate token expiration
                var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
                if (jwtToken.ValidTo >= DateTime.UtcNow)
                {
                    // Parse user info and create claims
                    var user = JsonSerializer.Deserialize<UserInfo>(userJson);
                    if (user != null)
                    {
                        var identity = new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                            new Claim("sub", user.Id.ToString()),
                            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                            new Claim("given_name", user.FirstName),
                            new Claim("family_name", user.LastName),
                            new Claim(ClaimTypes.Email, user.Email),
                            new Claim("email", user.Email),
                            new Claim(ClaimTypes.Role, user.Role),
                            new Claim("role", user.Role)
                        }, "jwt");

                        _currentUser = new ClaimsPrincipal(identity);
                        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
                        return;
                    }
                }
            }

            // If we get here, no valid authentication found
            await LogoutAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing authentication: {ex.Message}");
            await LogoutAsync();
        }
    }
}