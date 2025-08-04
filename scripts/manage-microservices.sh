#!/bin/bash

# Neo Service Layer Microservices Management Script

set -e

COMPOSE_FILE="docker-compose.full-stack.yml"
COMPOSE_CMD="docker compose -f $COMPOSE_FILE"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

function print_usage() {
    echo "Usage: $0 {start|stop|restart|status|logs|build|clean}"
    echo "Commands:"
    echo "  start   - Start all microservices"
    echo "  stop    - Stop all microservices"
    echo "  restart - Restart all microservices"
    echo "  status  - Show status of all services"
    echo "  logs    - Show logs (optionally specify service name)"
    echo "  build   - Build all service images"
    echo "  clean   - Stop and remove all containers, volumes, and images"
}

function check_health() {
    echo -e "${YELLOW}Checking service health...${NC}"
    
    # Check Consul
    if curl -s http://localhost:8500/v1/agent/services > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Consul is healthy${NC}"
        
        # List registered services
        echo "Registered services:"
        curl -s http://localhost:8500/v1/agent/services | jq -r 'to_entries[] | "  - \(.value.Service) (\(.value.Address):\(.value.Port))"'
    else
        echo -e "${RED}✗ Consul is not responding${NC}"
    fi
    
    # Check Prometheus
    if curl -s http://localhost:9090/-/healthy > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Prometheus is healthy${NC}"
    else
        echo -e "${RED}✗ Prometheus is not responding${NC}"
    fi
    
    # Check Grafana
    if curl -s http://localhost:3000/api/health > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Grafana is healthy${NC}"
    else
        echo -e "${RED}✗ Grafana is not responding${NC}"
    fi
    
    # Check API Gateway
    if curl -s http://localhost:5000/health > /dev/null 2>&1; then
        echo -e "${GREEN}✓ API Gateway is healthy${NC}"
    else
        echo -e "${RED}✗ API Gateway is not responding${NC}"
    fi
}

case "$1" in
    start)
        echo -e "${GREEN}Starting Neo Service Layer Microservices...${NC}"
        
        # Start infrastructure first
        echo "Starting infrastructure services..."
        $COMPOSE_CMD up -d consul postgres redis rabbitmq
        
        echo "Waiting for infrastructure to be ready..."
        sleep 10
        
        # Start monitoring
        echo "Starting monitoring services..."
        $COMPOSE_CMD up -d prometheus grafana
        
        # Start microservices
        echo "Starting microservices..."
        $COMPOSE_CMD up -d
        
        echo -e "${GREEN}All services started!${NC}"
        sleep 5
        check_health
        
        echo -e "\n${YELLOW}Access points:${NC}"
        echo "  - API Gateway: http://localhost:5000"
        echo "  - Consul UI: http://localhost:8500"
        echo "  - Grafana: http://localhost:3000 (admin/admin)"
        echo "  - Prometheus: http://localhost:9090"
        echo "  - RabbitMQ: http://localhost:15672 (guest/guest)"
        ;;
        
    stop)
        echo -e "${YELLOW}Stopping all services...${NC}"
        $COMPOSE_CMD down
        echo -e "${GREEN}All services stopped.${NC}"
        ;;
        
    restart)
        echo -e "${YELLOW}Restarting all services...${NC}"
        $0 stop
        sleep 2
        $0 start
        ;;
        
    status)
        echo -e "${YELLOW}Service Status:${NC}"
        $COMPOSE_CMD ps
        echo ""
        check_health
        ;;
        
    logs)
        if [ -z "$2" ]; then
            $COMPOSE_CMD logs -f --tail=100
        else
            $COMPOSE_CMD logs -f --tail=100 "$2"
        fi
        ;;
        
    build)
        echo -e "${GREEN}Building all service images...${NC}"
        
        # Build base images first
        echo "Building base images..."
        ./scripts/build-base-images.sh
        
        # Build services
        echo "Building notification service..."
        docker build -t neoservicelayer/notification-service:latest -f Dockerfile.notification-simple .
        
        echo "Building storage service..."
        docker build -t neoservicelayer/storage-service:metrics -f Dockerfile.storage-metrics .
        
        echo "Building configuration service..."
        docker build -t neoservicelayer/configuration-service:latest -f Dockerfile.configuration-service .
        
        echo "Building health service..."
        docker build -t neoservicelayer/health-service:latest -f Dockerfile.health-service .
        
        echo "Building API gateway..."
        docker build -t neoservicelayer/api-gateway:latest -f Dockerfile.gateway-simple .
        
        echo -e "${GREEN}All images built successfully!${NC}"
        ;;
        
    clean)
        echo -e "${RED}WARNING: This will remove all containers, volumes, and data!${NC}"
        read -p "Are you sure? (y/N) " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            echo -e "${YELLOW}Cleaning up...${NC}"
            $COMPOSE_CMD down -v --remove-orphans
            docker system prune -f
            echo -e "${GREEN}Cleanup complete.${NC}"
        else
            echo "Cleanup cancelled."
        fi
        ;;
        
    *)
        print_usage
        exit 1
        ;;
esac