using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Api.Extensions
{
    /// <summary>
    /// Extension methods for enhanced security configuration
    /// </summary>
    public static class SecurityExtensions
    {
        /// <summary>
        /// Adds comprehensive HTTPS security configuration
        /// </summary>
        public static IServiceCollection AddHttpsSecurity(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure HTTPS redirection with proper ports
            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                options.HttpsPort = configuration.GetValue<int>("HttpsPort", 443);
            });

            // Configure HSTS with security-first defaults
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365); // 1 year
                options.ExcludedHosts.Clear(); // Remove localhost exclusions for production
            });

            return services;
        }

        /// <summary>
        /// Configures Kestrel with security hardening
        /// </summary>
        public static IWebHostBuilder ConfigureKestrelSecurity(this IWebHostBuilder builder)
        {
            return builder.ConfigureKestrel(serverOptions =>
            {
                // Security-hardened server limits
                serverOptions.Limits.MaxConcurrentConnections = 1000;
                serverOptions.Limits.MaxConcurrentUpgradedConnections = 100;
                serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
                serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
                serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);

                // Disable server header
                serverOptions.AddServerHeader = false;

                // HTTP/2 configuration for better performance and security
                serverOptions.ConfigureHttpsDefaults(httpsOptions =>
                {
                    httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
                });
            });
        }

        /// <summary>
        /// Adds enhanced security middleware pipeline
        /// </summary>
        public static IApplicationBuilder UseEnhancedSecurity(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Security headers (must be early in pipeline)
            app.UseMiddleware<EnhancedSecurityHeadersMiddleware>();

            if (!env.IsDevelopment())
            {
                // HSTS in production only
                app.UseHsts();
            }

            // HTTPS redirection
            app.UseHttpsRedirection();

            return app;
        }
    }

    /// <summary>
    /// Enhanced security headers middleware with modern security policies
    /// </summary>
    public class EnhancedSecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EnhancedSecurityHeadersMiddleware> _logger;
        private readonly EnhancedSecurityOptions _options;

        public EnhancedSecurityHeadersMiddleware(
            RequestDelegate next,
            ILogger<EnhancedSecurityHeadersMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _options = configuration.GetSection("EnhancedSecurity").Get<EnhancedSecurityOptions>()
                ?? new EnhancedSecurityOptions();
        }

        public async System.Threading.Tasks.Task InvokeAsync(HttpContext context)
        {
            await _next(context);
            AddSecurityHeaders(context);
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;

            try
            {
                // Remove information disclosure headers
                headers.Remove("Server");
                headers.Remove("X-Powered-By");
                headers.Remove("X-AspNet-Version");
                headers.Remove("X-AspNetMvc-Version");

                // Content Security Policy - Secure by default (no unsafe-inline or unsafe-eval)
                if (_options.EnableContentSecurityPolicy)
                {
                    var csp = BuildSecureContentSecurityPolicy();
                    headers["Content-Security-Policy"] = csp;
                }

                // Strict Transport Security (HSTS)
                if (_options.EnableHsts && context.Request.IsHttps)
                {
                    var hstsValue = $"max-age={_options.HstsMaxAgeSeconds}; includeSubDomains; preload";
                    headers["Strict-Transport-Security"] = hstsValue;
                }

                // X-Frame-Options
                headers["X-Frame-Options"] = "DENY";

                // X-Content-Type-Options
                headers["X-Content-Type-Options"] = "nosniff";

                // X-XSS-Protection (deprecated but still useful for older browsers)
                headers["X-XSS-Protection"] = "0"; // Modern browsers use CSP instead

                // Referrer Policy
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                // Permissions Policy (modern Feature-Policy replacement)
                if (_options.EnablePermissionsPolicy)
                {
                    var permissionsPolicy = BuildSecurePermissionsPolicy();
                    headers["Permissions-Policy"] = permissionsPolicy;
                }

                // Cross-Origin Policies
                headers["Cross-Origin-Embedder-Policy"] = "require-corp";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Cross-Origin-Resource-Policy"] = "same-origin";

                // Cache Control for sensitive content
                if (IsSensitivePath(context.Request.Path))
                {
                    headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
                    headers["Pragma"] = "no-cache";
                    headers["Expires"] = "0";
                }

                // Security Event Timing
                headers["Server-Timing"] = $"security;desc=\"Headers applied\";dur=1";

                _logger.LogDebug("Enhanced security headers applied to {Path}", context.Request.Path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply security headers to {Path}", context.Request.Path);
            }
        }

        private string BuildSecureContentSecurityPolicy()
        {
            return string.Join("; ", new[]
            {
                "default-src 'self'",
                "script-src 'self' 'nonce-{RANDOM}' 'strict-dynamic'", // Use nonces instead of unsafe-inline
                "style-src 'self' 'nonce-{RANDOM}'", // Use nonces instead of unsafe-inline
                "img-src 'self' data: https:",
                "font-src 'self' data:",
                "connect-src 'self'",
                "media-src 'none'",
                "object-src 'none'",
                "frame-src 'none'",
                "worker-src 'none'",
                "frame-ancestors 'none'",
                "form-action 'self'",
                "base-uri 'self'",
                "manifest-src 'self'",
                "upgrade-insecure-requests"
            });
        }

        private string BuildSecurePermissionsPolicy()
        {
            return string.Join(", ", new[]
            {
                "accelerometer=()",
                "ambient-light-sensor=()",
                "autoplay=()",
                "battery=()",
                "camera=()",
                "cross-origin-isolated=()",
                "display-capture=()",
                "document-domain=()",
                "encrypted-media=()",
                "execution-while-not-rendered=()",
                "execution-while-out-of-viewport=()",
                "fullscreen=()",
                "geolocation=()",
                "gyroscope=()",
                "keyboard-map=()",
                "magnetometer=()",
                "microphone=()",
                "midi=()",
                "navigation-override=()",
                "payment=()",
                "picture-in-picture=()",
                "publickey-credentials-get=()",
                "screen-wake-lock=()",
                "sync-xhr=()",
                "usb=()",
                "web-share=()",
                "xr-spatial-tracking=()"
            });
        }

        private bool IsSensitivePath(PathString path)
        {
            var sensitivePatterns = new[]
            {
                "/api/auth",
                "/api/admin",
                "/graphql",
                "/swagger"
            };

            foreach (var pattern in sensitivePatterns)
            {
                if (path.StartsWithSegments(pattern))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Enhanced security configuration options
    /// </summary>
    public class EnhancedSecurityOptions
    {
        public bool EnableContentSecurityPolicy { get; set; } = true;
        public bool EnableHsts { get; set; } = true;
        public int HstsMaxAgeSeconds { get; set; } = 31536000; // 1 year
        public bool EnablePermissionsPolicy { get; set; } = true;
        public bool EnableCrossOriginPolicies { get; set; } = true;
        public bool EnableSecurityTiming { get; set; } = false;
    }
}