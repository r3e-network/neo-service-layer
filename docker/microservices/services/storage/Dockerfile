# Dockerfile for Storage Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.Storage/NeoServiceLayer.Services.Storage.csproj", "src/Services/NeoServiceLayer.Services.Storage/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.Storage/NeoServiceLayer.Services.Storage.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.Storage/", "src/Services/NeoServiceLayer.Services.Storage/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.Storage/NeoServiceLayer.Services.Storage.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.Storage/NeoServiceLayer.Services.Storage.csproj" \
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
ENV SERVICE_NAME=StorageService
ENV SERVICE_TYPE=Storage
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - Storage Service"
LABEL service.type="storage"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/StorageServiceHost.cs
RUN sed -i "s/{ServiceName}/Storage/g" /app/StorageServiceHost.cs && \
    sed -i "s/{servicename}/storage/g" /app/StorageServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.Storage.dll"]
