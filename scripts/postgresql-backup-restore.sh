#!/bin/bash

# Neo Service Layer PostgreSQL Backup and Restore Script

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Load environment variables
if [ -f "$PROJECT_ROOT/.env" ]; then
    export $(cat "$PROJECT_ROOT/.env" | grep -v '^#' | xargs)
fi

# Configuration
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-neo_service_layer}"
DB_USER="${DB_USER:-neo_user}"
BACKUP_DIR="$PROJECT_ROOT/backups/postgresql"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# Ensure backup directory exists
mkdir -p "$BACKUP_DIR"

usage() {
    echo "Usage: $0 [command] [options]"
    echo ""
    echo "Commands:"
    echo "  backup          Create full database backup"
    echo "  backup-schema   Create schema-only backup"
    echo "  backup-data     Create data-only backup"
    echo "  backup-sgx      Create SGX sealed data backup"
    echo "  restore [file]  Restore from backup file"
    echo "  list            List available backups"
    echo "  cleanup [days]  Remove backups older than N days (default: 30)"
    echo "  verify [file]   Verify backup integrity"
    echo ""
    echo "Options:"
    echo "  --compress      Compress backup with gzip"
    echo "  --encrypt       Encrypt backup (requires GPG)"
    echo "  --remote [url]  Upload to remote storage"
    echo ""
    echo "Examples:"
    echo "  $0 backup --compress"
    echo "  $0 backup-sgx --encrypt"
    echo "  $0 restore backups/postgresql/backup_20250821_120000.sql"
    echo "  $0 cleanup 7"
}

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

check_dependencies() {
    if ! command -v docker-compose &> /dev/null; then
        echo "Error: docker-compose is required but not installed."
        exit 1
    fi
}

backup_full() {
    local compress=${1:-false}
    local encrypt=${2:-false}
    
    log "Starting full database backup..."
    
    local backup_file="$BACKUP_DIR/backup_full_${TIMESTAMP}.sql"
    
    # Create backup
    docker-compose exec -T neo-postgres pg_dump \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        --verbose \
        --no-password \
        --format=custom \
        --compress=9 \
        > "${backup_file}.dump"
    
    # Also create plain SQL version for manual inspection
    docker-compose exec -T neo-postgres pg_dump \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        --verbose \
        --no-password \
        --format=plain \
        --inserts \
        > "$backup_file"
    
    if [ "$compress" = true ]; then
        log "Compressing backup..."
        gzip "$backup_file"
        backup_file="${backup_file}.gz"
    fi
    
    if [ "$encrypt" = true ]; then
        log "Encrypting backup..."
        gpg --symmetric --cipher-algo AES256 "$backup_file"
        rm "$backup_file"
        backup_file="${backup_file}.gpg"
    fi
    
    local size=$(du -h "$backup_file" | cut -f1)
    log "✅ Full backup completed: $backup_file ($size)"
    
    # Create backup metadata
    cat > "${backup_file}.meta" << EOF
{
    "type": "full",
    "timestamp": "$TIMESTAMP",
    "database": "$DB_NAME",
    "size": "$size",
    "compressed": $compress,
    "encrypted": $encrypt,
    "tables": $(docker-compose exec -T neo-postgres psql -U "$DB_USER" -d "$DB_NAME" -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema NOT IN ('information_schema', 'pg_catalog')"),
    "schemas": ["core", "auth", "sgx", "oracle", "voting", "crosschain", "monitoring", "eventsourcing"]
}
EOF

    echo "$backup_file"
}

backup_schema() {
    log "Starting schema-only backup..."
    
    local backup_file="$BACKUP_DIR/backup_schema_${TIMESTAMP}.sql"
    
    docker-compose exec -T neo-postgres pg_dump \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        --verbose \
        --no-password \
        --schema-only \
        > "$backup_file"
    
    local size=$(du -h "$backup_file" | cut -f1)
    log "✅ Schema backup completed: $backup_file ($size)"
    echo "$backup_file"
}

backup_data() {
    log "Starting data-only backup..."
    
    local backup_file="$BACKUP_DIR/backup_data_${TIMESTAMP}.sql"
    
    docker-compose exec -T neo-postgres pg_dump \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        --verbose \
        --no-password \
        --data-only \
        --inserts \
        > "$backup_file"
    
    local size=$(du -h "$backup_file" | cut -f1)
    log "✅ Data backup completed: $backup_file ($size)"
    echo "$backup_file"
}

backup_sgx() {
    local encrypt=${1:-false}
    
    log "Starting SGX sealed data backup..."
    
    local backup_file="$BACKUP_DIR/backup_sgx_${TIMESTAMP}.sql"
    
    # Backup SGX-specific data with extra security
    docker-compose exec -T neo-postgres pg_dump \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        --verbose \
        --no-password \
        --schema=sgx \
        --format=custom \
        --compress=9 \
        > "${backup_file}.dump"
    
    # Plain SQL for inspection (without sensitive data)
    docker-compose exec -T neo-postgres psql -U "$DB_USER" -d "$DB_NAME" -c "
    COPY (
        SELECT id, key, service_name, sealing_policy, version, created_at, updated_at, expires_at, 
               access_count, last_accessed_at, is_active, 
               'REDACTED' as sealed_data,
               metadata
        FROM sgx.sealed_data_items
    ) TO STDOUT WITH CSV HEADER;
    " > "${backup_file}.csv"
    
    if [ "$encrypt" = true ]; then
        log "Encrypting SGX backup..."
        gpg --symmetric --cipher-algo AES256 "${backup_file}.dump"
        gpg --symmetric --cipher-algo AES256 "${backup_file}.csv"
        rm "${backup_file}.dump" "${backup_file}.csv"
        backup_file="${backup_file}.dump.gpg"
    fi
    
    local size=$(du -h "${backup_file}"* | awk '{s+=$1} END {print s "K"}')
    log "✅ SGX backup completed: $backup_file ($size)"
    echo "$backup_file"
}

restore_backup() {
    local backup_file="$1"
    
    if [ ! -f "$backup_file" ]; then
        echo "Error: Backup file not found: $backup_file"
        exit 1
    fi
    
    log "Starting restore from: $backup_file"
    
    # Confirm restore operation
    read -p "⚠️  This will overwrite the current database. Continue? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log "Restore cancelled."
        exit 0
    fi
    
    # Check if file is encrypted
    if [[ "$backup_file" == *.gpg ]]; then
        log "Decrypting backup..."
        gpg --decrypt "$backup_file" > "${backup_file%.gpg}"
        backup_file="${backup_file%.gpg}"
    fi
    
    # Check if file is compressed
    if [[ "$backup_file" == *.gz ]]; then
        log "Decompressing backup..."
        gunzip -c "$backup_file" > "${backup_file%.gz}"
        backup_file="${backup_file%.gz}"
    fi
    
    # Restore based on file format
    if [[ "$backup_file" == *.dump ]]; then
        log "Restoring from custom format..."
        docker-compose exec -T neo-postgres pg_restore \
            -U "$DB_USER" \
            -d "$DB_NAME" \
            --verbose \
            --clean \
            --if-exists \
            < "$backup_file"
    else
        log "Restoring from SQL format..."
        docker-compose exec -T neo-postgres psql \
            -U "$DB_USER" \
            -d "$DB_NAME" \
            < "$backup_file"
    fi
    
    log "✅ Restore completed successfully"
}

list_backups() {
    log "Available backups in $BACKUP_DIR:"
    echo ""
    
    if [ ! -d "$BACKUP_DIR" ] || [ -z "$(ls -A "$BACKUP_DIR" 2>/dev/null)" ]; then
        echo "No backups found."
        return
    fi
    
    for file in "$BACKUP_DIR"/*.{sql,dump,gpg,gz} 2>/dev/null; do
        if [ -f "$file" ]; then
            local size=$(du -h "$file" | cut -f1)
            local date=$(stat -c %y "$file" | cut -d' ' -f1,2 | cut -d'.' -f1)
            printf "%-50s %8s %s\n" "$(basename "$file")" "$size" "$date"
        fi
    done
}

cleanup_backups() {
    local days=${1:-30}
    
    log "Cleaning up backups older than $days days..."
    
    local count=$(find "$BACKUP_DIR" -name "backup_*" -type f -mtime +$days | wc -l)
    
    if [ "$count" -eq 0 ]; then
        log "No old backups found."
        return
    fi
    
    log "Found $count backup(s) to remove:"
    find "$BACKUP_DIR" -name "backup_*" -type f -mtime +$days -exec basename {} \;
    
    read -p "Continue with cleanup? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        find "$BACKUP_DIR" -name "backup_*" -type f -mtime +$days -delete
        log "✅ Cleanup completed."
    else
        log "Cleanup cancelled."
    fi
}

verify_backup() {
    local backup_file="$1"
    
    if [ ! -f "$backup_file" ]; then
        echo "Error: Backup file not found: $backup_file"
        exit 1
    fi
    
    log "Verifying backup: $backup_file"
    
    if [[ "$backup_file" == *.gpg ]]; then
        log "Testing GPG decryption..."
        if gpg --decrypt "$backup_file" > /dev/null 2>&1; then
            log "✅ GPG encryption is valid"
        else
            log "❌ GPG decryption failed"
            exit 1
        fi
    fi
    
    if [[ "$backup_file" == *.gz ]]; then
        log "Testing gzip compression..."
        if gzip -t "$backup_file"; then
            log "✅ Gzip compression is valid"
        else
            log "❌ Gzip test failed"
            exit 1
        fi
    fi
    
    if [[ "$backup_file" == *.dump ]]; then
        log "Testing PostgreSQL dump format..."
        if pg_restore --list "$backup_file" > /dev/null 2>&1; then
            log "✅ PostgreSQL dump format is valid"
        else
            log "❌ PostgreSQL dump format test failed"
            exit 1
        fi
    fi
    
    log "✅ Backup verification completed successfully"
}

# Main script logic
check_dependencies

case "${1:-}" in
    backup)
        shift
        compress=false
        encrypt=false
        while [[ $# -gt 0 ]]; do
            case $1 in
                --compress) compress=true; shift ;;
                --encrypt) encrypt=true; shift ;;
                *) echo "Unknown option: $1"; usage; exit 1 ;;
            esac
        done
        backup_full "$compress" "$encrypt"
        ;;
    backup-schema)
        backup_schema
        ;;
    backup-data)
        backup_data
        ;;
    backup-sgx)
        shift
        encrypt=false
        while [[ $# -gt 0 ]]; do
            case $1 in
                --encrypt) encrypt=true; shift ;;
                *) echo "Unknown option: $1"; usage; exit 1 ;;
            esac
        done
        backup_sgx "$encrypt"
        ;;
    restore)
        if [ -z "$2" ]; then
            echo "Error: Backup file required for restore"
            usage
            exit 1
        fi
        restore_backup "$2"
        ;;
    list)
        list_backups
        ;;
    cleanup)
        cleanup_backups "$2"
        ;;
    verify)
        if [ -z "$2" ]; then
            echo "Error: Backup file required for verification"
            usage
            exit 1
        fi
        verify_backup "$2"
        ;;
    *)
        usage
        exit 1
        ;;
esac