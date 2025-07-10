# Dockerfile for NetworkSecurity Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.NetworkSecurity/NeoServiceLayer.Services.NetworkSecurity.csproj", "src/Services/NeoServiceLayer.Services.NetworkSecurity/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.NetworkSecurity/NeoServiceLayer.Services.NetworkSecurity.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.NetworkSecurity/", "src/Services/NeoServiceLayer.Services.NetworkSecurity/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.NetworkSecurity/NeoServiceLayer.Services.NetworkSecurity.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.NetworkSecurity/NeoServiceLayer.Services.NetworkSecurity.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage
FROM ${RUNTIME_BASE_IMAGE} AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Service-specific environment variables
ENV SERVICE_NAME=NetworkSecurityService
ENV SERVICE_TYPE=NetworkSecurity
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - NetworkSecurity Service"
LABEL service.type="networksecurity"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/NetworkSecurityServiceHost.cs
RUN sed -i "s/{ServiceName}/NetworkSecurity/g" /app/NetworkSecurityServiceHost.cs && \
    sed -i "s/{servicename}/networksecurity/g" /app/NetworkSecurityServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.NetworkSecurity.dll"]
