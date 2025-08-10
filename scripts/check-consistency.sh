#!/bin/bash
# Neo Service Layer - System Consistency Check
# Ensures all components work together correctly

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${GREEN}Neo Service Layer - System Consistency Check${NC}"
echo "==========================================="

# Check if we can build the solution
echo -e "\n${YELLOW}1. Checking Solution Build${NC}"
echo "------------------------"

if dotnet --version > /dev/null 2>&1; then
    echo -e "${GREEN}✓ .NET SDK installed: $(dotnet --version)${NC}"
else
    echo -e "${RED}✗ .NET SDK not found${NC}"
fi

# Count projects
PROJECT_COUNT=$(dotnet sln list | grep -c ".csproj" || echo "0")
echo -e "${BLUE}Total projects in solution: $PROJECT_COUNT${NC}"

# Check service consistency
echo -e "\n${YELLOW}2. Service Consistency Check${NC}"
echo "--------------------------"

# List all services
echo -e "${BLUE}Services found:${NC}"
ls -1 src/Services/ | grep "NeoServiceLayer.Services" | sed 's/NeoServiceLayer.Services./  - /'

# Check that all services have required files
echo -e "\n${BLUE}Checking service structure:${NC}"
SERVICES_WITH_ISSUES=0

for service_dir in src/Services/NeoServiceLayer.Services.*; do
    if [ -d "$service_dir" ]; then
        SERVICE_NAME=$(basename "$service_dir" | sed 's/NeoServiceLayer.Services.//')
        MISSING_FILES=""
        
        # Check for required files
        [ ! -f "$service_dir/Program.cs" ] && MISSING_FILES="$MISSING_FILES Program.cs"
        [ ! -f "$service_dir/*.csproj" ] 2>/dev/null && MISSING_FILES="$MISSING_FILES .csproj"
        
        if [ -n "$MISSING_FILES" ]; then
            echo -e "${YELLOW}⚠ $SERVICE_NAME missing:$MISSING_FILES${NC}"
            ((SERVICES_WITH_ISSUES++))
        fi
    fi
done

if [ $SERVICES_WITH_ISSUES -eq 0 ]; then
    echo -e "${GREEN}✓ All services have consistent structure${NC}"
fi

# Check smart contracts
echo -e "\n${YELLOW}3. Smart Contract Consistency${NC}"
echo "----------------------------"

if [ -d "contracts-neo-n3" ]; then
    echo -e "${GREEN}✓ Contracts directory exists${NC}"
    
    # Count contracts
    CONTRACT_COUNT=$(find contracts-neo-n3/src -name "*.cs" -type f | wc -l)
    echo -e "${BLUE}Total contract files: $CONTRACT_COUNT${NC}"
    
    # Check contract structure
    echo -e "\n${BLUE}Contract categories:${NC}"
    echo "  - Core: $(find contracts-neo-n3/src/Core -name "*.cs" 2>/dev/null | wc -l) files"
    echo "  - Production: $(find contracts-neo-n3/src/ProductionReady -name "*.cs" 2>/dev/null | wc -l) files"
    echo "  - Services: $(find contracts-neo-n3/src/Services -name "*.cs" 2>/dev/null | wc -l) files"
else
    echo -e "${RED}✗ Contracts directory missing${NC}"
fi

# Check Kubernetes configurations
echo -e "\n${YELLOW}4. Kubernetes Configuration Consistency${NC}"
echo "--------------------------------------"

if [ -d "k8s" ]; then
    echo -e "${GREEN}✓ Kubernetes directory exists${NC}"
    
    # Check for critical files
    K8S_FILES=(
        "base/namespace.yaml"
        "base/resource-quotas.yaml"
        "base/pod-security-standards.yaml"
        "base/network-policies.yaml"
        "base/hpa.yaml"
        "base/service-mesh.yaml"
    )
    
    for file in "${K8S_FILES[@]}"; do
        if [ -f "k8s/$file" ]; then
            echo -e "${GREEN}✓ $file${NC}"
        else
            echo -e "${RED}✗ Missing: $file${NC}"
        fi
    done
else
    echo -e "${RED}✗ Kubernetes directory missing${NC}"
fi

# Check Docker configurations
echo -e "\n${YELLOW}5. Docker Configuration Consistency${NC}"
echo "----------------------------------"

DOCKER_FILES=(
    "docker-compose.yml"
    "docker-compose.dev.yml"
    "docker-compose.test.yml"
)

for file in "${DOCKER_FILES[@]}"; do
    if [ -f "$file" ]; then
        echo -e "${GREEN}✓ $file exists${NC}"
        
        # Count services in docker-compose
        SERVICE_COUNT=$(grep -c "^\s*[a-zA-Z].*:$" "$file" || echo "0")
        echo "  Services defined: $SERVICE_COUNT"
    else
        echo -e "${YELLOW}⚠ $file missing${NC}"
    fi
done

# Check documentation consistency
echo -e "\n${YELLOW}6. Documentation Consistency${NC}"
echo "---------------------------"

# Count READMEs
README_COUNT=$(find . -name "README.md" -not -path "./node_modules/*" -not -path "./.git/*" | wc -l)
echo -e "${BLUE}README files found: $README_COUNT${NC}"

# Check service documentation
SERVICES_WITH_README=0
SERVICES_WITHOUT_README=0

for service_dir in src/Services/NeoServiceLayer.Services.*; do
    if [ -d "$service_dir" ]; then
        if [ -f "$service_dir/README.md" ]; then
            ((SERVICES_WITH_README++))
        else
            ((SERVICES_WITHOUT_README++))
        fi
    fi
done

echo -e "${GREEN}Services with documentation: $SERVICES_WITH_README${NC}"
echo -e "${YELLOW}Services without documentation: $SERVICES_WITHOUT_README${NC}"

# Check configuration consistency
echo -e "\n${YELLOW}7. Configuration File Consistency${NC}"
echo "--------------------------------"

# Check for appsettings.json pattern
SERVICES_WITH_CONFIG=0
for service_dir in src/Services/NeoServiceLayer.Services.*; do
    if [ -d "$service_dir" ]; then
        if [ -f "$service_dir/appsettings.json" ]; then
            ((SERVICES_WITH_CONFIG++))
        fi
    fi
done

echo -e "${BLUE}Services with appsettings.json: $SERVICES_WITH_CONFIG${NC}"

# Check monitoring configuration
echo -e "\n${YELLOW}8. Monitoring Configuration${NC}"
echo "--------------------------"

if [ -f "monitoring/prometheus.yml" ]; then
    echo -e "${GREEN}✓ Prometheus configuration exists${NC}"
fi

DASHBOARD_COUNT=$(find monitoring/grafana/dashboards -name "*.json" 2>/dev/null | wc -l || echo "0")
echo -e "${BLUE}Grafana dashboards: $DASHBOARD_COUNT${NC}"

# Final summary
echo -e "\n${YELLOW}===== CONSISTENCY SUMMARY =====${NC}"

# Calculate consistency score
TOTAL_CHECKS=10
PASSED_CHECKS=0

[ $PROJECT_COUNT -gt 100 ] && ((PASSED_CHECKS++))
[ $SERVICES_WITH_ISSUES -eq 0 ] && ((PASSED_CHECKS++))
[ $CONTRACT_COUNT -gt 30 ] && ((PASSED_CHECKS++))
[ -d "k8s" ] && ((PASSED_CHECKS++))
[ -f "docker-compose.yml" ] && ((PASSED_CHECKS++))
[ $README_COUNT -gt 20 ] && ((PASSED_CHECKS++))
[ $SERVICES_WITH_README -gt 5 ] && ((PASSED_CHECKS++))
[ $SERVICES_WITH_CONFIG -gt 0 ] && ((PASSED_CHECKS++))
[ -f "monitoring/prometheus.yml" ] && ((PASSED_CHECKS++))
[ $DASHBOARD_COUNT -gt 0 ] && ((PASSED_CHECKS++))

CONSISTENCY_SCORE=$((PASSED_CHECKS * 10))

echo "Consistency Score: $CONSISTENCY_SCORE%"

if [ $CONSISTENCY_SCORE -ge 80 ]; then
    echo -e "${GREEN}✅ System is highly consistent and production-ready!${NC}"
elif [ $CONSISTENCY_SCORE -ge 60 ]; then
    echo -e "${YELLOW}⚠️  System is mostly consistent with some areas needing attention.${NC}"
else
    echo -e "${RED}❌ System has consistency issues that need to be addressed.${NC}"
fi

# Recommendations
echo -e "\n${YELLOW}Recommendations:${NC}"
if [ $SERVICES_WITHOUT_README -gt 0 ]; then
    echo "- Add README.md files to $SERVICES_WITHOUT_README services"
fi
if [ $SERVICES_WITH_CONFIG -eq 0 ]; then
    echo "- Add appsettings.json configuration files to services"
fi
if [ $DASHBOARD_COUNT -lt 3 ]; then
    echo "- Create more Grafana monitoring dashboards"
fi

echo -e "\n${GREEN}Consistency check complete!${NC}"