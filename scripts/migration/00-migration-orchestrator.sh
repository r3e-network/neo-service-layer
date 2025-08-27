#!/bin/bash

# Neo Service Layer - Migration Orchestrator
# Master script to coordinate the complete microservices migration
# Executes all migration phases in proper sequence with validation

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
LOG_DIR="${PROJECT_ROOT}/logs"
MASTER_LOG="${LOG_DIR}/migration-master-$(date +%Y%m%d_%H%M%S).log"

# Migration phases
PHASE_1_SCRIPT="${SCRIPT_DIR}/01-database-migration.sh"
PHASE_2_SCRIPT="${SCRIPT_DIR}/02-service-extraction.sh"
PHASE_3_SCRIPT="${SCRIPT_DIR}/03-deployment-migration.sh"

# Migration configuration
MIGRATION_MODE="${MIGRATION_MODE:-full}"  # full, database-only, services-only, deploy-only
DRY_RUN="${DRY_RUN:-false}"
SKIP_VALIDATION="${SKIP_VALIDATION:-false}"
ROLLBACK_ON_FAILURE="${ROLLBACK_ON_FAILURE:-true}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

# Logging functions
log() {
    echo -e "${1}" | tee -a "${MASTER_LOG}"
}

log_header() {
    log "${CYAN}${BOLD}$1${NC}"
    log "${CYAN}$(printf '=%.0s' $(seq 1 ${#1}))${NC}"
}

log_info() {
    log "${BLUE}[INFO]${NC} $1"
}

log_success() {
    log "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    log "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    log "${RED}[ERROR]${NC} $1"
}

log_phase() {
    log "${BOLD}${BLUE}[PHASE $1]${NC} $2"
}

# Display banner
show_banner() {
    log_header "Neo Service Layer - Microservices Migration"
    log ""
    log "${BOLD}Migration Configuration:${NC}"
    log "  Mode: ${MIGRATION_MODE}"
    log "  Dry Run: ${DRY_RUN}"
    log "  Skip Validation: ${SKIP_VALIDATION}"
    log "  Rollback on Failure: ${ROLLBACK_ON_FAILURE}"
    log "  Project Root: ${PROJECT_ROOT}"
    log "  Log Directory: ${LOG_DIR}"
    log ""
    log "${BOLD}Migration Phases:${NC}"
    log "  1. Database Migration - Extract databases for microservices"
    log "  2. Service Extraction - Create microservice templates and code"
    log "  3. Deployment Migration - Deploy and configure services"
    log ""
}

# Check prerequisites for entire migration
check_global_prerequisites() {
    log_phase "0" "Checking Global Prerequisites"
    
    # Create log directory
    mkdir -p "${LOG_DIR}"
    
    # Check if all phase scripts exist
    local missing_scripts=()
    
    if [ ! -f "${PHASE_1_SCRIPT}" ]; then
        missing_scripts+=("Phase 1: Database Migration")
    fi
    
    if [ ! -f "${PHASE_2_SCRIPT}" ]; then
        missing_scripts+=("Phase 2: Service Extraction")
    fi
    
    if [ ! -f "${PHASE_3_SCRIPT}" ]; then
        missing_scripts+=("Phase 3: Deployment Migration")
    fi
    
    if [ ${#missing_scripts[@]} -gt 0 ]; then
        log_error "Missing migration scripts:"
        printf '%s\n' "${missing_scripts[@]}" | tee -a "${MASTER_LOG}"
        exit 1
    fi
    
    # Make scripts executable
    chmod +x "${PHASE_1_SCRIPT}" "${PHASE_2_SCRIPT}" "${PHASE_3_SCRIPT}"
    
    # Check system requirements
    local required_tools=("psql" "pg_dump" "dotnet" "kubectl" "docker")
    local missing_tools=()
    
    for tool in "${required_tools[@]}"; do
        if ! command -v "${tool}" &> /dev/null; then
            missing_tools+=("${tool}")
        fi
    done
    
    if [ ${#missing_tools[@]} -gt 0 ]; then
        log_warning "Missing optional tools (may be needed for specific phases): ${missing_tools[*]}"
    fi
    
    # Check disk space (minimum 10GB)
    local available_space
    available_space=$(df "${PROJECT_ROOT}" | awk 'NR==2 {print $4}')
    if [ "${available_space}" -lt 10485760 ]; then  # 10GB in KB
        log_warning "Low disk space detected. Migration may require more space for backups."
    fi
    
    log_success "Global prerequisites check completed"
}

# Validate environment before migration
validate_environment() {
    if [ "${SKIP_VALIDATION}" == "true" ]; then
        log_info "Skipping environment validation (SKIP_VALIDATION=true)"
        return 0
    fi
    
    log_phase "0" "Validating Environment"
    
    # Check if this is a valid Neo Service Layer project
    if [ ! -f "${PROJECT_ROOT}/README.md" ] && [ ! -d "${PROJECT_ROOT}/src" ]; then
        log_error "This doesn't appear to be a valid Neo Service Layer project"
        log_error "Please run this script from the project root directory"
        exit 1
    fi
    
    # Check for existing backup directories
    if [ -d "${PROJECT_ROOT}/backups" ]; then
        local backup_count
        backup_count=$(find "${PROJECT_ROOT}/backups" -maxdepth 1 -type d | wc -l)
        if [ "${backup_count}" -gt 10 ]; then
            log_warning "Many backup directories found (${backup_count}). Consider cleaning old backups."
        fi
    fi
    
    # Validate database connectivity if database migration is requested
    if [[ "${MIGRATION_MODE}" == "full" || "${MIGRATION_MODE}" == "database-only" ]]; then
        log_info "Testing database connectivity..."
        
        # Use default values or environment variables
        local db_host="${DB_HOST:-localhost}"
        local db_port="${DB_PORT:-5432}"
        local db_user="${DB_USER:-neo_admin}"
        local db_name="${DB_NAME:-neo_service_layer}"
        local db_password="${DB_PASSWORD:-}"
        
        if ! PGPASSWORD="${db_password}" psql -h "${db_host}" -p "${db_port}" -U "${db_user}" -d "${db_name}" -c '\q' 2>/dev/null; then
            log_warning "Cannot connect to database. Database migration phase may fail."
            log_info "Set DB_HOST, DB_PORT, DB_USER, DB_NAME, DB_PASSWORD environment variables if needed"
        else
            log_success "Database connectivity validated"
        fi
    fi
    
    # Validate Kubernetes connectivity if deployment is requested
    if [[ "${MIGRATION_MODE}" == "full" || "${MIGRATION_MODE}" == "deploy-only" ]]; then
        log_info "Testing Kubernetes connectivity..."
        
        if command -v kubectl &> /dev/null; then
            if kubectl cluster-info &> /dev/null; then
                local cluster_name
                cluster_name=$(kubectl config current-context)
                log_success "Kubernetes connectivity validated (cluster: ${cluster_name})"
            else
                log_warning "Cannot connect to Kubernetes cluster. Deployment phase may fail."
                log_info "Configure kubectl with valid cluster credentials if needed"
            fi
        else
            log_warning "kubectl not found. Deployment phase will be skipped."
        fi
    fi
    
    log_success "Environment validation completed"
}

# Execute database migration phase
execute_phase_1() {
    log_phase "1" "Database Migration"
    
    if [[ "${MIGRATION_MODE}" != "full" && "${MIGRATION_MODE}" != "database-only" ]]; then
        log_info "Skipping database migration (mode: ${MIGRATION_MODE})"
        return 0
    fi
    
    log_info "Starting database migration..."
    
    if [ "${DRY_RUN}" == "true" ]; then
        log_info "DRY RUN: Would execute database migration script"
        log_info "Command: ${PHASE_1_SCRIPT}"
        return 0
    fi
    
    # Execute phase 1 script
    if "${PHASE_1_SCRIPT}" 2>&1 | tee -a "${MASTER_LOG}"; then
        log_success "Phase 1 (Database Migration) completed successfully"
        return 0
    else
        local exit_code=$?
        log_error "Phase 1 (Database Migration) failed with exit code ${exit_code}"
        return ${exit_code}
    fi
}

# Execute service extraction phase
execute_phase_2() {
    log_phase "2" "Service Extraction"
    
    if [[ "${MIGRATION_MODE}" != "full" && "${MIGRATION_MODE}" != "services-only" ]]; then
        log_info "Skipping service extraction (mode: ${MIGRATION_MODE})"
        return 0
    fi
    
    log_info "Starting service extraction..."
    
    if [ "${DRY_RUN}" == "true" ]; then
        log_info "DRY RUN: Would execute service extraction script"
        log_info "Command: ${PHASE_2_SCRIPT}"
        return 0
    fi
    
    # Execute phase 2 script
    if "${PHASE_2_SCRIPT}" 2>&1 | tee -a "${MASTER_LOG}"; then
        log_success "Phase 2 (Service Extraction) completed successfully"
        return 0
    else
        local exit_code=$?
        log_error "Phase 2 (Service Extraction) failed with exit code ${exit_code}"
        return ${exit_code}
    fi
}

# Execute deployment migration phase
execute_phase_3() {
    log_phase "3" "Deployment Migration"
    
    if [[ "${MIGRATION_MODE}" != "full" && "${MIGRATION_MODE}" != "deploy-only" ]]; then
        log_info "Skipping deployment migration (mode: ${MIGRATION_MODE})"
        return 0
    fi
    
    log_info "Starting deployment migration..."
    
    if [ "${DRY_RUN}" == "true" ]; then
        log_info "DRY RUN: Would execute deployment migration script"
        log_info "Command: ${PHASE_3_SCRIPT}"
        return 0
    fi
    
    # Execute phase 3 script
    if "${PHASE_3_SCRIPT}" 2>&1 | tee -a "${MASTER_LOG}"; then
        log_success "Phase 3 (Deployment Migration) completed successfully"
        return 0
    else
        local exit_code=$?
        log_error "Phase 3 (Deployment Migration) failed with exit code ${exit_code}"
        return ${exit_code}
    fi
}

# Generate comprehensive migration report
generate_migration_report() {
    log_phase "REPORT" "Generating Migration Report"
    
    local report_file="${LOG_DIR}/migration-final-report-$(date +%Y%m%d_%H%M%S).md"
    
    cat > "${report_file}" << EOF
# Neo Service Layer - Complete Migration Report

**Migration Completed:** $(date)
**Migration Mode:** ${MIGRATION_MODE}
**Project Root:** ${PROJECT_ROOT}
**Master Log:** ${MASTER_LOG}

## Executive Summary

The Neo Service Layer microservices migration has been completed using the automated migration orchestrator. This report provides a comprehensive overview of all migration phases, their outcomes, and next steps.

## Migration Configuration

- **Migration Mode:** ${MIGRATION_MODE}
- **Dry Run:** ${DRY_RUN}
- **Skip Validation:** ${SKIP_VALIDATION}
- **Rollback on Failure:** ${ROLLBACK_ON_FAILURE}

## Migration Phases

### Phase 1: Database Migration
EOF
    
    if [[ "${MIGRATION_MODE}" == "full" || "${MIGRATION_MODE}" == "database-only" ]]; then
        if [ -d "${PROJECT_ROOT}/backups" ]; then
            local backup_count
            backup_count=$(find "${PROJECT_ROOT}/backups" -name "*.sql" | wc -l)
            echo "- Status: ✅ Completed" >> "${report_file}"
            echo "- Database backups created: ${backup_count} files" >> "${report_file}"
            echo "- Microservice databases: auth, oracle, compute, storage, secrets, voting, monitoring, health" >> "${report_file}"
        else
            echo "- Status: ❌ Failed or not executed" >> "${report_file}"
        fi
    else
        echo "- Status: ⏭️ Skipped (mode: ${MIGRATION_MODE})" >> "${report_file}"
    fi
    
    cat >> "${report_file}" << EOF

### Phase 2: Service Extraction
EOF
    
    if [[ "${MIGRATION_MODE}" == "full" || "${MIGRATION_MODE}" == "services-only" ]]; then
        if [ -d "${PROJECT_ROOT}/extracted_services" ]; then
            local service_count
            service_count=$(find "${PROJECT_ROOT}/extracted_services" -maxdepth 1 -type d | wc -l)
            echo "- Status: ✅ Completed" >> "${report_file}"
            echo "- Services extracted: ${service_count} microservices" >> "${report_file}"
            echo "- Location: \`${PROJECT_ROOT}/extracted_services\`" >> "${report_file}"
        else
            echo "- Status: ❌ Failed or not executed" >> "${report_file}"
        fi
    else
        echo "- Status: ⏭️ Skipped (mode: ${MIGRATION_MODE})" >> "${report_file}"
    fi
    
    cat >> "${report_file}" << EOF

### Phase 3: Deployment Migration
EOF
    
    if [[ "${MIGRATION_MODE}" == "full" || "${MIGRATION_MODE}" == "deploy-only" ]]; then
        if command -v kubectl &> /dev/null && kubectl cluster-info &> /dev/null; then
            local pod_count
            pod_count=$(kubectl get pods --all-namespaces | grep neo- | wc -l 2>/dev/null || echo "0")
            if [ "${pod_count}" -gt 0 ]; then
                echo "- Status: ✅ Completed" >> "${report_file}"
                echo "- Deployed pods: ${pod_count} Neo service pods" >> "${report_file}"
                echo "- Kubernetes cluster: $(kubectl config current-context 2>/dev/null || echo 'Unknown')" >> "${report_file}"
            else
                echo "- Status: ❌ Failed or no pods deployed" >> "${report_file}"
            fi
        else
            echo "- Status: ❌ Failed (no Kubernetes access)" >> "${report_file}"
        fi
    else
        echo "- Status: ⏭️ Skipped (mode: ${MIGRATION_MODE})" >> "${report_file}"
    fi
    
    cat >> "${report_file}" << EOF

## File Locations

### Generated Files and Directories
- **Migration Logs:** \`${LOG_DIR}/\`
- **Database Backups:** \`${PROJECT_ROOT}/backups/\`
- **Extracted Services:** \`${PROJECT_ROOT}/extracted_services/\`
- **Kubernetes Manifests:** \`${PROJECT_ROOT}/k8s/\`

### Key Configuration Files
- **Service Projects:** \`extracted_services/*/src/*.csproj\`
- **Dockerfiles:** \`extracted_services/*/Dockerfile\`
- **K8s Manifests:** \`extracted_services/*/k8s/\`
- **Database Schemas:** \`backups/*/migration_report.txt\`

## Architecture Overview

The migration has transformed the monolithic Neo Service Layer into a microservices architecture:

### Service Domains
1. **Authentication Service** - User authentication, authorization, JWT management
2. **Oracle Service** - Data feeds, consensus mechanisms, price data
3. **Compute Service** - SGX enclave operations, secure computing
4. **Storage Service** - Distributed storage, file management
5. **Secrets Service** - Secure secrets management, key rotation
6. **Voting Service** - Governance, voting mechanisms
7. **Cross-chain Service** - Multi-blockchain operations
8. **Health Service** - System health monitoring, diagnostics

### Infrastructure Components
- **PostgreSQL Clusters** - Database-per-service pattern
- **Redis Cluster** - Caching and session storage
- **Istio Service Mesh** - Traffic management, security, observability
- **Prometheus + Grafana** - Monitoring and alerting
- **Jaeger** - Distributed tracing
- **ArgoCD** - GitOps deployment management

## Post-Migration Tasks

### Immediate Actions Required
1. **Test Service Functionality**
   - Verify all health endpoints are responding
   - Test authentication flows
   - Validate database connectivity
   - Check service-to-service communication

2. **Monitor System Performance**
   - Review Grafana dashboards
   - Check Prometheus metrics
   - Analyze Jaeger traces
   - Monitor resource utilization

3. **Update Client Applications**
   - Update API endpoints to use new service URLs
   - Implement proper retry and circuit breaker patterns
   - Test error handling and fallback mechanisms

### Short-term Improvements (1-2 weeks)
1. **Implement Comprehensive Testing**
   - Create integration tests for each service
   - Set up automated API testing
   - Implement chaos engineering tests

2. **Optimize Performance**
   - Tune database queries and indexes
   - Configure caching strategies
   - Adjust resource limits and scaling policies

3. **Enhance Security**
   - Review and update security policies
   - Implement network policies
   - Set up security scanning and compliance checks

### Long-term Goals (1-3 months)
1. **Advanced Observability**
   - Implement custom metrics and dashboards
   - Set up advanced alerting rules
   - Create operational runbooks

2. **Automation and DevOps**
   - Implement automated rollback procedures
   - Set up blue-green deployments
   - Create disaster recovery procedures

3. **Scale and Optimize**
   - Implement horizontal pod autoscaling
   - Optimize for cost and resource usage
   - Plan for multi-region deployment

## Troubleshooting Guide

### Common Issues and Solutions

#### Database Connection Issues
\`\`\`bash
# Check database pods
kubectl get pods -n neo-databases

# Test database connectivity
kubectl exec -it <postgres-pod> -n neo-databases -- psql -U neo_user -l
\`\`\`

#### Service Discovery Problems
\`\`\`bash
# Check service mesh configuration
kubectl get virtualservice,destinationrule --all-namespaces

# Verify DNS resolution
kubectl exec -it <pod-name> -- nslookup neo-auth-service.neo-services.svc.cluster.local
\`\`\`

#### Authentication Failures
\`\`\`bash
# Check JWT token configuration
kubectl get secret -n neo-services | grep jwt

# Verify auth service logs
kubectl logs -f deployment/neo-auth-service -n neo-services
\`\`\`

## Support and Documentation

- **Architecture Documentation:** \`${PROJECT_ROOT}/docs/architecture/\`
- **Migration Scripts:** \`${PROJECT_ROOT}/scripts/migration/\`
- **Kubernetes Manifests:** \`${PROJECT_ROOT}/k8s/\`
- **Service Code:** \`${PROJECT_ROOT}/extracted_services/\`

## Migration Statistics

- **Total Migration Time:** $(date -d @$(($(date +%s) - $(stat -c %Y "${MASTER_LOG}" 2>/dev/null || date +%s)))) -u +%H:%M:%S 2>/dev/null || echo "Unknown")
- **Log File Size:** $(du -h "${MASTER_LOG}" | cut -f1 2>/dev/null || echo "Unknown")
- **Generated Files:** $(find "${PROJECT_ROOT}" -newer "${MASTER_LOG}" -type f | wc -l 2>/dev/null || echo "Unknown")

---

**Migration completed successfully!** 🎉

For questions or issues, refer to the troubleshooting guide above or consult the project documentation.
EOF
    
    log_success "Migration report generated: ${report_file}"
    log_info "Report location: ${report_file}"
}

# Rollback function
perform_rollback() {
    log_error "MIGRATION FAILED - Initiating rollback procedure"
    
    # Stop any running processes
    log_info "Stopping migration processes..."
    
    # Rollback deployment if it was started
    if [[ "${MIGRATION_MODE}" == "full" || "${MIGRATION_MODE}" == "deploy-only" ]]; then
        if command -v kubectl &> /dev/null; then
            log_info "Rolling back Kubernetes deployments..."
            kubectl delete -f "${PROJECT_ROOT}/k8s/services/" --ignore-not-found=true 2>&1 | tee -a "${MASTER_LOG}" || true
            kubectl delete -f "${PROJECT_ROOT}/extracted_services/*/k8s/" --ignore-not-found=true 2>&1 | tee -a "${MASTER_LOG}" || true
        fi
    fi
    
    # Clean up extracted services if they were created
    if [ -d "${PROJECT_ROOT}/extracted_services" ]; then
        log_warning "Extracted services directory exists. Manual cleanup may be required."
        log_info "Location: ${PROJECT_ROOT}/extracted_services"
    fi
    
    # Database rollback information
    if [ -d "${PROJECT_ROOT}/backups" ]; then
        log_info "Database backups are preserved in: ${PROJECT_ROOT}/backups"
        log_info "Manual database restoration may be required if databases were modified"
    fi
    
    log_error "Rollback completed. Check logs for details: ${MASTER_LOG}"
}

# Main orchestration function
main() {
    # Setup
    show_banner
    
    # Set up error handling
    if [ "${ROLLBACK_ON_FAILURE}" == "true" ]; then
        trap 'perform_rollback; exit 1' ERR
    fi
    
    # Pre-migration checks
    check_global_prerequisites
    validate_environment
    
    log_header "Starting Migration Phases"
    
    # Execute migration phases
    local start_time
    start_time=$(date +%s)
    
    execute_phase_1
    execute_phase_2
    execute_phase_3
    
    local end_time
    end_time=$(date +%s)
    local duration=$((end_time - start_time))
    
    # Post-migration
    log_header "Migration Completed Successfully"
    log_success "Total migration time: $(date -d @${duration} -u +%H:%M:%S)"
    
    generate_migration_report
    
    log ""
    log "${GREEN}${BOLD}🎉 Neo Service Layer Migration Completed! 🎉${NC}"
    log ""
    log "${BOLD}Next Steps:${NC}"
    log "1. Review the migration report: ${LOG_DIR}/migration-final-report-*.md"
    log "2. Test service functionality and performance"
    log "3. Update client applications to use new service endpoints"
    log "4. Monitor system health using Grafana dashboards"
    log "5. Gradually increase traffic to microservices"
    log ""
    log "${BOLD}Support:${NC}"
    log "- Migration logs: ${MASTER_LOG}"
    log "- Architecture docs: ${PROJECT_ROOT}/docs/architecture/"
    log "- Troubleshooting: Check the migration report"
    log ""
}

# Help function
show_help() {
    cat << EOF
Neo Service Layer - Migration Orchestrator

USAGE:
    $0 [OPTIONS]

OPTIONS:
    -h, --help              Show this help message
    -m, --mode MODE         Migration mode (full|database-only|services-only|deploy-only)
    -d, --dry-run          Perform dry run without making changes
    -s, --skip-validation  Skip environment validation
    -r, --no-rollback      Disable rollback on failure

ENVIRONMENT VARIABLES:
    MIGRATION_MODE          Migration mode (default: full)
    DRY_RUN                Dry run flag (default: false)
    SKIP_VALIDATION        Skip validation flag (default: false)
    ROLLBACK_ON_FAILURE    Enable rollback on failure (default: true)
    
    Database configuration:
    DB_HOST                Database host (default: localhost)
    DB_PORT                Database port (default: 5432)
    DB_USER                Database user (default: neo_admin)
    DB_NAME                Database name (default: neo_service_layer)
    DB_PASSWORD            Database password

EXAMPLES:
    # Full migration
    $0

    # Database-only migration
    $0 --mode database-only

    # Dry run
    $0 --dry-run

    # Skip validation and perform services extraction only
    $0 --mode services-only --skip-validation

    # Deploy-only with environment variables
    MIGRATION_MODE=deploy-only $0
EOF
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                show_help
                exit 0
                ;;
            -m|--mode)
                MIGRATION_MODE="$2"
                shift 2
                ;;
            -d|--dry-run)
                DRY_RUN="true"
                shift
                ;;
            -s|--skip-validation)
                SKIP_VALIDATION="true"
                shift
                ;;
            -r|--no-rollback)
                ROLLBACK_ON_FAILURE="false"
                shift
                ;;
            *)
                log_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done
    
    # Validate migration mode
    case "${MIGRATION_MODE}" in
        full|database-only|services-only|deploy-only)
            ;;
        *)
            log_error "Invalid migration mode: ${MIGRATION_MODE}"
            log_error "Valid modes: full, database-only, services-only, deploy-only"
            exit 1
            ;;
    esac
}

# Handle script interruption
trap 'log_error "Migration interrupted by user"; if [ "${ROLLBACK_ON_FAILURE}" == "true" ]; then perform_rollback; fi; exit 130' INT

# Execute main function if script is run directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    parse_args "$@"
    main
fi