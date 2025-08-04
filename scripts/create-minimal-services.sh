#!/bin/bash

# Create minimal working versions of services to test deployment

set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}Creating minimal service implementations...${NC}"

# Create a minimal API Gateway Program.cs
cat > src/Api/NeoServiceLayer.Api/Program.Minimal.cs << 'EOF'
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

// Add a simple endpoint
app.MapGet("/", () => "Neo Service Layer API Gateway");
app.MapGet("/api/status", () => new { status = "healthy", service = "api-gateway", timestamp = DateTime.UtcNow });

app.Run();
EOF

# Create a minimal Smart Contracts service
mkdir -p src/Services/NeoServiceLayer.Services.SmartContracts.Minimal

cat > src/Services/NeoServiceLayer.Services.SmartContracts.Minimal/NeoServiceLayer.Services.SmartContracts.Minimal.csproj << 'EOF'
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

cat > src/Services/NeoServiceLayer.Services.SmartContracts.Minimal/Program.cs << 'EOF'
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

// Add service endpoints
app.MapGet("/", () => "Neo Smart Contracts Service");
app.MapGet("/api/contracts", () => new[] { new { name = "SampleContract", address = "0x123...", status = "deployed" } });
app.MapGet("/api/status", () => new { status = "healthy", service = "smart-contracts", timestamp = DateTime.UtcNow });

app.Run();
EOF

# Create minimal Dockerfiles
cat > src/Api/NeoServiceLayer.Api/Dockerfile.minimal << 'EOF'
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj .
RUN dotnet restore
COPY src/Api/NeoServiceLayer.Api/Program.Minimal.cs ./Program.cs
RUN dotnet build -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/build .
EXPOSE 80
ENTRYPOINT ["dotnet", "NeoServiceLayer.Api.dll"]
EOF

cat > src/Services/NeoServiceLayer.Services.SmartContracts.Minimal/Dockerfile << 'EOF'
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Services/NeoServiceLayer.Services.SmartContracts.Minimal/NeoServiceLayer.Services.SmartContracts.Minimal.csproj .
RUN dotnet restore
COPY src/Services/NeoServiceLayer.Services.SmartContracts.Minimal/Program.cs .
RUN dotnet build -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/build .
EXPOSE 80
ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.SmartContracts.Minimal.dll"]
EOF

echo -e "${GREEN}Minimal services created!${NC}"