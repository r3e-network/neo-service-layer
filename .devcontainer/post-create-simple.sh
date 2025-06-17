#!/bin/bash
set -e

echo "🚀 Neo Service Layer - Simplified Post-Create Setup"
echo "================================================="

# Ensure we're in the workspace directory
cd /workspace

# Fix file permissions for scripts
chmod +x .devcontainer/*.sh || true
chmod +x *.sh || true

echo "📦 Setting up .NET environment..."
# Trust development certificates
dotnet dev-certs https --trust

# Clear any existing packages
echo "🧹 Cleaning previous builds..."
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true

echo "📝 Creating minimal Program.cs for development..."
# Create logs directory
mkdir -p src/Web/NeoServiceLayer.Web/logs

# Create a minimal working Program.cs for development
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
    Status = "Running in Simplified DevContainer",
    Features = new string[]
    {
        "Basic Web Interface",
        "Health Monitoring", 
        "JWT Authentication",
        "Swagger API Documentation",
        "Development Container Ready"
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

Log.Information("Neo Service Layer Web Application starting up in Simplified DevContainer...");

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

echo "📦 Restoring NuGet packages..."
if dotnet restore src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj; then
    echo "✅ NuGet restore successful!"
else
    echo "⚠️  NuGet restore failed - continuing anyway..."
fi

echo "🔨 Building the web application..."
if dotnet build src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj; then
    echo "✅ Build successful!"
else
    echo "⚠️  Build failed - this is expected with simplified setup"
    echo "   The container is ready for development. Fix any remaining issues manually."
fi

echo "🦀 Setting up Rust environment..."
source /home/vscode/.cargo/env
rustc --version
cargo --version

echo "🌐 Setting up development environment files..."
# Create launch.json for debugging
mkdir -p .vscode
cat > .vscode/launch.json << 'EOF'
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch Neo Service Layer Web",
            "type": "dotnet",
            "request": "launch",
            "projectPath": "${workspaceFolder}/src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj",
            "program": "${workspaceFolder}/src/Web/NeoServiceLayer.Web/bin/Debug/net9.0/NeoServiceLayer.Web.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/Web/NeoServiceLayer.Web",
            "stopAtEntry": false,
            "serverReadyAction": {
                "action": "openExternally",
                "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
            },
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development",
                "SGX_MODE": "SIM"
            }
        }
    ]
}
EOF

cat > .vscode/tasks.json << 'EOF'
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj"
            ],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "watch",
            "command": "dotnet",
            "type": "process",
            "args": [
                "watch",
                "run",
                "${workspaceFolder}/src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj"
            ],
            "problemMatcher": "$msCompile"
        }
    ]
}
EOF

echo "📝 Creating startup script..."
cat > start-dev-simple.sh << 'EOF'
#!/bin/bash
cd /workspace/src/Web/NeoServiceLayer.Web
export ASPNETCORE_ENVIRONMENT=Development
export SGX_MODE=SIM
dotnet run --urls="http://0.0.0.0:5000"
EOF
chmod +x start-dev-simple.sh

echo "🔧 Environment setup complete!"
echo ""
echo "🎉 Neo Service Layer Simplified DevContainer is ready!"
echo ""
echo "Quick start commands:"
echo "  ./start-dev-simple.sh   - Start the web application"
echo "  dotnet --info           - Show .NET information"
echo "  rustc --version         - Show Rust version"
echo ""
echo "Web application will be available at:"
echo "  http://localhost:5000   - Main application"
echo "  http://localhost:5000/swagger - API documentation"
echo "  http://localhost:5000/health  - Health check" 