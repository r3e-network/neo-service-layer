#!/bin/bash

# Comprehensive health check for Phase 2 services
# Checks: Key Management, Notification, Monitoring, Health, AI Pattern Recognition, AI Prediction, RabbitMQ

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

# Test Phase 2 services health
test_phase2_services() {
    log_section "Phase 2 Services Health Checks"
    
    local services=(
        "neo-keymanagement-phase2:8082:Key Management Service"
        "neo-notification-phase2:8083:Notification Service"
        "neo-monitoring-phase2:8084:Monitoring Service"
        "neo-health-phase2:8085:Health Service"
        "neo-ai-pattern-recognition-phase2:8086:AI Pattern Recognition Service"
        "neo-ai-prediction-phase2:8087:AI Prediction Service"
    )
    
    for service_info in "${services[@]}"; do
        IFS=':' read -r container port name <<< "$service_info"
        
        # Check if container is running
        if ! docker ps --format "{{.Names}}" | grep -q "^$container$"; then
            log_error "$name container not running"
            continue
        fi
        
        # Check health endpoint
        if response=$(curl -s --max-time 10 http://localhost:${port}/health 2>/dev/null); then
            if echo "$response" | jq -e '.status == "Healthy"' >/dev/null 2>&1; then
                log_success "$name health endpoint responding"
            else
                log_error "$name health endpoint unhealthy: $response"
            fi
        else
            log_error "$name health endpoint not accessible"
        fi
        
        # Check ready endpoint
        if curl -s --max-time 5 http://localhost:${port}/health/ready >/dev/null 2>&1; then
            log_success "$name ready endpoint responding"
        else
            log_warn "$name ready endpoint not accessible"
        fi
    done
}

# Test RabbitMQ health
test_rabbitmq() {
    log_section "RabbitMQ Health Checks"
    
    # Check if RabbitMQ container is running
    if ! docker ps --format "{{.Names}}" | grep -q "neo-rabbitmq-phase2"; then
        log_error "RabbitMQ container not running"
        return
    fi
    
    # Check RabbitMQ status
    if docker exec neo-rabbitmq-phase2 rabbitmqctl status >/dev/null 2>&1; then
        log_success "RabbitMQ node responding"
    else
        log_error "RabbitMQ node not responding"
    fi
    
    # Check RabbitMQ management interface
    if curl -s --max-time 10 http://localhost:15672/api/overview >/dev/null 2>&1; then
        log_success "RabbitMQ management interface accessible"
    else
        log_warn "RabbitMQ management interface not accessible"
    fi
    
    # Check queues
    if queue_info=$(docker exec neo-rabbitmq-phase2 rabbitmqctl list_queues 2>/dev/null); then
        local queue_count=$(echo "$queue_info" | wc -l)
        log_success "RabbitMQ queues accessible: $queue_count queues"
    else
        log_warn "Cannot retrieve RabbitMQ queue information"
    fi
    
    # Check exchanges
    if exchange_info=$(docker exec neo-rabbitmq-phase2 rabbitmqctl list_exchanges 2>/dev/null); then
        local exchange_count=$(echo "$exchange_info" | wc -l)
        log_success "RabbitMQ exchanges accessible: $exchange_count exchanges"
    else
        log_warn "Cannot retrieve RabbitMQ exchange information"
    fi
}

# Test service integrations
test_service_integrations() {
    log_section "Service Integration Health Checks"
    
    # Test Key Management Service encryption capabilities
    if response=$(curl -s --max-time 10 http://localhost:8082/api/keys/test 2>/dev/null); then
        log_success "Key Management Service API responding"
    else
        log_warn "Key Management Service API test endpoint not accessible"
    fi
    
    # Test Notification Service endpoints
    if response=$(curl -s --max-time 10 http://localhost:8083/api/notifications/channels 2>/dev/null); then
        log_success "Notification Service API responding"
    else
        log_warn "Notification Service API not accessible"
    fi
    
    # Test Monitoring Service metrics
    if response=$(curl -s --max-time 10 http://localhost:8084/api/metrics/summary 2>/dev/null); then
        log_success "Monitoring Service metrics API responding"
    else
        log_warn "Monitoring Service metrics API not accessible"
    fi
    
    # Test Health Service diagnostics
    if response=$(curl -s --max-time 10 http://localhost:8085/api/health/services 2>/dev/null); then
        log_success "Health Service diagnostics API responding"
    else
        log_warn "Health Service diagnostics API not accessible"
    fi
    
    # Test AI Pattern Recognition Service
    if response=$(curl -s --max-time 10 http://localhost:8086/api/patterns/status 2>/dev/null); then
        log_success "AI Pattern Recognition Service API responding"
    else
        log_warn "AI Pattern Recognition Service API not accessible"
    fi
    
    # Test AI Prediction Service
    if response=$(curl -s --max-time 10 http://localhost:8087/api/predictions/status 2>/dev/null); then
        log_success "AI Prediction Service API responding"
    else
        log_warn "AI Prediction Service API not accessible"
    fi
}

# Test Consul service registration
test_consul_registration() {
    log_section "Consul Service Registration Health Checks"
    
    # Check if Phase 2 services are registered
    if services=$(curl -s http://localhost:8500/v1/agent/services 2>/dev/null); then
        local expected_services=("KeyManagementService" "NotificationService" "MonitoringService" "HealthService" "AIPatternRecognitionService" "AIPredictionService")
        
        for service in "${expected_services[@]}"; do
            if echo "$services" | jq -e ".$service" >/dev/null 2>&1; then
                log_success "$service registered in Consul"
            else
                log_warn "$service not registered in Consul"
            fi
        done
    else
        log_error "Cannot retrieve Consul services"
    fi
    
    # Check health checks for Phase 2 services
    if health_checks=$(curl -s http://localhost:8500/v1/agent/checks 2>/dev/null); then
        local phase2_passing=$(echo "$health_checks" | jq '[.[] | select(.Status == "passing" and (.ServiceName == "KeyManagementService" or .ServiceName == "NotificationService" or .ServiceName == "MonitoringService" or .ServiceName == "HealthService" or .ServiceName == "AIPatternRecognitionService" or .ServiceName == "AIPredictionService"))] | length' 2>/dev/null || echo "0")
        local phase2_failing=$(echo "$health_checks" | jq '[.[] | select(.Status != "passing" and (.ServiceName == "KeyManagementService" or .ServiceName == "NotificationService" or .ServiceName == "MonitoringService" or .ServiceName == "HealthService" or .ServiceName == "AIPatternRecognitionService" or .ServiceName == "AIPredictionService"))] | length' 2>/dev/null || echo "0")
        
        if [ "$phase2_failing" -eq 0 ]; then
            log_success "All Phase 2 health checks passing: $phase2_passing"
        else
            log_warn "Phase 2 health checks - Passing: $phase2_passing, Failing: $phase2_failing"
        fi
    else
        log_error "Cannot retrieve Consul health checks"
    fi
}

# Test resource usage for Phase 2 services
test_resource_usage() {
    log_section "Phase 2 Resource Usage Checks"
    
    local containers=("neo-keymanagement-phase2" "neo-notification-phase2" "neo-monitoring-phase2" "neo-health-phase2" "neo-ai-pattern-recognition-phase2" "neo-ai-prediction-phase2" "neo-rabbitmq-phase2")
    
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

# Test Phase 2 service logs for errors
test_service_logs() {
    log_section "Phase 2 Service Log Analysis"
    
    local containers=("neo-keymanagement-phase2" "neo-notification-phase2" "neo-monitoring-phase2" "neo-health-phase2" "neo-ai-pattern-recognition-phase2" "neo-ai-prediction-phase2" "neo-rabbitmq-phase2")
    
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

# Test integration with Phase 1 services
test_phase1_integration() {
    log_section "Phase 1 Integration Health Checks"
    
    # Test database connectivity from Phase 2 services
    if docker exec neo-keymanagement-phase2 curl -sf http://neo-postgres-phase1:5432 >/dev/null 2>&1; then
        log_success "Phase 2 services can reach PostgreSQL"
    else
        log_warn "Phase 2 services cannot reach PostgreSQL"
    fi
    
    # Test Redis connectivity from Phase 2 services
    if docker exec neo-notification-phase2 nc -z neo-redis-phase1 6379 >/dev/null 2>&1; then
        log_success "Phase 2 services can reach Redis"
    else
        log_warn "Phase 2 services cannot reach Redis"
    fi
    
    # Test Consul connectivity from Phase 2 services
    if docker exec neo-monitoring-phase2 curl -sf http://neo-consul-phase1:8500/v1/status/leader >/dev/null 2>&1; then
        log_success "Phase 2 services can reach Consul"
    else
        log_warn "Phase 2 services cannot reach Consul"
    fi
}

# Main function
main() {
    echo -e "${BLUE}=========================================${NC}"
    echo -e "${BLUE}     Phase 2 Comprehensive Health Check  ${NC}"
    echo -e "${BLUE}=========================================${NC}"
    echo ""
    
    # Load environment variables
    if [ -f ".env.production" ]; then
        set -a
        source .env.production
        set +a
    fi
    
    # Run all health checks
    test_phase2_services
    test_rabbitmq
    test_service_integrations
    test_consul_registration
    test_resource_usage
    test_service_logs
    test_phase1_integration
    
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
            log_success "All Phase 2 health checks passed! Phase 2 is fully operational."
            echo ""
            echo "✅ Ready for Phase 3 deployment"
        else
            log_warn "Phase 2 health checks passed with $WARNINGS warnings"
            echo ""
            echo "⚠️  Review warnings before proceeding to Phase 3"
        fi
        return 0
    else
        log_error "$FAILED critical health checks failed"
        echo ""
        echo "❌ Fix issues before proceeding"
        echo ""
        echo "Troubleshooting commands:"
        echo "  - View service logs: docker logs [service-name]"
        echo "  - Check service status: docker ps"
        echo "  - Restart Phase 2 services: docker compose -f docker compose.phase2.yml restart"
        echo ""
        return 1
    fi
}

# Run main function
main