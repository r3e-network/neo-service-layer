#!/bin/bash

# Phase 2 Deployment Script for Neo Service Layer
# Services: Key Management, Notification, Monitoring, Health
# AI Services: Pattern Recognition, Prediction

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
COMPOSE_FILE="docker-compose.phase2.yml"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DEPLOYMENT_TIMEOUT=600 # 10 minutes

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

log_section() {
    echo ""
    echo -e "${BLUE}=== $1 ===${NC}"
}

# Check if Phase 1 is healthy
check_phase1_health() {
    log_section "Checking Phase 1 Health"
    
    if ! "$SCRIPT_DIR/health-check-phase1.sh"; then
        log_error "Phase 1 is not healthy. Fix Phase 1 issues before deploying Phase 2."
        exit 1
    fi
    
    log_success "Phase 1 is healthy, proceeding with Phase 2 deployment"
}

# Generate Phase 2 Docker Compose file
generate_compose_file() {
    log_section "Generating Phase 2 Docker Compose Configuration"
    
    cat > "$PROJECT_ROOT/$COMPOSE_FILE" << 'EOF'
version: '3.8'

services:
  # Key Management Service
  neo-keymanagement-phase2:
    build:
      context: ./src/Services/NeoServiceLayer.Services.KeyManagement
      dockerfile: Dockerfile
    container_name: neo-keymanagement-phase2
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - SERVICE_NAME=KeyManagementService
      - SERVICE_PORT=8082
      - SGX_MODE=${SGX_MODE:-SIM}
      - ENCLAVE_CONFIG_FILE=/app/enclave.json
    ports:
      - "8082:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    volumes:
      - keymanagement-data:/app/data
      - ./enclave.json:/app/enclave.json:ro
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '1.0'
        reservations:
          memory: 512M
          cpus: '0.5'

  # Notification Service
  neo-notification-phase2:
    build:
      context: ./src/Services/NeoServiceLayer.Services.Notification
      dockerfile: Dockerfile
    container_name: neo-notification-phase2
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - RABBITMQ_CONNECTION_STRING=amqp://guest:guest@neo-rabbitmq-phase2:5672/
      - SERVICE_NAME=NotificationService
      - SERVICE_PORT=8083
      - EMAIL_SMTP_HOST=${EMAIL_SMTP_HOST:-smtp.gmail.com}
      - EMAIL_SMTP_PORT=${EMAIL_SMTP_PORT:-587}
      - EMAIL_USERNAME=${EMAIL_USERNAME}
      - EMAIL_PASSWORD=${EMAIL_PASSWORD}
      - SMS_API_KEY=${SMS_API_KEY}
      - WEBHOOK_SECRET=${WEBHOOK_SECRET}
    ports:
      - "8083:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
      - neo-rabbitmq-phase2
    volumes:
      - notification-data:/app/data
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.25'

  # Monitoring Service
  neo-monitoring-phase2:
    build:
      context: ./src/Services/NeoServiceLayer.Services.Monitoring
      dockerfile: Dockerfile
    container_name: neo-monitoring-phase2
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - PROMETHEUS_URL=http://neo-prometheus-phase1:9090
      - GRAFANA_URL=http://neo-grafana-phase1:3000
      - SERVICE_NAME=MonitoringService
      - SERVICE_PORT=8084
      - METRICS_COLLECTION_INTERVAL=30
      - ALERT_NOTIFICATION_WEBHOOK=${ALERT_WEBHOOK_URL}
    ports:
      - "8084:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
      - neo-prometheus-phase1
      - neo-grafana-phase1
    volumes:
      - monitoring-data:/app/data
      - ./monitoring/dashboards:/app/dashboards:ro
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.25'

  # Health Service
  neo-health-phase2:
    build:
      context: ./src/Services/NeoServiceLayer.Services.Health
      dockerfile: Dockerfile
    container_name: neo-health-phase2
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - SERVICE_NAME=HealthService
      - SERVICE_PORT=8085
      - HEALTH_CHECK_INTERVAL=30
      - DEPENDENCY_TIMEOUT=10
    ports:
      - "8085:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    volumes:
      - health-data:/app/data
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 256M
          cpus: '0.25'
        reservations:
          memory: 128M
          cpus: '0.1'

  # AI Pattern Recognition Service
  neo-ai-pattern-recognition-phase2:
    build:
      context: ./src/AI/NeoServiceLayer.AI.PatternRecognition
      dockerfile: Dockerfile
    container_name: neo-ai-pattern-recognition-phase2
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - SERVICE_NAME=AIPatternRecognitionService
      - SERVICE_PORT=8086
      - ML_MODEL_PATH=/app/models
      - PATTERN_ANALYSIS_THREADS=4
      - BATCH_SIZE=100
    ports:
      - "8086:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    volumes:
      - ai-pattern-models:/app/models
      - ai-pattern-data:/app/data
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: '2.0'
        reservations:
          memory: 1G
          cpus: '1.0'

  # AI Prediction Service
  neo-ai-prediction-phase2:
    build:
      context: ./src/AI/NeoServiceLayer.AI.Prediction
      dockerfile: Dockerfile
    container_name: neo-ai-prediction-phase2
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - SERVICE_NAME=AIPredictionService
      - SERVICE_PORT=8087
      - ML_MODEL_PATH=/app/models
      - PREDICTION_CACHE_TTL=300
      - MODEL_REFRESH_INTERVAL=3600
    ports:
      - "8087:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    volumes:
      - ai-prediction-models:/app/models
      - ai-prediction-data:/app/data
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: '2.0'
        reservations:
          memory: 1G
          cpus: '1.0'

  # RabbitMQ for message queuing
  neo-rabbitmq-phase2:
    image: rabbitmq:3.12-management
    container_name: neo-rabbitmq-phase2
    environment:
      - RABBITMQ_DEFAULT_USER=admin
      - RABBITMQ_DEFAULT_PASS=${RABBITMQ_PASSWORD}
      - RABBITMQ_SERVER_ADDITIONAL_ERL_ARGS=-rabbit log_levels [{connection,error},{default,warning}]
    ports:
      - "5672:5672"
      - "15672:15672"
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq
      - ./rabbitmq/enabled_plugins:/etc/rabbitmq/enabled_plugins:ro
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "rabbitmqctl", "status"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '1.0'
        reservations:
          memory: 512M
          cpus: '0.5'

volumes:
  keymanagement-data:
    driver: local
  notification-data:
    driver: local
  monitoring-data:
    driver: local
  health-data:
    driver: local
  ai-pattern-models:
    driver: local
  ai-pattern-data:
    driver: local
  ai-prediction-models:
    driver: local
  ai-prediction-data:
    driver: local
  rabbitmq-data:
    driver: local

networks:
  neo-network:
    external: true

EOF

    log_success "Generated Phase 2 Docker Compose configuration"
}

# Create RabbitMQ configuration
create_rabbitmq_config() {
    log_section "Creating RabbitMQ Configuration"
    
    mkdir -p "$PROJECT_ROOT/rabbitmq"
    
    cat > "$PROJECT_ROOT/rabbitmq/enabled_plugins" << 'EOF'
[rabbitmq_management,rabbitmq_prometheus,rabbitmq_shovel,rabbitmq_shovel_management].
EOF

    log_success "Created RabbitMQ configuration"
}

# Update gateway configuration for Phase 2 services
update_gateway_config() {
    log_section "Updating API Gateway Configuration for Phase 2"
    
    # Add Phase 2 services to Consul configuration
    cat > "$PROJECT_ROOT/consul/config/phase2-services.json" << EOF
{
  "services": [
    {
      "name": "KeyManagementService",
      "id": "keymanagement-1",
      "address": "neo-keymanagement-phase2",
      "port": 8080,
      "tags": ["phase2", "security", "keys"],
      "check": {
        "http": "http://neo-keymanagement-phase2:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    },
    {
      "name": "NotificationService",
      "id": "notification-1",
      "address": "neo-notification-phase2",
      "port": 8080,
      "tags": ["phase2", "communication", "alerts"],
      "check": {
        "http": "http://neo-notification-phase2:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    },
    {
      "name": "MonitoringService",
      "id": "monitoring-1",
      "address": "neo-monitoring-phase2",
      "port": 8080,
      "tags": ["phase2", "monitoring", "metrics"],
      "check": {
        "http": "http://neo-monitoring-phase2:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    },
    {
      "name": "HealthService",
      "id": "health-1",
      "address": "neo-health-phase2",
      "port": 8080,
      "tags": ["phase2", "health", "diagnostics"],
      "check": {
        "http": "http://neo-health-phase2:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    },
    {
      "name": "AIPatternRecognitionService",
      "id": "ai-pattern-recognition-1",
      "address": "neo-ai-pattern-recognition-phase2",
      "port": 8080,
      "tags": ["phase2", "ai", "pattern-recognition"],
      "check": {
        "http": "http://neo-ai-pattern-recognition-phase2:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    },
    {
      "name": "AIPredictionService",
      "id": "ai-prediction-1",
      "address": "neo-ai-prediction-phase2",
      "port": 8080,
      "tags": ["phase2", "ai", "prediction"],
      "check": {
        "http": "http://neo-ai-prediction-phase2:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    }
  ]
}
EOF

    log_success "Updated API Gateway configuration for Phase 2 services"
}

# Deploy Phase 2 services
deploy_services() {
    log_section "Deploying Phase 2 Services"
    
    cd "$PROJECT_ROOT"
    
    # Load environment variables
    if [ -f ".env.production" ]; then
        set -a
        source .env.production
        set +a
        log_info "Loaded production environment variables"
    else
        log_error "Production environment file not found"
        exit 1
    fi
    
    # Pull images first
    log_info "Pulling required Docker images..."
    docker compose -f "$COMPOSE_FILE" pull --ignore-pull-failures
    
    # Build and start services
    log_info "Building and starting Phase 2 services..."
    docker compose -f "$COMPOSE_FILE" up -d --build
    
    log_success "Phase 2 services deployment initiated"
}

# Wait for services to be ready
wait_for_services() {
    log_section "Waiting for Phase 2 Services to be Ready"
    
    local services=("neo-keymanagement-phase2" "neo-notification-phase2" "neo-monitoring-phase2" "neo-health-phase2" "neo-ai-pattern-recognition-phase2" "neo-ai-prediction-phase2" "neo-rabbitmq-phase2")
    local timeout=$DEPLOYMENT_TIMEOUT
    local start_time=$(date +%s)
    
    while [ $(($(date +%s) - start_time)) -lt $timeout ]; do
        local all_ready=true
        
        for service in "${services[@]}"; do
            if ! docker exec "$service" curl -sf http://localhost:8080/health >/dev/null 2>&1; then
                if [ "$service" = "neo-rabbitmq-phase2" ]; then
                    if ! docker exec "$service" rabbitmqctl status >/dev/null 2>&1; then
                        all_ready=false
                        break
                    fi
                else
                    all_ready=false
                    break
                fi
            fi
        done
        
        if [ "$all_ready" = true ]; then
            log_success "All Phase 2 services are ready"
            return 0
        fi
        
        log_info "Waiting for services to be ready... (${services[*]})"
        sleep 10
    done
    
    log_error "Timeout waiting for Phase 2 services to be ready"
    return 1
}

# Register services with Consul
register_services() {
    log_section "Registering Phase 2 Services with Consul"
    
    # Wait for Consul to be ready
    timeout 30 bash -c 'until curl -sf http://localhost:8500/v1/status/leader; do sleep 1; done'
    
    # Register services
    curl -X PUT http://localhost:8500/v1/agent/service/register -d @"$PROJECT_ROOT/consul/config/phase2-services.json" >/dev/null 2>&1
    
    if [ $? -eq 0 ]; then
        log_success "Registered Phase 2 services with Consul"
    else
        log_warn "Failed to register some Phase 2 services with Consul"
    fi
}

# Run Phase 2 health checks
run_health_checks() {
    log_section "Running Phase 2 Health Checks"
    
    if [ -f "$SCRIPT_DIR/health-check-phase2.sh" ]; then
        if "$SCRIPT_DIR/health-check-phase2.sh"; then
            log_success "Phase 2 health checks passed"
            return 0
        else
            log_error "Phase 2 health checks failed"
            return 1
        fi
    else
        log_warn "Phase 2 health check script not found, skipping"
        return 0
    fi
}

# Main deployment function
main() {
    echo -e "${BLUE}=========================================${NC}"
    echo -e "${BLUE}        Phase 2 Deployment Script       ${NC}"
    echo -e "${BLUE}=========================================${NC}"
    echo ""
    echo "Services to deploy:"
    echo "  - Key Management Service"
    echo "  - Notification Service"
    echo "  - Monitoring Service"
    echo "  - Health Service"
    echo "  - AI Pattern Recognition Service"
    echo "  - AI Prediction Service"
    echo "  - RabbitMQ Message Broker"
    echo ""
    
    # Verify prerequisites
    if ! "$SCRIPT_DIR/verify-prerequisites.sh"; then
        log_error "Prerequisites not met"
        exit 1
    fi
    
    # Check Phase 1 health
    check_phase1_health
    
    # Generate configuration files
    generate_compose_file
    create_rabbitmq_config
    update_gateway_config
    
    # Deploy services
    deploy_services
    
    # Wait for services to be ready
    if ! wait_for_services; then
        log_error "Phase 2 deployment failed - services not ready"
        
        # Show logs for debugging
        echo ""
        echo "Service logs for debugging:"
        docker compose -f "$COMPOSE_FILE" logs --tail=50
        
        exit 1
    fi
    
    # Register services
    register_services
    
    # Run health checks
    if ! run_health_checks; then
        log_warn "Phase 2 deployment completed but health checks failed"
        echo ""
        echo "Check service logs:"
        echo "  docker compose -f $COMPOSE_FILE logs [service-name]"
        echo ""
        exit 1
    fi
    
    # Success
    log_success "Phase 2 deployment completed successfully!"
    echo ""
    echo "Phase 2 Services Status:"
    docker compose -f "$COMPOSE_FILE" ps
    echo ""
    echo "Access Points:"
    echo "  Key Management API:           http://localhost:8082"
    echo "  Notification API:             http://localhost:8083"
    echo "  Monitoring API:               http://localhost:8084"
    echo "  Health API:                   http://localhost:8085"
    echo "  AI Pattern Recognition API:   http://localhost:8086"
    echo "  AI Prediction API:            http://localhost:8087"
    echo "  RabbitMQ Management:          http://localhost:15672"
    echo ""
    echo "Next Steps:"
    echo "  1. Verify Phase 2 functionality with integration tests"
    echo "  2. Monitor service metrics in Grafana"
    echo "  3. Proceed with Phase 3 deployment when ready"
    echo ""
    echo "✅ Phase 2 deployment complete - Ready for Phase 3"
}

# Run main function
main "$@"