# Dockerfile for KeyManagement Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.KeyManagement/NeoServiceLayer.Services.KeyManagement.csproj", "src/Services/NeoServiceLayer.Services.KeyManagement/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.KeyManagement/NeoServiceLayer.Services.KeyManagement.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.KeyManagement/", "src/Services/NeoServiceLayer.Services.KeyManagement/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.KeyManagement/NeoServiceLayer.Services.KeyManagement.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.KeyManagement/NeoServiceLayer.Services.KeyManagement.csproj" \
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
ENV SERVICE_NAME=KeyManagementService
ENV SERVICE_TYPE=KeyManagement
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - KeyManagement Service"
LABEL service.type="keymanagement"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/KeyManagementServiceHost.cs
RUN sed -i "s/{ServiceName}/KeyManagement/g" /app/KeyManagementServiceHost.cs && \
    sed -i "s/{servicename}/keymanagement/g" /app/KeyManagementServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.KeyManagement.dll"]
