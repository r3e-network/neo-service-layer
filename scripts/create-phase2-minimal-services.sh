#!/bin/bash

# Create minimal Phase 2 service implementations

set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}Creating minimal Phase 2 service implementations...${NC}"

# Services to create
services=(
    "KeyManagement"
    "Notification"
    "Monitoring"
    "Health"
)

ai_services=(
    "PatternRecognition"
    "Prediction"
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
    features = new[] { "Feature1", "Feature2", "Feature3" }
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

# Create regular services
port=8090
for service in "${services[@]}"; do
    create_service "$service" "src/Services/NeoServiceLayer.Services.$service.Minimal" "$port"
    ((port++))
done

# Create AI services
port=8100
for service in "${ai_services[@]}"; do
    create_service "$service" "src/AI/NeoServiceLayer.AI.$service.Minimal" "$port"
    ((port++))
done

echo -e "${GREEN}All Phase 2 minimal services created!${NC}"