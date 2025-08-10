#!/bin/bash
# Verify that all services properly use the service framework

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${GREEN}Neo Service Layer - Service Framework Usage Verification${NC}"
echo "======================================================="

# Counters
TOTAL_SERVICES=0
FRAMEWORK_COMPLIANT=0
NON_COMPLIANT=0
WARNINGS=0

# Arrays to store results
declare -a COMPLIANT_SERVICES=()
declare -a NON_COMPLIANT_SERVICES=()
declare -a WARNING_SERVICES=()

# Function to check service compliance
check_service() {
    local service_dir=$1
    local service_name=$(basename "$service_dir" | sed 's/NeoServiceLayer.Services.//')
    
    ((TOTAL_SERVICES++))
    
    local issues=""
    local warnings=""
    
    # Check for service implementation
    if ! find "$service_dir" -name "${service_name}Service.cs" -o -name "*ServiceCore.cs" -o -name "*Service.Core.cs" | grep -q .; then
        issues="${issues}Missing service implementation file\n"
    else
        # Check if inherits from ServiceBase
        if ! grep -q ": \(ServiceBase\|EnclaveServiceBase\|BlockchainServiceBase\|AIServiceBase\|CryptographicServiceBase\|DataServiceBase\|PersistentServiceBase\|EnclaveBlockchainServiceBase\)" "$service_dir"/*.cs 2>/dev/null; then
            issues="${issues}Does not inherit from ServiceBase or derivatives\n"
        fi
    fi
    
    # Check for interface
    if ! find "$service_dir" -name "I${service_name}Service.cs" | grep -q .; then
        warnings="${warnings}Missing service interface (I${service_name}Service.cs)\n"
    fi
    
    # Check for Program.cs
    if ! [ -f "$service_dir/Program.cs" ]; then
        issues="${issues}Missing Program.cs\n"
    else
        # Check if uses MicroserviceHost
        if ! grep -q "MicroserviceHost" "$service_dir/Program.cs" 2>/dev/null; then
            warnings="${warnings}Program.cs does not use MicroserviceHost\n"
        fi
    fi
    
    # Check for proper lifecycle methods
    local service_file=$(find "$service_dir" -name "${service_name}Service.cs" -o -name "*ServiceCore.cs" -o -name "*Service.Core.cs" | head -1)
    if [ -n "$service_file" ] && [ -f "$service_file" ]; then
        if ! grep -q "OnInitializeAsync" "$service_file"; then
            warnings="${warnings}Missing OnInitializeAsync override\n"
        fi
        if ! grep -q "OnStartAsync" "$service_file"; then
            warnings="${warnings}Missing OnStartAsync override\n"
        fi
        if ! grep -q "OnStopAsync" "$service_file"; then
            warnings="${warnings}Missing OnStopAsync override\n"
        fi
        if ! grep -q "OnGetHealthAsync" "$service_file"; then
            warnings="${warnings}Missing OnGetHealthAsync override\n"
        fi
    fi
    
    # Check for configuration files
    if ! [ -f "$service_dir/appsettings.json" ]; then
        warnings="${warnings}Missing appsettings.json\n"
    fi
    
    # Check for Dockerfile
    if ! [ -f "$service_dir/Dockerfile" ]; then
        warnings="${warnings}Missing Dockerfile\n"
    fi
    
    # Check for README
    if ! [ -f "$service_dir/README.md" ]; then
        warnings="${warnings}Missing README.md\n"
    fi
    
    # Report results
    if [ -z "$issues" ]; then
        if [ -z "$warnings" ]; then
            echo -e "${GREEN}✓ $service_name${NC} - Fully compliant"
            ((FRAMEWORK_COMPLIANT++))
            COMPLIANT_SERVICES+=("$service_name")
        else
            echo -e "${YELLOW}⚠ $service_name${NC} - Compliant with warnings:"
            echo -e "${YELLOW}$warnings${NC}"
            ((FRAMEWORK_COMPLIANT++))
            ((WARNINGS++))
            WARNING_SERVICES+=("$service_name")
        fi
    else
        echo -e "${RED}✗ $service_name${NC} - Non-compliant:"
        echo -e "${RED}$issues${NC}"
        if [ -n "$warnings" ]; then
            echo -e "${YELLOW}Additional warnings:${NC}"
            echo -e "${YELLOW}$warnings${NC}"
        fi
        ((NON_COMPLIANT++))
        NON_COMPLIANT_SERVICES+=("$service_name")
    fi
    echo ""
}

# Check all services
echo -e "${BLUE}Checking all services...${NC}"
echo ""

for service_dir in src/Services/NeoServiceLayer.Services.*; do
    if [ -d "$service_dir" ]; then
        # Skip template directories
        if [[ "$service_dir" == *"ServiceHostTemplate"* ]] || [[ "$service_dir" == *"Abstractions"* ]]; then
            continue
        fi
        
        check_service "$service_dir"
    fi
done

# Check AI services
echo -e "${BLUE}Checking AI services...${NC}"
echo ""

for service_dir in src/AI/NeoServiceLayer.AI.*; do
    if [ -d "$service_dir" ]; then
        check_service "$service_dir"
    fi
done

# Summary
echo -e "${YELLOW}===== FRAMEWORK COMPLIANCE SUMMARY =====${NC}"
echo "Total Services: $TOTAL_SERVICES"
echo -e "${GREEN}Framework Compliant: $FRAMEWORK_COMPLIANT${NC}"
echo -e "${RED}Non-Compliant: $NON_COMPLIANT${NC}"
echo -e "${YELLOW}Services with Warnings: $WARNINGS${NC}"
echo ""

# Calculate compliance percentage
if [ $TOTAL_SERVICES -gt 0 ]; then
    COMPLIANCE_PERCENT=$((FRAMEWORK_COMPLIANT * 100 / TOTAL_SERVICES))
    echo "Compliance Rate: $COMPLIANCE_PERCENT%"
    
    if [ $COMPLIANCE_PERCENT -eq 100 ]; then
        echo -e "${GREEN}✅ All services are framework compliant!${NC}"
    elif [ $COMPLIANCE_PERCENT -ge 80 ]; then
        echo -e "${YELLOW}⚠️  Most services are compliant, but some need attention.${NC}"
    else
        echo -e "${RED}❌ Many services are not framework compliant.${NC}"
    fi
fi

# List compliant services
if [ ${#COMPLIANT_SERVICES[@]} -gt 0 ]; then
    echo -e "\n${GREEN}Fully Compliant Services:${NC}"
    for service in "${COMPLIANT_SERVICES[@]}"; do
        echo "  ✓ $service"
    done
fi

# List services with warnings
if [ ${#WARNING_SERVICES[@]} -gt 0 ]; then
    echo -e "\n${YELLOW}Services with Warnings:${NC}"
    for service in "${WARNING_SERVICES[@]}"; do
        echo "  ⚠ $service"
    done
fi

# List non-compliant services
if [ ${#NON_COMPLIANT_SERVICES[@]} -gt 0 ]; then
    echo -e "\n${RED}Non-Compliant Services:${NC}"
    for service in "${NON_COMPLIANT_SERVICES[@]}"; do
        echo "  ✗ $service"
    done
fi

# Recommendations
if [ $NON_COMPLIANT -gt 0 ] || [ $WARNINGS -gt 0 ]; then
    echo -e "\n${BLUE}Recommendations:${NC}"
    
    if [ $NON_COMPLIANT -gt 0 ]; then
        echo "1. Update non-compliant services to inherit from ServiceBase"
        echo "2. Implement required lifecycle methods (OnInitializeAsync, OnStartAsync, etc.)"
        echo "3. Use MicroserviceHost for service hosting"
    fi
    
    if [ $WARNINGS -gt 0 ]; then
        echo "4. Add missing configuration files (appsettings.json)"
        echo "5. Create Dockerfiles for containerization"
        echo "6. Add README.md documentation"
        echo "7. Define service interfaces (IServiceName)"
    fi
    
    echo -e "\nUse the service generator script to create new compliant services:"
    echo "  ./scripts/create-new-service.sh ServiceName [options]"
fi

# Exit with appropriate code
if [ $NON_COMPLIANT -gt 0 ]; then
    exit 1
else
    exit 0
fi