# Dockerfile for Configuration Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.Configuration/NeoServiceLayer.Services.Configuration.csproj", "src/Services/NeoServiceLayer.Services.Configuration/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.Configuration/NeoServiceLayer.Services.Configuration.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.Configuration/", "src/Services/NeoServiceLayer.Services.Configuration/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.Configuration/NeoServiceLayer.Services.Configuration.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.Configuration/NeoServiceLayer.Services.Configuration.csproj" \
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
ENV SERVICE_NAME=ConfigurationService
ENV SERVICE_TYPE=Configuration
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - Configuration Service"
LABEL service.type="configuration"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/ConfigurationServiceHost.cs
RUN sed -i "s/{ServiceName}/Configuration/g" /app/ConfigurationServiceHost.cs && \
    sed -i "s/{servicename}/configuration/g" /app/ConfigurationServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.Configuration.dll"]
