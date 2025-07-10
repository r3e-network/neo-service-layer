#\!/bin/bash

# Script to deploy Neo Service Layer microservices

set -e

# Configuration
COMPOSE_FILE="docker-compose.microservices-complete.yml"
BUILD_BASE_IMAGES=${BUILD_BASE_IMAGES:-true}
PULL_IMAGES=${PULL_IMAGES:-false}
CLEANUP=${CLEANUP:-false}

echo "=========================================="
echo "Neo Service Layer Microservices Deployment"
echo "=========================================="

# Cleanup existing containers if requested
if [ "$CLEANUP" = "true" ]; then
    echo "Cleaning up existing containers..."
    docker compose -f $COMPOSE_FILE down -v --remove-orphans
    docker system prune -f
fi

# Build base images if requested
if [ "$BUILD_BASE_IMAGES" = "true" ]; then
    echo "Building base images..."
    ./scripts/build-base-images.sh
fi

# Pull external images if requested
if [ "$PULL_IMAGES" = "true" ]; then
    echo "Pulling external images..."
    docker compose -f $COMPOSE_FILE pull consul postgres redis rabbitmq prometheus grafana
fi

# Generate environment file if it doesn't exist
if [ \! -f .env ]; then
    echo "Creating environment file..."
    cat > .env << ENVEOF
# Neo Service Layer Environment Configuration
ASPNETCORE_ENVIRONMENT=Development
JWT_SECRET_KEY=$(openssl rand -base64 32)
GRAFANA_PASSWORD=admin
POSTGRES_PASSWORD=neopass123
REDIS_PASSWORD=
RABBITMQ_PASSWORD=guest
SGX_MODE=SIM

# Service ports (can be overridden)
NOTIFICATION_PORT=5010
CONFIGURATION_PORT=5011
BACKUP_PORT=5012
STORAGE_PORT=5013
SMART_CONTRACTS_PORT=5014
CROSS_CHAIN_PORT=5015
ORACLE_PORT=5016
PROOF_OF_RESERVE_PORT=5017
KEY_MANAGEMENT_PORT=5018
ABSTRACT_ACCOUNT_PORT=5019
ZERO_KNOWLEDGE_PORT=5020
COMPLIANCE_PORT=5021
SECRETS_MANAGEMENT_PORT=5022
SOCIAL_RECOVERY_PORT=5023
NETWORK_SECURITY_PORT=5024
MONITORING_PORT=5025
HEALTH_PORT=5026
AUTOMATION_PORT=5027
EVENT_SUBSCRIPTION_PORT=5028
COMPUTE_PORT=5029
RANDOMNESS_PORT=5030
VOTING_PORT=5031
ENCLAVE_STORAGE_PORT=5032
ENVEOF
fi

# Start infrastructure services first
echo "Starting infrastructure services..."
docker compose -f $COMPOSE_FILE up -d consul postgres redis rabbitmq prometheus grafana

# Wait for infrastructure to be ready
echo "Waiting for infrastructure services to be ready..."
sleep 30

# Check if Consul is ready
echo "Checking Consul health..."
timeout 60 bash -c 'until curl -s http://localhost:8500/v1/status/leader  < /dev/null |  grep -q "\""; do sleep 2; done'

# Check if PostgreSQL is ready
echo "Checking PostgreSQL health..."
timeout 60 bash -c 'until docker compose -f $COMPOSE_FILE exec -T postgres pg_isready -U neouser; do sleep 2; done'

# Start all services
echo "Starting all microservices..."
docker compose -f $COMPOSE_FILE up -d

# Wait for services to start
echo "Waiting for services to start..."
sleep 60

# Check service health
echo "Checking service health..."
services=(
    "http://localhost:5000/health"  # API Gateway
    "http://localhost:5010/health"  # Notification
    "http://localhost:5011/health"  # Configuration
    "http://localhost:5012/health"  # Backup
)

for service in "${services[@]}"; do
    echo "Checking $service..."
    curl -f "$service" || echo "Warning: $service is not responding"
done

# Show running services
echo "=========================================="
echo "Deployment completed\!"
echo "=========================================="
echo "Services status:"
docker compose -f $COMPOSE_FILE ps

echo ""
echo "Access points:"
echo "  - API Gateway: http://localhost:5000"
echo "  - Consul UI: http://localhost:8500"
echo "  - Grafana: http://localhost:3000 (admin/admin)"
echo "  - Prometheus: http://localhost:9090"
echo ""
echo "To view logs: docker compose -f $COMPOSE_FILE logs -f [service-name]"
echo "To stop all services: docker compose -f $COMPOSE_FILE down"
