using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NeoServiceLayer.Services.Authentication;

namespace NeoServiceLayer.Api.Middleware
{
    /// <summary>
    /// Middleware for JWT token validation and authentication
    /// </summary>
    public class JwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtValidationMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public JwtValidationMiddleware(
            RequestDelegate next,
            ILogger<JwtValidationMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            _tokenHandler = new JwtSecurityTokenHandler();

            var jwtSecret = configuration["Authentication:JwtSecret"] 
                ?? throw new InvalidOperationException("JWT secret not configured");
            var issuer = configuration["Authentication:Issuer"] ?? "NeoServiceLayer";
            var audience = configuration["Authentication:Audience"] ?? "NeoServiceLayer";

            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for public endpoints
            if (ShouldSkipAuthentication(context))
            {
                await _next(context);
                return;
            }

            var token = ExtractToken(context);
            
            if (string.IsNullOrEmpty(token))
            {
                // No token provided, continue without authentication
                await _next(context);
                return;
            }

            try
            {
                // Check if token is blacklisted
                var authService = context.RequestServices.GetService<IAuthenticationService>();
                if (authService != null && await authService.IsTokenBlacklistedAsync(token))
                {
                    await HandleUnauthorizedAsync(context, "Token has been revoked");
                    return;
                }

                // Validate token
                var principal = ValidateToken(token);
                
                if (principal == null)
                {
                    await HandleUnauthorizedAsync(context, "Invalid token");
                    return;
                }

                // Check if user account is still active
                var userId = GetUserId(principal);
                if (!string.IsNullOrEmpty(userId) && authService != null)
                {
                    var accountStatus = await authService.GetAccountSecurityStatusAsync(userId);
                    if (accountStatus?.IsLocked == true)
                    {
                        await HandleUnauthorizedAsync(context, "Account is locked");
                        return;
                    }
                }

                // Set user principal
                context.User = principal;
                
                // Add token to items for downstream use
                context.Items["Token"] = token;
                context.Items["UserId"] = userId;

                // Log successful authentication
                _logger.LogDebug("Successfully authenticated user {UserId}", userId);
            }
            catch (SecurityTokenExpiredException)
            {
                await HandleUnauthorizedAsync(context, "Token has expired");
                return;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                await HandleUnauthorizedAsync(context, "Invalid token");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token validation");
                await HandleUnauthorizedAsync(context, "Authentication failed");
                return;
            }

            await _next(context);
        }

        private string ExtractToken(HttpContext context)
        {
            // Check Authorization header
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader))
            {
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return authHeader.Substring(7).Trim();
                }
            }

            // Check for token in cookie (for web applications)
            if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
            {
                return cookieToken;
            }

            // Check for token in query string (for WebSocket connections)
            if (context.Request.Query.TryGetValue("access_token", out var queryToken))
            {
                return queryToken.ToString();
            }

            return null;
        }

        private ClaimsPrincipal ValidateToken(string token)
        {
            try
            {
                var principal = _tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
                
                // Additional validation
                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    // Ensure token uses the correct algorithm
                    if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new SecurityTokenException("Invalid token algorithm");
                    }

                    // Check token not before time
                    if (jwtToken.ValidFrom > DateTime.UtcNow)
                    {
                        throw new SecurityTokenException("Token not yet valid");
                    }
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Token validation failed");
                return null;
            }
        }

        private string GetUserId(ClaimsPrincipal principal)
        {
            if (principal == null)
                return null;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)
                ?? principal.FindFirst("sub")
                ?? principal.FindFirst("user_id");

            return userIdClaim?.Value;
        }

        private bool ShouldSkipAuthentication(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            // Public endpoints that don't require authentication
            var publicPaths = new[]
            {
                "/health",
                "/metrics",
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/forgot-password",
                "/api/auth/reset-password",
                "/api/auth/verify-email",
                "/api/auth/refresh",
                "/swagger",
                "/api-docs"
            };

            return publicPaths.Any(p => path.StartsWith(p));
        }

        private async Task HandleUnauthorizedAsync(HttpContext context, string message)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "unauthorized",
                message = message,
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    /// <summary>
    /// Extension methods for JWT validation middleware
    /// </summary>
    public static class JwtValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtValidationMiddleware>();
        }
    }
}