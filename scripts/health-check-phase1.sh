#!/bin/bash

# Comprehensive health check for Phase 1 services
# Checks: API Gateway, SmartContracts, Configuration, Automation

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Counters
PASSED=0
FAILED=0
WARNINGS=0

log_info() {
    echo "[$(date +'%H:%M:%S')] $1"
}

log_success() {
    echo -e "${GREEN}✓ $1${NC}"
    ((PASSED++))
}

log_warn() {
    echo -e "${YELLOW}⚠ $1${NC}"
    ((WARNINGS++))
}

log_error() {
    echo -e "${RED}✗ $1${NC}"
    ((FAILED++))
}

log_section() {
    echo ""
    echo -e "${BLUE}=== $1 ===${NC}"
}

# Test API Gateway health
test_api_gateway() {
    log_section "API Gateway Health Checks"
    
    # Basic health endpoint
    if response=$(curl -s --max-time 10 http://localhost/health 2>/dev/null); then
        if echo "$response" | jq -e '.status == "Healthy"' >/dev/null 2>&1; then
            log_success "API Gateway health endpoint responding"
        else
            log_error "API Gateway health endpoint unhealthy: $response"
        fi
    else
        log_error "API Gateway health endpoint not accessible"
    fi
    
    # Ready endpoint
    if curl -s --max-time 10 http://localhost/health/ready >/dev/null 2>&1; then
        log_success "API Gateway ready endpoint responding"
    else
        log_warn "API Gateway ready endpoint not accessible"
    fi
    
    # Live endpoint
    if curl -s --max-time 10 http://localhost/health/live >/dev/null 2>&1; then
        log_success "API Gateway live endpoint responding"
    else
        log_warn "API Gateway live endpoint not accessible"
    fi
    
    # Info endpoint
    if response=$(curl -s --max-time 10 http://localhost/api/info 2>/dev/null); then
        if echo "$response" | jq -e '.name' >/dev/null 2>&1; then
            local name=$(echo "$response" | jq -r '.name')
            log_success "API Gateway info endpoint responding: $name"
        else
            log_warn "API Gateway info endpoint responding but malformed"
        fi
    else
        log_warn "API Gateway info endpoint not accessible"
    fi
    
    # Test authentication endpoint
    if response_code=$(curl -s -o /dev/null -w "%{http_code}" http://localhost/api/keymanagement/keys 2>/dev/null); then
        if [ "$response_code" = "401" ]; then
            log_success "API Gateway authentication working (401 unauthorized)"
        else
            log_warn "API Gateway authentication response: $response_code"
        fi
    else
        log_warn "API Gateway authentication endpoint not accessible"
    fi
}

# Test service discovery and registration
test_service_discovery() {
    log_section "Service Discovery Health Checks"
    
    # Check Consul agent
    if curl -s http://localhost:8500/v1/agent/self >/dev/null 2>&1; then
        log_success "Consul agent responding"
    else
        log_error "Consul agent not responding"
    fi
    
    # Check registered services
    if services=$(curl -s http://localhost:8500/v1/agent/services 2>/dev/null); then
        local service_count=$(echo "$services" | jq '. | length' 2>/dev/null || echo "0")
        if [ "$service_count" -gt 0 ]; then
            log_success "Services registered in Consul: $service_count"
        else
            log_warn "No services registered in Consul"
        fi
    else
        log_error "Cannot retrieve Consul services"
    fi
    
    # Check health checks
    if health_checks=$(curl -s http://localhost:8500/v1/agent/checks 2>/dev/null); then
        local check_count=$(echo "$health_checks" | jq '. | length' 2>/dev/null || echo "0")
        if [ "$check_count" -gt 0 ]; then
            log_success "Health checks registered: $check_count"
            
            # Count passing vs failing
            local passing=$(echo "$health_checks" | jq '[.[] | select(.Status == "passing")] | length' 2>/dev/null || echo "0")
            local failing=$(echo "$health_checks" | jq '[.[] | select(.Status != "passing")] | length' 2>/dev/null || echo "0")
            
            if [ "$failing" -eq 0 ]; then
                log_success "All health checks passing: $passing"
            else
                log_warn "Health checks - Passing: $passing, Failing: $failing"
            fi
        else
            log_warn "No health checks registered"
        fi
    else
        log_error "Cannot retrieve Consul health checks"
    fi
}

# Test database connectivity
test_database() {
    log_section "Database Health Checks"
    
    # Check PostgreSQL connection
    if docker exec neo-postgres-phase1 pg_isready -U ${DB_USER:-neo_service_user} >/dev/null 2>&1; then
        log_success "PostgreSQL accepting connections"
    else
        log_error "PostgreSQL not accepting connections"
    fi
    
    # Check database exists and is accessible
    if docker exec neo-postgres-phase1 psql -U ${DB_USER:-neo_service_user} -d ${DB_NAME:-neo_service_layer} -c "SELECT 1;" >/dev/null 2>&1; then
        log_success "Database accessible"
    else
        log_error "Database not accessible"
    fi
    
    # Check connection count
    if conn_count=$(docker exec neo-postgres-phase1 psql -U ${DB_USER:-neo_service_user} -d ${DB_NAME:-neo_service_layer} -t -c "SELECT count(*) FROM pg_stat_activity WHERE state = 'active';" 2>/dev/null); then
        conn_count=$(echo "$conn_count" | tr -d ' ')
        log_success "Database active connections: $conn_count"
    else
        log_warn "Cannot retrieve database connection count"
    fi
    
    # Check database size
    if db_size=$(docker exec neo-postgres-phase1 psql -U ${DB_USER:-neo_service_user} -d ${DB_NAME:-neo_service_layer} -t -c "SELECT pg_size_pretty(pg_database_size('${DB_NAME:-neo_service_layer}'));" 2>/dev/null); then
        db_size=$(echo "$db_size" | tr -d ' ')
        log_success "Database size: $db_size"
    else
        log_warn "Cannot retrieve database size"
    fi
}

# Test Redis connectivity
test_redis() {
    log_section "Redis Health Checks"
    
    # Check Redis ping
    if docker exec neo-redis-phase1 redis-cli ping >/dev/null 2>&1; then
        log_success "Redis responding to ping"
    else
        log_error "Redis not responding to ping"
    fi
    
    # Test Redis operations
    if docker exec neo-redis-phase1 redis-cli set health_check_key "test_value" >/dev/null 2>&1; then
        if value=$(docker exec neo-redis-phase1 redis-cli get health_check_key 2>/dev/null); then
            if [ "$value" = "test_value" ]; then
                log_success "Redis read/write operations working"
                docker exec neo-redis-phase1 redis-cli del health_check_key >/dev/null 2>&1
            else
                log_error "Redis read operation failed"
            fi
        else
            log_error "Redis read operation failed"
        fi
    else
        log_error "Redis write operation failed"
    fi
    
    # Check Redis info
    if redis_info=$(docker exec neo-redis-phase1 redis-cli info server 2>/dev/null); then
        local version=$(echo "$redis_info" | grep "redis_version" | cut -d: -f2 | tr -d '\r')
        local uptime=$(echo "$redis_info" | grep "uptime_in_seconds" | cut -d: -f2 | tr -d '\r')
        log_success "Redis version: $version, uptime: ${uptime}s"
    else
        log_warn "Cannot retrieve Redis info"
    fi
}

# Test monitoring stack
test_monitoring() {
    log_section "Monitoring Health Checks"
    
    # Check Prometheus
    if curl -s http://localhost:9090/-/ready >/dev/null 2>&1; then
        log_success "Prometheus ready"
        
        # Check if Prometheus is scraping targets
        if targets=$(curl -s http://localhost:9090/api/v1/targets 2>/dev/null); then
            local active_targets=$(echo "$targets" | jq '.data.activeTargets | length' 2>/dev/null || echo "0")
            if [ "$active_targets" -gt 0 ]; then
                log_success "Prometheus scraping $active_targets targets"
            else
                log_warn "Prometheus has no active targets"
            fi
        fi
    else
        log_error "Prometheus not ready"
    fi
    
    # Check Grafana
    if curl -s http://localhost:3000/api/health >/dev/null 2>&1; then
        log_success "Grafana responding"
    else
        log_error "Grafana not responding"
    fi
}

# Test container resource usage
test_resources() {
    log_section "Resource Usage Checks"
    
    local containers=("neo-api-gateway-phase1" "neo-smart-contracts-phase1" "neo-configuration-phase1" "neo-automation-phase1")
    
    for container in "${containers[@]}"; do
        if docker stats "$container" --no-stream >/dev/null 2>&1; then
            local stats=$(docker stats "$container" --no-stream --format "table {{.CPUPerc}}\t{{.MemUsage}}" | tail -1)
            local cpu=$(echo "$stats" | awk '{print $1}' | sed 's/%//')
            local mem=$(echo "$stats" | awk '{print $2}')
            
            log_success "$container - CPU: ${cpu}%, Memory: $mem"
            
            # Check for resource alerts
            if (( $(echo "$cpu > 80" | bc -l 2>/dev/null || echo "0") )); then
                log_warn "$container high CPU usage: ${cpu}%"
            fi
        else
            log_warn "Cannot get stats for $container"
        fi
    done
}

# Test service logs for errors
test_service_logs() {
    log_section "Service Log Analysis"
    
    local containers=("neo-api-gateway-phase1" "neo-smart-contracts-phase1" "neo-configuration-phase1" "neo-automation-phase1")
    
    for container in "${containers[@]}"; do
        if docker ps --format "{{.Names}}" | grep -q "$container"; then
            local error_count=$(docker logs "$container" --since="10m" 2>&1 | grep -ci "error\|exception\|failed\|fatal" || echo "0")
            local warn_count=$(docker logs "$container" --since="10m" 2>&1 | grep -ci "warn" || echo "0")
            
            if [ "$error_count" -eq 0 ]; then
                log_success "$container - No errors in recent logs"
            else
                log_warn "$container - $error_count errors in recent logs"
            fi
            
            if [ "$warn_count" -gt 5 ]; then
                log_warn "$container - $warn_count warnings in recent logs"
            fi
        else
            log_error "$container not running"
        fi
    done
}

# Main function
main() {
    echo -e "${BLUE}=========================================${NC}"
    echo -e "${BLUE}     Phase 1 Comprehensive Health Check  ${NC}"
    echo -e "${BLUE}=========================================${NC}"
    echo ""
    
    # Load environment variables
    if [ -f ".env.production" ]; then
        set -a
        source .env.production
        set +a
    fi
    
    # Run all health checks
    test_api_gateway
    test_service_discovery
    test_database
    test_redis
    test_monitoring
    test_resources
    test_service_logs
    
    # Summary
    echo ""
    echo -e "${BLUE}=========================================${NC}"
    echo -e "${BLUE}            Health Check Summary          ${NC}"
    echo -e "${BLUE}=========================================${NC}"
    echo ""
    echo -e "Passed:   ${GREEN}$PASSED${NC}"
    echo -e "Warnings: ${YELLOW}$WARNINGS${NC}"
    echo -e "Failed:   ${RED}$FAILED${NC}"
    echo ""
    
    # Overall status
    if [ $FAILED -eq 0 ]; then
        if [ $WARNINGS -eq 0 ]; then
            log_success "All health checks passed! Phase 1 is fully operational."
            echo ""
            echo "✅ Ready for Phase 2 deployment"
        else
            log_warn "Health checks passed with $WARNINGS warnings"
            echo ""
            echo "⚠️  Review warnings before proceeding to Phase 2"
        fi
        return 0
    else
        log_error "$FAILED critical health checks failed"
        echo ""
        echo "❌ Fix issues before proceeding"
        echo ""
        echo "Troubleshooting commands:"
        echo "  - View service logs: docker compose logs [service-name]"
        echo "  - Check service status: docker compose ps"
        echo "  - Restart services: docker compose restart [service-name]"
        echo ""
        return 1
    fi
}

# Run main function
main