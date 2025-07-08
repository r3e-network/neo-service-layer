#!/bin/bash

# Script to generate Dockerfiles for all microservices

SERVICES_DIR="src/Services"
DOCKER_DIR="docker/microservices/services"

# Create docker directory if it doesn't exist
mkdir -p "$DOCKER_DIR"

# List of services to generate Dockerfiles for
SERVICES=(
    "Notification"
    "Configuration"
    "Backup"
    "ProofOfReserve"
    "SmartContracts"
    "CrossChain"
    "Monitoring"
    "Health"
    "KeyManagement"
    "Automation"
    "Storage"
    "Oracle"
    "Randomness"
    "Voting"
    "AbstractAccount"
    "ZeroKnowledge"
    "Compliance"
    "SecretsManagement"
    "SocialRecovery"
    "Compute"
    "EventSubscription"
    "EnclaveStorage"
    "NetworkSecurity"
)

# Generate Dockerfile for each service
for SERVICE in "${SERVICES[@]}"; do
    SERVICE_LOWER=$(echo "$SERVICE" | tr '[:upper:]' '[:lower:]' | sed 's/\([A-Z]\)/-\1/g' | sed 's/^-//')
    SERVICE_DIR="$DOCKER_DIR/$SERVICE_LOWER"
    
    echo "Generating Dockerfile for $SERVICE service..."
    
    mkdir -p "$SERVICE_DIR"
    
    cat > "$SERVICE_DIR/Dockerfile" << EOF
# Dockerfile for $SERVICE Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM \${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.$SERVICE/NeoServiceLayer.Services.$SERVICE.csproj", "src/Services/NeoServiceLayer.Services.$SERVICE/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.$SERVICE/NeoServiceLayer.Services.$SERVICE.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.$SERVICE/", "src/Services/NeoServiceLayer.Services.$SERVICE/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.$SERVICE/NeoServiceLayer.Services.$SERVICE.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.$SERVICE/NeoServiceLayer.Services.$SERVICE.csproj" \\
    -c Release \\
    -o /app/publish \\
    --no-restore \\
    /p:UseAppHost=false

# Runtime stage
FROM \${RUNTIME_BASE_IMAGE} AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Service-specific environment variables
ENV SERVICE_NAME=${SERVICE}Service
ENV SERVICE_TYPE=$SERVICE
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - $SERVICE Service"
LABEL service.type="$SERVICE_LOWER"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/${SERVICE}ServiceHost.cs
RUN sed -i "s/{ServiceName}/$SERVICE/g" /app/${SERVICE}ServiceHost.cs && \\
    sed -i "s/{servicename}/$SERVICE_LOWER/g" /app/${SERVICE}ServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.$SERVICE.dll"]
EOF

    # Generate docker-compose snippet
    cat > "$SERVICE_DIR/docker-compose.snippet.yml" << EOF
  $SERVICE_LOWER-service:
    build:
      context: .
      dockerfile: docker/microservices/services/$SERVICE_LOWER/Dockerfile
    container_name: neo-$SERVICE_LOWER-service
    <<: *service-defaults
    environment:
      <<: *common-variables
      SERVICE_NAME: ${SERVICE}Service
      SERVICE_TYPE: $SERVICE
    ports:
      - "\${${SERVICE^^}_PORT:-5020}:80"
EOF

done

echo "Dockerfile generation complete!"
echo "Generated Dockerfiles for ${#SERVICES[@]} services"