using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NeoServiceLayer.Api.Middleware;

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

        // Remove sensitive headers
        RemoveSensitiveHeaders(context);

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Strict Transport Security (HSTS)
        if (_options.EnableHsts)
        {
            headers["Strict-Transport-Security"] = $"max-age={_options.HstsMaxAge}; includeSubDomains; preload";
        }

        // Content Security Policy (CSP)
        if (!string.IsNullOrWhiteSpace(_options.ContentSecurityPolicy))
        {
            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
        }

        // X-Content-Type-Options
        headers["X-Content-Type-Options"] = "nosniff";

        // X-Frame-Options
        headers["X-Frame-Options"] = _options.XFrameOptions;

        // X-XSS-Protection
        headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer-Policy
        headers["Referrer-Policy"] = _options.ReferrerPolicy;

        // Permissions-Policy (formerly Feature-Policy)
        if (!string.IsNullOrWhiteSpace(_options.PermissionsPolicy))
        {
            headers["Permissions-Policy"] = _options.PermissionsPolicy;
        }

        // Cross-Origin-Embedder-Policy
        if (!string.IsNullOrWhiteSpace(_options.CrossOriginEmbedderPolicy))
        {
            headers["Cross-Origin-Embedder-Policy"] = _options.CrossOriginEmbedderPolicy;
        }

        // Cross-Origin-Opener-Policy
        if (!string.IsNullOrWhiteSpace(_options.CrossOriginOpenerPolicy))
        {
            headers["Cross-Origin-Opener-Policy"] = _options.CrossOriginOpenerPolicy;
        }

        // Cross-Origin-Resource-Policy
        if (!string.IsNullOrWhiteSpace(_options.CrossOriginResourcePolicy))
        {
            headers["Cross-Origin-Resource-Policy"] = _options.CrossOriginResourcePolicy;
        }

        // Custom headers
        foreach (var customHeader in _options.CustomHeaders)
        {
            headers[customHeader.Key] = customHeader.Value;
        }

        _logger.LogDebug("Security headers added to response");
    }

    private void RemoveSensitiveHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Remove server header
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
        headers.Remove("X-AspNetCore-Version");

        // Remove any other sensitive headers
        foreach (var headerToRemove in _options.HeadersToRemove)
        {
            headers.Remove(headerToRemove);
        }
    }
}

public class SecurityHeadersOptions
{
    // HSTS
    public bool EnableHsts { get; set; } = true;
    public int HstsMaxAge { get; set; } = 31536000; // 1 year

    // CSP - Production-ready policy
    public string ContentSecurityPolicy { get; set; } = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
        "img-src 'self' data: https: blob:; " +
        "font-src 'self' data: https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
        "connect-src 'self' https://api.neo.org wss://api.neo.org https://*.neo.org; " +
        "media-src 'self'; " +
        "object-src 'none'; " +
        "frame-src 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'; " +
        "upgrade-insecure-requests;";

    // X-Frame-Options
    public string XFrameOptions { get; set; } = "DENY";

    // Referrer-Policy
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    // Permissions-Policy
    public string PermissionsPolicy { get; set; } = 
        "accelerometer=(), " +
        "camera=(), " +
        "geolocation=(), " +
        "gyroscope=(), " +
        "magnetometer=(), " +
        "microphone=(), " +
        "payment=(), " +
        "usb=()";

    // COOP
    public string CrossOriginOpenerPolicy { get; set; } = "same-origin";

    // COEP
    public string CrossOriginEmbedderPolicy { get; set; } = "require-corp";

    // CORP
    public string CrossOriginResourcePolicy { get; set; } = "same-origin";

    // Headers to remove
    public string[] HeadersToRemove { get; set; } = new[]
    {
        "Server",
        "X-Powered-By",
        "X-AspNet-Version",
        "X-AspNetCore-Version"
    };

    // Custom headers
    public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>();
}

// Extension method to easily add security headers to the pipeline
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }

    public static IServiceCollection AddSecurityHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SecurityHeadersOptions>(configuration.GetSection("SecurityHeaders"));
        return services;
    }

    public static IApplicationBuilder UseProductionSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            // Production-specific security headers
            var headers = context.Response.Headers;

            // Strict Transport Security - 2 years, include subdomains, preload
            headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";

            // Enhanced CSP for production
            headers["Content-Security-Policy"] = 
                "default-src 'none'; " +
                "script-src 'self'; " +
                "style-src 'self'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self'; " +
                "connect-src 'self' https://api.neo.org wss://api.neo.org; " +
                "media-src 'none'; " +
                "object-src 'none'; " +
                "frame-src 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'; " +
                "frame-ancestors 'none'; " +
                "block-all-mixed-content; " +
                "upgrade-insecure-requests;";

            // Report-Only CSP for monitoring
            headers["Content-Security-Policy-Report-Only"] = 
                headers["Content-Security-Policy"] + " report-uri /api/csp-report;";

            // Additional security headers
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["X-XSS-Protection"] = "0"; // Disabled in modern browsers
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = 
                "accelerometer=(), camera=(), geolocation=(), gyroscope=(), " +
                "magnetometer=(), microphone=(), payment=(), usb=()";
            headers["Cross-Origin-Embedder-Policy"] = "require-corp";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";

            // Remove sensitive headers
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            // Add security nonce for inline scripts if needed
            var nonce = GenerateNonce();
            context.Items["csp-nonce"] = nonce;

            await next();
        });

        return app;
    }

    private static string GenerateNonce()
    {
        var random = new byte[16];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(random);
        }
        return Convert.ToBase64String(random);
    }
}

// CSP Report endpoint model
public class CspReport
{
    public CspReportData Report { get; set; }
}

public class CspReportData
{
    public string DocumentUri { get; set; }
    public string Referrer { get; set; }
    public string ViolatedDirective { get; set; }
    public string EffectiveDirective { get; set; }
    public string OriginalPolicy { get; set; }
    public string BlockedUri { get; set; }
    public int StatusCode { get; set; }
    public string SourceFile { get; set; }
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
}