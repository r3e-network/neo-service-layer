#!/bin/bash

# Minimal Phase 1 Deployment - Infrastructure Only
# This deploys just the core infrastructure services without the application services

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Phase 1 infrastructure is already running
check_infrastructure() {
    log_info "Checking Phase 1 infrastructure status..."
    
    local services=("neo-postgres-phase1" "neo-redis-phase1" "neo-consul-phase1" "neo-prometheus-phase1" "neo-grafana-phase1")
    local all_running=true
    
    for service in "${services[@]}"; do
        if docker ps --format "{{.Names}}" | grep -q "^$service$"; then
            log_success "$service is running"
        else
            log_error "$service is not running"
            all_running=false
        fi
    done
    
    if [ "$all_running" = true ]; then
        return 0
    else
        return 1
    fi
}

# Test infrastructure connectivity
test_infrastructure() {
    log_info "Testing infrastructure connectivity..."
    
    # Test PostgreSQL
    if docker exec neo-postgres-phase1 pg_isready -U neo_service_user >/dev/null 2>&1; then
        log_success "PostgreSQL is accessible at localhost:15432"
    else
        log_error "PostgreSQL is not accessible"
    fi
    
    # Test Redis
    if docker exec neo-redis-phase1 redis-cli ping >/dev/null 2>&1; then
        log_success "Redis is accessible at localhost:16379"
    else
        log_error "Redis is not accessible"
    fi
    
    # Test Consul
    if curl -s http://localhost:18500/v1/status/leader >/dev/null 2>&1; then
        log_success "Consul is accessible at http://localhost:18500"
    else
        log_error "Consul is not accessible"
    fi
    
    # Test Prometheus
    if curl -s http://localhost:19090/-/ready >/dev/null 2>&1; then
        log_success "Prometheus is accessible at http://localhost:19090"
    else
        log_error "Prometheus is not accessible"
    fi
    
    # Test Grafana
    if curl -s http://localhost:13000/api/health >/dev/null 2>&1; then
        log_success "Grafana is accessible at http://localhost:13000"
    else
        log_error "Grafana is not accessible"
    fi
}

# Create simple API test
create_api_test() {
    log_info "Creating simple API test endpoint..."
    
    # Create a simple test API using existing runtime base image
    cat > docker-compose.api-test.yml << 'EOF'
services:
  neo-api-test:
    image: mcr.microsoft.com/dotnet/samples:aspnetapp
    container_name: neo-api-test
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_ENVIRONMENT=Production
    networks:
      - neo-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080"]
      interval: 30s
      timeout: 10s
      retries: 3
    restart: unless-stopped

networks:
  neo-network:
    external: true
EOF

    log_info "Starting test API..."
    docker compose -f docker-compose.api-test.yml up -d
    
    # Wait for API to start
    sleep 10
    
    if curl -s http://localhost:8080 >/dev/null 2>&1; then
        log_success "Test API is running at http://localhost:8080"
    else
        log_error "Test API failed to start"
    fi
}

# Main
main() {
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}  Neo Service Layer - Minimal Phase 1   ${NC}"
    echo -e "${BLUE}========================================${NC}"
    echo ""
    
    # Check if infrastructure is running
    if check_infrastructure; then
        log_success "Phase 1 infrastructure is already running!"
        echo ""
        
        # Test connectivity
        test_infrastructure
        
        echo ""
        log_info "Infrastructure Summary:"
        echo "  PostgreSQL:  localhost:15432 (user: neo_service_user)"
        echo "  Redis:       localhost:16379"
        echo "  Consul UI:   http://localhost:18500"
        echo "  Prometheus:  http://localhost:19090"
        echo "  Grafana:     http://localhost:13000"
        echo ""
        
        # Ask if user wants to deploy test API
        read -p "Deploy a test API endpoint? (y/n): " deploy_test
        if [ "$deploy_test" = "y" ]; then
            create_api_test
        fi
        
        echo ""
        log_success "Infrastructure is ready for application deployment!"
        echo ""
        echo "Next steps:"
        echo "1. Fix the compilation errors in the SmartContracts service"
        echo "2. Run the full deployment script once fixed"
        echo ""
        echo "To view logs: docker compose -f docker-compose.phase1.yml logs -f [service-name]"
        echo "To stop all: docker compose -f docker-compose.phase1.yml down"
    else
        log_error "Phase 1 infrastructure is not fully running"
        echo ""
        echo "Please run ./scripts/deploy-neo-phase1.sh first"
    fi
}

# Run main
main "$@"