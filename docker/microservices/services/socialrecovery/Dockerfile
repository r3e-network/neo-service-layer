# Dockerfile for SocialRecovery Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.SocialRecovery/NeoServiceLayer.Services.SocialRecovery.csproj", "src/Services/NeoServiceLayer.Services.SocialRecovery/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.SocialRecovery/NeoServiceLayer.Services.SocialRecovery.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.SocialRecovery/", "src/Services/NeoServiceLayer.Services.SocialRecovery/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.SocialRecovery/NeoServiceLayer.Services.SocialRecovery.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.SocialRecovery/NeoServiceLayer.Services.SocialRecovery.csproj" \
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
ENV SERVICE_NAME=SocialRecoveryService
ENV SERVICE_TYPE=SocialRecovery
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - SocialRecovery Service"
LABEL service.type="socialrecovery"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/SocialRecoveryServiceHost.cs
RUN sed -i "s/{ServiceName}/SocialRecovery/g" /app/SocialRecoveryServiceHost.cs && \
    sed -i "s/{servicename}/socialrecovery/g" /app/SocialRecoveryServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.SocialRecovery.dll"]
