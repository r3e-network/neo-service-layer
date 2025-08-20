using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NeoServiceLayer.Services.Authentication;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System;


namespace NeoServiceLayer.Api.Middleware
{
    /// <summary>
    /// Enhanced authentication middleware with token validation and session management
    /// </summary>
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public AuthenticationMiddleware(
            RequestDelegate next,
            ILogger<AuthenticationMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;

            var jwtSecret = configuration["Authentication:JwtSecret"];
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
                ClockSkew = TimeSpan.Zero
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = ExtractToken(context);

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    // Get authentication service
                    var authService = context.RequestServices.GetService<IAuthenticationService>();

                    // Check if token is blacklisted
                    if (authService != null && await authService.IsTokenBlacklistedAsync(token))
                    {
                        _logger.LogWarning("Blacklisted token used: {Token}", token.Substring(0, 10) + "...");
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Token has been revoked");
                        return;
                    }

                    // Validate token
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);

                    // Extract claims
                    var jwtToken = validatedToken as JwtSecurityToken;
                    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var sessionId = jwtToken?.Claims?.FirstOrDefault(x => x.Type == "session_id")?.Value;

                    // Validate session if present
                    if (!string.IsNullOrEmpty(sessionId) && authService != null)
                    {
                        var sessions = await authService.GetActiveSessionsAsync(userId);
                        var currentSession = sessions?.FirstOrDefault(s => s.SessionId == sessionId);

                        if (currentSession == null || !currentSession.IsActive)
                        {
                            _logger.LogWarning("Invalid or expired session: {SessionId}", sessionId);
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync("Session expired or invalid");
                            return;
                        }

                        // Update session activity
                        currentSession.LastActivityAt = DateTime.UtcNow;
                    }

                    // Set user context
                    context.User = principal;

                    // Add custom claims to context
                    context.Items["UserId"] = userId;
                    context.Items["SessionId"] = sessionId;
                    context.Items["Token"] = token;

                    // Log successful authentication
                    _logger.LogDebug("User {UserId} authenticated successfully", userId);
                }
                catch (SecurityTokenExpiredException)
                {
                    _logger.LogWarning("Expired token used");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token expired");
                    return;
                }
                catch (SecurityTokenInvalidSignatureException)
                {
                    _logger.LogWarning("Invalid token signature");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid token");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Token validation failed");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Authentication failed");
                    return;
                }
            }

            await _next(context);
        }

        private string ExtractToken(HttpContext context)
        {
            // Check Authorization header
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring(7);
            }

            // Check cookie (for web applications)
            if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
            {
                return cookieToken;
            }

            // Check query string (for WebSocket connections)
            if (context.Request.Query.TryGetValue("access_token", out var queryToken))
            {
                return queryToken;
            }

            return null;
        }
    }

    /// <summary>
    /// Rate limiting middleware for authentication endpoints
    /// </summary>
    public class AuthenticationRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationRateLimitMiddleware> _logger;
        private readonly IRateLimitService _rateLimitService;

        public AuthenticationRateLimitMiddleware(
            RequestDelegate next,
            ILogger<AuthenticationRateLimitMiddleware> logger,
            IRateLimitService rateLimitService)
        {
            _next = next;
            _logger = logger;
            _rateLimitService = rateLimitService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only apply to authentication endpoints
            if (context.Request.Path.StartsWithSegments("/api/v1/authentication"))
            {
                var identifier = GetClientIdentifier(context);
                var endpoint = context.Request.Path.Value;

                // Check rate limit
                var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(identifier, endpoint);

                if (!rateLimitResult.IsAllowed)
                {
                    _logger.LogWarning("Rate limit exceeded for {Identifier} on {Endpoint}", identifier, endpoint);

                    context.Response.StatusCode = 429;
                    context.Response.Headers.Add("X-RateLimit-Limit", rateLimitResult.Limit.ToString());
                    context.Response.Headers.Add("X-RateLimit-Remaining", rateLimitResult.Remaining.ToString());
                    context.Response.Headers.Add("X-RateLimit-Reset", rateLimitResult.ResetAt.ToUnixTimeSeconds().ToString());
                    context.Response.Headers.Add("Retry-After", rateLimitResult.RetryAfter.ToString());

                    await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                    return;
                }

                // Add rate limit headers
                context.Response.Headers.Add("X-RateLimit-Limit", rateLimitResult.Limit.ToString());
                context.Response.Headers.Add("X-RateLimit-Remaining", rateLimitResult.Remaining.ToString());
                context.Response.Headers.Add("X-RateLimit-Reset", rateLimitResult.ResetAt.ToUnixTimeSeconds().ToString());
            }

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Try to get user ID if authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    return $"user:{userId}";
                }
            }

            // Fall back to IP address
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                return $"ip:{ipAddress}";
            }

            // Last resort: use a generic identifier
            return "anonymous";
        }
    }

    /// <summary>
    /// Security headers middleware
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Add("Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self' data:; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none';");

            // Remove server header
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await _next(context);
        }
    }

    /// <summary>
    /// Rate limit service interface
    /// </summary>
    public interface IRateLimitService
    {
        Task<RateLimitResult> CheckRateLimitAsync(string identifier, string resource);
        Task RecordRequestAsync(string identifier, string resource);
        Task ResetLimitAsync(string identifier, string resource);
    }

    /// <summary>
    /// Rate limit result
    /// </summary>
    public class RateLimitResult
    {
        public bool IsAllowed { get; set; }
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public DateTimeOffset ResetAt { get; set; }
        public int RetryAfter { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset ResetsAt => ResetAt; // Compatibility property
    }

    /// <summary>
    /// Extension methods for middleware registration
    /// </summary>
    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseEnhancedAuthentication(this IApplicationBuilder builder)
        {
            return builder
                .UseMiddleware<SecurityHeadersMiddleware>()
                .UseMiddleware<AuthenticationRateLimitMiddleware>()
                .UseMiddleware<AuthenticationMiddleware>();
        }
    }
}
