# Dockerfile for Compute Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.Compute/NeoServiceLayer.Services.Compute.csproj", "src/Services/NeoServiceLayer.Services.Compute/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.Compute/NeoServiceLayer.Services.Compute.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.Compute/", "src/Services/NeoServiceLayer.Services.Compute/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.Compute/NeoServiceLayer.Services.Compute.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.Compute/NeoServiceLayer.Services.Compute.csproj" \
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
ENV SERVICE_NAME=ComputeService
ENV SERVICE_TYPE=Compute
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - Compute Service"
LABEL service.type="compute"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/ComputeServiceHost.cs
RUN sed -i "s/{ServiceName}/Compute/g" /app/ComputeServiceHost.cs && \
    sed -i "s/{servicename}/compute/g" /app/ComputeServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.Compute.dll"]
