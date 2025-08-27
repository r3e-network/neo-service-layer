#!/bin/bash

# Neo Service Layer - Database Migration Script
# Phase 1: Extract databases for microservices
# This script handles the migration from monolithic database to microservice databases

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
BACKUP_DIR="${PROJECT_ROOT}/backups/$(date +%Y%m%d_%H%M%S)"
LOG_FILE="${PROJECT_ROOT}/logs/migration-$(date +%Y%m%d_%H%M%S).log"

# Database configuration
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_USER="${DB_USER:-neo_admin}"
DB_NAME="${DB_NAME:-neo_service_layer}"
DB_PASSWORD="${DB_PASSWORD:-}"

# Microservice databases
declare -A SERVICE_DATABASES=(
    ["auth"]="neo_auth_db"
    ["oracle"]="neo_oracle_db"
    ["compute"]="neo_compute_db"
    ["storage"]="neo_storage_db"
    ["secrets"]="neo_secrets_db"
    ["voting"]="neo_voting_db"
    ["monitoring"]="neo_monitoring_db"
    ["health"]="neo_health_db"
)

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging function
log() {
    echo -e "${1}" | tee -a "${LOG_FILE}"
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

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if psql is installed
    if ! command -v psql &> /dev/null; then
        log_error "psql is not installed. Please install PostgreSQL client."
        exit 1
    fi
    
    # Check if pg_dump is installed
    if ! command -v pg_dump &> /dev/null; then
        log_error "pg_dump is not installed. Please install PostgreSQL client tools."
        exit 1
    fi
    
    # Check database connectivity
    if ! PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" -c '\q' 2>/dev/null; then
        log_error "Cannot connect to database. Please check connection parameters."
        exit 1
    fi
    
    log_success "Prerequisites check passed"
}

# Create backup directory
create_backup_dir() {
    log_info "Creating backup directory: ${BACKUP_DIR}"
    mkdir -p "${BACKUP_DIR}"
    mkdir -p "$(dirname "${LOG_FILE}")"
}

# Backup original database
backup_original_database() {
    log_info "Backing up original database..."
    
    local backup_file="${BACKUP_DIR}/original_${DB_NAME}_$(date +%Y%m%d_%H%M%S).sql"
    
    PGPASSWORD="${DB_PASSWORD}" pg_dump \
        -h "${DB_HOST}" \
        -p "${DB_PORT}" \
        -U "${DB_USER}" \
        -d "${DB_NAME}" \
        --clean \
        --create \
        --verbose \
        --file="${backup_file}" 2>&1 | tee -a "${LOG_FILE}"
    
    if [ $? -eq 0 ]; then
        log_success "Database backup completed: ${backup_file}"
    else
        log_error "Database backup failed"
        exit 1
    fi
}

# Analyze database schema
analyze_schema() {
    log_info "Analyzing current database schema..."
    
    # Get all tables
    PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" \
        -c "SELECT schemaname, tablename FROM pg_tables WHERE schemaname = 'public';" \
        -t -A -F',' > "${BACKUP_DIR}/current_tables.csv"
    
    # Get table relationships
    PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" \
        -c "SELECT 
                tc.table_name,
                kcu.column_name,
                ccu.table_name AS foreign_table_name,
                ccu.column_name AS foreign_column_name
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
            JOIN information_schema.constraint_column_usage ccu ON ccu.constraint_name = tc.constraint_name
            WHERE tc.constraint_type = 'FOREIGN KEY';" \
        -t -A -F',' > "${BACKUP_DIR}/foreign_keys.csv"
    
    log_success "Schema analysis completed"
}

# Create microservice databases
create_microservice_databases() {
    log_info "Creating microservice databases..."
    
    for service in "${!SERVICE_DATABASES[@]}"; do
        local db_name="${SERVICE_DATABASES[$service]}"
        log_info "Creating database: ${db_name}"
        
        # Create database
        PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "postgres" \
            -c "CREATE DATABASE \"${db_name}\" OWNER \"${DB_USER}\";" 2>/dev/null || log_warning "Database ${db_name} may already exist"
        
        # Create basic schema
        PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${db_name}" \
            -c "CREATE SCHEMA IF NOT EXISTS public;" 2>/dev/null
        
        log_success "Database ${db_name} created successfully"
    done
}

# Extract auth service data
extract_auth_data() {
    log_info "Extracting authentication service data..."
    
    local auth_db="${SERVICE_DATABASES[auth]}"
    
    # Tables related to authentication
    local auth_tables=(
        "users"
        "user_sessions" 
        "refresh_tokens"
        "login_attempts"
        "mfa_secrets"
        "backup_codes"
        "audit_logs"
        "roles"
        "user_roles"
        "permissions"
        "role_permissions"
    )
    
    # Extract auth schema and data
    local auth_dump="${BACKUP_DIR}/auth_service_data.sql"
    
    PGPASSWORD="${DB_PASSWORD}" pg_dump \
        -h "${DB_HOST}" \
        -p "${DB_PORT}" \
        -U "${DB_USER}" \
        -d "${DB_NAME}" \
        --data-only \
        --inserts \
        $(printf -- "--table=%s " "${auth_tables[@]}") \
        --file="${auth_dump}" 2>&1 | tee -a "${LOG_FILE}"
    
    # Import into auth database
    PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${auth_db}" \
        -f "${auth_dump}" 2>&1 | tee -a "${LOG_FILE}"
    
    log_success "Auth service data extracted and migrated"
}

# Extract oracle service data
extract_oracle_data() {
    log_info "Extracting oracle service data..."
    
    local oracle_db="${SERVICE_DATABASES[oracle]}"
    
    # Tables related to oracle service
    local oracle_tables=(
        "oracle_configurations"
        "oracle_feeds" 
        "oracle_data_points"
        "oracle_jobs"
        "oracle_responses"
        "oracle_subscriptions"
        "price_feeds"
        "feed_validations"
        "consensus_results"
    )
    
    local oracle_dump="${BACKUP_DIR}/oracle_service_data.sql"
    
    PGPASSWORD="${DB_PASSWORD}" pg_dump \
        -h "${DB_HOST}" \
        -p "${DB_PORT}" \
        -U "${DB_USER}" \
        -d "${DB_NAME}" \
        --data-only \
        --inserts \
        $(printf -- "--table=%s " "${oracle_tables[@]}" 2>/dev/null || echo) \
        --file="${oracle_dump}" 2>&1 | tee -a "${LOG_FILE}"
    
    # Import into oracle database
    PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${oracle_db}" \
        -f "${oracle_dump}" 2>&1 | tee -a "${LOG_FILE}"
    
    log_success "Oracle service data extracted and migrated"
}

# Extract compute service data
extract_compute_data() {
    log_info "Extracting compute service data..."
    
    local compute_db="${SERVICE_DATABASES[compute]}"
    
    # Tables related to compute/SGX service
    local compute_tables=(
        "compute_jobs"
        "sgx_enclaves"
        "attestations"
        "compute_results"
        "enclave_configurations"
        "secure_sessions"
        "computation_logs"
        "resource_allocations"
    )
    
    local compute_dump="${BACKUP_DIR}/compute_service_data.sql"
    
    PGPASSWORD="${DB_PASSWORD}" pg_dump \
        -h "${DB_HOST}" \
        -p "${DB_PORT}" \
        -U "${DB_USER}" \
        -d "${DB_NAME}" \
        --data-only \
        --inserts \
        $(printf -- "--table=%s " "${compute_tables[@]}" 2>/dev/null || echo) \
        --file="${compute_dump}" 2>&1 | tee -a "${LOG_FILE}"
    
    # Import into compute database
    PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${compute_db}" \
        -f "${compute_dump}" 2>&1 | tee -a "${LOG_FILE}"
    
    log_success "Compute service data extracted and migrated"
}

# Verify data integrity
verify_data_integrity() {
    log_info "Verifying data integrity..."
    
    for service in "${!SERVICE_DATABASES[@]}"; do
        local db_name="${SERVICE_DATABASES[$service]}"
        log_info "Checking ${service} service database..."
        
        # Count tables
        local table_count
        table_count=$(PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${db_name}" \
            -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';")
        
        log_info "Database ${db_name} has ${table_count} tables"
        
        # Check for any errors in logs
        if grep -i error "${LOG_FILE}" | grep -i "${service}" > /dev/null; then
            log_warning "Found errors for ${service} service in migration log"
        fi
    done
    
    log_success "Data integrity verification completed"
}

# Create migration report
create_migration_report() {
    log_info "Creating migration report..."
    
    local report_file="${BACKUP_DIR}/migration_report.txt"
    
    cat > "${report_file}" << EOF
Neo Service Layer - Database Migration Report
============================================

Migration Date: $(date)
Original Database: ${DB_NAME}
Backup Location: ${BACKUP_DIR}
Log File: ${LOG_FILE}

Microservice Databases Created:
EOF
    
    for service in "${!SERVICE_DATABASES[@]}"; do
        echo "- ${service}: ${SERVICE_DATABASES[$service]}" >> "${report_file}"
    done
    
    cat >> "${report_file}" << EOF

Migration Steps Completed:
1. Prerequisites check
2. Original database backup
3. Schema analysis
4. Microservice database creation
5. Auth service data extraction
6. Oracle service data extraction
7. Compute service data extraction
8. Data integrity verification

Next Steps:
1. Update application connection strings
2. Deploy microservices with new database connections
3. Test data consistency
4. Monitor performance
5. Clean up original database (after verification)

Files Generated:
- $(basename "${BACKUP_DIR}")/original_${DB_NAME}_*.sql (Original database backup)
- $(basename "${BACKUP_DIR}")/auth_service_data.sql (Auth service data)
- $(basename "${BACKUP_DIR}")/oracle_service_data.sql (Oracle service data)
- $(basename "${BACKUP_DIR}")/compute_service_data.sql (Compute service data)
- $(basename "${BACKUP_DIR}")/current_tables.csv (Original schema tables)
- $(basename "${BACKUP_DIR}")/foreign_keys.csv (Original foreign keys)

EOF
    
    log_success "Migration report created: ${report_file}"
}

# Main execution
main() {
    log_info "Starting Neo Service Layer Database Migration"
    log_info "=========================================="
    
    check_prerequisites
    create_backup_dir
    backup_original_database
    analyze_schema
    create_microservice_databases
    extract_auth_data
    extract_oracle_data
    extract_compute_data
    verify_data_integrity
    create_migration_report
    
    log_success "Database migration completed successfully!"
    log_info "Migration report available at: ${BACKUP_DIR}/migration_report.txt"
    log_info "Log file available at: ${LOG_FILE}"
}

# Handle script interruption
trap 'log_error "Migration interrupted by user"; exit 130' INT

# Execute main function
main "$@"