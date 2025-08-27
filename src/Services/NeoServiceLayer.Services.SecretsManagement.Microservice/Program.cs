using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Services;
using Neo.SecretsManagement.Service.BackgroundServices;
using Neo.SecretsManagement.Service.HealthChecks;
using NeoServiceLayer.Common.Extensions;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add common Neo Service Layer components
builder.Services.AddNeoServiceCommon(builder.Configuration, Assembly.GetExecutingAssembly());
builder.Services.AddNeoServiceDatabase<SecretsDbContext>(builder.Configuration);
builder.Services.AddNeoServiceAuthentication(builder.Configuration);
builder.Services.AddNeoServiceTelemetry(builder.Configuration, "neo-secrets-management");
builder.Services.AddNeoServiceCors(builder.Configuration);

// JSON serialization options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Redis for distributed coordination and caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "SecretsManagement";
});

// Core services
builder.Services.AddScoped<ISecretService, SecretService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
builder.Services.AddScoped<ISecretPolicyService, SecretPolicyService>();
builder.Services.AddScoped<IRotationService, RotationService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IHsmService, HsmService>();

// Background services
builder.Services.AddHostedService<SecretRotationService>();
builder.Services.AddHostedService<KeyRotationService>();
builder.Services.AddHostedService<SecretExpirationService>();
builder.Services.AddHostedService<AuditLogCleanupService>();
builder.Services.AddHostedService<BackupSchedulerService>();

// HTTP clients for external services
builder.Services.AddHttpClient("HSM", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "Neo-SecretsManagement/1.0");
});

builder.Services.AddHttpClient("VaultBackend", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Add("User-Agent", "Neo-SecretsManagement/1.0");
});

// Secrets Management specific authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SecretReader", policy => 
        policy.RequireRole("SecretReader", "SecretWriter", "SecretAdmin", "SystemAdmin"));
    
    options.AddPolicy("SecretWriter", policy => 
        policy.RequireRole("SecretWriter", "SecretAdmin", "SystemAdmin"));
    
    options.AddPolicy("SecretAdmin", policy => 
        policy.RequireRole("SecretAdmin", "SystemAdmin"));
});

// Additional health checks specific to Secrets Management service
builder.Services.AddHealthChecks()
    .AddCheck<SecretsVaultHealthCheck>("secrets-vault")
    .AddCheck<EncryptionHealthCheck>("encryption")
    .AddCheck<HsmHealthCheck>("hsm")
    .AddCheck<RotationHealthCheck>("rotation");

// Response compression for better performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// Configure pipeline with common middleware
app.UseMiddleware<NeoServiceLayer.Common.Middleware.CorrelationIdMiddleware>();
app.UseMiddleware<NeoServiceLayer.Common.Middleware.RequestResponseLoggingMiddleware>();
app.UseMiddleware<NeoServiceLayer.Common.Middleware.GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Secrets Management API v1");
        c.RoutePrefix = string.Empty;
    });
}

// Security headers for Secrets Management
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    await next();
});

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Routing - require authorization by default for Secrets Management
app.MapControllers()
   .RequireAuthorization();

// Health checks endpoints
app.MapHealthChecks("/api/v1/monitoring/health")
   .AllowAnonymous();

app.MapHealthChecks("/api/v1/monitoring/ready")
   .AllowAnonymous();

app.MapHealthChecks("/api/v1/monitoring/live")
   .AllowAnonymous();

// Metrics endpoint
app.UseOpenTelemetryPrometheusScrapingEndpoint("/api/v1/monitoring/metrics");

// Startup tasks
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SecretsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync();
        
        // Initialize default encryption keys if none exist
        var keyService = scope.ServiceProvider.GetRequiredService<IKeyManagementService>();
        await keyService.EnsureDefaultKeysAsync();
        
        logger.LogInformation("Database initialization completed");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

app.Logger.LogInformation("Neo Secrets Management Service started");

app.Run();