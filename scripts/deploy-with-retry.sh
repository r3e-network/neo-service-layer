#!/bin/bash

# Neo Service Layer - Deployment with Automatic Retry
# This script includes retry logic for failed services

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
MAX_RETRIES=3
RETRY_DELAY=30
HEALTH_CHECK_TIMEOUT=300

# Function to check service health
check_service_health() {
    local service_name=$1
    local health_url=$2
    local attempts=0
    local max_attempts=$((HEALTH_CHECK_TIMEOUT / 5))
    
    while [ $attempts -lt $max_attempts ]; do
        if curl -s -f "$health_url" >/dev/null 2>&1; then
            return 0
        fi
        attempts=$((attempts + 1))
        sleep 5
    done
    
    return 1
}

# Function to deploy with retry
deploy_with_retry() {
    local compose_file=$1
    local phase_name=$2
    local retry_count=0
    
    while [ $retry_count -lt $MAX_RETRIES ]; do
        echo -e "${BLUE}Deploying $phase_name (Attempt $((retry_count + 1))/$MAX_RETRIES)...${NC}"
        
        if docker compose -f "$compose_file" up -d --build; then
            echo -e "${GREEN}✓ $phase_name deployed${NC}"
            return 0
        else
            retry_count=$((retry_count + 1))
            if [ $retry_count -lt $MAX_RETRIES ]; then
                echo -e "${YELLOW}⚠ Deployment failed, retrying in $RETRY_DELAY seconds...${NC}"
                sleep $RETRY_DELAY
                
                # Clean up failed containers
                docker compose -f "$compose_file" down --remove-orphans
            fi
        fi
    done
    
    echo -e "${RED}✗ Failed to deploy $phase_name after $MAX_RETRIES attempts${NC}"
    return 1
}

# Function to restart unhealthy services
restart_unhealthy_services() {
    local compose_file=$1
    
    # Get list of services
    local services=$(docker compose -f "$compose_file" ps --services 2>/dev/null)
    
    for service in $services; do
        # Check if container is running
        local container=$(docker compose -f "$compose_file" ps -q "$service" 2>/dev/null)
        
        if [ -z "$container" ]; then
            echo -e "${YELLOW}Restarting $service...${NC}"
            docker compose -f "$compose_file" up -d "$service"
        else
            # Check health status
            local health=$(docker inspect --format='{{.State.Health.Status}}' "$container" 2>/dev/null || echo "none")
            
            if [ "$health" = "unhealthy" ]; then
                echo -e "${YELLOW}Restarting unhealthy service: $service${NC}"
                docker compose -f "$compose_file" restart "$service"
            fi
        fi
    done
}

# Main deployment function
deploy_all_phases() {
    local phases=(
        "docker-compose.phase1-minimal.yml:Phase 1 - Infrastructure"
        "docker-compose.phase2-minimal.yml:Phase 2 - Management & AI"
        "docker-compose.phase3-minimal.yml:Phase 3 - Advanced Services"
        "docker-compose.phase4-minimal.yml:Phase 4 - Security & Governance"
    )
    
    # Clean up any existing deployment
    echo -e "${BLUE}Cleaning up existing deployment...${NC}"
    ./scripts/stop-all-services.sh 2>/dev/null || true
    
    # Create network
    docker network create neo-network 2>/dev/null || true
    
    # Deploy each phase with retry
    for phase_info in "${phases[@]}"; do
        IFS=':' read -r compose_file phase_name <<< "$phase_info"
        
        if [ -f "$compose_file" ]; then
            if ! deploy_with_retry "$compose_file" "$phase_name"; then
                echo -e "${RED}Deployment failed at $phase_name${NC}"
                return 1
            fi
            
            # Wait for services to stabilize
            echo -e "${BLUE}Waiting for services to stabilize...${NC}"
            sleep 20
            
            # Check and restart unhealthy services
            restart_unhealthy_services "$compose_file"
        fi
    done
    
    return 0
}

# Health check with retry
verify_with_retry() {
    local attempts=0
    
    while [ $attempts -lt $MAX_RETRIES ]; do
        echo -e "${BLUE}Verifying deployment (Attempt $((attempts + 1))/$MAX_RETRIES)...${NC}"
        
        if ./scripts/verify-complete-deployment-simple.sh; then
            return 0
        fi
        
        attempts=$((attempts + 1))
        if [ $attempts -lt $MAX_RETRIES ]; then
            echo -e "${YELLOW}Some services are not ready, checking for issues...${NC}"
            
            # Restart unhealthy services in all phases
            for phase in 1 2 3 4; do
                if [ -f "docker-compose.phase${phase}-minimal.yml" ]; then
                    restart_unhealthy_services "docker-compose.phase${phase}-minimal.yml"
                fi
            done
            
            sleep $RETRY_DELAY
        fi
    done
    
    return 1
}

# Main execution
echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║     Neo Service Layer - Deployment with Auto-Retry           ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check prerequisites
if [ ! -f ".env" ]; then
    if [ -f ".env.production" ]; then
        cp .env.production .env
    else
        echo -e "${RED}No .env file found!${NC}"
        exit 1
    fi
fi

# Deploy all phases
if deploy_all_phases; then
    echo -e "${GREEN}✓ All phases deployed successfully${NC}"
    
    # Verify with retry
    echo ""
    if verify_with_retry; then
        echo -e "${GREEN}🎉 Deployment completed and verified successfully!${NC}"
    else
        echo -e "${YELLOW}⚠ Deployment completed but some services may need attention${NC}"
    fi
else
    echo -e "${RED}✗ Deployment failed${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}Access Points:${NC}"
echo -e "  Main Dashboard: ${YELLOW}http://localhost:8200${NC}"
echo -e "  API Gateway: ${YELLOW}http://localhost:8080${NC}"
echo -e "  Grafana: ${YELLOW}http://localhost:13000${NC}"
echo -e "  Prometheus: ${YELLOW}http://localhost:19090${NC}"
echo -e "  Consul: ${YELLOW}http://localhost:18500${NC}"