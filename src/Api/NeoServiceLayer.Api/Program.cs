using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NeoServiceLayer.AI.PatternRecognition;
using NeoServiceLayer.AI.Prediction;
using NeoServiceLayer.Api.Extensions;
using NeoServiceLayer.Api.Filters;
using NeoServiceLayer.Api.HealthChecks;
using NeoServiceLayer.Api.Middleware;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Configuration;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Tee.Host.Services;
using Serilog;


// Create the builder
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console());

// Add services to the container
builder.Services.AddControllers();
    // Add global exception filter

// Configure API Versioning

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Neo Service Layer API",
        Version = "v1",
        Description = "Comprehensive blockchain service layer with SGX/TEE support",
        Contact = new OpenApiContact
        {
            Name = "Neo Service Layer Team",
            Email = "support@neo-service.io"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Group operations by tags
    c.TagActionsBy(api => new[] { api.GroupName ?? "General" });

    // Order tags alphabetically
    c.OrderActionsBy(api => $"{api.GroupName}_{api.HttpMethod}_{api.RelativePath}");

    // Custom schema IDs to avoid conflicts
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

// Configure JWT Authentication with secure key management

// SECURITY: JWT secret MUST come from environment variable only - no config fallback

// In test environment, use a fixed key for consistency
        // Only for development/testing - never in production
    // Validate JWT secret key for production - STRICT enforcement

    // Ensure minimum key length for security

// Prevent use of default/example keys



// Configure Authorization

// Configure CORS
        // Get endpoints configuration

            // Allow localhost origins in development only
            // No CORS in production without configuration

// Configure Rate Limiting

// Add Comprehensive Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddCheck<ConfigurationHealthCheck>("configuration", tags: new[] { "ready", "configuration" })
    .AddCheck<NeoServicesHealthCheck>("neo-services", tags: new[] { "ready", "services" })
    .AddCheck<ResourceHealthCheck>("resources", tags: new[] { "ready", "resources" });
        // Check SGX enclave status

// Add secure configuration services

// Add Neo Service Layer with improved architecture
builder.Services.AddNeoServiceLayer(builder.Configuration, options =>
{
    options.EnableServiceDiscovery = true;
    options.EnableInterServiceCommunication = true;
    options.EnableOrchestration = true;
    options.EnableHealthChecks = true;
    options.EnableMetrics = true;
    options.EnableTracing = true;
    options.AutoRegisterServices = true;
    
    // Specify assemblies to scan for services
    options.ServiceAssemblies = new[]
    {
        typeof(Program).Assembly,
        typeof(NeoServiceLayer.Services.Authentication.AuthenticationService).Assembly,
        typeof(NeoServiceLayer.Services.KeyManagement.KeyManagementService).Assembly,
        // Add other service assemblies as needed
    };
});

// Configure service communication
builder.Services.ConfigureServiceCommunication(config =>
{
    config.Protocol = NeoServiceLayer.Core.ServiceArchitecture.CommunicationProtocol.Http;
    config.DefaultTimeout = TimeSpan.FromSeconds(30);
    config.MaxRetries = 3;
    config.EnableCircuitBreaker = true;
    config.EnableCompression = true;
    config.EnableEncryption = true;
});

// Add service orchestration
builder.Services.AddServiceOrchestration(config =>
{
    config.AutoStart = true;
    config.EnableDependencyResolution = true;
    config.EnableHealthMonitoring = true;
    config.HealthCheckInterval = TimeSpan.FromSeconds(30);
    config.StartupStrategy = NeoServiceLayer.Core.ServiceArchitecture.StartupStrategy.DependencyOrder;
    config.ShutdownStrategy = NeoServiceLayer.Core.ServiceArchitecture.ShutdownStrategy.Graceful;
});

// Add service versioning
builder.Services.AddServiceVersioning(config =>
{
    config.Strategy = NeoServiceLayer.Core.ServiceArchitecture.VersioningStrategy.Semantic;
    config.EnableBackwardCompatibility = true;
    config.MaxSupportedVersions = 3;
});

// Add specific service groups
builder.Services.AddNeoServiceGroup("SecurityServices", group =>
{
    group.AddService<NeoServiceLayer.Services.Authentication.IAuthenticationService, 
                     NeoServiceLayer.Services.Authentication.AuthenticationService>()
         .AddService<NeoServiceLayer.Services.NetworkSecurity.INetworkSecurityService,
                     NeoServiceLayer.Services.NetworkSecurity.NetworkSecurityService>()
         .WithLifetime(ServiceLifetime.Singleton)
         .EnableHealthChecks()
         .EnableMetrics();
});

builder.Services.AddNeoServiceGroup("DataServices", group =>
{
    group.AddService<NeoServiceLayer.Services.EnclaveStorage.IEnclaveStorageService,
                     NeoServiceLayer.Services.EnclaveStorage.EnclaveStorageService>()
         .AddService<NeoServiceLayer.Services.Backup.IBackupService,
                     NeoServiceLayer.Services.Backup.BackupService>()
         .WithLifetime(ServiceLifetime.Scoped)
         .EnableHealthChecks()
         .EnableMetrics();
});

// Configure service endpoints

    // Validate endpoints in production



// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Service Layer API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at app's root
        c.DocumentTitle = "Neo Service Layer API Documentation";
        c.DisplayRequestDuration();
        c.EnableFilter();
        c.EnableDeepLinking();
        c.ShowExtensions();
        c.ShowCommonExtensions();
    });
}

// Add global error handling middleware (must be early in pipeline)
app.UseMiddleware<NeoServiceLayer.Api.Middleware.ErrorHandlingMiddleware>();

// Add security headers middleware (early in pipeline)
app.UseMiddleware<NeoServiceLayer.Api.Middleware.SecurityHeadersMiddleware>();

// Add IP security middleware (before rate limiting)
app.UseMiddleware<NeoServiceLayer.Api.Middleware.IpSecurityMiddleware>();

// Add audit logging middleware (before authentication)
app.UseMiddleware<NeoServiceLayer.Api.Middleware.AuditLoggingMiddleware>();

// Add rate limiting middleware
app.UseMiddleware<NeoServiceLayer.Api.Middleware.RateLimitingMiddleware>();

app.UseHttpsRedirection();

// Add JWT validation middleware (replaces UseAuthentication for custom handling)
app.UseMiddleware<NeoServiceLayer.Api.Middleware.JwtValidationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


// Map Health Checks with detailed responses
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                component = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Map simple health check for load balancers
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Map liveness probe
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// Map info endpoint
app.MapGet("/info", () => new
{
    service = "Neo Service Layer API",
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
});



// Make Program accessible for integration tests
/// <summary>
/// The main program class for the Neo Service Layer API.
/// </summary>

app.Run();

public partial class Program 
{ 
    // Expose for integration tests
}
