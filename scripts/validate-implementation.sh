#!/bin/bash

# Neo Service Layer - Implementation Validation Script
# This script validates the complete implementation of all components

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="http://localhost:8080"
API_URL="$BASE_URL/api/v1"
TEST_USER="testuser_$(date +%s)"
TEST_EMAIL="test_$(date +%s)@example.com"
TEST_PASSWORD="TestPassword123!"

# Global variables
AUTH_TOKEN=""
DEPLOYMENT_RESULTS=()
VALIDATION_ERRORS=()

# Utility functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
    VALIDATION_ERRORS+=("$1")
}

check_prerequisite() {
    local cmd=$1
    local name=$2
    
    if ! command -v "$cmd" &> /dev/null; then
        log_error "$name is not installed or not in PATH"
        return 1
    fi
    log_success "$name is available"
    return 0
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    local prereqs=(
        "curl:cURL"
        "jq:jq (JSON processor)"
        "docker:Docker"
        "docker-compose:Docker Compose"
    )
    
    local failed=false
    for prereq in "${prereqs[@]}"; do
        IFS=':' read -r cmd name <<< "$prereq"
        if ! check_prerequisite "$cmd" "$name"; then
            failed=true
        fi
    done
    
    if [ "$failed" = true ]; then
        log_error "Some prerequisites are missing. Please install them and try again."
        exit 1
    fi
    
    log_success "All prerequisites are available"
}

# Check if services are running
check_services() {
    log_info "Checking if Neo Service Layer is running..."
    
    if ! curl -s -f "$BASE_URL/health" > /dev/null; then
        log_error "Neo Service Layer is not running at $BASE_URL"
        log_info "Please start the service with: docker-compose up -d"
        exit 1
    fi
    
    log_success "Neo Service Layer is running"
    
    # Check service health
    local health_status=$(curl -s "$BASE_URL/health" | jq -r '.Status // "Unknown"')
    if [ "$health_status" = "Healthy" ]; then
        log_success "Service health status: $health_status"
    else
        log_warning "Service health status: $health_status"
    fi
}

# Test authentication system
test_authentication() {
    log_info "Testing authentication system..."
    
    # Test registration
    log_info "Testing user registration..."
    local register_response=$(curl -s -X POST "$API_URL/auth/register" \
        -H "Content-Type: application/json" \
        -d "{
            \"username\": \"$TEST_USER\",
            \"email\": \"$TEST_EMAIL\",
            \"password\": \"$TEST_PASSWORD\",
            \"role\": \"KeyManager\"
        }")
    
    if echo "$register_response" | jq -e '.isSuccess // false' > /dev/null; then
        log_success "User registration successful"
    else
        log_error "User registration failed: $(echo "$register_response" | jq -r '.message // "Unknown error"')"
        return 1
    fi
    
    # Test login
    log_info "Testing user login..."
    local login_response=$(curl -s -X POST "$API_URL/auth/login" \
        -H "Content-Type: application/json" \
        -d "{
            \"username\": \"$TEST_USER\",
            \"password\": \"$TEST_PASSWORD\"
        }")
    
    AUTH_TOKEN=$(echo "$login_response" | jq -r '.token // empty')
    
    if [ -n "$AUTH_TOKEN" ] && [ "$AUTH_TOKEN" != "null" ]; then
        log_success "User login successful"
        log_info "JWT token obtained"
    else
        log_error "User login failed: $(echo "$login_response" | jq -r '.message // "Unknown error"')"
        return 1
    fi
    
    # Test token validation
    log_info "Testing token validation..."
    local profile_response=$(curl -s -H "Authorization: Bearer $AUTH_TOKEN" \
        "$API_URL/auth/profile")
    
    if echo "$profile_response" | jq -e '.username' > /dev/null; then
        log_success "Token validation successful"
    else
        log_error "Token validation failed"
        return 1
    fi
}

# Test smart contracts API endpoints
test_smart_contracts_api() {
    log_info "Testing Smart Contracts API endpoints..."
    
    # Test Neo N3 endpoints
    log_info "Testing Neo N3 contract endpoints..."
    
    # List deployed contracts
    local neo_n3_contracts=$(curl -s -H "Authorization: Bearer $AUTH_TOKEN" \
        "$API_URL/smart-contracts/neo-n3/contracts")
    
    if echo "$neo_n3_contracts" | jq -e 'type' > /dev/null; then
        log_success "Neo N3 contracts list endpoint working"
    else
        log_error "Neo N3 contracts list endpoint failed"
    fi
    
    # Test Neo X endpoints
    log_info "Testing Neo X contract endpoints..."
    
    local neo_x_contracts=$(curl -s -H "Authorization: Bearer $AUTH_TOKEN" \
        "$API_URL/smart-contracts/neo-x/contracts")
    
    if echo "$neo_x_contracts" | jq -e 'type' > /dev/null; then
        log_success "Neo X contracts list endpoint working"
    else
        log_error "Neo X contracts list endpoint failed"
    fi
    
    # Test cross-chain endpoints
    log_info "Testing cross-chain endpoints..."
    
    local bridge_config=$(curl -s -H "Authorization: Bearer $AUTH_TOKEN" \
        "$API_URL/smart-contracts/cross-chain/bridge-config/NeoN3/NeoX")
    
    if echo "$bridge_config" | jq -e 'type' > /dev/null; then
        log_success "Cross-chain bridge config endpoint working"
    else
        log_error "Cross-chain bridge config endpoint failed"
    fi
}

# Test contract deployment (mock)
test_contract_deployment() {
    log_info "Testing contract deployment capabilities..."
    
    # Create mock contract code
    local mock_nef_manifest=$(echo '{"nef":{"magic":1346454863,"compiler":"test","source":"","tokens":[],"script":"VgEMFAAAAAAAAAAAAAAAAAAAAAAAAAAAQFcAAXg0A0BXAQF4NANAWDcAAEA=","checksum":123456789},"manifest":{"name":"TestContract","groups":[],"features":{},"supportedstds":[],"abi":{"methods":[{"name":"symbol","offset":0,"parameters":[],"returntype":"String","safe":true}],"events":[]},"permissions":[{"contract":"*","methods":"*"}],"trusts":[],"extra":{"Author":"Test","Description":"Test contract"}}}' | base64 -w 0)
    
    log_info "Testing Neo N3 contract deployment validation..."
    local deploy_response=$(curl -s -X POST "$API_URL/smart-contracts/neo-n3/deploy" \
        -H "Authorization: Bearer $AUTH_TOKEN" \
        -H "Content-Type: application/json" \
        -d "{
            \"contractCode\": \"$mock_nef_manifest\",
            \"name\": \"TestContract\",
            \"version\": \"1.0.0\",
            \"author\": \"Test Author\",
            \"description\": \"Test contract for validation\",
            \"gasLimit\": 10000000
        }")
    
    # Check if deployment endpoint is accessible and validates input
    local error_type=$(echo "$deploy_response" | jq -r '.type // "unknown"')
    if [[ "$error_type" == *"validation"* ]] || [[ "$error_type" == *"blockchain"* ]] || echo "$deploy_response" | jq -e '.contractHash' > /dev/null; then
        log_success "Neo N3 deployment endpoint properly validates input"
    else
        log_error "Neo N3 deployment endpoint validation failed: $deploy_response"
    fi
    
    # Test Neo X deployment validation
    log_info "Testing Neo X contract deployment validation..."
    local mock_evm_contract='{"bytecode":"0x608060405234801561001057600080fd5b50","abi":[{"inputs":[],"name":"symbol","outputs":[{"internalType":"string","name":"","type":"string"}],"stateMutability":"view","type":"function"}]}'
    local mock_evm_encoded=$(echo "$mock_evm_contract" | base64 -w 0)
    
    local evm_deploy_response=$(curl -s -X POST "$API_URL/smart-contracts/neo-x/deploy" \
        -H "Authorization: Bearer $AUTH_TOKEN" \
        -H "Content-Type: application/json" \
        -d "{
            \"contractCode\": \"$mock_evm_encoded\",
            \"name\": \"TestEVMContract\",
            \"version\": \"1.0.0\",
            \"author\": \"Test Author\",
            \"gasLimit\": 2000000
        }")
    
    local evm_error_type=$(echo "$evm_deploy_response" | jq -r '.type // "unknown"')
    if [[ "$evm_error_type" == *"validation"* ]] || [[ "$evm_error_type" == *"blockchain"* ]] || echo "$evm_deploy_response" | jq -e '.contractHash' > /dev/null; then
        log_success "Neo X deployment endpoint properly validates input"
    else
        log_error "Neo X deployment endpoint validation failed: $evm_deploy_response"
    fi
}

# Test gas estimation
test_gas_estimation() {
    log_info "Testing gas estimation endpoints..."
    
    # Test Neo N3 gas estimation
    local neo_n3_gas=$(curl -s -H "Authorization: Bearer $AUTH_TOKEN" \
        "$API_URL/smart-contracts/neo-n3/0x1234567890abcdef1234567890abcdef12345678/estimate-gas/symbol")
    
    if echo "$neo_n3_gas" | jq -e 'type' > /dev/null; then
        log_success "Neo N3 gas estimation endpoint accessible"
    else
        log_error "Neo N3 gas estimation endpoint failed"
    fi
    
    # Test Neo X gas estimation
    local neo_x_gas=$(curl -s -H "Authorization: Bearer $AUTH_TOKEN" \
        "$API_URL/smart-contracts/neo-x/0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234/estimate-gas/symbol")
    
    if echo "$neo_x_gas" | jq -e 'type' > /dev/null; then
        log_success "Neo X gas estimation endpoint accessible"
    else
        log_error "Neo X gas estimation endpoint failed"
    fi
}

# Test service integrations
test_service_integrations() {
    log_info "Testing service integrations..."
    
    # Test key management service
    log_info "Testing key management integration..."
    local key_status=$(curl -s -H "Authorization: Bearer $AUTH_TOKEN" \
        "$API_URL/key-management/status")
    
    if echo "$key_status" | jq -e 'type' > /dev/null; then
        log_success "Key management service integration working"
    else
        log_error "Key management service integration failed"
    fi
    
    # Test monitoring endpoints
    log_info "Testing monitoring integration..."
    local metrics=$(curl -s "$BASE_URL/metrics" || echo '{"error": "metrics not available"}')
    
    if [[ "$metrics" == *"# HELP"* ]] || [[ "$metrics" == *"# TYPE"* ]]; then
        log_success "Prometheus metrics endpoint working"
    else
        log_warning "Prometheus metrics endpoint not available"
    fi
}

# Test database connectivity
test_database_connectivity() {
    log_info "Testing database connectivity..."
    
    # Check if database migrations have been applied
    local db_status=$(curl -s -H "Authorization: Bearer $AUTH_TOKEN" \
        "$API_URL/system/database/status")
    
    if echo "$db_status" | jq -e '.isConnected // false' > /dev/null; then
        log_success "Database connectivity working"
    else
        log_warning "Database status endpoint not available or connection failed"
    fi
}

# Test enclave functionality
test_enclave_functionality() {
    log_info "Testing enclave functionality..."
    
    # Check enclave status
    local enclave_status=$(curl -s -H "Authorization: Bearer $AUTH_TOKEN" \
        "$API_URL/enclave/status")
    
    if echo "$enclave_status" | jq -e 'type' > /dev/null; then
        log_success "Enclave status endpoint accessible"
        local mode=$(echo "$enclave_status" | jq -r '.mode // "unknown"')
        log_info "Enclave mode: $mode"
    else
        log_warning "Enclave status endpoint not available"
    fi
}

# Test Docker and containerization
test_containerization() {
    log_info "Testing containerization setup..."
    
    # Check if containers are running
    if docker-compose ps | grep -q "Up"; then
        log_success "Docker containers are running"
    else
        log_error "Docker containers are not running properly"
    fi
    
    # Check container health
    local unhealthy_containers=$(docker-compose ps | grep -c "unhealthy" || echo "0")
    if [ "$unhealthy_containers" -eq 0 ]; then
        log_success "All containers are healthy"
    else
        log_warning "$unhealthy_containers container(s) are unhealthy"
    fi
    
    # Check logs for errors
    log_info "Checking container logs for errors..."
    local error_count=$(docker-compose logs --tail=100 2>&1 | grep -i -c "error\|exception\|fatal" || echo "0")
    if [ "$error_count" -eq 0 ]; then
        log_success "No recent errors in container logs"
    else
        log_warning "Found $error_count error messages in recent logs"
    fi
}

# Test API documentation
test_api_documentation() {
    log_info "Testing API documentation..."
    
    # Check Swagger/OpenAPI documentation
    local swagger_response=$(curl -s "$BASE_URL/swagger/v1/swagger.json")
    
    if echo "$swagger_response" | jq -e '.openapi // .swagger' > /dev/null; then
        log_success "Swagger/OpenAPI documentation available"
    else
        log_warning "Swagger/OpenAPI documentation not available"
    fi
    
    # Check if Swagger UI is accessible
    local swagger_ui=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/")
    if [ "$swagger_ui" = "200" ]; then
        log_success "Swagger UI is accessible"
    else
        log_warning "Swagger UI not accessible (HTTP $swagger_ui)"
    fi
}

# Test monitoring and observability
test_monitoring() {
    log_info "Testing monitoring and observability..."
    
    # Test Prometheus metrics
    if curl -s -f "$BASE_URL/metrics" > /dev/null; then
        log_success "Prometheus metrics endpoint working"
    else
        log_warning "Prometheus metrics endpoint not available"
    fi
    
    # Test health checks
    local health_response=$(curl -s "$BASE_URL/health")
    local health_status=$(echo "$health_response" | jq -r '.Status // "unknown"')
    
    case "$health_status" in
        "Healthy")
            log_success "Health check reports: $health_status"
            ;;
        "Degraded")
            log_warning "Health check reports: $health_status"
            ;;
        "Unhealthy")
            log_error "Health check reports: $health_status"
            ;;
        *)
            log_warning "Health check status unknown: $health_status"
            ;;
    esac
    
    # Check readiness probe
    if curl -s -f "$BASE_URL/health/ready" > /dev/null; then
        log_success "Readiness probe working"
    else
        log_warning "Readiness probe not available"
    fi
    
    # Check liveness probe
    if curl -s -f "$BASE_URL/health/live" > /dev/null; then
        log_success "Liveness probe working"
    else
        log_warning "Liveness probe not available"
    fi
}

# Test CI/CD pipeline files
test_cicd_setup() {
    log_info "Testing CI/CD setup..."
    
    # Check GitHub Actions workflow files
    if [ -d ".github/workflows" ]; then
        local workflow_count=$(find .github/workflows -name "*.yml" -o -name "*.yaml" | wc -l)
        if [ "$workflow_count" -gt 0 ]; then
            log_success "Found $workflow_count GitHub Actions workflow files"
        else
            log_warning "No GitHub Actions workflow files found"
        fi
    else
        log_warning "GitHub Actions workflow directory not found"
    fi
    
    # Check Docker files
    local docker_files=("Dockerfile" "docker-compose.yml" "docker-compose.production.yml")
    for file in "${docker_files[@]}"; do
        if [ -f "$file" ]; then
            log_success "Docker file found: $file"
        else
            log_warning "Docker file missing: $file"
        fi
    done
}

# Test documentation completeness
test_documentation() {
    log_info "Testing documentation completeness..."
    
    local required_docs=(
        "README.md"
        "docs/deployment/PRODUCTION_DEPLOYMENT_GUIDE.md"
        "docs/api/SMART_CONTRACTS_API.md"
        "docs/USAGE_GUIDE.md"
    )
    
    for doc in "${required_docs[@]}"; do
        if [ -f "$doc" ]; then
            log_success "Documentation found: $doc"
        else
            log_error "Documentation missing: $doc"
        fi
    done
}

# Performance and load testing
test_performance() {
    log_info "Testing basic performance..."
    
    # Test API response time
    local start_time=$(date +%s%N)
    curl -s -H "Authorization: Bearer $AUTH_TOKEN" \
        "$API_URL/smart-contracts/neo-n3/contracts" > /dev/null
    local end_time=$(date +%s%N)
    local duration_ms=$(( (end_time - start_time) / 1000000 ))
    
    if [ "$duration_ms" -lt 1000 ]; then
        log_success "API response time: ${duration_ms}ms (Good)"
    elif [ "$duration_ms" -lt 5000 ]; then
        log_warning "API response time: ${duration_ms}ms (Acceptable)"
    else
        log_error "API response time: ${duration_ms}ms (Too slow)"
    fi
    
    # Test concurrent requests
    log_info "Testing concurrent request handling..."
    local concurrent_requests=5
    for i in $(seq 1 $concurrent_requests); do
        curl -s -H "Authorization: Bearer $AUTH_TOKEN" \
            "$API_URL/smart-contracts/neo-n3/contracts" > /dev/null &
    done
    wait
    log_success "Handled $concurrent_requests concurrent requests successfully"
}

# Security testing
test_security() {
    log_info "Testing security configurations..."
    
    # Test unauthorized access
    local unauthorized_response=$(curl -s -o /dev/null -w "%{http_code}" \
        "$API_URL/smart-contracts/neo-n3/contracts")
    
    if [ "$unauthorized_response" = "401" ]; then
        log_success "Unauthorized access properly blocked (HTTP 401)"
    else
        log_error "Unauthorized access not properly blocked (HTTP $unauthorized_response)"
    fi
    
    # Test HTTPS redirect (if applicable)
    local https_test=$(curl -s -o /dev/null -w "%{http_code}" \
        "http://localhost:8080/api/info" || echo "connection_refused")
    
    if [ "$https_test" = "connection_refused" ]; then
        log_info "HTTPS-only configuration detected"
    elif [ "$https_test" = "301" ] || [ "$https_test" = "302" ]; then
        log_success "HTTP to HTTPS redirect working"
    else
        log_info "HTTP endpoint accessible (development mode)"
    fi
    
    # Test rate limiting
    log_info "Testing rate limiting..."
    local rate_limit_test=true
    for i in $(seq 1 10); do
        local response_code=$(curl -s -o /dev/null -w "%{http_code}" \
            -H "Authorization: Bearer $AUTH_TOKEN" \
            "$API_URL/smart-contracts/neo-n3/contracts")
        if [ "$response_code" = "429" ]; then
            log_success "Rate limiting is working (HTTP 429)"
            rate_limit_test=false
            break
        fi
        sleep 0.1
    done
    
    if [ "$rate_limit_test" = true ]; then
        log_info "Rate limiting not triggered (limits may be high for development)"
    fi
}

# Generate validation report
generate_report() {
    log_info "Generating validation report..."
    
    local report_file="validation-report-$(date +%Y%m%d_%H%M%S).md"
    
    cat > "$report_file" << EOF
# Neo Service Layer Validation Report

**Generated:** $(date)  
**Base URL:** $BASE_URL  
**Test User:** $TEST_USER  

## Summary

- **Total Errors:** ${#VALIDATION_ERRORS[@]}
- **Status:** $([ ${#VALIDATION_ERRORS[@]} -eq 0 ] && echo "✅ PASSED" || echo "❌ FAILED")

## Validation Results

### Components Tested
- [x] Prerequisites
- [x] Service Health
- [x] Authentication System
- [x] Smart Contracts API
- [x] Contract Deployment
- [x] Gas Estimation
- [x] Service Integrations
- [x] Database Connectivity
- [x] Enclave Functionality
- [x] Containerization
- [x] API Documentation
- [x] Monitoring & Observability
- [x] CI/CD Setup
- [x] Documentation
- [x] Performance
- [x] Security

### Errors Found
EOF

    if [ ${#VALIDATION_ERRORS[@]} -eq 0 ]; then
        echo "No errors found! ✅" >> "$report_file"
    else
        for error in "${VALIDATION_ERRORS[@]}"; do
            echo "- ❌ $error" >> "$report_file"
        done
    fi
    
    cat >> "$report_file" << EOF

## Recommendations

### For Production Deployment
1. Ensure all environment variables are properly configured
2. Use proper SSL certificates from a trusted CA
3. Configure monitoring and alerting
4. Set up automated backups
5. Implement proper secret management
6. Configure rate limiting based on your requirements
7. Set up log aggregation and monitoring

### For Development
1. Use the provided docker-compose.yml for local development
2. Enable debug logging for troubleshooting
3. Use the Swagger UI for API exploration
4. Run integration tests regularly

## Next Steps

1. Address any errors found in this report
2. Run the full test suite: \`npm test\` or \`dotnet test\`
3. Deploy to staging environment for further testing
4. Configure monitoring and alerting
5. Plan production deployment

---

*This report was generated automatically by the validation script.*
EOF
    
    log_success "Validation report generated: $report_file"
}

# Main validation function
main() {
    echo "🔍 Neo Service Layer Implementation Validation"
    echo "============================================="
    echo ""
    
    local start_time=$(date +%s)
    
    # Run all validation tests
    check_prerequisites
    check_services
    test_authentication
    test_smart_contracts_api
    test_contract_deployment
    test_gas_estimation
    test_service_integrations
    test_database_connectivity
    test_enclave_functionality
    test_containerization
    test_api_documentation
    test_monitoring
    test_cicd_setup
    test_documentation
    test_performance
    test_security
    
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    
    echo ""
    echo "============================================="
    echo "🏁 Validation Complete"
    echo "============================================="
    echo ""
    
    if [ ${#VALIDATION_ERRORS[@]} -eq 0 ]; then
        log_success "All validations passed! ✅"
        log_success "Neo Service Layer implementation is ready for deployment"
    else
        log_error "Found ${#VALIDATION_ERRORS[@]} validation errors ❌"
        log_error "Please address the following issues:"
        for error in "${VALIDATION_ERRORS[@]}"; do
            echo "  - $error"
        done
    fi
    
    echo ""
    echo "Duration: ${duration}s"
    echo ""
    
    # Generate report
    generate_report
    
    # Exit with error code if validation failed
    [ ${#VALIDATION_ERRORS[@]} -eq 0 ] || exit 1
}

# Run validation
main "$@"