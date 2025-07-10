# Dockerfile for Backup Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.Backup/NeoServiceLayer.Services.Backup.csproj", "src/Services/NeoServiceLayer.Services.Backup/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.Backup/NeoServiceLayer.Services.Backup.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.Backup/", "src/Services/NeoServiceLayer.Services.Backup/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.Backup/NeoServiceLayer.Services.Backup.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.Backup/NeoServiceLayer.Services.Backup.csproj" \
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
ENV SERVICE_NAME=BackupService
ENV SERVICE_TYPE=Backup
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - Backup Service"
LABEL service.type="backup"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/BackupServiceHost.cs
RUN sed -i "s/{ServiceName}/Backup/g" /app/BackupServiceHost.cs && \
    sed -i "s/{servicename}/backup/g" /app/BackupServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.Backup.dll"]
