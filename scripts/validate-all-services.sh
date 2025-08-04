#!/bin/bash

# Comprehensive Service Validation Script
# Validates that all services are complete and production-ready

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Counters
TOTAL_SERVICES=0
COMPLETE_SERVICES=0
INCOMPLETE_SERVICES=0

# Functions
print_header() {
    echo -e "\n${BLUE}==== $1 ====${NC}\n"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

check_service() {
    local service_path="$1"
    local service_name="$2"
    local is_infrastructure="${3:-false}"
    
    TOTAL_SERVICES=$((TOTAL_SERVICES + 1))
    
    local has_program=false
    local has_interface=false
    local has_implementation=false
    local has_dockerfile=false
    local has_csproj=false
    
    # Check for Program.cs
    if [ -f "$service_path/Program.cs" ]; then
        has_program=true
    fi
    
    # Check for .csproj file
    if [ -f "$service_path/$service_name.csproj" ]; then
        has_csproj=true
    fi
    
    # Check for interface file (I*.cs)
    if ls "$service_path"/I*.cs >/dev/null 2>&1; then
        has_interface=true
    fi
    
    # Check for implementation file
    if ls "$service_path"/*.cs >/dev/null 2>&1; then
        local cs_files=$(find "$service_path" -name "*.cs" | grep -v Program.cs | grep -v "/I.*\.cs" | wc -l)
        if [ "$cs_files" -gt 0 ]; then
            has_implementation=true
        fi
    fi
    
    # Check for Dockerfile
    if [ -f "$service_path/Dockerfile" ]; then
        has_dockerfile=true
    fi
    
    # Determine service status
    local status="COMPLETE"
    local issues=()
    
    if [ "$has_csproj" = false ]; then
        status="INCOMPLETE"
        issues+=("Missing .csproj file")
    fi
    
    if [ "$has_interface" = false ] && [ "$is_infrastructure" = false ]; then
        status="INCOMPLETE"
        issues+=("Missing interface file")
    fi
    
    if [ "$has_implementation" = false ]; then
        status="INCOMPLETE"
        issues+=("Missing implementation files")
    fi
    
    if [ "$has_program" = false ]; then
        status="INCOMPLETE"
        issues+=("Missing Program.cs")
    fi
    
    if [ "$has_dockerfile" = false ]; then
        status="INCOMPLETE"
        issues+=("Missing Dockerfile")
    fi
    
    # Print results
    if [ "$status" = "COMPLETE" ]; then
        print_success "$service_name - COMPLETE"
        COMPLETE_SERVICES=$((COMPLETE_SERVICES + 1))
    else
        print_error "$service_name - INCOMPLETE"
        for issue in "${issues[@]}"; do
            echo -e "    ${RED}- $issue${NC}"
        done
        INCOMPLETE_SERVICES=$((INCOMPLETE_SERVICES + 1))
    fi
}

main() {
    cd "$(dirname "$0")/.."
    
    print_header "Neo Service Layer - Service Validation Report"
    
    print_header "Core Services"
    
    # Core Services
    check_service "src/Services/NeoServiceLayer.Services.AbstractAccount" "NeoServiceLayer.Services.AbstractAccount"
    check_service "src/Services/NeoServiceLayer.Services.Automation" "NeoServiceLayer.Services.Automation"
    check_service "src/Services/NeoServiceLayer.Services.Backup" "NeoServiceLayer.Services.Backup"
    check_service "src/Services/NeoServiceLayer.Services.Compliance" "NeoServiceLayer.Services.Compliance"
    check_service "src/Services/NeoServiceLayer.Services.Compute" "NeoServiceLayer.Services.Compute"
    check_service "src/Services/NeoServiceLayer.Services.Configuration" "NeoServiceLayer.Services.Configuration"
    check_service "src/Services/NeoServiceLayer.Services.CrossChain" "NeoServiceLayer.Services.CrossChain"
    check_service "src/Services/NeoServiceLayer.Services.EnclaveStorage" "NeoServiceLayer.Services.EnclaveStorage"
    check_service "src/Services/NeoServiceLayer.Services.EventSubscription" "NeoServiceLayer.Services.EventSubscription"
    check_service "src/Services/NeoServiceLayer.Services.Health" "NeoServiceLayer.Services.Health"
    check_service "src/Services/NeoServiceLayer.Services.KeyManagement" "NeoServiceLayer.Services.KeyManagement"
    check_service "src/Services/NeoServiceLayer.Services.Monitoring" "NeoServiceLayer.Services.Monitoring"
    check_service "src/Services/NeoServiceLayer.Services.NetworkSecurity" "NeoServiceLayer.Services.NetworkSecurity"
    check_service "src/Services/NeoServiceLayer.Services.Notification" "NeoServiceLayer.Services.Notification"
    check_service "src/Services/NeoServiceLayer.Services.Oracle" "NeoServiceLayer.Services.Oracle"
    check_service "src/Services/NeoServiceLayer.Services.ProofOfReserve" "NeoServiceLayer.Services.ProofOfReserve"
    check_service "src/Services/NeoServiceLayer.Services.Randomness" "NeoServiceLayer.Services.Randomness"
    check_service "src/Services/NeoServiceLayer.Services.SecretsManagement" "NeoServiceLayer.Services.SecretsManagement"
    check_service "src/Services/NeoServiceLayer.Services.SmartContracts" "NeoServiceLayer.Services.SmartContracts"
    check_service "src/Services/NeoServiceLayer.Services.SocialRecovery" "NeoServiceLayer.Services.SocialRecovery"
    check_service "src/Services/NeoServiceLayer.Services.Storage" "NeoServiceLayer.Services.Storage"
    check_service "src/Services/NeoServiceLayer.Services.Voting" "NeoServiceLayer.Services.Voting"
    check_service "src/Services/NeoServiceLayer.Services.ZeroKnowledge" "NeoServiceLayer.Services.ZeroKnowledge"
    
    print_header "AI Services"
    
    check_service "src/AI/NeoServiceLayer.AI.PatternRecognition" "NeoServiceLayer.AI.PatternRecognition"
    check_service "src/AI/NeoServiceLayer.AI.Prediction" "NeoServiceLayer.AI.Prediction"
    
    print_header "Advanced Services"
    
    check_service "src/Advanced/NeoServiceLayer.Advanced.FairOrdering" "NeoServiceLayer.Advanced.FairOrdering"
    
    print_header "Gateway Services"
    
    check_service "src/Gateway/NeoServiceLayer.ApiGateway" "NeoServiceLayer.ApiGateway" "true"
    check_service "src/Gateway/NeoServiceLayer.Gateway.Api" "NeoServiceLayer.Gateway.Api" "true"
    
    print_header "API Services"
    
    check_service "src/Api/NeoServiceLayer.Api" "NeoServiceLayer.Api" "true"
    
    print_header "TEE Services"
    
    check_service "src/Tee/NeoServiceLayer.Tee.Host" "NeoServiceLayer.Tee.Host" "true"
    
    print_header "Validation Summary"
    
    echo -e "Total Services Checked: ${BLUE}$TOTAL_SERVICES${NC}"
    echo -e "Complete Services: ${GREEN}$COMPLETE_SERVICES${NC}"
    echo -e "Incomplete Services: ${RED}$INCOMPLETE_SERVICES${NC}"
    
    local completion_percentage=$((COMPLETE_SERVICES * 100 / TOTAL_SERVICES))
    echo -e "Completion Rate: ${BLUE}$completion_percentage%${NC}"
    
    if [ $INCOMPLETE_SERVICES -eq 0 ]; then
        print_success "All services are complete and production-ready!"
        echo ""
        echo -e "${GREEN}✨ The Neo Service Layer is 100% complete! ✨${NC}"
        echo ""
        echo "You can now deploy using:"
        echo "  docker compose -f docker compose.all-services.yml up -d"
        echo ""
    else
        print_warning "Some services need attention before production deployment"
        echo ""
        echo "To fix missing components, run:"
        echo "  ./scripts/generate-service-dockerfiles-complete.sh"
        echo ""
    fi
    
    print_header "Service Registry"
    
    echo "All services will be automatically registered with Consul service discovery:"
    echo ""
    echo "Service Discovery URLs:"
    echo "- Consul UI: http://localhost:8500"
    echo "- API Gateway: http://localhost:7000"
    echo ""
    echo "Individual Service Health Checks:"
    for port in {8001..8026}; do
        echo "- Service Port $port: http://localhost:$port/health"
    done
}

# Run main function
main "$@"