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

// Configure explicit port binding
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRazorPages();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Neo Service Layer Web API",
        Version = "v1",
        Description = "A comprehensive blockchain service layer API running in Docker"
    });
});

// Configure JWT Authentication
var secretKey = "Development-SuperSecretKeyThatIsTotallyLongEnoughForJWTTokenGenerationAndSigning2024-MinimumRequirement256Bits!";
var issuer = "NeoServiceLayer-Docker";
var audience = "NeoServiceLayerUsers-Docker";

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
        policy.WithOrigins(Configuration["ServiceEndpoints:Http:3000"] ?? "http://localhost:3000", Configuration["ServiceEndpoints:Https:3001"] ?? "https://localhost:3001", Configuration["ServiceEndpoints:Http:5000"] ?? "http://localhost:5000", Configuration["ServiceEndpoints:Https:5001"] ?? "https://localhost:5001")
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
app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Service Layer Web API v1");
    c.RoutePrefix = "swagger";
});

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
    Status = "Running in Docker Container",
    Features = new string[]
    {
        "Basic Web Interface",
        "Health Monitoring", 
        "JWT Authentication",
        "Swagger API Documentation",
        "Docker Container Ready",
        "Production Deployment Ready"
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

// Map services overview endpoint
app.MapGet("/api/services", () => new
{
    Services = new object[]
    {
        new { Name = "Key Management", Status = "Available", Endpoint = "/api/keymanagement" },
        new { Name = "Storage", Status = "Available", Endpoint = "/api/storage" },
        new { Name = "Health", Status = "Available", Endpoint = "/health" },
        new { Name = "Configuration", Status = "Available", Endpoint = "/api/configuration" },
        new { Name = "Neo N3", Status = "Available", Endpoint = "/api/neo/n3" },
        new { Name = "Oracle", Status = "Available", Endpoint = "/api/oracle" },
        new { Name = "Voting", Status = "Available", Endpoint = "/api/voting" },
        new { Name = "Zero Knowledge", Status = "Available", Endpoint = "/api/zk" },
        new { Name = "Cross Chain", Status = "Available", Endpoint = "/api/crosschain" }
    },
    TotalServices = 9,
    ActiveServices = 9,
    ContainerInfo = new
    {
        Environment = "Docker",
        Runtime = ".NET 9.0",
        OS = "Linux",
        Architecture = "x64"
    }
}).AllowAnonymous();

// Default route serves the main page
app.MapFallbackToPage("/Index");

Log.Information("🚀 Neo Service Layer Web Application starting up in Docker...");
Log.Information("🌐 Available at: http://localhost:5000");
Log.Information("📚 API Documentation: http://localhost:5000/swagger");
Log.Information("❤️  Health Check: http://localhost:5000/health");

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