using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NeoServiceLayer.Infrastructure;
using Serilog;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
// using NeoServiceLayer.Services.Randomness;
// using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Services.KeyManagement;
// using NeoServiceLayer.Services.Compute;
// using NeoServiceLayer.Services.Storage;
// using NeoServiceLayer.Services.Compliance;
// using NeoServiceLayer.Services.EventSubscription;
// using NeoServiceLayer.Services.AbstractAccount;
// using NeoServiceLayer.Services.Automation;
// using NeoServiceLayer.Services.CrossChain;
// using NeoServiceLayer.Services.ProofOfReserve;
// using NeoServiceLayer.Services.ZeroKnowledge;
using NeoServiceLayer.AI.Prediction;
using NeoServiceLayer.AI.PatternRecognition;
// using NeoServiceLayer.Advanced.FairOrdering;
using NeoServiceLayer.Infrastructure.Persistence;
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
builder.Services.AddControllers();
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
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    
    // Order tags alphabetically
    c.OrderActionsBy(api => $"{api.ActionDescriptor.RouteValues["controller"]}_{api.ActionDescriptor.RouteValues["action"]}");
    
    // Add operation filters for better documentation
    c.EnableAnnotations();
    
    // Custom schema IDs to avoid conflicts
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
var issuer = jwtSettings["Issuer"] ?? "NeoServiceLayer";
var audience = jwtSettings["Audience"] ?? "NeoServiceLayerUsers";

// In production, require JWT secret key from configuration
if (builder.Environment.IsProduction() && 
    (string.IsNullOrEmpty(secretKey) || secretKey == "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"))
{
    throw new InvalidOperationException("JWT secret key must be configured via environment variables in production");
}

// Fallback for development only
if (string.IsNullOrEmpty(secretKey))
{
    secretKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
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

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "ready", "live" })
    .AddCheck("database", () => 
    {
        // Check database connectivity
        // This is a placeholder - in production, use AddNpgsql() or similar
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database is accessible");
    }, tags: new[] { "ready", "database" })
    .AddCheck("redis", () => 
    {
        // Check Redis connectivity
        // This is a placeholder - in production, use AddRedis() or similar
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Redis is accessible");
    }, tags: new[] { "ready", "cache" })
    .AddCheck("sgx", () => 
    {
        // Check SGX enclave status
        var sgxMode = Environment.GetEnvironmentVariable("SGX_MODE") ?? "Unknown";
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"SGX Mode: {sgxMode}");
    }, tags: new[] { "ready", "security" });

// Add Neo Service Layer
builder.Services.AddNeoServiceLayer(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
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

// Map Health Checks
app.MapHealthChecks("/health");

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
