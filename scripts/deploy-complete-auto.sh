#!/bin/bash

# Neo Service Layer - Complete Automated Deployment
# This script handles the entire deployment process automatically

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m'
BOLD='\033[1m'

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
LOG_DIR="$PROJECT_ROOT/logs"
LOG_FILE="$LOG_DIR/deployment-$TIMESTAMP.log"
AUTO_YES="${AUTO_YES:-true}"
DEPLOYMENT_MODE="${DEPLOYMENT_MODE:-minimal}"

# Create log directory
mkdir -p "$LOG_DIR"

# Logging functions
log() {
    local message="[$(date +'%Y-%m-%d %H:%M:%S')] $1"
    echo -e "${BLUE}$message${NC}" | tee -a "$LOG_FILE"
}

log_success() {
    local message="[$(date +'%Y-%m-%d %H:%M:%S')] ✓ $1"
    echo -e "${GREEN}$message${NC}" | tee -a "$LOG_FILE"
}

log_error() {
    local message="[$(date +'%Y-%m-%d %H:%M:%S')] ✗ $1"
    echo -e "${RED}$message${NC}" | tee -a "$LOG_FILE"
}

log_warn() {
    local message="[$(date +'%Y-%m-%d %H:%M:%S')] ⚠ $1"
    echo -e "${YELLOW}$message${NC}" | tee -a "$LOG_FILE"
}

# Cleanup function
cleanup() {
    if [ $? -ne 0 ]; then
        log_error "Deployment failed. Check log file: $LOG_FILE"
        echo ""
        echo -e "${YELLOW}To retry deployment, run:${NC}"
        echo -e "  ${GREEN}$0${NC}"
        echo ""
        echo -e "${YELLOW}To view logs, run:${NC}"
        echo -e "  ${GREEN}tail -f $LOG_FILE${NC}"
    fi
}

trap cleanup EXIT

# Change to project root
cd "$PROJECT_ROOT"

# Header
clear
echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║${BOLD}      Neo Service Layer - Automated Complete Deployment${NC}${BLUE}      ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${PURPLE}Version:${NC} 2.0 (Fully Automated)"
echo -e "${PURPLE}Mode:${NC} $DEPLOYMENT_MODE"
echo -e "${PURPLE}Log File:${NC} $LOG_FILE"
echo ""

# Function to check prerequisites
check_prerequisites() {
    log "Checking prerequisites..."
    
    local prereqs_met=true
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        prereqs_met=false
    else
        log_success "Docker is installed"
    fi
    
    # Check Docker Compose
    if ! docker compose version &> /dev/null; then
        log_error "Docker Compose v2 is not installed"
        prereqs_met=false
    else
        log_success "Docker Compose is installed"
    fi
    
    # Check disk space (10GB minimum)
    local available_space=$(df . | tail -1 | awk '{print $4}')
    if [ "$available_space" -lt 10485760 ]; then
        log_error "Insufficient disk space (need at least 10GB)"
        prereqs_met=false
    else
        log_success "Sufficient disk space available"
    fi
    
    # Check memory (4GB minimum)
    local available_memory=$(free -m | awk 'NR==2{print $7}')
    if [ "$available_memory" -lt 4096 ]; then
        log_warn "Low memory available (recommend at least 4GB)"
    else
        log_success "Sufficient memory available"
    fi
    
    if [ "$prereqs_met" = false ]; then
        log_error "Prerequisites not met. Please install missing components."
        exit 1
    fi
    
    log_success "All prerequisites met"
}

# Function to setup environment
setup_environment() {
    log "Setting up environment..."
    
    # Create .env file if it doesn't exist
    if [ ! -f ".env" ]; then
        if [ -f ".env.production" ]; then
            cp .env.production .env
            log_success "Created .env from .env.production"
        elif [ -f ".env.production.template" ]; then
            cp .env.production.template .env
            
            # Generate secure passwords if template is used
            log "Generating secure credentials..."
            sed -i "s/YOUR_DB_PASSWORD/$(openssl rand -base64 32 | tr -d '=' | tr -d '/')/g" .env
            sed -i "s/YOUR_REDIS_PASSWORD/$(openssl rand -base64 32 | tr -d '=' | tr -d '/')/g" .env
            sed -i "s/YOUR_JWT_SECRET/$(openssl rand -base64 64 | tr -d '=' | tr -d '/')/g" .env
            
            log_success "Generated secure credentials"
        else
            log_error "No environment configuration found"
            exit 1
        fi
    else
        log_success "Environment file exists"
    fi
    
    # Source environment variables
    set -a
    source .env
    set +a
}

# Function to clean existing deployment
clean_existing_deployment() {
    log "Cleaning existing deployment..."
    
    # Stop all services
    "$SCRIPT_DIR/stop-all-services.sh" 2>/dev/null || true
    
    # Clean up volumes if requested
    if [ "${CLEAN_VOLUMES:-false}" = "true" ]; then
        log_warn "Removing data volumes..."
        docker volume prune -f
    fi
    
    # Clean up dangling images
    docker image prune -f
    
    log_success "Cleanup completed"
}

# Function to deploy services
deploy_services() {
    log "Starting service deployment..."
    
    # Create network
    docker network create neo-network 2>/dev/null || true
    
    if [ "$DEPLOYMENT_MODE" = "minimal" ]; then
        deploy_minimal_services
    else
        deploy_production_services
    fi
}

# Function to deploy minimal services
deploy_minimal_services() {
    log "Deploying minimal services..."
    
    local phases=("phase1" "phase2" "phase3" "phase4")
    local phase_names=(
        "Infrastructure & Core Services"
        "Management & AI Services"
        "Advanced Services"
        "Security & Governance"
    )
    
    for i in "${!phases[@]}"; do
        local phase="${phases[$i]}"
        local phase_name="${phase_names[$i]}"
        
        log "Deploying Phase $((i+1)): $phase_name..."
        
        if [ -f "docker-compose.${phase}-minimal.yml" ]; then
            if docker compose -f "docker-compose.${phase}-minimal.yml" up -d --build; then
                log_success "Phase $((i+1)) deployed successfully"
                
                # Wait between phases
                if [ $i -lt $((${#phases[@]} - 1)) ]; then
                    log "Waiting for services to stabilize..."
                    sleep 20
                fi
            else
                log_error "Phase $((i+1)) deployment failed"
                return 1
            fi
        else
            log_error "docker-compose.${phase}-minimal.yml not found"
            return 1
        fi
    done
    
    log_success "All phases deployed"
}

# Function to verify deployment
verify_deployment() {
    log "Verifying deployment..."
    
    # Wait for services to be ready
    log "Waiting for services to initialize..."
    sleep 30
    
    # Run verification script
    if [ -f "$SCRIPT_DIR/verify-complete-deployment-simple.sh" ]; then
        if "$SCRIPT_DIR/verify-complete-deployment-simple.sh" >> "$LOG_FILE" 2>&1; then
            log_success "Deployment verified successfully"
            return 0
        else
            log_warn "Some services may not be ready yet"
            return 1
        fi
    else
        log_warn "Verification script not found"
        return 1
    fi
}

# Function to display summary
display_summary() {
    echo ""
    echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║${BOLD}                  DEPLOYMENT COMPLETE                        ${NC}${BLUE}║${NC}"
    echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
    echo ""
    
    if verify_deployment; then
        echo -e "${GREEN}${BOLD}🎉 All services are running successfully!${NC}"
    else
        echo -e "${YELLOW}${BOLD}⚠️  Deployment completed with warnings${NC}"
    fi
    
    echo ""
    echo -e "${PURPLE}${BOLD}Access Points:${NC}"
    echo -e "  ${BOLD}Main Dashboard:${NC} ${YELLOW}http://localhost:8200${NC}"
    echo -e "  ${BOLD}API Gateway:${NC} ${YELLOW}http://localhost:8080${NC}"
    echo -e "  ${BOLD}Grafana:${NC} ${YELLOW}http://localhost:13000${NC} (admin/admin)"
    echo -e "  ${BOLD}Prometheus:${NC} ${YELLOW}http://localhost:19090${NC}"
    echo -e "  ${BOLD}Consul:${NC} ${YELLOW}http://localhost:18500${NC}"
    
    echo ""
    echo -e "${PURPLE}${BOLD}Management Commands:${NC}"
    echo -e "  ${BOLD}View logs:${NC} docker compose -f docker-compose.phase1-minimal.yml logs -f [service]"
    echo -e "  ${BOLD}Stop all:${NC} $SCRIPT_DIR/stop-all-services.sh"
    echo -e "  ${BOLD}Verify:${NC} $SCRIPT_DIR/verify-complete-deployment-simple.sh"
    echo -e "  ${BOLD}Restart:${NC} $0"
    
    echo ""
    echo -e "${GREEN}Deployment log: $LOG_FILE${NC}"
    echo ""
}

# Main deployment flow
main() {
    # Step 1: Check prerequisites
    check_prerequisites
    
    # Step 2: Setup environment
    setup_environment
    
    # Step 3: Clean existing deployment
    clean_existing_deployment
    
    # Step 4: Deploy services
    if deploy_services; then
        # Step 5: Display summary
        display_summary
        exit 0
    else
        log_error "Deployment failed"
        exit 1
    fi
}

# Run main function
main "$@"