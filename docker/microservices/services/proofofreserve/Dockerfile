# Dockerfile for ProofOfReserve Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.ProofOfReserve/NeoServiceLayer.Services.ProofOfReserve.csproj", "src/Services/NeoServiceLayer.Services.ProofOfReserve/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.ProofOfReserve/NeoServiceLayer.Services.ProofOfReserve.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.ProofOfReserve/", "src/Services/NeoServiceLayer.Services.ProofOfReserve/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.ProofOfReserve/NeoServiceLayer.Services.ProofOfReserve.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.ProofOfReserve/NeoServiceLayer.Services.ProofOfReserve.csproj" \
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
ENV SERVICE_NAME=ProofOfReserveService
ENV SERVICE_TYPE=ProofOfReserve
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - ProofOfReserve Service"
LABEL service.type="proofofreserve"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/ProofOfReserveServiceHost.cs
RUN sed -i "s/{ServiceName}/ProofOfReserve/g" /app/ProofOfReserveServiceHost.cs && \
    sed -i "s/{servicename}/proofofreserve/g" /app/ProofOfReserveServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.ProofOfReserve.dll"]
