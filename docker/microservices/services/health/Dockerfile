# Dockerfile for Health Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.Health/NeoServiceLayer.Services.Health.csproj", "src/Services/NeoServiceLayer.Services.Health/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.Health/NeoServiceLayer.Services.Health.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.Health/", "src/Services/NeoServiceLayer.Services.Health/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.Health/NeoServiceLayer.Services.Health.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.Health/NeoServiceLayer.Services.Health.csproj" \
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
ENV SERVICE_NAME=HealthService
ENV SERVICE_TYPE=Health
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - Health Service"
LABEL service.type="health"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/HealthServiceHost.cs
RUN sed -i "s/{ServiceName}/Health/g" /app/HealthServiceHost.cs && \
    sed -i "s/{servicename}/health/g" /app/HealthServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.Health.dll"]
