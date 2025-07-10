# Dockerfile for Oracle Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.Oracle/NeoServiceLayer.Services.Oracle.csproj", "src/Services/NeoServiceLayer.Services.Oracle/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.Oracle/NeoServiceLayer.Services.Oracle.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.Oracle/", "src/Services/NeoServiceLayer.Services.Oracle/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.Oracle/NeoServiceLayer.Services.Oracle.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.Oracle/NeoServiceLayer.Services.Oracle.csproj" \
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
ENV SERVICE_NAME=OracleService
ENV SERVICE_TYPE=Oracle
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - Oracle Service"
LABEL service.type="oracle"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/OracleServiceHost.cs
RUN sed -i "s/{ServiceName}/Oracle/g" /app/OracleServiceHost.cs && \
    sed -i "s/{servicename}/oracle/g" /app/OracleServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.Oracle.dll"]
