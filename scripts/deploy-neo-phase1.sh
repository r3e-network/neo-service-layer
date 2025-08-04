#!/bin/bash

# Neo Service Layer - Phase 1 Deployment (Fixed)
# Uses different ports to avoid conflicts with existing services

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

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

# Create docker-compose.phase1.yml with non-conflicting ports
create_phase1_compose() {
    log_info "Creating Phase 1 Docker Compose configuration..."
    
    cat > docker-compose.phase1.yml << 'EOF'
services:
  # PostgreSQL Database
  neo-postgres-phase1:
    image: postgres:16-alpine
    container_name: neo-postgres-phase1
    environment:
      POSTGRES_DB: neo_service_layer
      POSTGRES_USER: neo_service_user
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    ports:
      - "15432:5432"  # Using 15432 to avoid conflict
    volumes:
      - postgres_data_phase1:/var/lib/postgresql/data
    networks:
      - neo-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U neo_service_user"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  # Redis Cache
  neo-redis-phase1:
    image: redis:7-alpine
    container_name: neo-redis-phase1
    command: redis-server --requirepass ${REDIS_PASSWORD}
    ports:
      - "16379:6379"  # Using 16379 to avoid conflict
    volumes:
      - redis_data_phase1:/data
    networks:
      - neo-network
    healthcheck:
      test: ["CMD", "redis-cli", "--raw", "-a", "${REDIS_PASSWORD}", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  # Consul Service Discovery
  neo-consul-phase1:
    image: hashicorp/consul:1.17
    container_name: neo-consul-phase1
    command: agent -server -bootstrap-expect=1 -ui -client=0.0.0.0
    ports:
      - "18500:8500"  # Using 18500 to avoid conflict
      - "18600:8600/udp"
    volumes:
      - consul_data_phase1:/consul/data
    networks:
      - neo-network
    healthcheck:
      test: ["CMD", "consul", "members"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  # Prometheus Monitoring
  neo-prometheus-phase1:
    image: prom/prometheus:v2.48.1
    container_name: neo-prometheus-phase1
    volumes:
      - ./monitoring/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus_data_phase1:/prometheus
    ports:
      - "19090:9090"  # Using 19090 to avoid conflict
    networks:
      - neo-network
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/usr/share/prometheus/console_libraries'
      - '--web.console.templates=/usr/share/prometheus/consoles'
    restart: unless-stopped

  # Grafana Dashboards
  neo-grafana-phase1:
    image: grafana/grafana:10.2.3
    container_name: neo-grafana-phase1
    volumes:
      - grafana_data_phase1:/var/lib/grafana
      - ./monitoring/grafana/dashboards:/etc/grafana/provisioning/dashboards:ro
      - ./monitoring/grafana/datasources:/etc/grafana/provisioning/datasources:ro
    ports:
      - "13000:3000"  # Using 13000 to avoid conflict
    environment:
      GF_SECURITY_ADMIN_PASSWORD: ${GRAFANA_ADMIN_PASSWORD}
      GF_USERS_ALLOW_SIGN_UP: "false"
      GF_INSTALL_PLUGINS: grafana-piechart-panel
    networks:
      - neo-network
    depends_on:
      - neo-prometheus-phase1
    restart: unless-stopped

  # API Gateway
  neo-api-gateway-phase1:
    build:
      context: .
      dockerfile: src/Api/NeoServiceLayer.Api/Dockerfile
    container_name: neo-api-gateway-phase1
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:80
      ConnectionStrings__DefaultConnection: Host=neo-postgres-phase1;Database=neo_service_layer;Username=neo_service_user;Password=${DB_PASSWORD}
      Redis__Configuration: neo-redis-phase1:6379,password=${REDIS_PASSWORD}
      Consul__Address: http://neo-consul-phase1:8500
      Jwt__SecretKey: ${JWT_SECRET_KEY}
      SERVICE_NAME: ApiGateway
    ports:
      - "8080:80"  # Using 8080 for API
    networks:
      - neo-network
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    restart: unless-stopped

  # Smart Contracts Service
  neo-smart-contracts-phase1:
    build:
      context: .
      dockerfile: src/Services/NeoServiceLayer.Services.SmartContracts/Dockerfile
    container_name: neo-smart-contracts-phase1
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: Host=neo-postgres-phase1;Database=neo_service_layer;Username=neo_service_user;Password=${DB_PASSWORD}
      Redis__Configuration: neo-redis-phase1:6379,password=${REDIS_PASSWORD}
      Consul__Address: http://neo-consul-phase1:8500
      Jwt__SecretKey: ${JWT_SECRET_KEY}
      SERVICE_NAME: SmartContracts
      SERVICE_PORT: 8081
    ports:
      - "8081:8080"
    networks:
      - neo-network
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    restart: unless-stopped

networks:
  neo-network:
    driver: bridge

volumes:
  postgres_data_phase1:
    driver: local
  redis_data_phase1:
    driver: local
  consul_data_phase1:
    driver: local
  prometheus_data_phase1:
    driver: local
  grafana_data_phase1:
    driver: local
EOF

    log_success "Phase 1 Docker Compose configuration created"
}

# Check environment
check_environment() {
    log_info "Checking environment..."
    
    # Check for .env.production
    if [ ! -f ".env.production" ]; then
        log_error ".env.production file not found"
        log_info "Creating from template..."
        if [ -f ".env.production.template" ]; then
            cp .env.production.template .env.production
            log_warn "Please edit .env.production with your actual values"
            exit 1
        else
            log_error "No .env.production.template found either"
            exit 1
        fi
    fi
    
    # Source environment variables
    set -a
    source .env.production 2>/dev/null || {
        log_error "Failed to load .env.production"
        exit 1
    }
    set +a
    
    # Check required variables
    local required_vars=("DB_PASSWORD" "REDIS_PASSWORD" "JWT_SECRET_KEY" "GRAFANA_ADMIN_PASSWORD")
    local missing=()
    
    for var in "${required_vars[@]}"; do
        if [ -z "${!var}" ]; then
            missing+=("$var")
        fi
    done
    
    if [ ${#missing[@]} -gt 0 ]; then
        log_error "Missing required environment variables: ${missing[*]}"
        exit 1
    fi
    
    log_success "Environment check passed"
}

# Stop conflicting services
stop_conflicting_services() {
    log_info "Checking for conflicting services..."
    
    # Stop any existing neo-service-layer containers
    local existing=$(docker ps -a --format '{{.Names}}' | grep -E '^neo-' || true)
    if [ -n "$existing" ]; then
        log_warn "Stopping existing Neo Service Layer containers..."
        echo "$existing" | xargs -r docker stop
        echo "$existing" | xargs -r docker rm
    fi
    
    log_success "Conflicting services handled"
}

# Deploy Phase 1
deploy_phase1() {
    log_info "Starting Phase 1 deployment..."
    
    # Create network if it doesn't exist
    docker network create neo-network 2>/dev/null || true
    
    # Start infrastructure services first
    log_info "Starting infrastructure services..."
    docker compose -f docker-compose.phase1.yml up -d \
        neo-postgres-phase1 \
        neo-redis-phase1 \
        neo-consul-phase1 \
        neo-prometheus-phase1 \
        neo-grafana-phase1
    
    # Wait for infrastructure
    log_info "Waiting for infrastructure services to be ready..."
    sleep 20
    
    # Check infrastructure health
    log_info "Checking infrastructure health..."
    
    # PostgreSQL
    if docker exec neo-postgres-phase1 pg_isready -U neo_service_user >/dev/null 2>&1; then
        log_success "PostgreSQL is ready"
    else
        log_error "PostgreSQL is not ready"
        return 1
    fi
    
    # Redis
    if docker exec neo-redis-phase1 redis-cli -a ${REDIS_PASSWORD} ping >/dev/null 2>&1; then
        log_success "Redis is ready"
    else
        log_error "Redis is not ready"
        return 1
    fi
    
    # Consul
    if curl -s http://localhost:18500/v1/status/leader >/dev/null 2>&1; then
        log_success "Consul is ready"
    else
        log_error "Consul is not ready"
        return 1
    fi
    
    # Start application services
    log_info "Starting application services..."
    docker compose -f docker-compose.phase1.yml up -d \
        neo-api-gateway-phase1 \
        neo-smart-contracts-phase1
    
    # Wait for services
    log_info "Waiting for application services to be ready..."
    sleep 30
    
    log_success "Phase 1 deployment completed"
}

# Show status
show_status() {
    echo ""
    log_info "Phase 1 Service Status:"
    docker compose -f docker-compose.phase1.yml ps
    
    echo ""
    log_info "Service URLs:"
    echo "  API Gateway:    http://localhost:8080"
    echo "  Smart Contracts: http://localhost:8081"
    echo "  Consul UI:      http://localhost:18500"
    echo "  Prometheus:     http://localhost:19090"
    echo "  Grafana:        http://localhost:13000 (admin/${GRAFANA_ADMIN_PASSWORD})"
    echo "  PostgreSQL:     localhost:15432"
    echo "  Redis:          localhost:16379"
    echo ""
    log_info "Health Check URLs:"
    echo "  API Health:     http://localhost:8080/health"
    echo "  Smart Contracts: http://localhost:8081/health"
    echo ""
}

# Main
main() {
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}  Neo Service Layer - Phase 1 Deployment${NC}"
    echo -e "${BLUE}========================================${NC}"
    echo ""
    
    # Check environment
    check_environment
    
    # Stop conflicting services
    stop_conflicting_services
    
    # Create compose file
    create_phase1_compose
    
    # Deploy
    if deploy_phase1; then
        show_status
        log_success "Phase 1 deployment successful!"
        echo ""
        echo "To stop services: docker compose -f docker-compose.phase1.yml down"
        echo "To view logs: docker compose -f docker-compose.phase1.yml logs -f [service-name]"
    else
        log_error "Phase 1 deployment failed"
        echo "Check logs: docker compose -f docker-compose.phase1.yml logs"
        exit 1
    fi
}

# Run main
main "$@"