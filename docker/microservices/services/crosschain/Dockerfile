# Dockerfile for CrossChain Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.CrossChain/NeoServiceLayer.Services.CrossChain.csproj", "src/Services/NeoServiceLayer.Services.CrossChain/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.CrossChain/NeoServiceLayer.Services.CrossChain.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.CrossChain/", "src/Services/NeoServiceLayer.Services.CrossChain/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.CrossChain/NeoServiceLayer.Services.CrossChain.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.CrossChain/NeoServiceLayer.Services.CrossChain.csproj" \
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
ENV SERVICE_NAME=CrossChainService
ENV SERVICE_TYPE=CrossChain
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - CrossChain Service"
LABEL service.type="crosschain"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/CrossChainServiceHost.cs
RUN sed -i "s/{ServiceName}/CrossChain/g" /app/CrossChainServiceHost.cs && \
    sed -i "s/{servicename}/crosschain/g" /app/CrossChainServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.CrossChain.dll"]
