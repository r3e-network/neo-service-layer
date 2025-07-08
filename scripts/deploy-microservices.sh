#!/bin/bash

# Script to deploy Neo Service Layer as microservices

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
COMPOSE_FILE="docker-compose.microservices.yml"
ENV_FILE=".env.microservices"

echo -e "${GREEN}Neo Service Layer - Microservices Deployment${NC}"
echo "============================================="

# Function to check prerequisites
check_prerequisites() {
    echo -e "${YELLOW}Checking prerequisites...${NC}"
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}Docker is not installed. Please install Docker first.${NC}"
        exit 1
    fi
    
    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        echo -e "${RED}Docker Compose is not installed. Please install Docker Compose first.${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}Prerequisites check passed!${NC}"
}

# Function to create environment file
create_env_file() {
    if [ ! -f "$ENV_FILE" ]; then
        echo -e "${YELLOW}Creating environment file...${NC}"
        cat > "$ENV_FILE" << EOF
# Neo Service Layer Microservices Configuration

# Environment
ASPNETCORE_ENVIRONMENT=Development

# JWT Configuration
JWT_SECRET_KEY=$(openssl rand -base64 32)

# Database
POSTGRES_PASSWORD=$(openssl rand -base64 16)

# RabbitMQ
RABBITMQ_PASSWORD=$(openssl rand -base64 16)

# Grafana
GRAFANA_PASSWORD=admin

# Service Ports
NOTIFICATION_PORT=5010
CONFIGURATION_PORT=5011
BACKUP_PORT=5012
PROOF_OF_RESERVE_PORT=5013
SMART_CONTRACTS_PORT=5014
CROSS_CHAIN_PORT=5015
MONITORING_PORT=5016
HEALTH_PORT=5017
KEY_MANAGEMENT_PORT=5018
AUTOMATION_PORT=5019

# Blockchain RPCs
NEO_N3_RPC_URL=http://seed1.neo.org:10332
NEO_X_RPC_URL=http://seed1.neox.org:10332

# SGX Mode
SGX_MODE=SIM

# AWS Configuration (optional)
# AWS_REGION=us-east-1
# AWS_ACCESS_KEY_ID=
# AWS_SECRET_ACCESS_KEY=
# BACKUP_S3_BUCKET=

# SMTP Configuration (optional)
# SMTP_HOST=smtp.gmail.com
# SMTP_PORT=587
# SMTP_USER=
# SMTP_PASSWORD=
EOF
        echo -e "${GREEN}Environment file created: $ENV_FILE${NC}"
        echo -e "${YELLOW}Please review and update the configuration as needed.${NC}"
    else
        echo -e "${GREEN}Using existing environment file: $ENV_FILE${NC}"
    fi
}

# Function to build base images
build_base_images() {
    echo -e "${YELLOW}Building base images...${NC}"
    
    # Build runtime base
    docker build -f docker/microservices/Dockerfile.base \
        --target base \
        -t neoservicelayer/runtime-base:latest .
    
    # Build build base
    docker build -f docker/microservices/Dockerfile.base \
        --target build-base \
        -t neoservicelayer/build-base:latest .
    
    echo -e "${GREEN}Base images built successfully!${NC}"
}

# Function to generate service Dockerfiles
generate_dockerfiles() {
    echo -e "${YELLOW}Generating service Dockerfiles...${NC}"
    
    if [ -f "scripts/generate-service-dockerfiles.sh" ]; then
        bash scripts/generate-service-dockerfiles.sh
    else
        echo -e "${RED}Dockerfile generation script not found!${NC}"
        exit 1
    fi
}

# Function to deploy services
deploy_services() {
    echo -e "${YELLOW}Deploying services...${NC}"
    
    # Start infrastructure services first
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d \
        consul postgres redis rabbitmq
    
    echo "Waiting for infrastructure services to be ready..."
    sleep 10
    
    # Build and start all services
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d --build
    
    echo -e "${GREEN}Services deployed successfully!${NC}"
}

# Function to show service status
show_status() {
    echo -e "${YELLOW}Service Status:${NC}"
    docker-compose -f "$COMPOSE_FILE" ps
    
    echo -e "\n${YELLOW}Service URLs:${NC}"
    echo "API Gateway: http://localhost:5000"
    echo "Consul UI: http://localhost:8500"
    echo "RabbitMQ Management: http://localhost:15672"
    echo "Grafana: http://localhost:3000"
    echo "Prometheus: http://localhost:9090"
}

# Function to show logs
show_logs() {
    SERVICE=$1
    if [ -z "$SERVICE" ]; then
        docker-compose -f "$COMPOSE_FILE" logs -f
    else
        docker-compose -f "$COMPOSE_FILE" logs -f "$SERVICE"
    fi
}

# Function to scale service
scale_service() {
    SERVICE=$1
    COUNT=$2
    
    if [ -z "$SERVICE" ] || [ -z "$COUNT" ]; then
        echo -e "${RED}Usage: $0 scale <service> <count>${NC}"
        exit 1
    fi
    
    docker-compose -f "$COMPOSE_FILE" up -d --scale "$SERVICE=$COUNT"
}

# Function to stop services
stop_services() {
    echo -e "${YELLOW}Stopping services...${NC}"
    docker-compose -f "$COMPOSE_FILE" down
    echo -e "${GREEN}Services stopped!${NC}"
}

# Function to clean up
cleanup() {
    echo -e "${YELLOW}Cleaning up...${NC}"
    docker-compose -f "$COMPOSE_FILE" down -v
    docker system prune -f
    echo -e "${GREEN}Cleanup complete!${NC}"
}

# Main menu
case "$1" in
    deploy)
        check_prerequisites
        create_env_file
        build_base_images
        generate_dockerfiles
        deploy_services
        show_status
        ;;
    start)
        docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d
        show_status
        ;;
    stop)
        stop_services
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs "$2"
        ;;
    scale)
        scale_service "$2" "$3"
        ;;
    clean)
        cleanup
        ;;
    rebuild)
        stop_services
        build_base_images
        deploy_services
        show_status
        ;;
    *)
        echo "Neo Service Layer - Microservices Management"
        echo ""
        echo "Usage: $0 {deploy|start|stop|status|logs|scale|clean|rebuild}"
        echo ""
        echo "Commands:"
        echo "  deploy    - Deploy all services from scratch"
        echo "  start     - Start all services"
        echo "  stop      - Stop all services"
        echo "  status    - Show service status"
        echo "  logs      - Show logs (optionally specify service)"
        echo "  scale     - Scale a service (usage: scale <service> <count>)"
        echo "  clean     - Clean up all resources"
        echo "  rebuild   - Rebuild and redeploy all services"
        echo ""
        echo "Examples:"
        echo "  $0 deploy"
        echo "  $0 logs notification-service"
        echo "  $0 scale notification-service 3"
        exit 1
        ;;
esac