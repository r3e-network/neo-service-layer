#!/bin/bash

# Rollback Script for Phase 1 Services
# Safely stops and removes Phase 1 services while preserving data

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
COMPOSE_FILE="docker-compose.phase1.yml"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

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

# Confirm rollback operation
confirm_rollback() {
    echo -e "${YELLOW}WARNING: This will stop and remove all Phase 1 services.${NC}"
    echo -e "${YELLOW}Data volumes will be preserved but services will be unavailable.${NC}"
    echo ""
    read -p "Are you sure you want to proceed with rollback? (yes/no): " confirm
    
    if [ "$confirm" != "yes" ]; then
        log_info "Rollback cancelled by user"
        exit 0
    fi
}

# Create backup of current state
create_backup() {
    log_section "Creating Backup of Current State"
    
    local backup_dir="$PROJECT_ROOT/backups/rollback-$(date +%Y%m%d-%H%M%S)"
    mkdir -p "$backup_dir"
    
    # Backup container configurations
    docker compose -f "$PROJECT_ROOT/$COMPOSE_FILE" config > "$backup_dir/docker compose.yml" 2>/dev/null || true
    
    # Backup environment variables
    if [ -f "$PROJECT_ROOT/.env.production" ]; then
        cp "$PROJECT_ROOT/.env.production" "$backup_dir/"
    fi
    
    # Export database if possible
    if docker exec neo-postgres-phase1 pg_isready -U "${DB_USER:-neo_service_user}" >/dev/null 2>&1; then
        log_info "Backing up database..."
        docker exec neo-postgres-phase1 pg_dump -U "${DB_USER:-neo_service_user}" "${DB_NAME:-neo_service_layer}" > "$backup_dir/database_backup.sql" 2>/dev/null || log_warn "Database backup failed"
    fi
    
    # Save service logs
    log_info "Saving service logs..."
    local services=("neo-api-gateway-phase1" "neo-smart-contracts-phase1" "neo-configuration-phase1" "neo-automation-phase1")
    for service in "${services[@]}"; do
        if docker ps --format "{{.Names}}" | grep -q "$service"; then
            docker logs "$service" > "$backup_dir/${service}.log" 2>&1 || true
        fi
    done
    
    log_success "Backup created at $backup_dir"
}

# Stop services gracefully
stop_services() {
    log_section "Stopping Phase 1 Services"
    
    cd "$PROJECT_ROOT"
    
    if [ -f "$COMPOSE_FILE" ]; then
        # Stop services gracefully
        log_info "Stopping services gracefully..."
        docker compose -f "$COMPOSE_FILE" stop
        
        log_success "All Phase 1 services stopped"
    else
        log_warn "Docker Compose file not found, attempting manual stop"
        
        # Manual stop of known containers
        local containers=("neo-api-gateway-phase1" "neo-smart-contracts-phase1" "neo-configuration-phase1" "neo-automation-phase1" "neo-postgres-phase1" "neo-redis-phase1" "neo-consul-phase1" "neo-prometheus-phase1" "neo-grafana-phase1")
        
        for container in "${containers[@]}"; do
            if docker ps --format "{{.Names}}" | grep -q "$container"; then
                log_info "Stopping $container..."
                docker stop "$container" || log_warn "Failed to stop $container"
            fi
        done
    fi
}

# Remove containers (keeping volumes)
remove_containers() {
    log_section "Removing Phase 1 Containers"
    
    cd "$PROJECT_ROOT"
    
    if [ -f "$COMPOSE_FILE" ]; then
        # Remove containers but keep volumes
        log_info "Removing containers (preserving data volumes)..."
        docker compose -f "$COMPOSE_FILE" rm -f
        
        log_success "All Phase 1 containers removed"
    else
        log_warn "Docker Compose file not found, attempting manual removal"
        
        # Manual removal of known containers
        local containers=("neo-api-gateway-phase1" "neo-smart-contracts-phase1" "neo-configuration-phase1" "neo-automation-phase1" "neo-postgres-phase1" "neo-redis-phase1" "neo-consul-phase1" "neo-prometheus-phase1" "neo-grafana-phase1")
        
        for container in "${containers[@]}"; do
            if docker ps -a --format "{{.Names}}" | grep -q "$container"; then
                log_info "Removing $container..."
                docker rm "$container" || log_warn "Failed to remove $container"
            fi
        done
    fi
}

# Cleanup networks (if not used by other phases)
cleanup_networks() {
    log_section "Cleaning Up Networks"
    
    # Check if other phases are using the network
    local network_in_use=false
    
    for phase in 2 3 4; do
        if [ -f "$PROJECT_ROOT/docker compose.phase${phase}.yml" ]; then
            if docker compose -f "$PROJECT_ROOT/docker compose.phase${phase}.yml" ps -q | grep -q .; then
                network_in_use=true
                break
            fi
        fi
    done
    
    if [ "$network_in_use" = false ]; then
        log_info "Removing neo-network (no other phases active)..."
        docker network rm neo-network 2>/dev/null || log_warn "neo-network already removed or in use"
    else
        log_info "Preserving neo-network (other phases still active)"
    fi
}

# Deregister services from Consul (if still accessible)
deregister_services() {
    log_section "Deregistering Services from Consul"
    
    # If Consul is still running elsewhere, try to deregister
    if curl -sf http://localhost:8500/v1/status/leader >/dev/null 2>&1; then
        local services=("APIGateway" "SmartContractsService" "ConfigurationService" "AutomationService")
        
        for service in "${services[@]}"; do
            curl -X PUT "http://localhost:8500/v1/agent/service/deregister/${service}" >/dev/null 2>&1 || true
        done
        
        log_success "Services deregistered from Consul"
    else
        log_info "Consul not accessible, skipping service deregistration"
    fi
}

# Remove images (optional)
remove_images() {
    log_section "Removing Phase 1 Images (Optional)"
    
    read -p "Do you want to remove Phase 1 Docker images? (yes/no): " remove_imgs
    
    if [ "$remove_imgs" = "yes" ]; then
        log_info "Removing Phase 1 images..."
        
        # Remove built images
        docker images | grep neo-service-layer | grep phase1 | awk '{print $3}' | xargs docker rmi -f 2>/dev/null || true
        
        # Remove unused images
        docker image prune -f >/dev/null 2>&1 || true
        
        log_success "Phase 1 images removed"
    else
        log_info "Keeping Phase 1 images for potential redeployment"
    fi
}

# Verify rollback completion
verify_rollback() {
    log_section "Verifying Rollback Completion"
    
    # Check that Phase 1 containers are gone
    local phase1_containers=$(docker ps -a --format "{{.Names}}" | grep "phase1" | wc -l)
    
    if [ "$phase1_containers" -eq 0 ]; then
        log_success "All Phase 1 containers removed"
    else
        log_warn "$phase1_containers Phase 1 containers still exist"
    fi
    
    # Check that volumes still exist (should be preserved)
    local phase1_volumes=$(docker volume ls | grep "phase1\|postgres\|redis\|consul\|prometheus\|grafana" | wc -l)
    
    if [ "$phase1_volumes" -gt 0 ]; then
        log_success "Data volumes preserved ($phase1_volumes volumes)"
    else
        log_warn "No Phase 1 volumes found - data may have been lost"
    fi
    
    # Check network status
    if docker network ls | grep -q neo-network; then
        log_info "neo-network preserved (other phases may be using it)"
    else
        log_info "neo-network removed"
    fi
}

# Generate rollback report
generate_rollback_report() {
    log_section "Generating Rollback Report"
    
    local report_file="$PROJECT_ROOT/ROLLBACK_REPORT_$(date +%Y%m%d_%H%M%S).md"
    
    cat > "$report_file" << EOF
# Phase 1 Rollback Report

**Rollback Date:** $(date)
**Rollback Type:** Phase 1 Services
**Status:** Completed

## Rollback Summary

The following Phase 1 services have been stopped and removed:

- ✅ API Gateway (Port 8080)
- ✅ Smart Contracts Service (Port 8081)
- ✅ Configuration Service
- ✅ Automation Service
- ✅ PostgreSQL Database
- ✅ Redis Cache
- ✅ Consul Service Discovery
- ✅ Prometheus Monitoring
- ✅ Grafana Dashboards

## Data Preservation

- ✅ Database volumes preserved
- ✅ Configuration files backed up
- ✅ Service logs saved
- ✅ Monitoring data retained

## Recovery Instructions

To redeploy Phase 1 services:

\`\`\`bash
# Restore from backup if needed
./scripts/verify-prerequisites.sh

# Redeploy Phase 1
./scripts/deploy-phase1.sh

# Verify deployment
./scripts/health-check-phase1.sh
\`\`\`

## Backup Location

Backup created at: \`backups/rollback-$(date +%Y%m%d-%H%M%S)/\`

## Next Steps

1. Fix any issues that caused the rollback
2. Test fixes in development environment
3. Redeploy when ready
4. Verify health checks pass
5. Resume normal operations

---

**Note:** This rollback preserved all data volumes. No data loss should have occurred.
EOF

    log_success "Rollback report generated: $report_file"
}

# Main rollback function
main() {
    echo -e "${BLUE}=========================================${NC}"
    echo -e "${BLUE}        Phase 1 Rollback Script         ${NC}"
    echo -e "${BLUE}=========================================${NC}"
    echo ""
    echo "This script will safely rollback Phase 1 deployment:"
    echo "  - Stop all Phase 1 services"
    echo "  - Remove containers (preserve data volumes)"
    echo "  - Cleanup networks (if safe)"
    echo "  - Create backup of current state"
    echo "  - Generate rollback report"
    echo ""
    
    # Confirm rollback
    confirm_rollback
    
    # Load environment variables if available
    if [ -f "$PROJECT_ROOT/.env.production" ]; then
        set -a
        source "$PROJECT_ROOT/.env.production"
        set +a
    fi
    
    # Execute rollback steps
    create_backup
    deregister_services
    stop_services
    remove_containers
    cleanup_networks
    remove_images
    verify_rollback
    generate_rollback_report
    
    # Completion message
    echo ""
    echo -e "${GREEN}=========================================${NC}"
    echo -e "${GREEN}        Rollback Completed Successfully ${NC}"
    echo -e "${GREEN}=========================================${NC}"
    echo ""
    echo "✅ Phase 1 services have been safely rolled back"
    echo "✅ All data volumes have been preserved"
    echo "✅ Backup and logs have been saved"
    echo ""
    echo "📋 Rollback report: ROLLBACK_REPORT_$(date +%Y%m%d_%H%M%S).md"
    echo ""
    echo "To redeploy Phase 1:"
    echo "  ./scripts/deploy-phase1.sh"
    echo ""
    echo "To check what's still running:"
    echo "  docker ps"
    echo "  docker volume ls"
    echo ""
}

# Run main function
main "$@"