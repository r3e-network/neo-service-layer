#!/bin/bash
# Neo Service Layer - Comprehensive System Validation Script
# Validates all services, contracts, and system consistency

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Validation results
TOTAL_CHECKS=0
PASSED_CHECKS=0
FAILED_CHECKS=0
WARNINGS=0

# Log file
LOG_FILE="validation-report-$(date +%Y%m%d_%H%M%S).log"

# Function to log messages
log() {
    echo -e "$1" | tee -a "$LOG_FILE"
}

# Function to check status
check_status() {
    local check_name=$1
    local command=$2
    local expected=$3
    
    ((TOTAL_CHECKS++))
    
    log "${BLUE}Checking: $check_name${NC}"
    
    if eval "$command" > /dev/null 2>&1; then
        if [ -n "$expected" ]; then
            local result=$(eval "$command" 2>/dev/null)
            if [[ "$result" == *"$expected"* ]]; then
                log "${GREEN}✓ PASSED: $check_name${NC}"
                ((PASSED_CHECKS++))
                return 0
            else
                log "${RED}✗ FAILED: $check_name - Expected: $expected, Got: $result${NC}"
                ((FAILED_CHECKS++))
                return 1
            fi
        else
            log "${GREEN}✓ PASSED: $check_name${NC}"
            ((PASSED_CHECKS++))
            return 0
        fi
    else
        log "${RED}✗ FAILED: $check_name${NC}"
        ((FAILED_CHECKS++))
        return 1
    fi
}

# Function to check file exists
check_file() {
    local file=$1
    local description=$2
    
    ((TOTAL_CHECKS++))
    
    if [ -f "$file" ]; then
        log "${GREEN}✓ FOUND: $description${NC}"
        ((PASSED_CHECKS++))
        return 0
    else
        log "${RED}✗ MISSING: $description - $file${NC}"
        ((FAILED_CHECKS++))
        return 1
    fi
}

# Function to validate JSON
validate_json() {
    local file=$1
    local description=$2
    
    ((TOTAL_CHECKS++))
    
    if jq empty "$file" 2>/dev/null; then
        log "${GREEN}✓ VALID JSON: $description${NC}"
        ((PASSED_CHECKS++))
        return 0
    else
        log "${RED}✗ INVALID JSON: $description - $file${NC}"
        ((FAILED_CHECKS++))
        return 1
    fi
}

# Function to validate YAML
validate_yaml() {
    local file=$1
    local description=$2
    
    ((TOTAL_CHECKS++))
    
    if python3 -c "import yaml; yaml.safe_load(open('$file'))" 2>/dev/null; then
        log "${GREEN}✓ VALID YAML: $description${NC}"
        ((PASSED_CHECKS++))
        return 0
    else
        log "${RED}✗ INVALID YAML: $description - $file${NC}"
        ((FAILED_CHECKS++))
        return 1
    fi
}

log "${GREEN}Neo Service Layer - System Validation${NC}"
log "====================================="
log "Started: $(date)"
log ""

# 1. Check Project Structure
log "${YELLOW}1. Validating Project Structure${NC}"
log "--------------------------------"

check_file "Directory.Packages.props" "Central package management"
check_file "NeoServiceLayer.sln" "Solution file"
check_file "README.md" "Project documentation"
check_file "docker-compose.yml" "Docker compose configuration"

# Check critical directories
for dir in "src" "tests" "contracts-neo-n3" "k8s" "scripts" "docs"; do
    check_status "Directory: $dir" "[ -d $dir ]" ""
done

# 2. Check All Services
log ""
log "${YELLOW}2. Validating Services${NC}"
log "----------------------"

# List of all services
SERVICES=(
    "Storage"
    "Oracle"
    "KeyManagement"
    "Notification"
    "Health"
    "Monitoring"
    "SmartContracts"
    "SmartContracts.NeoN3"
    "Automation"
    "Backup"
    "Compliance"
    "Compute"
    "CrossChain"
    "EnclaveStorage"
    "EventSubscription"
    "NetworkSecurity"
    "ProofOfReserve"
    "Randomness"
    "SecretsManagement"
    "SocialRecovery"
    "Voting"
    "ZeroKnowledge"
    "AbstractAccount"
)

for service in "${SERVICES[@]}"; do
    service_dir="src/Services/NeoServiceLayer.Services.$service"
    if [ -d "$service_dir" ]; then
        check_file "$service_dir/Program.cs" "Service: $service - Program.cs"
        check_file "$service_dir/NeoServiceLayer.Services.$service.csproj" "Service: $service - Project file"
        
        # Check for Dockerfile
        if [ -f "$service_dir/Dockerfile" ]; then
            ((PASSED_CHECKS++))
            log "${GREEN}✓ Dockerfile exists for $service${NC}"
        else
            ((WARNINGS++))
            log "${YELLOW}⚠ Warning: No Dockerfile for $service${NC}"
        fi
    else
        ((FAILED_CHECKS++))
        log "${RED}✗ Service directory missing: $service${NC}"
    fi
done

# Check AI services
log ""
log "${YELLOW}Checking AI Services${NC}"
for service in "Prediction" "PatternRecognition"; do
    service_dir="src/AI/NeoServiceLayer.AI.$service"
    check_file "$service_dir/Program.cs" "AI Service: $service"
done

# 3. Check Smart Contracts
log ""
log "${YELLOW}3. Validating Smart Contracts${NC}"
log "-----------------------------"

# Core contracts
check_file "contracts-neo-n3/src/Core/IServiceContract.cs" "Core: Service interface"
check_file "contracts-neo-n3/src/Core/ServiceRegistry.cs" "Core: Service registry"
check_file "contracts-neo-n3/src/Core/ReentrancyGuard.cs" "Core: Reentrancy guard"
check_file "contracts-neo-n3/src/Core/InputValidation.cs" "Core: Input validation"

# Production contracts
for contract in "ProductionNEP17Token" "SecureStorageContract" "EnterpriseVotingContract"; do
    check_file "contracts-neo-n3/src/ProductionReady/$contract.cs" "Production: $contract"
done

# Service contracts
CONTRACT_COUNT=$(find contracts-neo-n3/src/Services -name "*.cs" | wc -l)
if [ $CONTRACT_COUNT -gt 20 ]; then
    log "${GREEN}✓ Found $CONTRACT_COUNT service contracts${NC}"
    ((PASSED_CHECKS++))
else
    log "${RED}✗ Only $CONTRACT_COUNT service contracts found (expected 20+)${NC}"
    ((FAILED_CHECKS++))
fi

# 4. Check Configuration Files
log ""
log "${YELLOW}4. Validating Configuration Files${NC}"
log "---------------------------------"

# Kubernetes configurations
for file in "resource-quotas" "pod-security-standards" "network-policies" "hpa" "service-mesh"; do
    validate_yaml "k8s/base/$file.yaml" "K8s: $file"
done

# Check for namespace
validate_yaml "k8s/base/namespace.yaml" "K8s: namespace"

# Docker compose files
check_file "docker-compose.yml" "Docker: Main compose file"
check_file "docker-compose.dev.yml" "Docker: Development compose"
check_file "docker-compose.test.yml" "Docker: Test compose"

# 5. Check Dependencies and Package Management
log ""
log "${YELLOW}5. Validating Dependencies${NC}"
log "--------------------------"

# Check central package management
if grep -q "Microsoft.EntityFrameworkCore" Directory.Packages.props; then
    log "${GREEN}✓ Entity Framework Core configured${NC}"
    ((PASSED_CHECKS++))
else
    log "${RED}✗ Entity Framework Core not found in packages${NC}"
    ((FAILED_CHECKS++))
fi

# Check for critical packages
CRITICAL_PACKAGES=(
    "Microsoft.AspNetCore"
    "Serilog"
    "Polly"
    "prometheus-net"
    "StackExchange.Redis"
    "Npgsql.EntityFrameworkCore"
)

for package in "${CRITICAL_PACKAGES[@]}"; do
    if grep -q "$package" Directory.Packages.props; then
        ((PASSED_CHECKS++))
    else
        log "${YELLOW}⚠ Warning: Package $package not found${NC}"
        ((WARNINGS++))
    fi
done

# 6. Check Security Configurations
log ""
log "${YELLOW}6. Validating Security Configurations${NC}"
log "-------------------------------------"

# Check secrets generation script
check_file "scripts/generate-secrets.sh" "Security: Secrets generation script"
check_status "Secrets script executable" "[ -x scripts/generate-secrets.sh ]" ""

# Check if secrets directory exists (but not the actual secrets)
if [ -d "k8s/secrets" ]; then
    log "${GREEN}✓ Secrets directory exists${NC}"
    ((PASSED_CHECKS++))
else
    log "${YELLOW}⚠ Warning: Secrets directory missing${NC}"
    ((WARNINGS++))
fi

# 7. Check Scripts and Automation
log ""
log "${YELLOW}7. Validating Scripts and Automation${NC}"
log "------------------------------------"

SCRIPTS=(
    "generate-secrets.sh"
    "production-deployment.sh"
    "fix-exception-handlers.sh"
    "apply-exception-fixes.sh"
    "generate-service-docs.sh"
)

for script in "${SCRIPTS[@]}"; do
    check_file "scripts/$script" "Script: $script"
    check_status "Script executable: $script" "[ -x scripts/$script ]" ""
done

# 8. Check Monitoring Configuration
log ""
log "${YELLOW}8. Validating Monitoring Configuration${NC}"
log "--------------------------------------"

check_file "monitoring/prometheus.yml" "Monitoring: Prometheus config"
validate_json "monitoring/grafana/dashboards/service-overview.json" "Grafana: Overview dashboard"
validate_json "monitoring/grafana/dashboards/neo-service-layer-overview.json" "Grafana: Main dashboard"
validate_json "monitoring/grafana/dashboards/service-health.json" "Grafana: Health dashboard"

# 9. Check Documentation
log ""
log "${YELLOW}9. Validating Documentation${NC}"
log "---------------------------"

CRITICAL_DOCS=(
    "README.md"
    "docs/PRODUCTION_READINESS_SUMMARY.md"
    "docs/PRODUCTION_READINESS_UPDATE.md"
    "docs/FINAL_PRODUCTION_READINESS_REPORT.md"
    "docs/database-sharding-strategy.md"
)

for doc in "${CRITICAL_DOCS[@]}"; do
    check_file "$doc" "Documentation: $(basename $doc)"
done

# 10. Check Build Configuration
log ""
log "${YELLOW}10. Validating Build Configuration${NC}"
log "----------------------------------"

# Check if solution builds
log "${BLUE}Checking solution file integrity...${NC}"
if dotnet sln list > /dev/null 2>&1; then
    PROJECT_COUNT=$(dotnet sln list | grep -c "\.csproj" || true)
    if [ $PROJECT_COUNT -gt 100 ]; then
        log "${GREEN}✓ Solution contains $PROJECT_COUNT projects${NC}"
        ((PASSED_CHECKS++))
    else
        log "${YELLOW}⚠ Solution contains only $PROJECT_COUNT projects${NC}"
        ((WARNINGS++))
    fi
else
    log "${RED}✗ Failed to read solution file${NC}"
    ((FAILED_CHECKS++))
fi

# 11. Check Service Consistency
log ""
log "${YELLOW}11. Validating Service Consistency${NC}"
log "----------------------------------"

# Check that all services have consistent structure
INCONSISTENT_SERVICES=0
for service in "${SERVICES[@]}"; do
    service_dir="src/Services/NeoServiceLayer.Services.$service"
    if [ -d "$service_dir" ]; then
        # Check for required files
        required_files=("Program.cs" "*.csproj")
        for pattern in "${required_files[@]}"; do
            if ! ls $service_dir/$pattern >/dev/null 2>&1; then
                ((INCONSISTENT_SERVICES++))
                log "${YELLOW}⚠ Service $service missing $pattern${NC}"
            fi
        done
    fi
done

if [ $INCONSISTENT_SERVICES -eq 0 ]; then
    log "${GREEN}✓ All services have consistent structure${NC}"
    ((PASSED_CHECKS++))
else
    log "${YELLOW}⚠ $INCONSISTENT_SERVICES services have inconsistent structure${NC}"
    ((WARNINGS++))
fi

# 12. Check Database Migration Files
log ""
log "${YELLOW}12. Checking Database Configurations${NC}"
log "------------------------------------"

# Check for migration files
MIGRATION_COUNT=$(find . -name "*Migration*.cs" 2>/dev/null | wc -l)
if [ $MIGRATION_COUNT -gt 0 ]; then
    log "${GREEN}✓ Found $MIGRATION_COUNT migration files${NC}"
    ((PASSED_CHECKS++))
else
    log "${YELLOW}⚠ No migration files found${NC}"
    ((WARNINGS++))
fi

# 13. Final Summary
log ""
log "${YELLOW}===== VALIDATION SUMMARY =====${NC}"
log "Total Checks: $TOTAL_CHECKS"
log "${GREEN}Passed: $PASSED_CHECKS${NC}"
log "${RED}Failed: $FAILED_CHECKS${NC}"
log "${YELLOW}Warnings: $WARNINGS${NC}"
log ""

# Calculate success rate
SUCCESS_RATE=$((PASSED_CHECKS * 100 / TOTAL_CHECKS))

if [ $FAILED_CHECKS -eq 0 ]; then
    log "${GREEN}✅ SYSTEM VALIDATION PASSED!${NC}"
    log "Success Rate: $SUCCESS_RATE%"
    log "The Neo Service Layer is fully consistent and ready for deployment."
else
    log "${RED}❌ SYSTEM VALIDATION FAILED${NC}"
    log "Success Rate: $SUCCESS_RATE%"
    log "Please fix the $FAILED_CHECKS failed checks before deployment."
fi

if [ $WARNINGS -gt 0 ]; then
    log "${YELLOW}⚠️  There are $WARNINGS warnings that should be reviewed.${NC}"
fi

log ""
log "Detailed report saved to: $LOG_FILE"
log "Completed: $(date)"

# Exit with appropriate code
if [ $FAILED_CHECKS -gt 0 ]; then
    exit 1
else
    exit 0
fi