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

// Configure Kestrel for production SSL/TLS
builder.WebHost.ConfigureKestrelSecurity();

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
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (string.IsNullOrEmpty(jwtSecret))
{
    // JWT_SECRET_KEY is required in ALL environments for security
    throw new InvalidOperationException("JWT_SECRET_KEY environment variable is required in all environments. " +
        "Please set a secure JWT secret key (minimum 64 characters) in your environment variables.");
}

// Validate JWT secret key - STRICT enforcement for all environments
if (jwtSecret.Length < 64)
{
    throw new InvalidOperationException("JWT secret key must be at least 64 characters long for production-grade security");
}

// Prevent use of default/example keys
var forbiddenKeys = new[]
{
    "your-secret-key-here",
    "development-jwt-secret",
    "test-secret",
    "default-key",
    "example-key",
    "demo-key"
};

if (forbiddenKeys.Any(forbidden => jwtSecret.Contains(forbidden, StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException("Cannot use example or default JWT secret keys in production");
}

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NeoServiceLayer",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NeoServiceLayer",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(5),
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
        
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false; // Don't store tokens in AuthenticationProperties
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // Use structured logging instead of console output
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT Authentication failed: {ErrorMessage}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // Use structured logging for successful token validation
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogDebug("JWT Token validated for user: {UserName}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("RequireUser", policy => 
        policy.RequireRole("User", "Admin"));
    
    options.AddPolicy("RequireEnclave", policy => 
        policy.RequireClaim("enclave_access", "true"));
        
    options.AddPolicy("RequireHighSecurity", policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireClaim("security_level", "high");
        policy.RequireAuthenticatedUser();
    });
});

// Configure CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Allow localhost origins in development only
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:8080")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Production CORS - strict configuration required
            if (corsOrigins.Length == 0)
            {
                throw new InvalidOperationException("CORS origins must be configured for production deployment");
            }
            
            policy.WithOrigins(corsOrigins)
                  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization", "X-Correlation-ID")
                  .AllowCredentials()
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(30));
        }
    });
});

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.AddFixedWindowLimiter("GlobalApi", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = builder.Environment.IsDevelopment() ? 1000 : 100;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 50;
    });
    
    options.AddFixedWindowLimiter("AuthApi", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 10; // Stricter for auth endpoints
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });
});

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
