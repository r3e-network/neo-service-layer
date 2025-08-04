#!/bin/bash

# Service readiness verification script
# Usage: ./verify-service-readiness.sh <service-name> <expected-version>

set -e

SERVICE_NAME="${1:-api-gateway}"
EXPECTED_VERSION="${2:-1.0.0}"
MAX_RETRIES=30
RETRY_INTERVAL=10

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log_info() {
    echo "[$(date +'%H:%M:%S')] $1"
}

log_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

log_warn() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

log_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Check if service container is running
check_container_status() {
    if docker ps --format "table {{.Names}}" | grep -q "neo-${SERVICE_NAME}"; then
        log_success "Container neo-${SERVICE_NAME} is running"
        return 0
    else
        log_error "Container neo-${SERVICE_NAME} is not running"
        return 1
    fi
}

# Check service health endpoint
check_health_endpoint() {
    local service_url="http://localhost"
    
    # Determine service URL based on service type
    case "$SERVICE_NAME" in
        "api-gateway")
            service_url="http://localhost/health"
            ;;
        *)
            service_url="http://localhost:8080/health"
            ;;
    esac
    
    log_info "Checking health endpoint: $service_url"
    
    local retry_count=0
    while [ $retry_count -lt $MAX_RETRIES ]; do
        if response=$(curl -s --max-time 10 "$service_url" 2>/dev/null); then
            if echo "$response" | jq -e '.status == "Healthy"' >/dev/null 2>&1; then
                log_success "Health endpoint responding with Healthy status"
                return 0
            elif echo "$response" | grep -q "Healthy\|UP\|OK"; then
                log_success "Health endpoint responding positively"
                return 0
            else
                log_warn "Health endpoint responding but status unclear: $response"
            fi
        else
            log_info "Health endpoint not ready (attempt $((retry_count + 1))/$MAX_RETRIES)"
        fi
        
        retry_count=$((retry_count + 1))
        if [ $retry_count -lt $MAX_RETRIES ]; then
            sleep $RETRY_INTERVAL
        fi
    done
    
    log_error "Health endpoint failed after $MAX_RETRIES attempts"
    return 1
}

# Check service info endpoint
check_info_endpoint() {
    local service_url="http://localhost/api/info"
    
    case "$SERVICE_NAME" in
        "api-gateway")
            service_url="http://localhost/api/info"
            ;;
        *)
            service_url="http://localhost:8080/api/info"
            ;;
    esac
    
    log_info "Checking info endpoint: $service_url"
    
    if response=$(curl -s --max-time 10 "$service_url" 2>/dev/null); then
        if echo "$response" | jq -e '. != null' >/dev/null 2>&1; then
            local version=$(echo "$response" | jq -r '.version // .Version // "unknown"')
            local name=$(echo "$response" | jq -r '.name // .Name // "unknown"')
            
            log_success "Info endpoint responding - Name: $name, Version: $version"
            
            if [ "$version" != "unknown" ] && [ "$version" != "null" ]; then
                if [ "$version" = "$EXPECTED_VERSION" ]; then
                    log_success "Version matches expected: $EXPECTED_VERSION"
                else
                    log_warn "Version mismatch - Expected: $EXPECTED_VERSION, Got: $version"
                fi
            fi
            return 0
        else
            log_warn "Info endpoint responding but invalid JSON"
        fi
    else
        log_warn "Info endpoint not accessible"
    fi
    
    return 0  # Non-critical for service readiness
}

# Check metrics endpoint
check_metrics_endpoint() {
    local service_url="http://localhost:8080/metrics"
    
    case "$SERVICE_NAME" in
        "api-gateway")
            service_url="http://localhost/metrics"
            ;;
        *)
            service_url="http://localhost:8080/metrics"
            ;;
    esac
    
    log_info "Checking metrics endpoint: $service_url"
    
    if response=$(curl -s --max-time 10 "$service_url" 2>/dev/null); then
        local metric_count=$(echo "$response" | grep -c "^# HELP" || echo "0")
        if [ "$metric_count" -gt 5 ]; then
            log_success "Metrics endpoint responding with $metric_count metrics"
            return 0
        else
            log_warn "Metrics endpoint has limited metrics: $metric_count"
        fi
    else
        log_warn "Metrics endpoint not accessible"
    fi
    
    return 0  # Non-critical for basic readiness
}

# Check service logs for errors
check_service_logs() {
    log_info "Checking service logs for errors..."
    
    local error_count=$(docker logs "neo-${SERVICE_NAME}" --since="5m" 2>&1 | grep -ci "error\|exception\|failed\|fatal" || echo "0")
    local warn_count=$(docker logs "neo-${SERVICE_NAME}" --since="5m" 2>&1 | grep -ci "warn" || echo "0")
    
    if [ "$error_count" -eq 0 ]; then
        log_success "No errors found in recent logs"
    else
        log_warn "Found $error_count errors in recent logs"
        # Show last few error lines
        docker logs "neo-${SERVICE_NAME}" --since="5m" 2>&1 | grep -i "error\|exception\|failed\|fatal" | tail -3
    fi
    
    if [ "$warn_count" -gt 0 ]; then
        log_warn "Found $warn_count warnings in recent logs"
    fi
    
    return 0  # Non-critical unless excessive errors
}

# Check service dependencies
check_service_dependencies() {
    log_info "Checking service dependencies..."
    
    # Check database connectivity (if applicable)
    case "$SERVICE_NAME" in
        "api-gateway"|"storage"|"configuration"|"voting")
            if docker exec neo-postgres-phase1 pg_isready -U ${DB_USER:-neo_service_user} >/dev/null 2>&1; then
                log_success "Database dependency available"
            else
                log_error "Database dependency not available"
                return 1
            fi
            ;;
    esac
    
    # Check Redis connectivity (if applicable)
    case "$SERVICE_NAME" in
        "api-gateway"|"storage"|"notification")
            if docker exec neo-redis-phase1 redis-cli ping >/dev/null 2>&1; then
                log_success "Redis dependency available"
            else
                log_error "Redis dependency not available"
                return 1
            fi
            ;;
    esac
    
    # Check Consul connectivity
    if curl -s http://localhost:8500/v1/status/leader >/dev/null 2>&1; then
        log_success "Consul dependency available"
    else
        log_warn "Consul dependency not available"
    fi
    
    return 0
}

# Check resource usage
check_resource_usage() {
    log_info "Checking resource usage..."
    
    local container_stats=$(docker stats "neo-${SERVICE_NAME}" --no-stream --format "table {{.CPUPerc}}\t{{.MemUsage}}" 2>/dev/null | tail -1)
    
    if [ -n "$container_stats" ]; then
        local cpu_usage=$(echo "$container_stats" | awk '{print $1}' | sed 's/%//')
        local mem_usage=$(echo "$container_stats" | awk '{print $2}')
        
        log_success "Resource usage - CPU: ${cpu_usage}%, Memory: ${mem_usage}"
        
        # Check if CPU usage is excessive
        if (( $(echo "$cpu_usage > 80" | bc -l) )); then
            log_warn "High CPU usage: ${cpu_usage}%"
        fi
    else
        log_warn "Could not retrieve resource usage stats"
    fi
    
    return 0
}

# Main verification process
main() {
    echo "==========================================="
    echo "Service Readiness Verification"
    echo "Service: $SERVICE_NAME"
    echo "Expected Version: $EXPECTED_VERSION"
    echo "==========================================="
    echo ""
    
    local all_checks_passed=true
    
    # Critical checks (must pass)
    if ! check_container_status; then
        all_checks_passed=false
    fi
    
    if ! check_health_endpoint; then
        all_checks_passed=false
    fi
    
    if ! check_service_dependencies; then
        all_checks_passed=false
    fi
    
    # Non-critical checks (warnings only)
    check_info_endpoint
    check_metrics_endpoint
    check_service_logs
    check_resource_usage
    
    echo ""
    echo "==========================================="
    
    if $all_checks_passed; then
        log_success "$SERVICE_NAME is ready for production!"
        echo ""
        echo "Service Details:"
        echo "  Container: neo-${SERVICE_NAME}"
        echo "  Health: ✓ Healthy"
        echo "  Dependencies: ✓ Available"
        echo "  Version: $EXPECTED_VERSION"
        echo ""
        return 0
    else
        log_error "$SERVICE_NAME is not ready for production"
        echo ""
        echo "Please check the issues above and retry."
        echo "Logs: docker logs neo-${SERVICE_NAME}"
        echo ""
        return 1
    fi
}

# Run main function
main