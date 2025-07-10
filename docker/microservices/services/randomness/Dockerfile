# Dockerfile for Randomness Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.Randomness/NeoServiceLayer.Services.Randomness.csproj", "src/Services/NeoServiceLayer.Services.Randomness/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.Randomness/NeoServiceLayer.Services.Randomness.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.Randomness/", "src/Services/NeoServiceLayer.Services.Randomness/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.Randomness/NeoServiceLayer.Services.Randomness.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.Randomness/NeoServiceLayer.Services.Randomness.csproj" \
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
ENV SERVICE_NAME=RandomnessService
ENV SERVICE_TYPE=Randomness
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - Randomness Service"
LABEL service.type="randomness"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/RandomnessServiceHost.cs
RUN sed -i "s/{ServiceName}/Randomness/g" /app/RandomnessServiceHost.cs && \
    sed -i "s/{servicename}/randomness/g" /app/RandomnessServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.Randomness.dll"]
