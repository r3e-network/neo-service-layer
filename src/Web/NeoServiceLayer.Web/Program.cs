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

// Add Razor Pages for dynamic content
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
        policy.WithOrigins("http://localhost:3000", "https://localhost:3001", "http://localhost:5000", "https://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Rate limiting removed for compatibility

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// Add Neo Service Layer Core Services
builder.Services.AddNeoServiceLayer(builder.Configuration);

// Add Neo Service Framework (provides IServiceConfiguration)
builder.Services.AddNeoServiceFramework();

// Add Persistent Storage Configuration
builder.Configuration.AddJsonFile("appsettings.PersistentStorage.json", optional: true, reloadOnChange: true);
builder.Services.AddPersistentStorageServices(builder.Configuration);
builder.Services.ConfigureServicesWithPersistentStorage(builder.Configuration);

// Register all Neo Service Layer services
builder.Services.AddScoped<NeoServiceLayer.Services.KeyManagement.IKeyManagementService, NeoServiceLayer.Services.KeyManagement.KeyManagementService>();
builder.Services.AddScoped<NeoServiceLayer.Services.Randomness.IRandomnessService, NeoServiceLayer.Services.Randomness.RandomnessService>();
builder.Services.AddScoped<NeoServiceLayer.Services.Oracle.IOracleService, NeoServiceLayer.Services.Oracle.OracleService>();
builder.Services.AddScoped<NeoServiceLayer.Services.Storage.IStorageService, NeoServiceLayer.Services.Storage.StorageService>();
builder.Services.AddScoped<NeoServiceLayer.Services.Voting.IVotingService, NeoServiceLayer.Services.Voting.VotingService>();
builder.Services.AddScoped<NeoServiceLayer.Services.ZeroKnowledge.IZeroKnowledgeService, NeoServiceLayer.Services.ZeroKnowledge.ZeroKnowledgeService>();
builder.Services.AddScoped<NeoServiceLayer.Services.Backup.IBackupService, NeoServiceLayer.Services.Backup.BackupService>();
builder.Services.AddScoped<NeoServiceLayer.Services.Compliance.IComplianceService, NeoServiceLayer.Services.Compliance.ComplianceService>();
builder.Services.AddScoped<NeoServiceLayer.Services.ProofOfReserve.IProofOfReserveService, NeoServiceLayer.Services.ProofOfReserve.ProofOfReserveService>();
builder.Services.AddScoped<NeoServiceLayer.Services.Health.IHealthService, NeoServiceLayer.Services.Health.HealthService>();
builder.Services.AddScoped<NeoServiceLayer.Services.Monitoring.IMonitoringService, NeoServiceLayer.Services.Monitoring.MonitoringService>();
builder.Services.AddScoped<NeoServiceLayer.Services.Notification.INotificationService, NeoServiceLayer.Services.Notification.NotificationService>();
builder.Services.AddScoped<NeoServiceLayer.Services.AbstractAccount.IAbstractAccountService, NeoServiceLayer.Services.AbstractAccount.AbstractAccountService>();
builder.Services.AddScoped<NeoServiceLayer.Services.Configuration.IConfigurationService, NeoServiceLayer.Services.Configuration.ConfigurationService>();
builder.Services.AddScoped<NeoServiceLayer.Services.Compute.IComputeService, NeoServiceLayer.Services.Compute.ComputeService>();
builder.Services.AddScoped<NeoServiceLayer.Services.Automation.IAutomationService, NeoServiceLayer.Services.Automation.AutomationService>();
builder.Services.AddScoped<NeoServiceLayer.Services.CrossChain.ICrossChainService, NeoServiceLayer.Services.CrossChain.CrossChainService>();
builder.Services.AddScoped<NeoServiceLayer.Services.EventSubscription.IEventSubscriptionService, NeoServiceLayer.Services.EventSubscription.EventSubscriptionService>();

// Register AI Services
builder.Services.AddScoped<NeoServiceLayer.AI.PatternRecognition.IPatternRecognitionService, NeoServiceLayer.AI.PatternRecognition.PatternRecognitionService>();
builder.Services.AddScoped<NeoServiceLayer.AI.Prediction.IPredictionService, NeoServiceLayer.AI.Prediction.PredictionService>();

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
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Service Layer Web API v1");
        c.RoutePrefix = "swagger";
    });
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
app.MapHealthChecks("/health");

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

// Default route serves the main page
app.MapFallbackToPage("/Index");

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
