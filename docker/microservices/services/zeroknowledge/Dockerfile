# Dockerfile for ZeroKnowledge Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.ZeroKnowledge/NeoServiceLayer.Services.ZeroKnowledge.csproj", "src/Services/NeoServiceLayer.Services.ZeroKnowledge/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.ZeroKnowledge/NeoServiceLayer.Services.ZeroKnowledge.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.ZeroKnowledge/", "src/Services/NeoServiceLayer.Services.ZeroKnowledge/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.ZeroKnowledge/NeoServiceLayer.Services.ZeroKnowledge.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.ZeroKnowledge/NeoServiceLayer.Services.ZeroKnowledge.csproj" \
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
ENV SERVICE_NAME=ZeroKnowledgeService
ENV SERVICE_TYPE=ZeroKnowledge
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - ZeroKnowledge Service"
LABEL service.type="zeroknowledge"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/ZeroKnowledgeServiceHost.cs
RUN sed -i "s/{ServiceName}/ZeroKnowledge/g" /app/ZeroKnowledgeServiceHost.cs && \
    sed -i "s/{servicename}/zeroknowledge/g" /app/ZeroKnowledgeServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.ZeroKnowledge.dll"]
