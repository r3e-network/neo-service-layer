# Dockerfile for Compliance Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.Compliance/NeoServiceLayer.Services.Compliance.csproj", "src/Services/NeoServiceLayer.Services.Compliance/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.Compliance/NeoServiceLayer.Services.Compliance.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.Compliance/", "src/Services/NeoServiceLayer.Services.Compliance/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.Compliance/NeoServiceLayer.Services.Compliance.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.Compliance/NeoServiceLayer.Services.Compliance.csproj" \
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
ENV SERVICE_NAME=ComplianceService
ENV SERVICE_TYPE=Compliance
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - Compliance Service"
LABEL service.type="compliance"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/ComplianceServiceHost.cs
RUN sed -i "s/{ServiceName}/Compliance/g" /app/ComplianceServiceHost.cs && \
    sed -i "s/{servicename}/compliance/g" /app/ComplianceServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.Compliance.dll"]
