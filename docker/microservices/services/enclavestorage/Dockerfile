# Dockerfile for EnclaveStorage Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.EnclaveStorage/NeoServiceLayer.Services.EnclaveStorage.csproj", "src/Services/NeoServiceLayer.Services.EnclaveStorage/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.EnclaveStorage/NeoServiceLayer.Services.EnclaveStorage.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.EnclaveStorage/", "src/Services/NeoServiceLayer.Services.EnclaveStorage/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.EnclaveStorage/NeoServiceLayer.Services.EnclaveStorage.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.EnclaveStorage/NeoServiceLayer.Services.EnclaveStorage.csproj" \
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
ENV SERVICE_NAME=EnclaveStorageService
ENV SERVICE_TYPE=EnclaveStorage
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - EnclaveStorage Service"
LABEL service.type="enclavestorage"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/EnclaveStorageServiceHost.cs
RUN sed -i "s/{ServiceName}/EnclaveStorage/g" /app/EnclaveStorageServiceHost.cs && \
    sed -i "s/{servicename}/enclavestorage/g" /app/EnclaveStorageServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.EnclaveStorage.dll"]
