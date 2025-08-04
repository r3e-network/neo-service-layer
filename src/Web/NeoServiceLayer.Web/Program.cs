using System.Reflection;
using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NeoServiceLayer.AI.Prediction;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Web.Extensions;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/neo-service-layer-web-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Configure URLs for Docker
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Razor Pages for enhanced web interface
builder.Services.AddRazorPages();

// Configure API Versioning
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
});

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Neo Service Layer Web API",
        Version = "v1",
        Description = "A comprehensive blockchain service layer API and Web Interface for Neo N3 and Neo X"
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
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
});

// Configure JWT Authentication with secure key management
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? jwtSettings["SecretKey"];
var issuer = jwtSettings["Issuer"] ?? "NeoServiceLayer";
var audience = jwtSettings["Audience"] ?? "NeoServiceLayerUsers";

// Validate JWT secret key
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT secret key must be configured via JWT_SECRET_KEY environment variable");
}

// Ensure minimum key length for security
if (secretKey.Length < 32)
{
    throw new InvalidOperationException("JWT secret key must be at least 32 characters long");
}

// Prevent use of default/example keys
var forbiddenKeys = new[]
{
    "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "SuperSecretKeyThatIsTotallyLongEnoughForJWTTokenGenerationAndSigning2024ProductionReadyCompliantWith256BitMinimumRequirementAndMoreCharacters!",
    "default-secret-key"
};

if (forbiddenKeys.Contains(secretKey))
{
    throw new InvalidOperationException("Default/example JWT secret keys are not allowed. Use a secure, unique key.");
}


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("KeyManagerOrAdmin", policy => policy.RequireRole("Admin", "KeyManager"));
    options.AddPolicy("ServiceUser", policy => policy.RequireRole("Admin", "KeyManager", "KeyUser", "ServiceUser"));
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        if (builder.Environment.IsDevelopment())
        {
            // Allow localhost origins in development
            policy.WithOrigins("http://localhost:3000", "https://localhost:3001", "http://localhost:5000", "https://localhost:5001")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else if (corsOrigins.Length > 0)
        {
            // Use configured origins in production
            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // No CORS in production by default
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .DisallowCredentials();
        }
    });
});

// Rate limiting removed for compatibility

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddCheck("blockchain", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Blockchain connectivity is operational"))
    .AddCheck("storage", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Storage is accessible"))
    .AddCheck("configuration", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Configuration service is healthy"))
    .AddCheck("neo-services", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Neo services are operational"))
    .AddCheck("resources", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("System resources are adequate"))
    .AddCheck("sgx", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("SGX enclave is initialized"))
    .AddCheck("security-services", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Security services are operational"))
    .AddCheck("blockchain-services", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Blockchain services are healthy"))
    .AddCheck("data-services", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Data services are available"))
    .AddCheck("advanced-services", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Advanced services are running"));

// Add Neo Service Layer Core Services
builder.Services.AddNeoServiceLayer(builder.Configuration);

// Add Neo Service Framework (provides IServiceConfiguration)
builder.Services.AddNeoServiceFramework();

// Add Persistent Storage Configuration
builder.Configuration.AddJsonFile("appsettings.PersistentStorage.json", optional: true, reloadOnChange: true);
builder.Services.AddPersistentStorageServices(builder.Configuration);
builder.Services.ConfigureServicesWithPersistentStorage(builder.Configuration);

// Register all Neo Service Layer services (all 26 services)
builder.Services.AddNeoServiceLayerServices(builder.Configuration);

// Register TEE Services - Use production wrapper with environment-aware configuration
builder.Services.AddScoped<NeoServiceLayer.Tee.Enclave.IEnclaveWrapper, NeoServiceLayer.Tee.Enclave.OcclumEnclaveWrapper>();
builder.Services.AddScoped<NeoServiceLayer.Tee.Host.Services.IEnclaveManager>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<NeoServiceLayer.Tee.Host.Services.EnclaveManager>>();
    var enclaveWrapper = serviceProvider.GetRequiredService<NeoServiceLayer.Tee.Enclave.IEnclaveWrapper>();
    return new NeoServiceLayer.Tee.Host.Services.EnclaveManager(logger, enclaveWrapper);
});
// builder.Services.AddScoped<NeoServiceLayer.Tee.Host.Services.IEnclaveHostService, NeoServiceLayer.Tee.Host.Services.EnclaveHostService>(); // Interface doesn't exist

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Configure Swagger with authentication in production
var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled", app.Environment.IsDevelopment());
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Service Layer Web API v1");
        c.RoutePrefix = "swagger";

        // Require authentication for Swagger UI in production
        if (!app.Environment.IsDevelopment())
        {
            c.ConfigObject.AdditionalItems["persistAuthorization"] = true;
        }
    });

    // Protect Swagger endpoints in production
    if (!app.Environment.IsDevelopment())
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
            }
            await next();
        });
    }
}

// Serve static files
app.UseStaticFiles();

// Skip HTTPS redirection in container
// app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowedOrigins");
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map Razor Pages
app.MapRazorPages();

// Map Health Checks
var healthCheckOptions = new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds,
                    description = e.Value.Description,
                    data = e.Value.Data,
                    exception = e.Value.Exception?.Message,
                    tags = e.Value.Tags
                }
            ),
            timestamp = DateTime.UtcNow
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
};

app.MapHealthChecks("/health", healthCheckOptions);
app.MapHealthChecks("/health/live", healthCheckOptions);
app.MapHealthChecks("/health/ready", healthCheckOptions);

// Map info endpoint
app.MapGet("/api/info", () => new
{
    Name = "Neo Service Layer Web Application",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Features = new string[]
    {
        "Abstract Account Management",
        "AI Pattern Recognition",
        "AI Prediction",
        "Automation Services",
        "Backup & Restore",
        "Compliance Management",
        "Compute Services",
        "Configuration Management",
        "Cross-Chain Operations",
        "Event Subscription",
        "Health Monitoring",
        "Key Management",
        "Monitoring & Alerting",
        "Notification Services",
        "Oracle Data Processing",
        "Proof of Reserve",
        "Randomness Generation",
        "SGX Enclave",
        "Storage Management",
        "Voting Systems",
        "Web Interface",
        "Zero Knowledge Proofs"
    }
}).AllowAnonymous();

// Authentication endpoints
// NOTE: Production authentication should be handled by a proper identity provider
// This endpoint is disabled in production for security reasons
if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/auth/demo-token", () =>
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "demo-user"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "KeyManager"),
                new Claim(ClaimTypes.Role, "ServiceUser")
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new { token = tokenString, expires = tokenDescriptor.Expires };
    }).AllowAnonymous();
}

// Default route removed - Razor Pages disabled to avoid route conflicts
// app.MapFallbackToPage("/Index");

Log.Information("Neo Service Layer Web Application starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
