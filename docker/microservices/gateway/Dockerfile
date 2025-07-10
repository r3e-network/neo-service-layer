# Dockerfile for API Gateway
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy gateway project files
COPY ["src/Gateway/NeoServiceLayer.Gateway.Api/NeoServiceLayer.Gateway.Api.csproj", "src/Gateway/NeoServiceLayer.Gateway.Api/"]

# Copy framework dependencies
COPY ["src/Core/NeoServiceLayer.ServiceFramework/NeoServiceLayer.ServiceFramework.csproj", "src/Core/NeoServiceLayer.ServiceFramework/"]
COPY ["src/Infrastructure/NeoServiceLayer.Infrastructure.ServiceDiscovery/NeoServiceLayer.Infrastructure.ServiceDiscovery.csproj", "src/Infrastructure/NeoServiceLayer.Infrastructure.ServiceDiscovery/"]

# Restore gateway dependencies
RUN dotnet restore "src/Gateway/NeoServiceLayer.Gateway.Api/NeoServiceLayer.Gateway.Api.csproj"

# Copy all source files
COPY ["src/Gateway/NeoServiceLayer.Gateway.Api/", "src/Gateway/NeoServiceLayer.Gateway.Api/"]
COPY ["src/Core/NeoServiceLayer.ServiceFramework/", "src/Core/NeoServiceLayer.ServiceFramework/"]
COPY ["src/Infrastructure/NeoServiceLayer.Infrastructure.ServiceDiscovery/", "src/Infrastructure/NeoServiceLayer.Infrastructure.ServiceDiscovery/"]

# Build gateway
RUN dotnet build "src/Gateway/NeoServiceLayer.Gateway.Api/NeoServiceLayer.Gateway.Api.csproj" -c Release --no-restore

# Publish gateway
RUN dotnet publish "src/Gateway/NeoServiceLayer.Gateway.Api/NeoServiceLayer.Gateway.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage
FROM ${RUNTIME_BASE_IMAGE} AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Gateway-specific environment variables
ENV SERVICE_NAME=ApiGateway
ENV SERVICE_TYPE=Gateway
ENV SERVICE_PORT=80

# Gateway-specific labels
LABEL service.name="Neo Service Layer - API Gateway"
LABEL service.type="gateway"
LABEL service.version="1.0.0"

# Expose HTTP and HTTPS ports
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "NeoServiceLayer.Gateway.Api.dll"]