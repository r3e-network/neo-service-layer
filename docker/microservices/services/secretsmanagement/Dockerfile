# Dockerfile for SecretsManagement Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.SecretsManagement/NeoServiceLayer.Services.SecretsManagement.csproj", "src/Services/NeoServiceLayer.Services.SecretsManagement/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.SecretsManagement/NeoServiceLayer.Services.SecretsManagement.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.SecretsManagement/", "src/Services/NeoServiceLayer.Services.SecretsManagement/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.SecretsManagement/NeoServiceLayer.Services.SecretsManagement.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.SecretsManagement/NeoServiceLayer.Services.SecretsManagement.csproj" \
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
ENV SERVICE_NAME=SecretsManagementService
ENV SERVICE_TYPE=SecretsManagement
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - SecretsManagement Service"
LABEL service.type="secretsmanagement"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/SecretsManagementServiceHost.cs
RUN sed -i "s/{ServiceName}/SecretsManagement/g" /app/SecretsManagementServiceHost.cs && \
    sed -i "s/{servicename}/secretsmanagement/g" /app/SecretsManagementServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.SecretsManagement.dll"]
