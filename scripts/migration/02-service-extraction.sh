#!/bin/bash

# Neo Service Layer - Service Extraction Script  
# Phase 2: Extract and refactor services from monolith
# This script handles code extraction and microservice creation

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
EXTRACTION_DIR="${PROJECT_ROOT}/extracted_services"
LOG_FILE="${PROJECT_ROOT}/logs/extraction-$(date +%Y%m%d_%H%M%S).log"

# Service definitions
declare -A SERVICES=(
    ["auth"]="Authentication and Authorization"
    ["oracle"]="Oracle Data Feeds"
    ["compute"]="SGX Compute Service"  
    ["storage"]="Distributed Storage"
    ["secrets"]="Secrets Management"
    ["voting"]="Voting and Governance"
    ["monitoring"]="Health and Monitoring"
    ["crosschain"]="Cross-chain Operations"
)

# Source directories to analyze
MONOLITH_SERVICES_DIR="${PROJECT_ROOT}/src/Services"
MONOLITH_CORE_DIR="${PROJECT_ROOT}/src/Core"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log() {
    echo -e "${1}" | tee -a "${LOG_FILE}"
}

log_info() {
    log "${BLUE}[INFO]${NC} $1"
}

log_success() {
    log "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    log "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    log "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking extraction prerequisites..."
    
    # Check if .NET CLI is available
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET CLI is not installed. Please install .NET 8.0 SDK."
        exit 1
    fi
    
    # Check .NET version
    local dotnet_version
    dotnet_version=$(dotnet --version)
    log_info "Using .NET version: ${dotnet_version}"
    
    # Create directories
    mkdir -p "${EXTRACTION_DIR}"
    mkdir -p "$(dirname "${LOG_FILE}")"
    
    log_success "Prerequisites check completed"
}

# Analyze existing monolith structure
analyze_monolith() {
    log_info "Analyzing existing monolith structure..."
    
    # Find all service-related files
    find "${MONOLITH_SERVICES_DIR}" -name "*.cs" > "${EXTRACTION_DIR}/monolith_files.txt" 2>/dev/null || true
    find "${MONOLITH_CORE_DIR}" -name "*.cs" >> "${EXTRACTION_DIR}/monolith_files.txt" 2>/dev/null || true
    
    local file_count
    file_count=$(wc -l < "${EXTRACTION_DIR}/monolith_files.txt")
    log_info "Found ${file_count} C# source files to analyze"
    
    # Analyze dependencies
    log_info "Analyzing service dependencies..."
    
    # Create dependency map
    cat > "${EXTRACTION_DIR}/dependency_analysis.txt" << EOF
Neo Service Layer - Dependency Analysis
======================================

Generated: $(date)

Service Directory Structure:
EOF
    
    if [ -d "${MONOLITH_SERVICES_DIR}" ]; then
        find "${MONOLITH_SERVICES_DIR}" -type d -name "NeoServiceLayer.Services.*" | while read -r dir; do
            local service_name
            service_name=$(basename "${dir}" | sed 's/NeoServiceLayer.Services.//')
            echo "- ${service_name}: ${dir}" >> "${EXTRACTION_DIR}/dependency_analysis.txt"
            
            # Count files in each service
            local cs_files
            cs_files=$(find "${dir}" -name "*.cs" | wc -l)
            echo "  Files: ${cs_files}" >> "${EXTRACTION_DIR}/dependency_analysis.txt"
        done
    fi
    
    log_success "Monolith analysis completed"
}

# Create microservice project structure
create_microservice_structure() {
    local service_name=$1
    local service_dir="${EXTRACTION_DIR}/${service_name}"
    
    log_info "Creating microservice structure for: ${service_name}"
    
    # Create directory structure
    mkdir -p "${service_dir}"/{src,tests,k8s,docs}
    mkdir -p "${service_dir}/src"/{Controllers,Services,Models,Data,Configuration}
    mkdir -p "${service_dir}/tests"/{Unit,Integration}
    
    # Create project files
    create_microservice_csproj "${service_name}" "${service_dir}"
    create_microservice_program "${service_name}" "${service_dir}"
    create_microservice_dockerfile "${service_name}" "${service_dir}"
    create_microservice_appsettings "${service_name}" "${service_dir}"
    
    log_success "Microservice structure created: ${service_dir}"
}

# Create .csproj file
create_microservice_csproj() {
    local service_name=$1
    local service_dir=$2
    
    cat > "${service_dir}/src/Neo.${service_name^}.Service.csproj" << EOF
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <AssemblyName>Neo.${service_name^}.Service</AssemblyName>
    <RootNamespace>Neo.${service_name^}.Service</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
    <PackageReference Include="Serilog.Sinks.Seq" Version="7.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="AspNetCore.HealthChecks.Npgsql" Version="8.0.1" />
    <PackageReference Include="AspNetCore.HealthChecks.Redis" Version="8.0.1" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.7.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.7.1" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.7.1" />
    <PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.0.0-beta.9" />
    <PackageReference Include="OpenTelemetry.Exporter.Jaeger" Version="1.5.1" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
    <PackageReference Include="MediatR" Version="12.2.0" />
    <PackageReference Include="AutoMapper" Version="13.0.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../shared/Neo.Shared.Contracts/Neo.Shared.Contracts.csproj" />
    <ProjectReference Include="../../shared/Neo.Shared.Infrastructure/Neo.Shared.Infrastructure.csproj" />
  </ItemGroup>

</Project>
EOF
}

# Create Program.cs file
create_microservice_program() {
    local service_name=$1
    local service_dir=$2
    
    cat > "${service_dir}/src/Program.cs" << EOF
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Neo.${service_name^}.Service.Data;
using Neo.${service_name^}.Service.Services;
using Neo.Shared.Infrastructure.Extensions;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.Seq(context.Configuration.GetConnectionString("Seq") ?? "http://seq:5341")
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "neo-${service_name}-service")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);
});

// Database configuration
builder.Services.AddDbContext<${service_name^}DbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Service registrations
builder.Services.AddScoped<I${service_name^}Service, ${service_name^}Service>();

// Infrastructure services
builder.Services.AddNeoInfrastructure(builder.Configuration);

// Authentication & Authorization
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.Audience = "neo-service-layer";
    });

builder.Services.AddAuthorization();

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Neo ${service_name^} Service API", 
        Version = "v1",
        Description = "Neo ${service_name^} Service - Part of Neo Service Layer Microservices"
    });
    
    var xmlFilename = \$"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
    .AddDbContextCheck<${service_name^}DbContext>();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("neo-${service_name}-service")
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("neo-${service_name}-service", "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["service.namespace"] = "neo-services",
                    ["k8s.cluster.name"] = "neo-cluster"
                }))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddJaegerExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["Jaeger:Endpoint"] ?? "http://jaeger-collector:14268/api/traces");
            });
    });

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo ${service_name^} Service API v1"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Database migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<${service_name^}DbContext>();
    context.Database.Migrate();
}

app.Run();
EOF
}

# Create Dockerfile
create_microservice_dockerfile() {
    local service_name=$1
    local service_dir=$2
    
    cat > "${service_dir}/Dockerfile" << EOF
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["src/Neo.${service_name^}.Service.csproj", "src/"]
COPY ["shared/Neo.Shared.Contracts/Neo.Shared.Contracts.csproj", "shared/Neo.Shared.Contracts/"]
COPY ["shared/Neo.Shared.Infrastructure/Neo.Shared.Infrastructure.csproj", "shared/Neo.Shared.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/Neo.${service_name^}.Service.csproj"

# Copy source code
COPY . .
WORKDIR "/src/src"

# Build application
RUN dotnet build "Neo.${service_name^}.Service.csproj" -c \$BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Neo.${service_name^}.Service.csproj" -c \$BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 neo && \\
    adduser --system --uid 1001 --gid 1001 neo

# Copy application
COPY --from=publish /app/publish .

# Set ownership and permissions
RUN chown -R neo:neo /app
USER neo

ENTRYPOINT ["dotnet", "Neo.${service_name^}.Service.dll"]
EOF
}

# Create appsettings.json
create_microservice_appsettings() {
    local service_name=$1
    local service_dir=$2
    
    cat > "${service_dir}/src/appsettings.json" << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=neo-${service_name}-db;Port=5432;Database=neo_${service_name}_db;Username=neo_user;Password=",
    "Redis": "neo-redis:6379",
    "Seq": "http://seq:5341"
  },
  "Auth": {
    "Authority": "http://neo-auth-service",
    "Audience": "neo-service-layer",
    "RequireHttpsMetadata": false
  },
  "Jaeger": {
    "Endpoint": "http://jaeger-collector:14268/api/traces"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "AllowedHosts": "*"
}
EOF

    cat > "${service_dir}/src/appsettings.Development.json" << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=neo_${service_name}_db_dev;Username=neo_dev;Password=neo_dev_password",
    "Redis": "localhost:6379",
    "Seq": "http://localhost:5341"
  },
  "Auth": {
    "Authority": "https://localhost:7001",
    "RequireHttpsMetadata": false
  },
  "Jaeger": {
    "Endpoint": "http://localhost:14268/api/traces"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
EOF
}

# Extract authentication service
extract_auth_service() {
    log_info "Extracting authentication service..."
    
    create_microservice_structure "auth"
    
    local auth_dir="${EXTRACTION_DIR}/auth"
    local source_auth_dir="${MONOLITH_SERVICES_DIR}/NeoServiceLayer.Services.Auth.Microservice"
    
    # Copy existing auth service if it exists
    if [ -d "${source_auth_dir}" ]; then
        log_info "Copying existing auth service implementation..."
        
        # Copy controllers
        if [ -d "${source_auth_dir}/Controllers" ]; then
            cp -r "${source_auth_dir}/Controllers" "${auth_dir}/src/"
        fi
        
        # Copy models
        if [ -d "${source_auth_dir}/Models" ]; then
            cp -r "${source_auth_dir}/Models" "${auth_dir}/src/"
        fi
        
        # Copy services
        if [ -d "${source_auth_dir}/Services" ]; then
            cp -r "${source_auth_dir}/Services" "${auth_dir}/src/"
        fi
        
        # Copy data layer
        if [ -d "${source_auth_dir}/Data" ]; then
            cp -r "${source_auth_dir}/Data" "${auth_dir}/src/"
        fi
        
        log_success "Auth service implementation copied"
    else
        log_warning "Auth service source directory not found, creating template..."
        create_auth_service_template "${auth_dir}"
    fi
    
    # Create Kubernetes manifests
    create_service_k8s_manifests "auth" "${auth_dir}"
}

# Create auth service template
create_auth_service_template() {
    local auth_dir=$1
    
    # Create basic controller
    cat > "${auth_dir}/src/Controllers/AuthController.cs" << 'EOF'
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neo.Auth.Service.Models;
using Neo.Auth.Service.Services;

namespace Neo.Auth.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return result.Success ? Ok(result) : Unauthorized();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        return result != null ? Ok(result) : Unauthorized();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return Ok();
    }
}
EOF
}

# Create Kubernetes manifests for service
create_service_k8s_manifests() {
    local service_name=$1
    local service_dir=$2
    
    log_info "Creating Kubernetes manifests for ${service_name} service..."
    
    # Create deployment manifest
    cat > "${service_dir}/k8s/deployment.yaml" << EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: neo-${service_name}-service
  namespace: neo-services
  labels:
    app: neo-${service_name}-service
    version: v1
spec:
  replicas: 3
  selector:
    matchLabels:
      app: neo-${service_name}-service
  template:
    metadata:
      labels:
        app: neo-${service_name}-service
        version: v1
        monitoring: "true"
    spec:
      securityContext:
        runAsNonRoot: true
        runAsUser: 1001
        runAsGroup: 1001
        fsGroup: 1001
      containers:
      - name: ${service_name}-service
        image: ghcr.io/neo-service-layer/neo-${service_name}-service:latest
        ports:
        - containerPort: 8080
          name: http
        - containerPort: 8081
          name: https
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        envFrom:
        - secretRef:
            name: neo-${service_name}-secret
        - configMapRef:
            name: neo-${service_name}-config
        resources:
          requests:
            memory: 256Mi
            cpu: 100m
          limits:
            memory: 512Mi
            cpu: 500m
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
        startupProbe:
          httpGet:
            path: /health
            port: 8080
          failureThreshold: 30
          periodSeconds: 10
      imagePullSecrets:
      - name: ghcr-secret
EOF

    # Create service manifest
    cat > "${service_dir}/k8s/service.yaml" << EOF
apiVersion: v1
kind: Service
metadata:
  name: neo-${service_name}-service
  namespace: neo-services
  labels:
    app: neo-${service_name}-service
    monitoring: "true"
spec:
  selector:
    app: neo-${service_name}-service
  ports:
  - name: http
    port: 80
    targetPort: 8080
    protocol: TCP
  - name: https
    port: 443
    targetPort: 8081
    protocol: TCP
  type: ClusterIP
EOF

    # Create configmap
    cat > "${service_dir}/k8s/configmap.yaml" << EOF
apiVersion: v1
kind: ConfigMap
metadata:
  name: neo-${service_name}-config
  namespace: neo-services
data:
  Auth__Authority: "http://neo-auth-service"
  Auth__Audience: "neo-service-layer"
  Auth__RequireHttpsMetadata: "false"
  Jaeger__Endpoint: "http://jaeger-collector:14268/api/traces"
  ConnectionStrings__Redis: "neo-redis:6379"
  ConnectionStrings__Seq: "http://seq:5341"
EOF

    log_success "Kubernetes manifests created for ${service_name} service"
}

# Generate extraction report
generate_extraction_report() {
    log_info "Generating extraction report..."
    
    local report_file="${EXTRACTION_DIR}/extraction_report.md"
    
    cat > "${report_file}" << EOF
# Neo Service Layer - Service Extraction Report

**Generated:** $(date)
**Location:** ${EXTRACTION_DIR}

## Services Extracted

EOF
    
    for service in "${!SERVICES[@]}"; do
        echo "### ${service^} Service" >> "${report_file}"
        echo "- **Description:** ${SERVICES[$service]}" >> "${report_file}"
        echo "- **Location:** \`${EXTRACTION_DIR}/${service}\`" >> "${report_file}"
        
        if [ -d "${EXTRACTION_DIR}/${service}" ]; then
            local file_count
            file_count=$(find "${EXTRACTION_DIR}/${service}" -name "*.cs" | wc -l)
            echo "- **Files Generated:** ${file_count} C# files" >> "${report_file}"
        fi
        echo "" >> "${report_file}"
    done
    
    cat >> "${report_file}" << EOF

## Generated Structure

\`\`\`
extracted_services/
├── auth/
│   ├── src/
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Models/
│   │   ├── Data/
│   │   └── Configuration/
│   ├── tests/
│   ├── k8s/
│   └── docs/
├── oracle/
├── compute/
└── ...
\`\`\`

## Next Steps

1. **Review extracted services** for completeness and correctness
2. **Update database connections** to use microservice-specific databases
3. **Implement service interfaces** and business logic
4. **Create comprehensive tests** for each service
5. **Deploy services to Kubernetes** using provided manifests
6. **Configure service mesh** routing and security
7. **Monitor service health** and performance
8. **Gradually migrate traffic** from monolith to microservices

## Files Generated

EOF
    
    find "${EXTRACTION_DIR}" -type f -name "*.cs" -o -name "*.yaml" -o -name "*.json" -o -name "Dockerfile" | while read -r file; do
        echo "- $(basename "${file}") in $(dirname "${file}" | sed "s|${EXTRACTION_DIR}/||")" >> "${report_file}"
    done
    
    log_success "Extraction report generated: ${report_file}"
}

# Main execution
main() {
    log_info "Starting Neo Service Layer Service Extraction"
    log_info "==========================================="
    
    check_prerequisites
    analyze_monolith
    
    # Extract key services
    extract_auth_service
    
    # Create basic structure for other services
    for service in oracle compute storage secrets voting monitoring crosschain; do
        if [[ " ${!SERVICES[@]} " =~ " ${service} " ]]; then
            log_info "Creating structure for ${service} service..."
            create_microservice_structure "${service}"
            create_service_k8s_manifests "${service}" "${EXTRACTION_DIR}/${service}"
        fi
    done
    
    generate_extraction_report
    
    log_success "Service extraction completed successfully!"
    log_info "Extracted services are available in: ${EXTRACTION_DIR}"
    log_info "Review the extraction report: ${EXTRACTION_DIR}/extraction_report.md"
}

# Handle script interruption
trap 'log_error "Extraction interrupted by user"; exit 130' INT

# Execute main function
main "$@"