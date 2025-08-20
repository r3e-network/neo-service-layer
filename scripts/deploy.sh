#!/bin/bash

# Neo Service Layer Deployment Script
# Usage: ./deploy.sh [environment] [action]
# Environments: dev, staging, production
# Actions: deploy, stop, restart, status, logs

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT=${1:-dev}
ACTION=${2:-deploy}
PROJECT_NAME="neo-service-layer"
COMPOSE_FILE="docker-compose.yml"

# Set environment-specific compose file
case $ENVIRONMENT in
    dev)
        COMPOSE_FILE="docker-compose.yml"
        ;;
    staging)
        COMPOSE_FILE="docker-compose.staging.yml"
        ;;
    production)
        COMPOSE_FILE="docker-compose.production.yml"
        ;;
    *)
        echo -e "${RED}Invalid environment: $ENVIRONMENT${NC}"
        echo "Usage: $0 [dev|staging|production] [deploy|stop|restart|status|logs]"
        exit 1
        ;;
esac

# Check if compose file exists
if [ ! -f "$COMPOSE_FILE" ]; then
    echo -e "${RED}Compose file $COMPOSE_FILE not found${NC}"
    exit 1
fi

# Load environment variables
if [ -f ".env.$ENVIRONMENT" ]; then
    echo -e "${GREEN}Loading environment variables from .env.$ENVIRONMENT${NC}"
    export $(cat .env.$ENVIRONMENT | grep -v '^#' | xargs)
fi

# Validate required environment variables
validate_env() {
    local required_vars=("JWT_SECRET_KEY" "DB_PASSWORD")
    
    if [ "$ENVIRONMENT" = "production" ]; then
        required_vars+=("GRAFANA_PASSWORD")
    fi
    
    for var in "${required_vars[@]}"; do
        if [ -z "${!var}" ]; then
            echo -e "${RED}Error: Required environment variable $var is not set${NC}"
            echo "Please set it in .env.$ENVIRONMENT or export it"
            exit 1
        fi
    done
}

# Deploy function
deploy() {
    echo -e "${GREEN}Deploying $PROJECT_NAME to $ENVIRONMENT environment...${NC}"
    
    validate_env
    
    # Build and start services
    docker-compose -f $COMPOSE_FILE build
    docker-compose -f $COMPOSE_FILE up -d
    
    # Wait for services to be healthy
    echo -e "${YELLOW}Waiting for services to be healthy...${NC}"
    sleep 10
    
    # Check service health
    docker-compose -f $COMPOSE_FILE ps
    
    echo -e "${GREEN}Deployment complete!${NC}"
    echo -e "${GREEN}Access the application at: http://localhost:5000${NC}"
    
    if [ "$ENVIRONMENT" = "production" ]; then
        echo -e "${GREEN}Grafana dashboard: http://localhost:3000${NC}"
        echo -e "${GREEN}Prometheus: http://localhost:9090${NC}"
        echo -e "${GREEN}Jaeger UI: http://localhost:16686${NC}"
    fi
}

# Stop function
stop() {
    echo -e "${YELLOW}Stopping $PROJECT_NAME...${NC}"
    docker-compose -f $COMPOSE_FILE down
    echo -e "${GREEN}Services stopped${NC}"
}

# Restart function
restart() {
    echo -e "${YELLOW}Restarting $PROJECT_NAME...${NC}"
    docker-compose -f $COMPOSE_FILE restart
    echo -e "${GREEN}Services restarted${NC}"
}

# Status function
status() {
    echo -e "${GREEN}Status of $PROJECT_NAME services:${NC}"
    docker-compose -f $COMPOSE_FILE ps
}

# Logs function
logs() {
    echo -e "${GREEN}Showing logs for $PROJECT_NAME...${NC}"
    docker-compose -f $COMPOSE_FILE logs -f --tail=100
}

# Execute action
case $ACTION in
    deploy)
        deploy
        ;;
    stop)
        stop
        ;;
    restart)
        restart
        ;;
    status)
        status
        ;;
    logs)
        logs
        ;;
    *)
        echo -e "${RED}Invalid action: $ACTION${NC}"
        echo "Usage: $0 [dev|staging|production] [deploy|stop|restart|status|logs]"
        exit 1
        ;;
esac