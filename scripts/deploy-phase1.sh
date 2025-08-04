#!/bin/bash

# Phase 1 Deployment Script - Core Foundation Services
# Deploys: API Gateway, SmartContracts, Configuration, Automation services

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
PHASE="Phase1"
DEPLOYMENT_LOG="deployment-phase1-$(date +%Y%m%d-%H%M%S).log"
TIMEOUT=300

echo -e "${BLUE}════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}    Neo Service Layer - Phase 1 Deployment          ${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════${NC}"
echo ""

# Logging function
log() {
    echo "[$(date +'%Y-%m-%d %H:%M:%S')] $1" | tee -a "$DEPLOYMENT_LOG"
}

log_success() {
    echo -e "${GREEN}✓ $1${NC}" | tee -a "$DEPLOYMENT_LOG"
}

log_warn() {
    echo -e "${YELLOW}⚠ $1${NC}" | tee -a "$DEPLOYMENT_LOG"
}

log_error() {
    echo -e "${RED}✗ $1${NC}" | tee -a "$DEPLOYMENT_LOG"
}

# Error handler
handle_error() {
    log_error "Deployment failed at step: $1"
    echo ""
    echo "Check deployment log: $DEPLOYMENT_LOG"
    echo "Run './scripts/rollback-phase1.sh' to rollback"
    exit 1
}

# Step 1: Verify Prerequisites
log "Step 1: Verifying prerequisites..."
if ! ./scripts/verify-prerequisites.sh; then
    handle_error "Prerequisites verification"
fi
log_success "Prerequisites verified"

# Step 2: Generate Production Credentials
log "Step 2: Generating production credentials..."
if [ ! -f ".env.production" ]; then
    if ! ./scripts/generate-secure-credentials.sh; then
        handle_error "Credential generation"
    fi
    log_success "Production credentials generated"
else
    log_warn "Production credentials already exist, skipping generation"
fi

# Step 3: Update Configuration
log "Step 3: Updating production configuration..."
if ! ./scripts/update-production-config.sh; then
    handle_error "Configuration update"
fi
log_success "Production configuration updated"

# Step 4: Create Phase 1 Docker Compose File
log "Step 4: Creating Phase 1 deployment configuration..."
cat > docker-compose.phase1.yml << 'EOF'
version: '3.8'

x-common-variables: &common-variables
  ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Production}
  Consul__Address: http://consul:8500
  ConnectionStrings__DefaultConnection: Host=${DB_HOST};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
  Redis__Configuration: ${REDIS_HOST}:${REDIS_PORT},password=${REDIS_PASSWORD}
  Jwt__SecretKey: ${JWT_SECRET_KEY}

x-service-defaults: &service-defaults
  restart: unless-stopped
  networks:
    - neo-network
  environment:
    <<: *common-variables
  depends_on:
    - consul
    - postgres
    - redis
  deploy:
    resources:
      limits:
        memory: 1G
      reservations:
        memory: 512M

services:
  # Infrastructure Services
  consul:
    image: hashicorp/consul:1.17
    container_name: neo-consul-phase1
    command: agent -server -bootstrap-expect=1 -ui -client=0.0.0.0
    ports:
      - "8500:8500"
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "consul", "members"]
      interval: 30s
      timeout: 3s
      retries: 3

  postgres:
    image: postgres:16-alpine
    container_name: neo-postgres-phase1
    environment:
      - POSTGRES_DB=${DB_NAME}
      - POSTGRES_USER=${DB_USER}
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER} -d ${DB_NAME}"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: neo-redis-phase1
    command: redis-server --requirepass ${REDIS_PASSWORD}
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "redis-cli", "--raw", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  prometheus:
    image: prom/prometheus:v2.48.1
    container_name: neo-prometheus-phase1
    volumes:
      - ./monitoring/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus_data:/prometheus
    ports:
      - "9090:9090"
    networks:
      - neo-network
    restart: unless-stopped

  grafana:
    image: grafana/grafana:10.2.3
    container_name: neo-grafana-phase1
    volumes:
      - grafana_data:/var/lib/grafana
      - ./monitoring/grafana/dashboards:/etc/grafana/provisioning/dashboards:ro
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_ADMIN_PASSWORD}
    networks:
      - neo-network
    restart: unless-stopped

  # Phase 1 Core Services
  api-gateway:
    build:
      context: .
      dockerfile: src/Api/NeoServiceLayer.Api/Dockerfile
    container_name: neo-api-gateway-phase1
    <<: *service-defaults
    ports:
      - "80:80"
      - "443:443"
    environment:
      <<: *common-variables
      SERVICE_NAME: ApiGateway
    volumes:
      - ./certificates:/https:ro
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  smart-contracts:
    build:
      context: .
      dockerfile: src/Services/NeoServiceLayer.Services.SmartContracts/Dockerfile
    container_name: neo-smart-contracts-phase1
    <<: *service-defaults
    environment:
      <<: *common-variables
      SERVICE_NAME: SmartContracts

  configuration:
    build:
      context: .
      dockerfile: src/Services/NeoServiceLayer.Services.Configuration/Dockerfile
    container_name: neo-configuration-phase1
    <<: *service-defaults
    environment:
      <<: *common-variables
      SERVICE_NAME: Configuration

  automation:
    build:
      context: .
      dockerfile: src/Services/NeoServiceLayer.Services.Automation/Dockerfile
    container_name: neo-automation-phase1
    <<: *service-defaults
    environment:
      <<: *common-variables
      SERVICE_NAME: Automation

networks:
  neo-network:
    driver: bridge

volumes:
  postgres_data:
  prometheus_data:
  grafana_data:
EOF

log_success "Phase 1 deployment configuration created"

# Step 5: Deploy Infrastructure Services
log "Step 5: Deploying infrastructure services..."
docker compose -f docker-compose.phase1.yml up -d consul postgres redis prometheus grafana

# Wait for infrastructure services
log "Waiting for infrastructure services to be ready..."
./scripts/wait-for-infrastructure.sh consul postgres redis

log_success "Infrastructure services deployed and ready"

# Step 6: Deploy Core Services
log "Step 6: Deploying core services..."
docker compose -f docker-compose.phase1.yml up -d api-gateway smart-contracts configuration automation

log_success "Core services deployed"

# Step 7: Wait for Service Readiness
log "Step 7: Waiting for services to be ready..."
sleep 30

# Check each service
SERVICES=("api-gateway" "smart-contracts" "configuration" "automation")
for service in "${SERVICES[@]}"; do
    log "Checking $service readiness..."
    if ! ./scripts/verify-service-readiness.sh "$service" "1.0.0"; then
        handle_error "$service readiness check"
    fi
    log_success "$service is ready"
done

# Step 8: Run Health Checks
log "Step 8: Running comprehensive health checks..."
if ! ./scripts/health-check-phase1.sh; then
    handle_error "Health checks"
fi
log_success "All health checks passed"

# Step 9: Run Smoke Tests
log "Step 9: Running smoke tests..."
if ! ./scripts/smoke-tests.sh "http://localhost"; then
    log_warn "Some smoke tests failed, but deployment continues"
else
    log_success "All smoke tests passed"
fi

# Step 10: Configure Monitoring
log "Step 10: Configuring monitoring dashboards..."
./scripts/setup-monitoring-phase1.sh
log_success "Monitoring configured"

# Deployment Summary
echo ""
echo -e "${GREEN}════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}         Phase 1 Deployment Completed Successfully   ${NC}"
echo -e "${GREEN}════════════════════════════════════════════════════${NC}"
echo ""

log "Phase 1 deployment completed successfully"

# Display service URLs
echo "Service URLs:"
echo "  API Gateway: http://localhost (Health: http://localhost/health)"
echo "  Consul UI: http://localhost:8500"
echo "  Prometheus: http://localhost:9090"
echo "  Grafana: http://localhost:3000 (admin/${GRAFANA_ADMIN_PASSWORD})"
echo ""

echo "Next Steps:"
echo "1. Verify all services are working: curl http://localhost/health"
echo "2. Check monitoring dashboards: http://localhost:3000"
echo "3. Review deployment logs: $DEPLOYMENT_LOG"
echo "4. Prepare for Phase 2: ./scripts/deploy-phase2.sh"
echo ""

echo "To rollback this deployment: ./scripts/rollback-phase1.sh"
echo "Deployment log saved to: $DEPLOYMENT_LOG"