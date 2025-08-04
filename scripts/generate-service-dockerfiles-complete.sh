#!/bin/bash

# Generate Dockerfiles for all services that don't have them

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Create Dockerfile template function
create_dockerfile() {
    local service_path="$1"
    local service_name="$2"
    local project_file="$3"
    local port="${4:-8080}"
    
    cat > "$service_path/Dockerfile" << EOF
# Multi-stage build for $service_name
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY *.sln .
COPY Directory.*.props .
COPY Directory.*.targets .
COPY NuGet.Config .

# Copy all project files for proper restore
COPY src/ src/
COPY tests/ tests/

# Restore packages
RUN dotnet restore "$project_file"

# Build the service
RUN dotnet build "$project_file" -c Release --no-restore

# Publish the service
RUN dotnet publish "$project_file" -c Release --no-build -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Create non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \\
    CMD curl -f http://localhost:$port/health || exit 1

# Expose port
EXPOSE $port

# Set environment variables
ENV ASPNETCORE_URLS=http://+:$port
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "${service_name}.dll"]
EOF
    
    print_success "Created Dockerfile for $service_name"
}

# Main function
main() {
    cd "$(dirname "$0")/.."
    
    print_warning "Generating Dockerfiles for all services..."
    
    # Core Services
    services=(
        "src/Services/NeoServiceLayer.Services.AbstractAccount:NeoServiceLayer.Services.AbstractAccount:8001"
        "src/Services/NeoServiceLayer.Services.Automation:NeoServiceLayer.Services.Automation:8002"
        "src/Services/NeoServiceLayer.Services.Backup:NeoServiceLayer.Services.Backup:8003"
        "src/Services/NeoServiceLayer.Services.Compliance:NeoServiceLayer.Services.Compliance:8004"
        "src/Services/NeoServiceLayer.Services.Compute:NeoServiceLayer.Services.Compute:8005"
        "src/Services/NeoServiceLayer.Services.Configuration:NeoServiceLayer.Services.Configuration:8006"
        "src/Services/NeoServiceLayer.Services.CrossChain:NeoServiceLayer.Services.CrossChain:8007"
        "src/Services/NeoServiceLayer.Services.EnclaveStorage:NeoServiceLayer.Services.EnclaveStorage:8008"
        "src/Services/NeoServiceLayer.Services.EventSubscription:NeoServiceLayer.Services.EventSubscription:8009"
        "src/Services/NeoServiceLayer.Services.Health:NeoServiceLayer.Services.Health:8010"
        "src/Services/NeoServiceLayer.Services.KeyManagement:NeoServiceLayer.Services.KeyManagement:8011"
        "src/Services/NeoServiceLayer.Services.Monitoring:NeoServiceLayer.Services.Monitoring:8012"
        "src/Services/NeoServiceLayer.Services.NetworkSecurity:NeoServiceLayer.Services.NetworkSecurity:8013"
        "src/Services/NeoServiceLayer.Services.Notification:NeoServiceLayer.Services.Notification:8014"
        "src/Services/NeoServiceLayer.Services.Oracle:NeoServiceLayer.Services.Oracle:8015"
        "src/Services/NeoServiceLayer.Services.ProofOfReserve:NeoServiceLayer.Services.ProofOfReserve:8016"
        "src/Services/NeoServiceLayer.Services.Randomness:NeoServiceLayer.Services.Randomness:8017"
        "src/Services/NeoServiceLayer.Services.SecretsManagement:NeoServiceLayer.Services.SecretsManagement:8018"
        "src/Services/NeoServiceLayer.Services.SmartContracts:NeoServiceLayer.Services.SmartContracts:8019"
        "src/Services/NeoServiceLayer.Services.SocialRecovery:NeoServiceLayer.Services.SocialRecovery:8020"
        "src/Services/NeoServiceLayer.Services.Storage:NeoServiceLayer.Services.Storage:8021"
        "src/Services/NeoServiceLayer.Services.Voting:NeoServiceLayer.Services.Voting:8022"
        "src/Services/NeoServiceLayer.Services.ZeroKnowledge:NeoServiceLayer.Services.ZeroKnowledge:8023"
    )
    
    # AI Services
    ai_services=(
        "src/AI/NeoServiceLayer.AI.PatternRecognition:NeoServiceLayer.AI.PatternRecognition:8024"
        "src/AI/NeoServiceLayer.AI.Prediction:NeoServiceLayer.AI.Prediction:8025"
    )
    
    # Advanced Services
    advanced_services=(
        "src/Advanced/NeoServiceLayer.Advanced.FairOrdering:NeoServiceLayer.Advanced.FairOrdering:8026"
    )
    
    # Process all services
    all_services=("${services[@]}" "${ai_services[@]}" "${advanced_services[@]}")
    
    for service_info in "${all_services[@]}"; do
        IFS=':' read -r service_path service_name port <<< "$service_info"
        
        if [ ! -f "$service_path/Dockerfile" ]; then
            project_file="$service_path/$service_name.csproj"
            create_dockerfile "$service_path" "$service_name" "$project_file" "$port"
        else
            print_warning "Dockerfile already exists for $service_name"
        fi
    done
    
    print_success "All service Dockerfiles have been generated!"
}

# Run main function
main "$@"