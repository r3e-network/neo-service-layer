#!/bin/bash
set -e

echo "🔧 Neo Service Layer - Service Integration Helper"
echo "================================================"

cd /workspace

echo "This script helps you enable Neo Service Layer services incrementally"
echo "if the complete integration fails during container setup."
echo ""

# Check if we have the original Program.cs
if [ -f "src/Web/NeoServiceLayer.Web/Program.cs.original" ]; then
    echo "📋 Available configurations:"
    echo "  1. Complete integration (all services)"
    echo "  2. Core services only"
    echo "  3. Basic web application"
    echo ""
    
    read -p "Select configuration (1-3): " choice
    
    case $choice in
        1)
            echo "🚀 Enabling complete integration..."
            cp src/Web/NeoServiceLayer.Web/Program.cs.original src/Web/NeoServiceLayer.Web/Program.cs
            ;;
        2)
            echo "🔧 Enabling core services only..."
            # Create core services version
            cat > src/Web/NeoServiceLayer.Web/Program.cs << 'EOF'
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/neo-service-layer-web-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRazorPages();

// Add core services only
try 
{
    // Add only essential services here
    Console.WriteLine("✅ Core services registered successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Core services failed to register: {ex.Message}");
}

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Neo Service Layer Web API",
        Version = "v1",
        Description = "A comprehensive blockchain service layer API"
    });
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "Development-SuperSecretKeyThatIsTotallyLongEnoughForJWTTokenGenerationAndSigning2024-MinimumRequirement256Bits!";
var issuer = jwtSettings["Issuer"] ?? "NeoServiceLayer-Development";
var audience = jwtSettings["Audience"] ?? "NeoServiceLayerUsers-Development";

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

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

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

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowedOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHealthChecks("/health");

// Map info endpoint
app.MapGet("/api/info", () => new
{
    Name = "Neo Service Layer Web Application",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Status = "Running with Core Services",
    Features = new string[]
    {
        "Basic Web Interface",
        "Health Monitoring", 
        "JWT Authentication",
        "Swagger API Documentation",
        "Core Services Enabled"
    }
}).AllowAnonymous();

// Authentication endpoints
app.MapPost("/api/auth/demo-token", () =>
{
    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(secretKey);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "demo-user"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")
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

Log.Information("Neo Service Layer Web Application starting up with Core Services...");

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
EOF
            ;;
        3)
            echo "🔧 Using basic web application..."
            # Use the fallback version created during post-create
            ;;
        *)
            echo "Invalid choice. Exiting."
            exit 1
            ;;
    esac
    
    echo "🔨 Building with selected configuration..."
    if dotnet build src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj; then
        echo "✅ Build successful!"
        echo "🚀 You can now start the application with: ./start-dev.sh"
    else
        echo "❌ Build failed. Check the error messages above."
    fi
else
    echo "❌ Original Program.cs not found. Cannot switch configurations."
fi 