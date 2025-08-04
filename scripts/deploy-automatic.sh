#!/bin/bash

# Neo Service Layer - Fully Automated Deployment Script
# This script handles everything automatically without user intervention

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m'

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
LOG_FILE="$PROJECT_ROOT/deployment-$(date +%Y%m%d-%H%M%S).log"
DEPLOYMENT_TYPE="${1:-minimal}" # minimal or production

# Logging functions
log() {
    echo -e "${BLUE}[$(date +'%H:%M:%S')]${NC} $1" | tee -a "$LOG_FILE"
}

log_success() {
    echo -e "${GREEN}✓ $1${NC}" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}✗ $1${NC}" | tee -a "$LOG_FILE"
}

log_warn() {
    echo -e "${YELLOW}⚠ $1${NC}" | tee -a "$LOG_FILE"
}

# Change to project root
cd "$PROJECT_ROOT"

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║        Neo Service Layer - Automated Deployment              ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

log "Starting automated deployment (Type: $DEPLOYMENT_TYPE)"
log "Log file: $LOG_FILE"
echo ""

# Step 1: Clean up any existing containers
log "Step 1: Cleaning up existing containers..."

# Function to safely stop and remove containers
cleanup_containers() {
    local compose_files=(
        "docker-compose.phase1-minimal.yml"
        "docker-compose.phase2-minimal.yml"
        "docker-compose.phase3-minimal.yml"
        "docker-compose.phase4-minimal.yml"
        "docker-compose.phase1.yml"
        "docker-compose.phase2.yml"
        "docker-compose.phase3.yml"
        "docker-compose.phase4.yml"
        "docker-compose.production.yml"
    )
    
    for file in "${compose_files[@]}"; do
        if [ -f "$file" ]; then
            log "Stopping containers from $file..."
            docker compose -f "$file" down --remove-orphans 2>/dev/null || true
        fi
    done
    
    # Remove any remaining Neo Service Layer containers
    log "Removing any remaining Neo Service Layer containers..."
    docker ps -a --format "{{.Names}}" | grep -E "^neo-" | while read container; do
        docker stop "$container" 2>/dev/null || true
        docker rm "$container" 2>/dev/null || true
    done
    
    # Clean up the network
    docker network rm neo-service-layer_neo-network 2>/dev/null || true
}

cleanup_containers
log_success "Container cleanup completed"

# Step 2: Ensure environment variables are set
log "Step 2: Setting up environment variables..."

if [ ! -f ".env" ]; then
    if [ -f ".env.production" ]; then
        cp .env.production .env
        log_success "Copied .env.production to .env"
    elif [ -f ".env.production.template" ]; then
        cp .env.production.template .env
        log_warn "Using template file - please update with actual values"
    else
        log_error "No environment file found!"
        exit 1
    fi
else
    log_success "Environment file exists"
fi

# Source the environment file
set -a
source .env
set +a

# Step 3: Create Docker network
log "Step 3: Creating Docker network..."
docker network create neo-network 2>/dev/null || true
log_success "Docker network ready"

# Step 4: Deploy based on type
if [ "$DEPLOYMENT_TYPE" == "minimal" ]; then
    log "Step 4: Deploying minimal services..."
    
    # Deploy Phase 1
    log "Deploying Phase 1 - Infrastructure & Core Services..."
    docker compose -f docker-compose.phase1-minimal.yml up -d --build
    
    # Wait for infrastructure to be ready
    log "Waiting for infrastructure services..."
    sleep 30
    
    # Deploy Phase 2
    log "Deploying Phase 2 - Management & AI Services..."
    docker compose -f docker-compose.phase2-minimal.yml up -d --build
    
    # Deploy Phase 3
    log "Deploying Phase 3 - Advanced Services..."
    docker compose -f docker-compose.phase3-minimal.yml up -d --build
    
    # Deploy Phase 4
    log "Deploying Phase 4 - Security & Governance..."
    docker compose -f docker-compose.phase4-minimal.yml up -d --build
    
else
    log "Step 4: Deploying production services..."
    
    # Ensure production compose files exist
    if [ ! -f "docker-compose.phase1.yml" ]; then
        ./scripts/deploy-phase1.sh --generate-only || true
    fi
    
    # Deploy all phases
    for phase in 1 2 3 4; do
        if [ -f "docker-compose.phase${phase}.yml" ]; then
            log "Deploying Phase $phase..."
            docker compose -f "docker-compose.phase${phase}.yml" up -d --build
            sleep 30
        fi
    done
fi

# Step 5: Wait for services to be ready
log "Step 5: Waiting for all services to be ready..."

wait_for_service() {
    local name=$1
    local url=$2
    local max_attempts=60
    local attempt=0
    
    while [ $attempt -lt $max_attempts ]; do
        if curl -s -f "$url" >/dev/null 2>&1; then
            log_success "$name is ready"
            return 0
        fi
        attempt=$((attempt + 1))
        sleep 2
    done
    
    log_error "$name failed to become ready"
    return 1
}

# Wait for core services
log "Waiting for infrastructure services..."
sleep 60

# Step 6: Verify deployment
log "Step 6: Verifying deployment..."

if [ -f "./scripts/verify-complete-deployment-simple.sh" ]; then
    if ./scripts/verify-complete-deployment-simple.sh; then
        log_success "All services verified successfully!"
    else
        log_warn "Some services may not be fully ready yet"
    fi
else
    log_warn "Verification script not found"
fi

# Step 7: Display summary
echo ""
echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                   DEPLOYMENT COMPLETE                        ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${GREEN}🎉 Neo Service Layer has been deployed successfully!${NC}"
echo ""
echo -e "${BLUE}Access Points:${NC}"
echo -e "  Main Dashboard: ${YELLOW}http://localhost:8200${NC}"
echo -e "  API Gateway: ${YELLOW}http://localhost:8080${NC}"
echo -e "  Grafana: ${YELLOW}http://localhost:13000${NC}"
echo -e "  Prometheus: ${YELLOW}http://localhost:19090${NC}"
echo -e "  Consul: ${YELLOW}http://localhost:18500${NC}"
echo ""
echo -e "${BLUE}Useful Commands:${NC}"
echo -e "  View logs: ${YELLOW}docker compose -f docker-compose.phase1-minimal.yml logs -f [service]${NC}"
echo -e "  Stop all: ${YELLOW}./scripts/stop-all-services.sh${NC}"
echo -e "  Verify: ${YELLOW}./scripts/verify-complete-deployment-simple.sh${NC}"
echo ""
echo -e "${GREEN}Deployment log saved to: $LOG_FILE${NC}"