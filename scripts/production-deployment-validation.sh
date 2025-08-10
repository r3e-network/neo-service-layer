#!/bin/bash

# Production Deployment Validation Script
# Usage: ./scripts/production-deployment-validation.sh <environment>

set -euo pipefail

ENVIRONMENT="${1:-production}"
BASE_URL="${2:-https://api.neo-service-layer.com}"
TIMEOUT=30
MAX_RETRIES=5
RETRY_INTERVAL=10

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Counters
PASSED=0
FAILED=0

log() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

success() {
    echo -e "${GREEN}✅ $1${NC}"
    ((PASSED++))
}

error() {
    echo -e "${RED}❌ $1${NC}"
    echo -e "${RED}   Error: $2${NC}"
    ((FAILED++))
}

warn() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

# Retry function for HTTP requests
retry_request() {
    local url="$1"
    local expected_status="$2"
    local description="$3"
    local retries=0
    
    while [ $retries -lt $MAX_RETRIES ]; do
        local response_code
        response_code=$(curl -s -o /dev/null -w "%{http_code}" --max-time $TIMEOUT "$url" || echo "000")
        
        if [ "$response_code" = "$expected_status" ]; then
            success "$description"
            return 0
        fi
        
        ((retries++))
        if [ $retries -lt $MAX_RETRIES ]; then
            warn "Attempt $retries failed for $description (got $response_code), retrying in ${RETRY_INTERVAL}s..."
            sleep $RETRY_INTERVAL
        fi
    done
    
    error "$description" "Expected $expected_status, got $response_code after $MAX_RETRIES attempts"
    return 1
}

# Test SSL certificate and security headers
validate_ssl() {
    log "Validating SSL certificate and security headers..."
    
    # Check SSL certificate
    if openssl s_client -connect "${BASE_URL#https://}:443" -servername "${BASE_URL#https://}" </dev/null 2>/dev/null | grep -q "Verify return code: 0"; then
        success "SSL certificate is valid"
    else
        error "SSL certificate validation" "Invalid or expired certificate"
    fi
    
    # Check security headers
    local headers
    headers=$(curl -s -I "$BASE_URL" --max-time $TIMEOUT || echo "")
    
    if echo "$headers" | grep -qi "strict-transport-security"; then
        success "HSTS header present"
    else
        error "Security headers" "Missing HSTS header"
    fi
    
    if echo "$headers" | grep -qi "x-content-type-options"; then
        success "X-Content-Type-Options header present"
    else
        error "Security headers" "Missing X-Content-Type-Options header"
    fi
    
    if echo "$headers" | grep -qi "x-frame-options"; then
        success "X-Frame-Options header present"
    else
        error "Security headers" "Missing X-Frame-Options header"
    fi
}

# Validate core health endpoints
validate_health_endpoints() {
    log "Validating health endpoints..."
    
    retry_request "$BASE_URL/health" "200" "Main health endpoint"
    retry_request "$BASE_URL/health/ready" "200" "Readiness probe endpoint"
    retry_request "$BASE_URL/health/live" "200" "Liveness probe endpoint"
}

# Validate API endpoints
validate_api_endpoints() {
    log "Validating API endpoints..."
    
    retry_request "$BASE_URL/api/info" "200" "API info endpoint"
    
    # Test authentication (should require auth)
    retry_request "$BASE_URL/api/keymanagement/keys" "401" "Authentication protection"
}

# Validate monitoring endpoints
validate_monitoring() {
    log "Validating monitoring endpoints..."
    
    # Metrics endpoint (may be internal only)
    local metrics_response
    metrics_response=$(curl -s -o /dev/null -w "%{http_code}" --max-time $TIMEOUT "$BASE_URL/metrics" || echo "000")
    
    if [ "$metrics_response" = "200" ]; then
        success "Metrics endpoint accessible"
    else
        warn "Metrics endpoint not publicly accessible (may be internal only)"
    fi
}

# Validate rate limiting
validate_rate_limiting() {
    log "Validating rate limiting..."
    
    # Send burst of requests
    for i in {1..100}; do
        curl -s -o /dev/null "$BASE_URL/api/info" --max-time 5 &
    done
    wait
    
    # Check if rate limiting is working
    local rate_limit_response
    rate_limit_response=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/info" --max-time $TIMEOUT || echo "000")
    
    if [ "$rate_limit_response" = "429" ]; then
        success "Rate limiting is working"
    else
        warn "Rate limiting may not be configured or may have higher limits"
    fi
    
    # Wait for rate limit to reset
    sleep 30
}

# Validate service discovery and load balancing
validate_load_balancing() {
    log "Validating load balancing and service discovery..."
    
    local responses=()
    local unique_responses
    
    # Make multiple requests to check for load balancing
    for i in {1..10}; do
        local response
        response=$(curl -s "$BASE_URL/api/info" --max-time $TIMEOUT | jq -r '.instance' 2>/dev/null || echo "unknown")
        responses+=("$response")
    done
    
    unique_responses=$(printf '%s\n' "${responses[@]}" | sort -u | wc -l)
    
    if [ "$unique_responses" -gt 1 ]; then
        success "Load balancing detected ($unique_responses unique instances)"
    else
        warn "Single instance detected or load balancing not visible"
    fi
}

# Validate database connectivity
validate_database() {
    log "Validating database connectivity..."
    
    # This would typically require an authenticated request to a database-dependent endpoint
    # For now, we'll check if the application is responding properly (indicating DB is accessible)
    
    retry_request "$BASE_URL/health/ready" "200" "Database connectivity (via readiness check)"
}

# Performance validation
validate_performance() {
    log "Validating response times..."
    
    local response_time
    response_time=$(curl -o /dev/null -s -w "%{time_total}" "$BASE_URL/health" --max-time $TIMEOUT || echo "999")
    
    if (( $(echo "$response_time < 1.0" | bc -l) )); then
        success "Response time acceptable (${response_time}s)"
    else
        error "Performance" "Response time too slow (${response_time}s)"
    fi
}

# Kubernetes deployment validation
validate_k8s_deployment() {
    log "Validating Kubernetes deployment status..."
    
    if command -v kubectl >/dev/null 2>&1; then
        # Check if all pods are running
        local pod_status
        pod_status=$(kubectl get pods -n neo-service-layer --no-headers 2>/dev/null | awk '{print $3}' | sort | uniq -c || echo "")
        
        if echo "$pod_status" | grep -q "Running"; then
            local running_pods
            running_pods=$(echo "$pod_status" | grep "Running" | awk '{print $1}')
            success "Kubernetes pods running ($running_pods pods)"
        else
            error "Kubernetes validation" "No running pods found"
        fi
        
        # Check services
        local service_count
        service_count=$(kubectl get services -n neo-service-layer --no-headers 2>/dev/null | wc -l || echo "0")
        
        if [ "$service_count" -gt 0 ]; then
            success "Kubernetes services deployed ($service_count services)"
        else
            error "Kubernetes validation" "No services found"
        fi
    else
        warn "kubectl not available, skipping Kubernetes validation"
    fi
}

# Main validation execution
main() {
    echo "=================================================="
    echo "Neo Service Layer Production Deployment Validation"
    echo "Environment: $ENVIRONMENT"
    echo "Base URL: $BASE_URL"
    echo "=================================================="
    echo
    
    validate_ssl
    validate_health_endpoints
    validate_api_endpoints
    validate_monitoring
    validate_rate_limiting
    validate_load_balancing
    validate_database
    validate_performance
    validate_k8s_deployment
    
    echo
    echo "=================================================="
    echo "Validation Summary"
    echo "=================================================="
    echo -e "${GREEN}Passed: $PASSED${NC}"
    echo -e "${RED}Failed: $FAILED${NC}"
    echo
    
    if [ $FAILED -eq 0 ]; then
        echo -e "${GREEN}🎉 All validations passed! Deployment is ready for production.${NC}"
        exit 0
    else
        echo -e "${RED}❌ Some validations failed. Please investigate before proceeding.${NC}"
        exit 1
    fi
}

# Check dependencies
check_dependencies() {
    local missing_deps=0
    
    for cmd in curl openssl jq bc; do
        if ! command -v $cmd >/dev/null 2>&1; then
            error "Missing dependency" "$cmd is required but not installed"
            ((missing_deps++))
        fi
    done
    
    if [ $missing_deps -gt 0 ]; then
        echo -e "${RED}Please install missing dependencies before running this script.${NC}"
        exit 1
    fi
}

# Script execution
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    check_dependencies
    main "$@"
fi