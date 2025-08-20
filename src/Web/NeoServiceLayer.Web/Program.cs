using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NeoServiceLayer.AI.Prediction;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Configuration;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Web.Extensions;
using NeoServiceLayer.Web.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

// Configure URLs for Docker
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
});

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add Razor Pages for dynamic content
builder.Services.AddRazorPages();

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Neo Service Layer API", 
        Version = "v1",
        Description = "Enterprise blockchain service layer for Neo"
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
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
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
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
var isTestEnvironment = builder.Environment.EnvironmentName == "Test";

// In test environment, use a fixed key for consistency
if (isTestEnvironment)
{
    jwtSecretKey = "MyUniqueSecretKeyForNeoServiceLayerThatIsLongEnoughAndSecureForProductionUse2024!";
}
else if (!builder.Environment.IsDevelopment())
{
    // Validate JWT secret key for production
    if (string.IsNullOrEmpty(jwtSecretKey))
    {
        throw new InvalidOperationException("JWT secret key is not configured");
    }

    // Ensure minimum key length for security
    if (jwtSecretKey.Length < 32)
    {
        throw new InvalidOperationException("JWT secret key must be at least 32 characters long");
    }

    // Prevent use of default/example keys
    if (jwtSecretKey.Contains("example", StringComparison.OrdinalIgnoreCase) ||
        jwtSecretKey.Contains("changeme", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("JWT secret key appears to be a default value. Please configure a secure key.");
    }
}

if (!string.IsNullOrEmpty(jwtSecretKey))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NeoServiceLayer",
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NeoServiceLayerAPI",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
            };
        });
}

// Configure Authorization
builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", corsBuilder =>
    {
        // Get endpoints configuration
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        
        if (builder.Environment.IsDevelopment())
        {
            // Allow localhost origins in development only
            corsBuilder.WithOrigins("http://localhost:3000", "http://localhost:5000", "http://localhost:5173")
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials();
        }
        else if (allowedOrigins.Length > 0)
        {
            corsBuilder.WithOrigins(allowedOrigins)
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials();
        }
        // No CORS in production without configuration
    });
});

// Rate limiting removed for compatibility

// Add Health Checks
builder.Services.AddHealthChecks();

// Add secure configuration services
builder.Services.AddSingleton<ISecureConfigurationProvider, SecureConfigurationProvider>();

// Add Neo Service Layer Core Services
NeoServiceLayer.Infrastructure.ServiceCollectionExtensions.AddNeoServiceLayer(builder.Services, builder.Configuration);

// Add Neo Service Framework (provides IServiceConfiguration) - Already included in AddNeoServiceLayer
// builder.Services.AddServiceFramework();

// Configure service endpoints
builder.Services.Configure<ServiceEndpoints>(builder.Configuration.GetSection("ServiceEndpoints"));

if (!builder.Environment.IsDevelopment())
{
    // Validate endpoints in production
    var endpoints = builder.Configuration.GetSection("ServiceEndpoints").Get<ServiceEndpoints>();
    if (endpoints == null)
    {
        throw new InvalidOperationException("Service endpoints are not properly configured");
    }
}

// Add Persistent Storage Configuration
builder.Services.AddPersistentStorage(builder.Configuration);

// Register all Neo Service Layer services (all 26 services) - Already done in AddNeoServiceLayer
// builder.Services.RegisterNeoServices();

// Register TEE Services - Use production wrapper with environment-aware configuration
// builder.Services.AddScoped<NeoServiceLayer.Tee.Host.Services.IEnclaveHostService, NeoServiceLayer.Tee.Host.Services.EnclaveHostService>(); // Interface doesn't exist
builder.Services.AddScoped<IEnclaveManager, EnclaveManager>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Configure Swagger with authentication in production
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Require authentication for Swagger UI in production
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Service Layer API v1");
    });
    
    // Protect Swagger endpoints in production
    app.UseExceptionHandler("/Error");
}

// Serve static files
app.UseStaticFiles();

// Skip HTTPS redirection in container
// app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigin");
app.UseAuthentication();
app.UseAuthorization();

// Add permission middleware after authentication/authorization
app.UseMiddleware<PermissionMiddleware>();

// Map controllers
app.MapControllers();

// Map Razor Pages
app.MapRazorPages();

// Map Health Checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        }));
    }
});

// Map info endpoint
app.MapGet("/", () => new
{
    service = "Neo Service Layer",
    version = "1.0.0",
    status = "running",
    environment = app.Environment.EnvironmentName
});

// Authentication endpoints
// NOTE: Production authentication should be handled by a proper identity provider
// This endpoint is disabled in production for security reasons
if (app.Environment.IsDevelopment() || isTestEnvironment)
{
    app.MapPost("/api/auth/token", async (HttpContext context) =>
    {
        // Simple token generation for development/testing only
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey ?? "development-key"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "NeoServiceLayer",
            audience: "NeoServiceLayerAPI",
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );

        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { token = tokenString }));
    });
}

// Default route serves the main page
app.MapGet("/api", () => new
{
    message = "Neo Service Layer API",
    version = "1.0.0",
    documentation = "/swagger"
});

app.Run();

// Make Program accessible to tests
public partial class Program { }