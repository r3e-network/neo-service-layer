using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Api.Middleware
{
    /// <summary>
    /// Middleware to add security headers to HTTP responses
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;
        private readonly SecurityHeadersOptions _options;

        public SecurityHeadersMiddleware(
            RequestDelegate next,
            ILogger<SecurityHeadersMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _options = configuration.GetSection("SecurityHeaders").Get<SecurityHeadersOptions>() 
                ?? new SecurityHeadersOptions();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers before processing the request
            AddSecurityHeaders(context);

            // Process the request
            await _next(context);
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;

            // X-Content-Type-Options
            if (_options.EnableXContentTypeOptions)
            {
                headers["X-Content-Type-Options"] = "nosniff";
            }

            // X-Frame-Options
            if (_options.EnableXFrameOptions)
            {
                headers["X-Frame-Options"] = _options.XFrameOptions;
            }

            // X-XSS-Protection
            if (_options.EnableXXssProtection)
            {
                headers["X-XSS-Protection"] = "1; mode=block";
            }

            // Strict-Transport-Security (HSTS)
            if (_options.EnableHsts && context.Request.IsHttps)
            {
                var hstsValue = $"max-age={_options.HstsMaxAge}";
                if (_options.HstsIncludeSubDomains)
                {
                    hstsValue += "; includeSubDomains";
                }
                if (_options.HstsPreload)
                {
                    hstsValue += "; preload";
                }
                headers["Strict-Transport-Security"] = hstsValue;
            }

            // Content-Security-Policy
            if (_options.EnableContentSecurityPolicy && !string.IsNullOrEmpty(_options.ContentSecurityPolicy))
            {
                headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
            }

            // Referrer-Policy
            if (_options.EnableReferrerPolicy)
            {
                headers["Referrer-Policy"] = _options.ReferrerPolicy;
            }

            // Permissions-Policy (formerly Feature-Policy)
            if (_options.EnablePermissionsPolicy && !string.IsNullOrEmpty(_options.PermissionsPolicy))
            {
                headers["Permissions-Policy"] = _options.PermissionsPolicy;
            }

            // X-Permitted-Cross-Domain-Policies
            if (_options.EnableXPermittedCrossDomainPolicies)
            {
                headers["X-Permitted-Cross-Domain-Policies"] = "none";
            }

            // Remove server header
            if (_options.RemoveServerHeader)
            {
                headers.Remove("Server");
            }

            // Remove X-Powered-By header
            if (_options.RemoveXPoweredByHeader)
            {
                headers.Remove("X-Powered-By");
            }

            // Add custom security headers
            if (_options.CustomHeaders != null)
            {
                foreach (var customHeader in _options.CustomHeaders)
                {
                    if (!string.IsNullOrEmpty(customHeader.Key) && !string.IsNullOrEmpty(customHeader.Value))
                    {
                        headers[customHeader.Key] = customHeader.Value;
                    }
                }
            }

            // CORS headers (if not handled by CORS middleware)
            if (_options.EnableCorsHeaders)
            {
                // Only add CORS headers if not already present
                if (!headers.ContainsKey("Access-Control-Allow-Origin"))
                {
                    headers["Access-Control-Allow-Origin"] = _options.CorsAllowOrigin ?? "*";
                }
                
                if (!headers.ContainsKey("Access-Control-Allow-Methods"))
                {
                    headers["Access-Control-Allow-Methods"] = _options.CorsAllowMethods ?? "GET, POST, PUT, DELETE, OPTIONS";
                }
                
                if (!headers.ContainsKey("Access-Control-Allow-Headers"))
                {
                    headers["Access-Control-Allow-Headers"] = _options.CorsAllowHeaders ?? "Content-Type, Authorization";
                }

                if (_options.CorsAllowCredentials && !headers.ContainsKey("Access-Control-Allow-Credentials"))
                {
                    headers["Access-Control-Allow-Credentials"] = "true";
                }
            }

            _logger.LogDebug("Security headers added to response for {Path}", context.Request.Path);
        }
    }

    /// <summary>
    /// Configuration options for security headers
    /// </summary>
    public class SecurityHeadersOptions
    {
        // X-Content-Type-Options
        public bool EnableXContentTypeOptions { get; set; } = true;

        // X-Frame-Options
        public bool EnableXFrameOptions { get; set; } = true;
        public string XFrameOptions { get; set; } = "DENY"; // DENY, SAMEORIGIN, or ALLOW-FROM uri

        // X-XSS-Protection
        public bool EnableXXssProtection { get; set; } = true;

        // Strict-Transport-Security (HSTS)
        public bool EnableHsts { get; set; } = true;
        public int HstsMaxAge { get; set; } = 31536000; // 1 year in seconds
        public bool HstsIncludeSubDomains { get; set; } = true;
        public bool HstsPreload { get; set; } = false;

        // Content-Security-Policy
        public bool EnableContentSecurityPolicy { get; set; } = true;
        public string ContentSecurityPolicy { get; set; } = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        // Referrer-Policy
        public bool EnableReferrerPolicy { get; set; } = true;
        public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

        // Permissions-Policy (formerly Feature-Policy)
        public bool EnablePermissionsPolicy { get; set; } = true;
        public string PermissionsPolicy { get; set; } = 
            "accelerometer=(), " +
            "camera=(), " +
            "geolocation=(), " +
            "gyroscope=(), " +
            "magnetometer=(), " +
            "microphone=(), " +
            "payment=(), " +
            "usb=()";

        // X-Permitted-Cross-Domain-Policies
        public bool EnableXPermittedCrossDomainPolicies { get; set; } = true;

        // Server header removal
        public bool RemoveServerHeader { get; set; } = true;
        public bool RemoveXPoweredByHeader { get; set; } = true;

        // CORS headers (basic)
        public bool EnableCorsHeaders { get; set; } = false;
        public string CorsAllowOrigin { get; set; }
        public string CorsAllowMethods { get; set; }
        public string CorsAllowHeaders { get; set; }
        public bool CorsAllowCredentials { get; set; } = false;

        // Custom headers
        public CustomHeader[] CustomHeaders { get; set; }
    }

    /// <summary>
    /// Custom header configuration
    /// </summary>
    public class CustomHeader
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Extension methods for security headers middleware
    /// </summary>
    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}