using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace NeoServiceLayer.Common.Extensions;

/// <summary>
/// Common service collection extensions for consistent service registration across all microservices
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds common services required by all Neo Service Layer microservices
    /// </summary>
    public static IServiceCollection AddNeoServiceCommon(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly? executingAssembly = null)
    {
        // Add configuration validation
        services.AddOptions()
            .AddOptionsSnapshot<CommonServiceOptions>()
            .Bind(configuration.GetSection("NeoService"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Add structured logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders()
                .AddConfiguration(configuration.GetSection("Logging"))
                .AddConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
                })
                .AddDebug();
        });

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<CommonHealthCheck>("common");

        // Add memory cache by default
        services.AddMemoryCache();

        // Add HTTP client factory
        services.AddHttpClient();

        // Add correlation ID for distributed tracing
        services.AddSingleton<ICorrelationIdService, CorrelationIdService>();
        services.AddTransient<CorrelationIdMiddleware>();

        // Add exception handling
        services.AddTransient<GlobalExceptionHandlingMiddleware>();

        // Add request/response logging
        services.AddTransient<RequestResponseLoggingMiddleware>();

        // Add performance monitoring
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();

        // Register assembly for reflection-based operations
        if (executingAssembly != null)
        {
            services.AddSingleton(executingAssembly);
        }

        return services;
    }

    /// <summary>
    /// Adds database context with common configurations
    /// </summary>
    public static IServiceCollection AddNeoServiceDatabase<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? connectionStringName = null)
        where TContext : class
    {
        var connectionString = configuration.GetConnectionString(
            connectionStringName ?? "DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionStringName ?? "DefaultConnection"}' not found in configuration");
        }

        services.AddDbContext<TContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });

            // Enable sensitive data logging only in development
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            options.UseSnakeCaseNamingConvention();
            options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
        });

        return services;
    }

    /// <summary>
    /// Adds authentication and authorization with common policies
    /// </summary>
    public static IServiceCollection AddNeoServiceAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Authentication:Jwt");
        var secretKey = jwtSection["SecretKey"] ?? 
            throw new InvalidOperationException("JWT SecretKey not configured");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerHandler>>();
                        logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerHandler>>();
                        logger.LogDebug("JWT token validated for user: {UserId}", 
                            context.Principal?.Identity?.Name);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            // Common authorization policies
            options.AddPolicy("RequireSystemAdmin", policy =>
                policy.RequireRole("SystemAdmin"));
            
            options.AddPolicy("RequireServiceAccess", policy =>
                policy.RequireRole("SystemAdmin", "ServiceUser", "ServiceAdmin"));

            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }

    /// <summary>
    /// Adds OpenTelemetry with common configuration
    /// </summary>
    public static IServiceCollection AddNeoServiceTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .AddSource(serviceName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.client_ip", GetClientIpAddress(request));
                            activity.SetTag("http.user_agent", request.Headers.UserAgent.ToString());
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response.size", response.ContentLength ?? 0);
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                    })
                    .AddConsoleExporter();

                // Add Jaeger if configured
                var jaegerEndpoint = configuration["Telemetry:Jaeger:Endpoint"];
                if (!string.IsNullOrEmpty(jaegerEndpoint))
                {
                    builder.AddJaegerExporter(options =>
                    {
                        options.Endpoint = new Uri(jaegerEndpoint);
                    });
                }
            })
            .WithMetrics(builder =>
            {
                builder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddConsoleExporter();

                // Add Prometheus if configured
                var prometheusEnabled = configuration.GetValue<bool>("Telemetry:Prometheus:Enabled");
                if (prometheusEnabled)
                {
                    builder.AddPrometheusExporter();
                }
            });

        return services;
    }

    /// <summary>
    /// Adds CORS with common configuration
    /// </summary>
    public static IServiceCollection AddNeoServiceCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsSection = configuration.GetSection("Cors");
        var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var allowedMethods = corsSection.GetSection("AllowedMethods").Get<string[]>() 
            ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
        var allowedHeaders = corsSection.GetSection("AllowedHeaders").Get<string[]>()
            ?? new[] { "Content-Type", "Authorization", "X-Correlation-ID" };

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                if (allowedOrigins.Length > 0 && !allowedOrigins.Contains("*"))
                {
                    builder.WithOrigins(allowedOrigins);
                }
                else
                {
                    builder.AllowAnyOrigin();
                }

                builder
                    .WithMethods(allowedMethods)
                    .WithHeaders(allowedHeaders)
                    .AllowCredentials();

                if (allowedOrigins.Contains("*"))
                {
                    builder.SetIsOriginAllowed(_ => true);
                }
            });
        });

        return services;
    }

    private static string GetClientIpAddress(HttpRequest request)
    {
        return request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
            ?? request.Headers["X-Real-IP"].FirstOrDefault()
            ?? request.HttpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
    }
}