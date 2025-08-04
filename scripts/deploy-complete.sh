#!/bin/bash

# Complete Neo Service Layer Deployment Script
# Deploy all phases or specific phases with comprehensive validation

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
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

# Display usage information
show_usage() {
    cat << EOF
Usage: $0 [OPTIONS] [PHASES]

Deploy Neo Service Layer microservices in phases.

OPTIONS:
    -h, --help              Show this help message
    -c, --check-only        Only run health checks, don't deploy
    -f, --force             Skip confirmation prompts
    -v, --verbose           Enable verbose logging
    --dry-run              Show what would be deployed without executing
    --rollback PHASE       Rollback specified phase

PHASES:
    1                      Deploy Phase 1 (Core Infrastructure)
    2                      Deploy Phase 2 (Security & AI Services)  
    3                      Deploy Phase 3 (Advanced Services)
    4                      Deploy Phase 4 (Final Services & Web UI)
    all                    Deploy all phases sequentially (default)

EXAMPLES:
    $0                     # Deploy all phases
    $0 1 2                 # Deploy only phases 1 and 2
    $0 --check-only        # Run health checks on all deployed phases
    $0 --rollback 1        # Rollback phase 1
    $0 --dry-run all       # Show deployment plan for all phases

PHASE DESCRIPTIONS:
    Phase 1: API Gateway, Smart Contracts, Configuration, Automation
             Database (PostgreSQL), Cache (Redis), Service Discovery (Consul)
             Monitoring (Prometheus, Grafana)

    Phase 2: Key Management, Notification, Monitoring, Health Services
             AI Pattern Recognition, AI Prediction, Message Queue (RabbitMQ)

    Phase 3: Oracle, Storage, CrossChain, Proof of Reserve, Randomness
             Fair Ordering, TEE Host Services

    Phase 4: Voting, Zero Knowledge, Secrets Management, Social Recovery
             Enclave Storage, Network Security, Web Interface

EOF
}

# Parse command line arguments
parse_args() {
    PHASES_TO_DEPLOY=()
    CHECK_ONLY=false
    FORCE=false
    VERBOSE=false
    DRY_RUN=false
    ROLLBACK_PHASE=""
    
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                show_usage
                exit 0
                ;;
            -c|--check-only)
                CHECK_ONLY=true
                shift
                ;;
            -f|--force)
                FORCE=true
                shift
                ;;
            -v|--verbose)
                VERBOSE=true
                shift
                ;;
            --dry-run)
                DRY_RUN=true
                shift
                ;;
            --rollback)
                ROLLBACK_PHASE="$2"
                shift 2
                ;;
            1|2|3|4)
                PHASES_TO_DEPLOY+=($1)
                shift
                ;;
            all)
                PHASES_TO_DEPLOY=(1 2 3 4)
                shift
                ;;
            *)
                log_error "Unknown option: $1"
                show_usage
                exit 1
                ;;
        esac
    done
    
    # Default to all phases if none specified
    if [ ${#PHASES_TO_DEPLOY[@]} -eq 0 ] && [ "$CHECK_ONLY" = false ] && [ -z "$ROLLBACK_PHASE" ]; then
        PHASES_TO_DEPLOY=(1 2 3 4)
    fi
}

# Check prerequisites for deployment
check_prerequisites() {
    log_section "Checking Prerequisites"
    
    if ! "$SCRIPT_DIR/verify-prerequisites.sh"; then
        log_error "Prerequisites check failed"
        return 1
    fi
    
    log_success "All prerequisites met"
    return 0
}

# Check system resources
check_system_resources() {
    log_section "Checking System Resources"
    
    # Check available memory (minimum 8GB recommended)
    local available_mem=$(free -m | awk 'NR==2{printf "%.0f", $7}')
    if [ "$available_mem" -lt 4096 ]; then
        log_warn "Low available memory: ${available_mem}MB (recommend 8GB+)"
    else
        log_success "Available memory: ${available_mem}MB"
    fi
    
    # Check available disk space (minimum 20GB recommended)
    local available_disk=$(df -h / | awk 'NR==2 {print $4}' | sed 's/G//')
    if [ "${available_disk%.*}" -lt 20 ]; then
        log_warn "Low available disk space: ${available_disk}GB (recommend 20GB+)"
    else
        log_success "Available disk space: ${available_disk}GB"
    fi
    
    # Check CPU cores
    local cpu_cores=$(nproc)
    if [ "$cpu_cores" -lt 4 ]; then
        log_warn "Low CPU core count: $cpu_cores (recommend 4+)"
    else
        log_success "CPU cores: $cpu_cores"
    fi
}

# Show deployment plan
show_deployment_plan() {
    log_section "Deployment Plan"
    
    echo "📋 Neo Service Layer Deployment Plan"
    echo ""
    
    for phase in "${PHASES_TO_DEPLOY[@]}"; do
        case $phase in
            1)
                echo "🔵 Phase 1: Core Infrastructure & API Gateway"
                echo "   Services: API Gateway, Smart Contracts, Configuration, Automation"
                echo "   Infrastructure: PostgreSQL, Redis, Consul, Prometheus, Grafana"
                echo "   Estimated time: 5-8 minutes"
                ;;
            2)
                echo "🟢 Phase 2: Security & AI Services"
                echo "   Services: Key Management, Notification, Monitoring, Health"
                echo "   AI Services: Pattern Recognition, Prediction"
                echo "   Infrastructure: RabbitMQ"
                echo "   Estimated time: 4-6 minutes"
                ;;
            3)
                echo "🟡 Phase 3: Advanced Services"
                echo "   Services: Oracle, Storage, CrossChain, Proof of Reserve"
                echo "   Advanced: Randomness, Fair Ordering, TEE Host"
                echo "   Estimated time: 6-10 minutes"
                ;;
            4)
                echo "🟣 Phase 4: Final Services & Web Interface"
                echo "   Services: Voting, Zero Knowledge, Secrets Management"
                echo "   Security: Social Recovery, Enclave Storage, Network Security"
                echo "   Interface: Web UI"
                echo "   Estimated time: 6-8 minutes"
                ;;
        esac
        echo ""
    done
    
    local total_time=$((${#PHASES_TO_DEPLOY[@]} * 7))
    echo "⏱️  Total estimated deployment time: ${total_time}-$((total_time + 10)) minutes"
    echo "💾 Total disk space required: ~10-15GB"
    echo "🧠 Total memory required: ~8-12GB"
    echo ""
}

# Confirm deployment
confirm_deployment() {
    if [ "$FORCE" = true ]; then
        return 0
    fi
    
    echo -e "${YELLOW}⚠️  This will deploy ${#PHASES_TO_DEPLOY[@]} phase(s) of the Neo Service Layer.${NC}"
    echo -e "${YELLOW}   This is a significant deployment that will consume system resources.${NC}"
    echo ""
    read -p "Do you want to proceed? (yes/no): " confirm
    
    if [ "$confirm" != "yes" ]; then
        log_info "Deployment cancelled by user"
        exit 0
    fi
}

# Deploy a specific phase
deploy_phase() {
    local phase=$1
    local script_name="deploy-phase${phase}.sh"
    local script_path="$SCRIPT_DIR/$script_name"
    
    log_section "Deploying Phase $phase"
    
    if [ ! -f "$script_path" ]; then
        log_error "Phase $phase deployment script not found: $script_path"
        return 1
    fi
    
    if [ "$DRY_RUN" = true ]; then
        log_info "DRY RUN: Would execute $script_path"
        return 0
    fi
    
    local start_time=$(date +%s)
    
    if "$script_path"; then
        local end_time=$(date +%s)
        local duration=$((end_time - start_time))
        log_success "Phase $phase deployed successfully in ${duration}s"
        return 0
    else
        log_error "Phase $phase deployment failed"
        return 1
    fi
}

# Run health checks for a specific phase
run_phase_health_check() {
    local phase=$1
    local script_name="health-check-phase${phase}.sh"
    local script_path="$SCRIPT_DIR/$script_name"
    
    if [ ! -f "$script_path" ]; then
        log_warn "Health check script not found for phase $phase"
        return 0
    fi
    
    if "$script_path"; then
        log_success "Phase $phase health check passed"
        return 0
    else
        log_error "Phase $phase health check failed"
        return 1
    fi
}

# Run health checks for all requested phases
run_health_checks() {
    log_section "Running Health Checks"
    
    local all_healthy=true
    
    # Determine which phases to check
    local phases_to_check=()
    if [ ${#PHASES_TO_DEPLOY[@]} -gt 0 ]; then
        phases_to_check=("${PHASES_TO_DEPLOY[@]}")
    else
        # Check all phases that might be deployed
        for phase in 1 2 3 4; do
            if [ -f "$SCRIPT_DIR/health-check-phase${phase}.sh" ]; then
                phases_to_check+=($phase)
            fi
        done
    fi
    
    for phase in "${phases_to_check[@]}"; do
        if ! run_phase_health_check "$phase"; then
            all_healthy=false
        fi
    done
    
    return $all_healthy
}

# Rollback a specific phase
rollback_phase() {
    local phase=$1
    local script_name="rollback-phase${phase}.sh"
    local script_path="$SCRIPT_DIR/$script_name"
    
    log_section "Rolling Back Phase $phase"
    
    if [ ! -f "$script_path" ]; then
        log_error "Phase $phase rollback script not found: $script_path"
        return 1
    fi
    
    if "$script_path"; then
        log_success "Phase $phase rolled back successfully"
        return 0
    else
        log_error "Phase $phase rollback failed"
        return 1
    fi
}

# Deploy all requested phases
deploy_all_phases() {
    log_section "Starting Deployment Process"
    
    local deployment_start=$(date +%s)
    local failed_phases=()
    
    for phase in "${PHASES_TO_DEPLOY[@]}"; do
        if ! deploy_phase "$phase"; then
            failed_phases+=($phase)
            
            # Ask if user wants to continue with remaining phases
            if [ "$FORCE" = false ]; then
                echo ""
                read -p "Phase $phase failed. Continue with remaining phases? (yes/no): " continue_deploy
                if [ "$continue_deploy" != "yes" ]; then
                    log_error "Deployment stopped by user after Phase $phase failure"
                    return 1
                fi
            fi
        fi
    done
    
    local deployment_end=$(date +%s)
    local total_duration=$((deployment_end - deployment_start))
    
    # Deployment summary
    echo ""
    log_section "Deployment Summary"
    
    if [ ${#failed_phases[@]} -eq 0 ]; then
        log_success "All phases deployed successfully!"
        echo "🎉 Total deployment time: ${total_duration}s"
    else
        log_warn "Deployment completed with failures in phases: ${failed_phases[*]}"
        echo "⚠️  Total deployment time: ${total_duration}s"
        echo ""
        echo "Failed phases can be redeployed individually:"
        for failed_phase in "${failed_phases[@]}"; do
            echo "  ./scripts/deploy-phase${failed_phase}.sh"
        done
    fi
    
    return ${#failed_phases[@]}
}

# Generate deployment report
generate_deployment_report() {
    local report_file="$PROJECT_ROOT/DEPLOYMENT_REPORT_$(date +%Y%m%d_%H%M%S).md"
    
    cat > "$report_file" << EOF
# Neo Service Layer Deployment Report

**Deployment Date:** $(date)
**Phases Deployed:** ${PHASES_TO_DEPLOY[*]}
**Deployment Type:** $([ "$DRY_RUN" = true ] && echo "Dry Run" || echo "Full Deployment")

## Summary

$([ ${#PHASES_TO_DEPLOY[@]} -eq 4 ] && echo "Complete Neo Service Layer deployment executed." || echo "Partial deployment of ${#PHASES_TO_DEPLOY[@]} phase(s).")

### Deployed Phases

$(for phase in "${PHASES_TO_DEPLOY[@]}"; do
    case $phase in
        1) echo "- ✅ Phase 1: Core Infrastructure & API Gateway" ;;
        2) echo "- ✅ Phase 2: Security & AI Services" ;;
        3) echo "- ✅ Phase 3: Advanced Services" ;;
        4) echo "- ✅ Phase 4: Final Services & Web Interface" ;;
    esac
done)

## Access Points

- **Main API Gateway:** http://localhost:8080
- **Web Interface:** http://localhost:8101
- **Monitoring Dashboard:** http://localhost:3000
- **Service Discovery:** http://localhost:8500

## Health Check Commands

\`\`\`bash
# Check specific phases
$(for phase in "${PHASES_TO_DEPLOY[@]}"; do
    echo "./scripts/health-check-phase${phase}.sh"
done)

# Check all phases
./scripts/deploy-complete.sh --check-only
\`\`\`

## Troubleshooting

If issues occur:

1. Check service logs: \`docker logs [container-name]\`
2. Verify resource usage: \`docker stats\`
3. Run health checks: \`./scripts/health-check-phase[X].sh\`
4. Rollback if needed: \`./scripts/rollback-phase[X].sh\`

---

Generated by Neo Service Layer deployment script
EOF

    log_success "Deployment report generated: $report_file"
}

# Main function
main() {
    # Parse command line arguments
    parse_args "$@"
    
    # Show header
    echo -e "${BLUE}================================================================${NC}"
    echo -e "${BLUE}           Neo Service Layer Complete Deployment               ${NC}"
    echo -e "${BLUE}================================================================${NC}"
    echo ""
    
    # Handle rollback request
    if [ -n "$ROLLBACK_PHASE" ]; then
        rollback_phase "$ROLLBACK_PHASE"
        exit $?
    fi
    
    # Handle check-only request
    if [ "$CHECK_ONLY" = true ]; then
        if run_health_checks; then
            log_success "All health checks passed"
            exit 0
        else
            log_error "Some health checks failed"
            exit 1
        fi
    fi
    
    # Verify prerequisites
    if ! check_prerequisites; then
        exit 1
    fi
    
    # Check system resources
    check_system_resources
    
    # Show deployment plan
    show_deployment_plan
    
    # Confirm deployment (unless force flag is set)
    if [ "$DRY_RUN" = false ]; then
        confirm_deployment
    fi
    
    # Execute deployment
    if [ "$DRY_RUN" = true ]; then
        log_success "Dry run completed - no actual deployment performed"
        exit 0
    fi
    
    # Deploy all phases
    if deploy_all_phases; then
        # Run final health checks
        echo ""
        if run_health_checks; then
            log_success "Deployment completed successfully with all health checks passing!"
        else
            log_warn "Deployment completed but some health checks failed"
        fi
        
        # Generate report
        generate_deployment_report
        
        # Final success message
        echo ""
        echo -e "${GREEN}🎉 ================================================== 🎉${NC}"
        echo -e "${GREEN}    NEO SERVICE LAYER DEPLOYMENT COMPLETE!         ${NC}"
        echo -e "${GREEN}🎉 ================================================== 🎉${NC}"
        echo ""
        echo "🌐 Access the system at: http://localhost:8080"
        echo "📊 Monitor at: http://localhost:3000"
        echo "📖 Documentation: http://localhost:8080/swagger"
        echo ""
        
        exit 0
    else
        log_error "Deployment failed"
        exit 1
    fi
}

# Run main function with all arguments
main "$@"