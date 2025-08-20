using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using NeoServiceLayer.Services.Authentication.Services;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;


namespace NeoServiceLayer.Api.Middleware
{
    /// <summary>
    /// Enhanced authentication middleware with comprehensive security features:
    /// - JWT token validation
    /// - Role-based access control
    /// - Session validation
    /// - Request context enrichment
    /// - Security headers
    /// </summary>
    public class EnhancedAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EnhancedAuthenticationMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public EnhancedAuthenticationMiddleware(
            RequestDelegate next,
            ILogger<EnhancedAuthenticationMiddleware> logger,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            _cache = cache;

            _jwtSecret = configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("JWT secret not configured");
            _jwtIssuer = configuration["Jwt:Issuer"] ?? "NeoServiceLayer";
            _jwtAudience = configuration["Jwt:Audience"] ?? "NeoServiceLayer.Api";
        }

        public async Task InvokeAsync(HttpContext context, IEnhancedJwtTokenService tokenService)
        {
            // Add security headers
            AddSecurityHeaders(context);

            // Skip authentication for public endpoints
            if (IsPublicEndpoint(context.Request.Path))
            {
                await _next(context);
                return;
            }

            try
            {
                // Extract token from header
                var token = ExtractTokenFromHeader(context);

                if (string.IsNullOrEmpty(token))
                {
                    // Check if endpoint requires authentication
                    if (RequiresAuthentication(context))
                    {
                        await RespondWithUnauthorized(context, "No authentication token provided");
                        return;
                    }

                    await _next(context);
                    return;
                }

                // Validate token
                var validationResult = await ValidateTokenWithCacheAsync(token, tokenService);

                if (!validationResult.IsValid)
                {
                    await RespondWithUnauthorized(context, validationResult.Error ?? "Invalid token");
                    return;
                }

                // Set user context
                SetUserContext(context, validationResult);

                // Check role-based access
                if (!await CheckRoleBasedAccessAsync(context, validationResult))
                {
                    await RespondWithForbidden(context, "Insufficient permissions");
                    return;
                }

                // Track request
                TrackAuthenticatedRequest(context, validationResult);

                // Continue pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication middleware error");
                await RespondWithUnauthorized(context, "Authentication error");
            }
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

            if (_configuration.GetValue<bool>("Security:EnableHSTS", true))
            {
                context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }
        }

        private bool IsPublicEndpoint(PathString path)
        {
            var publicPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/forgot-password",
                "/api/auth/reset-password",
                "/api/auth/verify-email",
                "/api/health",
                "/swagger"
            };

            return publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        }

        private bool RequiresAuthentication(HttpContext context)
        {
            // Check if endpoint has [AllowAnonymous] attribute
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
            {
                return false;
            }

            // Default to requiring authentication for API endpoints
            return context.Request.Path.StartsWithSegments("/api");
        }

        private string ExtractTokenFromHeader(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader))
            {
                // Check cookie as fallback
                return context.Request.Cookies["access_token"];
            }

            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring(7);
            }

            return null;
        }

        private async Task<NeoServiceLayer.Services.Authentication.Services.TokenValidationResult> ValidateTokenWithCacheAsync(string token, IEnhancedJwtTokenService tokenService)
        {
            // Check cache first
            var cacheKey = $"token_validation_{ComputeTokenHash(token)}";

            if (_cache.TryGetValue<NeoServiceLayer.Services.Authentication.Services.TokenValidationResult>(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }

            // Validate token
            var result = await tokenService.ValidateAccessTokenAsync(token);

            if (result.IsValid)
            {
                // Cache successful validation for a short time
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(1));
            }

            return result;
        }

        private string ComputeTokenHash(string token)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hash);
        }

        private void SetUserContext(HttpContext context, NeoServiceLayer.Services.Authentication.Services.TokenValidationResult validationResult)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, validationResult.UserId.ToString()),
                new Claim(ClaimTypes.Name, validationResult.Username ?? ""),
                new Claim(ClaimTypes.Email, validationResult.Email ?? "")
            };

            foreach (var role in validationResult.Roles ?? new List<string>())
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add custom claims
            if (validationResult.Claims != null)
            {
                foreach (var claim in validationResult.Claims.Where(c =>
                    !c.Key.Equals(ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase) &&
                    !c.Key.Equals(ClaimTypes.Name, StringComparison.OrdinalIgnoreCase) &&
                    !c.Key.Equals(ClaimTypes.Email, StringComparison.OrdinalIgnoreCase) &&
                    !c.Key.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)))
                {
                    claims.Add(new Claim(claim.Key, claim.Value));
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            context.User = new ClaimsPrincipal(identity);

            // Store additional context
            context.Items["UserId"] = validationResult.UserId;
            context.Items["Username"] = validationResult.Username;
            context.Items["TokenId"] = validationResult.TokenId;
        }

        private async Task<bool> CheckRoleBasedAccessAsync(HttpContext context, NeoServiceLayer.Services.Authentication.Services.TokenValidationResult validationResult)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint == null)
            {
                return true;
            }

            // Check for [Authorize(Roles = "...")] attribute
            var authorizeAttribute = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
            if (authorizeAttribute?.Roles != null)
            {
                var requiredRoles = authorizeAttribute.Roles.Split(',').Select(r => r.Trim());
                var userRoles = validationResult.Roles ?? new List<string>();

                return requiredRoles.Any(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
            }

            // Check for custom permission attributes
            var permissionAttribute = endpoint.Metadata.GetMetadata<RequirePermissionAttribute>();
            if (permissionAttribute != null)
            {
                return await CheckPermissionAsync(validationResult.UserId, permissionAttribute.Permission);
            }

            return true;
        }

        private async Task<bool> CheckPermissionAsync(Guid userId, string permission)
        {
            // This would check user permissions from database or cache
            // Implementation would depend on your permission system
            var cacheKey = $"user_permissions_{userId}";

            var permissions = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(5);
                // Load permissions from database
                return new List<string>();
            });

            return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }

        private void TrackAuthenticatedRequest(HttpContext context, NeoServiceLayer.Services.Authentication.Services.TokenValidationResult validationResult)
        {
            // Log authenticated request for monitoring
            _logger.LogDebug(
                "Authenticated request from user {UserId} to {Path}",
                validationResult.UserId,
                context.Request.Path);

            // Update last activity timestamp for session tracking
            if (context.Items.TryGetValue("SessionId", out var sessionId))
            {
                var cacheKey = $"session_activity_{sessionId}";
                _cache.Set(cacheKey, DateTime.UtcNow, TimeSpan.FromMinutes(30));
            }
        }

        private async Task RespondWithUnauthorized(HttpContext context, string message)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Unauthorized",
                message = message,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }

        private async Task RespondWithForbidden(HttpContext context, string message)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Forbidden",
                message = message,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    }

    /// <summary>
    /// Custom attribute for permission-based authorization
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequirePermissionAttribute : Attribute
    {
        public string Permission { get; }

        public RequirePermissionAttribute(string permission)
        {
            Permission = permission;
        }
    }

    /// <summary>
    /// Extension methods for middleware registration
    /// </summary>
    public static class EnhancedAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseEnhancedAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EnhancedAuthenticationMiddleware>();
        }
    }
}