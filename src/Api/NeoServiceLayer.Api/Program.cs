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

    // Include XML comments

    // Add JWT Authentication to Swagger


    // Group operations by tags

    // Order tags alphabetically

    // Add operation filters for better documentation
    // c.EnableAnnotations(); // Method not available in this Swagger version

    // Custom schema IDs to avoid conflicts

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
    // .AddCheck<ConfigurationHealthCheck>("configuration", tags: new[] { "ready", "configuration" })
    // .AddCheck<NeoServicesHealthCheck>("neo-services", tags: new[] { "ready", "services" })
    // .AddCheck<ResourceHealthCheck>("resources", tags: new[] { "ready", "resources" })
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
    app.UseSwaggerUI();
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

// Map simple health check for load balancers

// Map liveness probe

// Map info endpoint



// Make Program accessible for integration tests
/// <summary>
/// The main program class for the Neo Service Layer API.
/// </summary>

app.Run();

public partial class Program 
{ 
    // Expose for integration tests
}
