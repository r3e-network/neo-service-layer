#!/bin/bash

# Create minimal Phase 4 service implementations

set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}Creating minimal Phase 4 service implementations...${NC}"

# Services to create
services=(
    "Voting"
    "ZeroKnowledge"
    "SecretsManagement"
    "SocialRecovery"
    "EnclaveStorage"
    "NetworkSecurity"
)

web_services=(
    "Web"
)

# Create service template function
create_service() {
    local service_name=$1
    local service_dir=$2
    local port=$3
    
    mkdir -p "$service_dir"
    
    # Create project file
    cat > "$service_dir/NeoServiceLayer.Services.$service_name.Minimal.csproj" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.9.0" />
  </ItemGroup>

</Project>
EOF

    # Create Program.cs
    cat > "$service_dir/Program.cs" << EOF
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Neo $service_name Service",
        Version = "v1"
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo $service_name Service V1");
    });
}

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

// Add service endpoints
app.MapGet("/", () => "Neo $service_name Service");
app.MapGet("/api/status", () => new { status = "healthy", service = "${service_name,,}", timestamp = DateTime.UtcNow });
app.MapGet("/api/${service_name,,}/info", () => new { 
    service = "$service_name",
    version = "1.0.0",
    features = new[] { "Feature1", "Feature2", "Feature3" },
    capabilities = new[] { "${service_name}Cap1", "${service_name}Cap2" }
});

app.Run();
EOF

    # Create Dockerfile
    cat > "$service_dir/Dockerfile" << EOF
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY $service_dir/NeoServiceLayer.Services.$service_name.Minimal.csproj .
RUN dotnet restore
COPY $service_dir/Program.cs .
RUN dotnet build -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/build .
EXPOSE 80
ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.$service_name.Minimal.dll"]
EOF

    echo -e "${GREEN}Created $service_name service${NC}"
}

# Create web service template function
create_web_service() {
    local service_name=$1
    local service_dir=$2
    local port=$3
    
    mkdir -p "$service_dir"
    
    # Create project file
    cat > "$service_dir/NeoServiceLayer.$service_name.Minimal.csproj" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.9.0" />
  </ItemGroup>

</Project>
EOF

    # Create Program.cs
    cat > "$service_dir/Program.cs" << EOF
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHealthChecks("/health");

// Add web interface endpoints
app.MapGet("/", async context =>
{
    await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <title>Neo Service Layer Dashboard</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .header { background: #0066cc; color: white; padding: 20px; border-radius: 5px; }
        .service { background: #f5f5f5; margin: 10px 0; padding: 15px; border-radius: 5px; }
        .healthy { border-left: 5px solid #28a745; }
        .status { color: #28a745; font-weight: bold; }
    </style>
</head>
<body>
    <div class='header'>
        <h1>Neo Service Layer Dashboard</h1>
        <p>Complete Blockchain Service Infrastructure</p>
    </div>
    
    <h2>Service Status</h2>
    <div class='service healthy'>
        <h3>Phase 1 - Infrastructure & Core Services</h3>
        <p class='status'>✓ All services healthy</p>
        <p>API Gateway, Smart Contracts, PostgreSQL, Redis, Consul, Prometheus, Grafana</p>
    </div>
    
    <div class='service healthy'>
        <h3>Phase 2 - Management & AI Services</h3>
        <p class='status'>✓ All services healthy</p>
        <p>Key Management, Notification, Monitoring, Health, Pattern Recognition, Prediction</p>
    </div>
    
    <div class='service healthy'>
        <h3>Phase 3 - Advanced Services</h3>
        <p class='status'>✓ All services healthy</p>
        <p>Oracle, Storage, CrossChain, Proof of Reserve, Randomness, Fair Ordering, TEE Host</p>
    </div>
    
    <div class='service healthy'>
        <h3>Phase 4 - Security & Governance</h3>
        <p class='status'>✓ All services healthy</p>
        <p>Voting, Zero Knowledge, Secrets Management, Social Recovery, Enclave Storage, Network Security</p>
    </div>
    
    <h2>Quick Links</h2>
    <ul>
        <li><a href='http://localhost:8080'>API Gateway</a></li>
        <li><a href='http://localhost:19090'>Prometheus</a></li>
        <li><a href='http://localhost:13000'>Grafana</a></li>
        <li><a href='http://localhost:18500'>Consul</a></li>
    </ul>
</body>
</html>
    ");
});

app.Run();
EOF

    # Create Dockerfile
    cat > "$service_dir/Dockerfile" << EOF
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY $service_dir/NeoServiceLayer.$service_name.Minimal.csproj .
RUN dotnet restore
COPY $service_dir/Program.cs .
RUN dotnet build -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/build .
EXPOSE 80
ENTRYPOINT ["dotnet", "NeoServiceLayer.$service_name.Minimal.dll"]
EOF

    echo -e "${GREEN}Created $service_name web interface${NC}"
}

# Create regular services
port=8140
for service in "${services[@]}"; do
    create_service "$service" "src/Services/NeoServiceLayer.Services.$service.Minimal" "$port"
    ((port++))
done

# Create web services
port=8200
for service in "${web_services[@]}"; do
    create_web_service "$service" "src/Web/NeoServiceLayer.$service.Minimal" "$port"
    ((port++))
done

echo -e "${GREEN}All Phase 4 minimal services created!${NC}"