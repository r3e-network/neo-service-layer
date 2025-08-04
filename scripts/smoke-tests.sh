#!/bin/bash

# Smoke tests for Neo Service Layer
# Usage: ./smoke-tests.sh <base_url>

set -e

BASE_URL="${1:-http://localhost}"
TIMEOUT=10
RETRY_INTERVAL=2
MAX_RETRIES=5

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Test counter
PASSED=0
FAILED=0

# Helper functions
print_test() {
    echo -e "${YELLOW}Testing: $1${NC}"
}

print_pass() {
    echo -e "${GREEN}✓ PASS: $1${NC}"
    ((PASSED++))
}

print_fail() {
    echo -e "${RED}✗ FAIL: $1${NC}"
    echo "  Error: $2"
    ((FAILED++))
}

# Retry logic for API calls
retry_curl() {
    local url=$1
    local expected_status=$2
    local retries=0
    
    while [ $retries -lt $MAX_RETRIES ]; do
        response=$(curl -s -o /dev/null -w "%{http_code}" --connect-timeout $TIMEOUT "$url" || echo "000")
        
        if [ "$response" == "$expected_status" ]; then
            return 0
        fi
        
        ((retries++))
        if [ $retries -lt $MAX_RETRIES ]; then
            sleep $RETRY_INTERVAL
        fi
    done
    
    return 1
}

# Test health endpoints
test_health_endpoints() {
    print_test "Health Endpoints"
    
    # Main health endpoint
    if retry_curl "$BASE_URL/health" "200"; then
        print_pass "Main health endpoint"
    else
        print_fail "Main health endpoint" "Expected 200, got $response"
    fi
    
    # Ready endpoint
    if retry_curl "$BASE_URL/health/ready" "200"; then
        print_pass "Ready endpoint"
    else
        print_fail "Ready endpoint" "Expected 200, got $response"
    fi
    
    # Live endpoint
    if retry_curl "$BASE_URL/health/live" "200"; then
        print_pass "Live endpoint"
    else
        print_fail "Live endpoint" "Expected 200, got $response"
    fi
}

# Test API info endpoint
test_api_info() {
    print_test "API Info Endpoint"
    
    response=$(curl -s --connect-timeout $TIMEOUT "$BASE_URL/api/info" || echo "{}")
    
    if echo "$response" | grep -q "Neo Service Layer API"; then
        print_pass "API info endpoint"
    else
        print_fail "API info endpoint" "Invalid response: $response"
    fi
}

# Test authentication
test_authentication() {
    print_test "Authentication"
    
    # Test unauthenticated access (should fail)
    response=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/keymanagement/keys" || echo "000")
    
    if [ "$response" == "401" ]; then
        print_pass "Unauthenticated access properly rejected"
    else
        print_fail "Unauthenticated access check" "Expected 401, got $response"
    fi
}

# Test rate limiting
test_rate_limiting() {
    print_test "Rate Limiting"
    
    # Make multiple rapid requests
    for i in {1..150}; do
        curl -s -o /dev/null "$BASE_URL/api/info" &
    done
    
    wait
    
    # Check if rate limiting kicked in
    response=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/info" || echo "000")
    
    if [ "$response" == "429" ]; then
        print_pass "Rate limiting working"
    else
        print_fail "Rate limiting" "Expected 429 after rapid requests, got $response"
    fi
}

# Test CORS headers
test_cors() {
    print_test "CORS Headers"
    
    response=$(curl -s -I -H "Origin: https://example.com" "$BASE_URL/api/info" || echo "")
    
    if echo "$response" | grep -q "Access-Control-Allow-Origin"; then
        print_pass "CORS headers present"
    else
        print_fail "CORS headers" "Missing Access-Control-Allow-Origin header"
    fi
}

# Test SSL/TLS
test_ssl() {
    print_test "SSL/TLS Configuration"
    
    if [[ "$BASE_URL" == https://* ]]; then
        # Test SSL certificate
        if curl -s --connect-timeout $TIMEOUT "$BASE_URL" > /dev/null 2>&1; then
            print_pass "SSL certificate valid"
        else
            print_fail "SSL certificate" "Certificate validation failed"
        fi
        
        # Test security headers
        response=$(curl -s -I "$BASE_URL" || echo "")
        
        if echo "$response" | grep -q "Strict-Transport-Security"; then
            print_pass "HSTS header present"
        else
            print_fail "HSTS header" "Missing Strict-Transport-Security header"
        fi
    else
        echo "  Skipping SSL tests (not HTTPS)"
    fi
}

# Test service discovery
test_service_discovery() {
    print_test "Service Discovery"
    
    # Check if Consul is accessible
    consul_url="${BASE_URL%:*}:8500/v1/agent/services"
    
    if curl -s --connect-timeout 5 "$consul_url" > /dev/null 2>&1; then
        print_pass "Consul service discovery accessible"
    else
        echo "  Consul not accessible (may be internal only)"
    fi
}

# Test monitoring endpoints
test_monitoring() {
    print_test "Monitoring Endpoints"
    
    # Prometheus metrics
    if retry_curl "$BASE_URL/metrics" "200"; then
        print_pass "Prometheus metrics endpoint"
    else
        echo "  Metrics endpoint not exposed (may be internal only)"
    fi
}

# Main execution
echo "========================================="
echo "Neo Service Layer Smoke Tests"
echo "Testing: $BASE_URL"
echo "========================================="
echo ""

# Run all tests
test_health_endpoints
test_api_info
test_authentication
test_rate_limiting
test_cors
test_ssl
test_service_discovery
test_monitoring

# Summary
echo ""
echo "========================================="
echo "Test Summary"
echo "========================================="
echo -e "${GREEN}Passed: $PASSED${NC}"
echo -e "${RED}Failed: $FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}All smoke tests passed!${NC}"
    exit 0
else
    echo -e "${RED}Some tests failed. Please investigate.${NC}"
    exit 1
fi