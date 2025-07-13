using System.Reflection;
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
using NeoServiceLayer.Infrastructure;
// using NeoServiceLayer.Advanced.FairOrdering;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
// using NeoServiceLayer.Services.Randomness;
// using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Tee.Host.Services;
using Serilog;
// using NeoServiceLayer.Services.Health;
// using NeoServiceLayer.Services.Monitoring;
// using NeoServiceLayer.Services.Configuration;
// using NeoServiceLayer.Services.Notification;
// using NeoServiceLayer.Services.Backup;
// using NeoServiceLayer.Services.Voting;
// using NeoServiceLayer.Blockchain.Neo.N3;
// using NeoServiceLayer.Blockchain.Neo.X;
// using NeoServiceLayer.Advanced.FairOrdering;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/neo-service-layer-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers(options =>
{
    // Add global exception filter
    options.Filters.Add<GlobalExceptionFilter>();
    
    // Add global validation filter
    options.Filters.Add<ConditionalValidationFilter>();
})
.ConfigureApiBehaviorOptions(options =>
{
    // Disable automatic model state validation to use our custom validation
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddEndpointsApiExplorer();

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
        Title = "Neo Service Layer API",
        Version = "v1",
        Description = "A comprehensive blockchain service layer API for Neo N3 and Neo X",
        Contact = new OpenApiContact
        {
            Name = "Neo Service Layer Team",
            Email = "support@neoservicelayer.io"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
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
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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
    c.DocInclusionPredicate((docName, apiDesc) => true);
    c.TagActionsBy(api => new[] { api.GroupName ?? (api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller) ? controller?.ToString() ?? "Unknown" : "Unknown") });

    // Order tags alphabetically
    c.OrderActionsBy(api => $"{(api.ActionDescriptor.RouteValues.TryGetValue("controller", out var ctrl) ? ctrl?.ToString() ?? "Unknown" : "Unknown")}_{(api.ActionDescriptor.RouteValues.TryGetValue("action", out var act) ? act?.ToString() ?? "Unknown" : "Unknown")}");

    // Add operation filters for better documentation
    // c.EnableAnnotations(); // Method not available in this Swagger version

    // Custom schema IDs to avoid conflicts
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
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
        policy.WithOrigins("http://localhost:3000", "https://localhost:3001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ApiRateLimit", configure =>
    {
        configure.PermitLimit = 100;
        configure.Window = TimeSpan.FromMinutes(1);
        configure.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        configure.QueueLimit = 10;
    });
});

// Add Comprehensive Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is running"), tags: new[] { "ready", "live" })
    .AddCheck<BlockchainHealthCheck>("blockchain", tags: new[] { "ready", "blockchain" })
    .AddCheck<StorageHealthCheck>("storage", tags: new[] { "ready", "storage" })
    // .AddCheck<ConfigurationHealthCheck>("configuration", tags: new[] { "ready", "configuration" })
    // .AddCheck<NeoServicesHealthCheck>("neo-services", tags: new[] { "ready", "services" })
    // .AddCheck<ResourceHealthCheck>("resources", tags: new[] { "ready", "resources" })
    .AddCheck("sgx", () =>
    {
        // Check SGX enclave status
        var sgxMode = Environment.GetEnvironmentVariable("SGX_MODE") ?? "Unknown";
        var data = new Dictionary<string, object> { ["SGXMode"] = sgxMode };
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"SGX Mode: {sgxMode}", data);
    }, tags: new[] { "ready", "security" })
    .AddCheck<SecurityServicesHealthCheck>("security-services", tags: new[] { "ready", "security", "services" })
    .AddCheck<BlockchainServicesHealthCheck>("blockchain-services", tags: new[] { "ready", "blockchain", "services" })
    .AddCheck<DataServicesHealthCheck>("data-services", tags: new[] { "ready", "data", "services" })
    .AddCheck<AdvancedServicesHealthCheck>("advanced-services", tags: new[] { "ready", "advanced", "services" });

// Add Neo Service Layer
builder.Services.AddNeoServiceLayer(builder.Configuration);

// Add services based on deployment mode
var deploymentMode = builder.Configuration["DeploymentMode"] ?? "Monolithic";
if (deploymentMode == "Microservices")
{
    // Use microservices with service discovery
    // TODO: Re-enable once microservices extension is fixed
    // builder.Services.AddNeoServiceLayerMicroservices(builder.Configuration);
}
else
{
    // Use monolithic deployment
    builder.Services.AddNeoServiceLayerServices(builder.Configuration);
}

var app = builder.Build();

// Configure the HTTP request pipeline

// Add global error handling middleware (must be early in pipeline)
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Note: Don't use UseDeveloperExceptionPage() with our custom error handling
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Service Layer API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowedOrigins");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("ApiRateLimit");

// Map Health Checks with detailed responses
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            Status = report.Status.ToString(),
            TotalDuration = report.TotalDuration.TotalMilliseconds,
            Checks = report.Entries.Select(x => new
            {
                Name = x.Key,
                Status = x.Value.Status.ToString(),
                Duration = x.Value.Duration.TotalMilliseconds,
                Description = x.Value.Description,
                Data = x.Value.Data,
                Exception = x.Value.Exception?.Message,
                Tags = x.Value.Tags
            }),
            Timestamp = DateTime.UtcNow
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
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
    Predicate = check => check.Tags.Contains("live")
});

// Map info endpoint
app.MapGet("/api/info", () => new
{
    Name = "Neo Service Layer API",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow
}).AllowAnonymous();

Log.Information("Neo Service Layer API starting up...");

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

// Make Program accessible for integration tests
/// <summary>
/// The main program class for the Neo Service Layer API.
/// </summary>
public partial class Program { }
