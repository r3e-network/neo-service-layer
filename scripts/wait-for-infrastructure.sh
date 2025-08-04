#!/bin/bash

# Wait for infrastructure services to be ready
# Usage: ./wait-for-infrastructure.sh [service1] [service2] [service3]

set -e

# Default services if none specified
SERVICES=(${@:-consul postgres redis})
MAX_WAIT=300  # 5 minutes
CHECK_INTERVAL=10

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

# Wait for Consul to be ready
wait_for_consul() {
    log_info "Waiting for Consul to be ready..."
    local elapsed=0
    
    while [ $elapsed -lt $MAX_WAIT ]; do
        if curl -s http://localhost:8500/v1/status/leader >/dev/null 2>&1; then
            if consul_leader=$(curl -s http://localhost:8500/v1/status/leader 2>/dev/null); then
                if [ "$consul_leader" != '""' ]; then
                    log_success "Consul is ready and has a leader"
                    return 0
                fi
            fi
        fi
        
        sleep $CHECK_INTERVAL
        elapsed=$((elapsed + CHECK_INTERVAL))
        log_info "Consul not ready yet... (${elapsed}s/${MAX_WAIT}s)"
    done
    
    log_error "Consul failed to become ready within ${MAX_WAIT} seconds"
    return 1
}

# Wait for PostgreSQL to be ready
wait_for_postgres() {
    log_info "Waiting for PostgreSQL to be ready..."
    local elapsed=0
    
    while [ $elapsed -lt $MAX_WAIT ]; do
        # Check if container is running
        if docker ps --format "{{.Names}}" | grep -q "neo-postgres"; then
            # Check if PostgreSQL is accepting connections
            if docker exec neo-postgres-phase1 pg_isready -U ${DB_USER:-neo_service_user} >/dev/null 2>&1; then
                log_success "PostgreSQL is ready and accepting connections"
                
                # Verify database exists
                if docker exec neo-postgres-phase1 psql -U ${DB_USER:-neo_service_user} -d ${DB_NAME:-neo_service_layer} -c "SELECT 1;" >/dev/null 2>&1; then
                    log_success "Database ${DB_NAME:-neo_service_layer} is accessible"
                    return 0
                else
                    log_warn "Database exists but not accessible yet"
                fi
            fi
        fi
        
        sleep $CHECK_INTERVAL
        elapsed=$((elapsed + CHECK_INTERVAL))
        log_info "PostgreSQL not ready yet... (${elapsed}s/${MAX_WAIT}s)"
    done
    
    log_error "PostgreSQL failed to become ready within ${MAX_WAIT} seconds"
    return 1
}

# Wait for Redis to be ready
wait_for_redis() {
    log_info "Waiting for Redis to be ready..."
    local elapsed=0
    
    while [ $elapsed -lt $MAX_WAIT ]; do
        # Check if container is running
        if docker ps --format "{{.Names}}" | grep -q "neo-redis"; then
            # Check if Redis is responding to ping
            if docker exec neo-redis-phase1 redis-cli ping >/dev/null 2>&1; then
                log_success "Redis is ready and responding"
                
                # Test basic operations
                if docker exec neo-redis-phase1 redis-cli set test_key "test_value" >/dev/null 2>&1; then
                    if docker exec neo-redis-phase1 redis-cli get test_key >/dev/null 2>&1; then
                        docker exec neo-redis-phase1 redis-cli del test_key >/dev/null 2>&1
                        log_success "Redis operations working correctly"
                        return 0
                    fi
                fi
            fi
        fi
        
        sleep $CHECK_INTERVAL
        elapsed=$((elapsed + CHECK_INTERVAL))
        log_info "Redis not ready yet... (${elapsed}s/${MAX_WAIT}s)"
    done
    
    log_error "Redis failed to become ready within ${MAX_WAIT} seconds"
    return 1
}

# Wait for Prometheus to be ready
wait_for_prometheus() {
    log_info "Waiting for Prometheus to be ready..."
    local elapsed=0
    
    while [ $elapsed -lt $MAX_WAIT ]; do
        if curl -s http://localhost:9090/-/ready >/dev/null 2>&1; then
            log_success "Prometheus is ready"
            return 0
        fi
        
        sleep $CHECK_INTERVAL
        elapsed=$((elapsed + CHECK_INTERVAL))
        log_info "Prometheus not ready yet... (${elapsed}s/${MAX_WAIT}s)"
    done
    
    log_error "Prometheus failed to become ready within ${MAX_WAIT} seconds"
    return 1
}

# Wait for Grafana to be ready
wait_for_grafana() {
    log_info "Waiting for Grafana to be ready..."
    local elapsed=0
    
    while [ $elapsed -lt $MAX_WAIT ]; do
        if curl -s http://localhost:3000/api/health >/dev/null 2>&1; then
            log_success "Grafana is ready"
            return 0
        fi
        
        sleep $CHECK_INTERVAL
        elapsed=$((elapsed + CHECK_INTERVAL))
        log_info "Grafana not ready yet... (${elapsed}s/${MAX_WAIT}s)"
    done
    
    log_error "Grafana failed to become ready within ${MAX_WAIT} seconds"
    return 1
}

# Wait for RabbitMQ to be ready
wait_for_rabbitmq() {
    log_info "Waiting for RabbitMQ to be ready..."
    local elapsed=0
    
    while [ $elapsed -lt $MAX_WAIT ]; do
        if docker exec neo-rabbitmq rabbitmq-diagnostics ping >/dev/null 2>&1; then
            log_success "RabbitMQ is ready"
            return 0
        fi
        
        sleep $CHECK_INTERVAL
        elapsed=$((elapsed + CHECK_INTERVAL))
        log_info "RabbitMQ not ready yet... (${elapsed}s/${MAX_WAIT}s)"
    done
    
    log_error "RabbitMQ failed to become ready within ${MAX_WAIT} seconds"
    return 1
}

# Main function
main() {
    echo "=========================================="
    echo "Infrastructure Readiness Check"
    echo "Services: ${SERVICES[*]}"
    echo "Max wait time: ${MAX_WAIT} seconds"
    echo "=========================================="
    echo ""
    
    local all_ready=true
    local start_time=$(date +%s)
    
    for service in "${SERVICES[@]}"; do
        case "$service" in
            "consul")
                if ! wait_for_consul; then
                    all_ready=false
                fi
                ;;
            "postgres"|"postgresql")
                if ! wait_for_postgres; then
                    all_ready=false
                fi
                ;;
            "redis")
                if ! wait_for_redis; then
                    all_ready=false
                fi
                ;;
            "prometheus")
                if ! wait_for_prometheus; then
                    all_ready=false
                fi
                ;;
            "grafana")
                if ! wait_for_grafana; then
                    all_ready=false
                fi
                ;;
            "rabbitmq")
                if ! wait_for_rabbitmq; then
                    all_ready=false
                fi
                ;;
            *)
                log_warn "Unknown service: $service"
                ;;
        esac
        
        echo ""
    done
    
    local end_time=$(date +%s)
    local total_time=$((end_time - start_time))
    
    echo "=========================================="
    if $all_ready; then
        log_success "All infrastructure services are ready! (${total_time}s)"
        echo ""
        echo "Service Status:"
        for service in "${SERVICES[@]}"; do
            echo "  ✓ $service"
        done
        echo ""
        return 0
    else
        log_error "Some infrastructure services failed to become ready"
        echo ""
        echo "Troubleshooting:"
        echo "  - Check container logs: docker compose logs <service>"
        echo "  - Check container status: docker compose ps"
        echo "  - Check resource usage: docker stats"
        echo "  - Check disk space: df -h"
        echo ""
        return 1
    fi
}

# Run main function
main