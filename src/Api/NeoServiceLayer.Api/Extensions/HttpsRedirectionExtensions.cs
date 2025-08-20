using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NeoServiceLayer.Api.Extensions;

/// <summary>
/// Extension methods for configuring HTTPS redirection and security headers
/// </summary>
public static class HttpsRedirectionExtensions
{
    /// <summary>
    /// Configure HTTPS redirection and HSTS for production security
    /// </summary>
    public static IServiceCollection AddHttpsSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure HTTPS redirection
        services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            options.HttpsPort = configuration.GetValue<int?>("Https:Port") ?? 443;
        });

        // Configure HSTS (HTTP Strict Transport Security)
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
            options.ExcludedHosts.Add("localhost");
            options.ExcludedHosts.Add("127.0.0.1");
            options.ExcludedHosts.Add("[::1]");
        });

        return services;
    }

    /// <summary>
    /// Configure security headers middleware
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
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
                "font-src 'self'; " +
                "connect-src 'self' https://api.neo.org wss://; " +
                "frame-ancestors 'none';");
            
            // Remove server header
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");
            
            await next();
        });
    }

    /// <summary>
    /// Configure Kestrel for production SSL/TLS
    /// </summary>
    public static IWebHostBuilder ConfigureKestrelSecurity(this IWebHostBuilder builder)
    {
        return builder.ConfigureKestrel((context, options) =>
        {
            options.AddServerHeader = false;
            
            // Configure HTTPS
            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | 
                                           System.Security.Authentication.SslProtocols.Tls13;
            });

            // Configure ports
            var httpsPort = context.Configuration.GetValue<int?>("Https:Port") ?? 443;
            var httpPort = context.Configuration.GetValue<int?>("Http:Port") ?? 80;

            options.ListenAnyIP(httpPort);
            options.ListenAnyIP(httpsPort, listenOptions =>
            {
                var certPath = context.Configuration["Https:Certificate:Path"];
                var certPassword = context.Configuration["Https:Certificate:Password"];
                
                if (!string.IsNullOrEmpty(certPath))
                {
                    listenOptions.UseHttps(certPath, certPassword);
                }
                else
                {
                    // Use development certificate in non-production
                    if (!context.HostingEnvironment.IsProduction())
                    {
                        listenOptions.UseHttps();
                    }
                }
            });
        });
    }
}