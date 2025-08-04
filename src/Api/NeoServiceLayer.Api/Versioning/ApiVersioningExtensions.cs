using System;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Asp.Versioning.Conventions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NeoServiceLayer.Api.Versioning;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiVersioningOptions>(configuration.GetSection("ApiVersioning"));

        services.AddApiVersioning(options =>
        {
            // Report API versions in response headers
            options.ReportApiVersions = true;

            // Use default version when not specified
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);

            // Read version from multiple sources
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new MediaTypeApiVersionReader("version"),
                new QueryStringApiVersionReader("api-version")
            );
        })
        .AddMvc(options =>
        {
            // Apply versioning conventions
            options.Conventions.Controller<VersionedControllerBase>()
                .HasApiVersion(1, 0)
                .HasApiVersion(2, 0)
                .HasDeprecatedApiVersion(0, 9);
        })
        .AddApiExplorer(options =>
        {
            // Format: 'v'major[.minor][-status]
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersionParameterDescription = "API Version";
        });

        // Add version negotiation - commented out due to API changes
        // services.AddSingleton<IApiVersionNegotiator, CustomApiVersionNegotiator>();

        // Add deprecation handler
        services.AddTransient<IStartupFilter, ApiDeprecationStartupFilter>();

        return services;
    }

    public static IApplicationBuilder UseApiVersioning(this IApplicationBuilder app)
    {
        // Add middleware for version validation
        app.UseMiddleware<ApiVersionValidationMiddleware>();

        // Add middleware for deprecation warnings
        app.UseMiddleware<ApiDeprecationMiddleware>();

        return app;
    }
}

// Custom version negotiator - commented out due to API changes
/*
public class CustomApiVersionNegotiator : IApiVersionNegotiator
{
    private readonly ApiVersioningOptions _options;

    public CustomApiVersionNegotiator(IOptions<ApiVersioningOptions> options)
    {
        _options = options.Value;
    }

    public ApiVersion SelectVersion(HttpContext context, ApiVersionModel model)
    {
        // Try to get requested version
        var requestedVersion = GetRequestedVersion(context);

        if (requestedVersion != null)
        {
            // Check if requested version is supported
            if (model.SupportedApiVersions.Contains(requestedVersion))
            {
                return requestedVersion;
            }

            // Check if requested version is deprecated but still supported
            if (model.DeprecatedApiVersions.Contains(requestedVersion))
            {
                context.Items["ApiVersionDeprecated"] = true;
                return requestedVersion;
            }
        }

        // Return the latest supported version
        return model.SupportedApiVersions.OrderByDescending(v => v).FirstOrDefault() 
            ?? _options.DefaultApiVersion;
    }

    private ApiVersion GetRequestedVersion(HttpContext context)
    {
        // Check URL segment
        if (context.GetRouteData().Values.TryGetValue("version", out var versionValue))
        {
            if (ApiVersion.TryParse(versionValue.ToString(), out var version))
            {
                return version;
            }
        }

        // Check header
        if (context.Request.Headers.TryGetValue("X-Api-Version", out var headerValue))
        {
            if (ApiVersion.TryParse(headerValue.ToString(), out var version))
            {
                return version;
            }
        }

        // Check query string
        if (context.Request.Query.TryGetValue("api-version", out var queryValue))
        {
            if (ApiVersion.TryParse(queryValue.ToString(), out var version))
            {
                return version;
            }
        }

        return null;
    }
}
*/

// Version validation middleware
public class ApiVersionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiVersionValidationMiddleware> _logger;

    public ApiVersionValidationMiddleware(RequestDelegate next, ILogger<ApiVersionValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip validation for non-API endpoints
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Check if version is specified
        var apiVersion = context.GetRequestedApiVersion();

        if (apiVersion == null)
        {
            _logger.LogWarning("API request without version specification: {Path}", context.Request.Path);
        }

        await _next(context);
    }
}

// Deprecation middleware
public class ApiDeprecationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiDeprecationMiddleware> _logger;
    private readonly ApiDeprecationOptions _options;

    public ApiDeprecationMiddleware(
        RequestDelegate next,
        ILogger<ApiDeprecationMiddleware> logger,
        IOptions<ApiDeprecationOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Check if the API version is deprecated
        if (context.Items.TryGetValue("ApiVersionDeprecated", out var deprecated) && (bool)deprecated)
        {
            var apiVersion = context.GetRequestedApiVersion();
            var deprecationInfo = _options.DeprecationInfos.FirstOrDefault(d => d.Version == apiVersion);

            if (deprecationInfo != null)
            {
                // Add deprecation headers
                context.Response.Headers["X-Api-Deprecated"] = "true";
                context.Response.Headers["X-Api-Deprecation-Date"] = deprecationInfo.DeprecationDate.ToString("O");
                context.Response.Headers["X-Api-Sunset-Date"] = deprecationInfo.SunsetDate.ToString("O");
                context.Response.Headers["X-Api-Deprecation-Info"] = deprecationInfo.Message;

                // Add link to migration guide
                if (!string.IsNullOrEmpty(deprecationInfo.MigrationGuideUrl))
                {
                    context.Response.Headers["Link"] = $"<{deprecationInfo.MigrationGuideUrl}>; rel=\"deprecation\"";
                }

                _logger.LogWarning(
                    "Deprecated API version {Version} was called. Sunset date: {SunsetDate}",
                    apiVersion,
                    deprecationInfo.SunsetDate);
            }
        }
    }
}

// Startup filter for deprecation
public class ApiDeprecationStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            builder.UseMiddleware<ApiDeprecationMiddleware>();
            next(builder);
        };
    }
}

// Base controller for versioned APIs
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class VersionedControllerBase : ControllerBase
{
    protected ApiVersion CurrentVersion => HttpContext.GetRequestedApiVersion() ?? new ApiVersion(1, 0);

    protected bool IsVersionDeprecated => HttpContext.Items.ContainsKey("ApiVersionDeprecated");

    protected IActionResult VersionAwareResponse<T>(T data)
    {
        Response.Headers["X-Api-Version"] = CurrentVersion.ToString();

        if (IsVersionDeprecated)
        {
            Response.Headers["Warning"] = "299 - \"This API version is deprecated\"";
        }

        return Ok(new VersionedResponse<T>
        {
            Data = data,
            Version = CurrentVersion.ToString(),
            Deprecated = IsVersionDeprecated
        });
    }
}

// Versioned response wrapper
public class VersionedResponse<T>
{
    public T Data { get; set; }
    public string Version { get; set; }
    public bool Deprecated { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

// Configuration classes
public class ApiVersioningOptions
{
    public bool ReportApiVersions { get; set; } = true;
    public bool AssumeDefaultVersionWhenUnspecified { get; set; } = true;
    public string DefaultApiVersion { get; set; } = "1.0";
    public List<string> SupportedVersions { get; set; } = new() { "1.0", "2.0" };
    public List<string> DeprecatedVersions { get; set; } = new() { "0.9" };
}

public class ApiDeprecationOptions
{
    public List<ApiDeprecationInfo> DeprecationInfos { get; set; } = new();
}

public class ApiDeprecationInfo
{
    public ApiVersion Version { get; set; }
    public DateTime DeprecationDate { get; set; }
    public DateTime SunsetDate { get; set; }
    public string Message { get; set; }
    public string MigrationGuideUrl { get; set; }
}

// Extension methods for version handling
public static class ApiVersionExtensions
{
    public static bool IsVersionSupported(this HttpContext context, ApiVersion version)
    {
        var feature = context.Features.Get<IApiVersioningFeature>();
        return feature?.RequestedApiVersion == version;
    }

    public static void SetApiVersionError(this HttpContext context, string error)
    {
        context.Items["ApiVersionError"] = error;
    }

    public static string GetApiVersionError(this HttpContext context)
    {
        return context.Items.TryGetValue("ApiVersionError", out var error)
            ? error?.ToString()
            : null;
    }
}
