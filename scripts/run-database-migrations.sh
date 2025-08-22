#!/bin/bash

# Neo Service Layer Database Migration Runner
# This script manages PostgreSQL database migrations

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-neoservice}"
DB_USER="${DB_USER:-neoservice_app}"
DB_PASSWORD="${DB_PASSWORD:-}"
MIGRATIONS_DIR="${MIGRATIONS_DIR:-/home/ubuntu/neo-service-layer/src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/Migrations}"
ACTION="${1:-migrate}"

# Function to print colored output
print_color() {
    local color=$1
    shift
    echo -e "${color}$*${NC}"
}

# Function to check if PostgreSQL is available
check_postgres() {
    print_color $BLUE "Checking PostgreSQL connection..."
    
    export PGPASSWORD=$DB_PASSWORD
    
    if pg_isready -h $DB_HOST -p $DB_PORT -U $DB_USER > /dev/null 2>&1; then
        print_color $GREEN "✓ PostgreSQL is available"
        return 0
    else
        print_color $RED "✗ PostgreSQL is not available"
        return 1
    fi
}

# Function to create database if it doesn't exist
create_database() {
    print_color $BLUE "Checking if database exists..."
    
    export PGPASSWORD=$DB_PASSWORD
    
    if psql -h $DB_HOST -p $DB_PORT -U $DB_USER -lqt | cut -d \| -f 1 | grep -qw $DB_NAME; then
        print_color $GREEN "✓ Database '$DB_NAME' exists"
    else
        print_color $YELLOW "Database '$DB_NAME' does not exist. Creating..."
        
        createdb -h $DB_HOST -p $DB_PORT -U $DB_USER $DB_NAME
        
        if [ $? -eq 0 ]; then
            print_color $GREEN "✓ Database '$DB_NAME' created successfully"
        else
            print_color $RED "✗ Failed to create database"
            exit 1
        fi
    fi
}

# Function to create migrations table
create_migrations_table() {
    print_color $BLUE "Creating migrations tracking table..."
    
    psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME <<EOF
CREATE TABLE IF NOT EXISTS schema_migrations (
    id SERIAL PRIMARY KEY,
    filename VARCHAR(255) NOT NULL UNIQUE,
    applied_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    checksum VARCHAR(64),
    execution_time_ms INTEGER,
    success BOOLEAN DEFAULT true,
    error_message TEXT
);

CREATE INDEX IF NOT EXISTS idx_migrations_filename ON schema_migrations(filename);
CREATE INDEX IF NOT EXISTS idx_migrations_applied_at ON schema_migrations(applied_at);
EOF
    
    print_color $GREEN "✓ Migrations table ready"
}

# Function to calculate file checksum
calculate_checksum() {
    local file=$1
    sha256sum "$file" | cut -d' ' -f1
}

# Function to check if migration was already applied
is_migration_applied() {
    local filename=$1
    local result=$(psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -tAc \
        "SELECT COUNT(*) FROM schema_migrations WHERE filename = '$filename' AND success = true")
    
    [ "$result" -gt 0 ]
}

# Function to record migration
record_migration() {
    local filename=$1
    local checksum=$2
    local execution_time=$3
    local success=$4
    local error_message="${5:-}"
    
    if [ "$success" = "true" ]; then
        psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME <<EOF
INSERT INTO schema_migrations (filename, checksum, execution_time_ms, success)
VALUES ('$filename', '$checksum', $execution_time, true)
ON CONFLICT (filename) DO UPDATE
SET applied_at = CURRENT_TIMESTAMP,
    checksum = EXCLUDED.checksum,
    execution_time_ms = EXCLUDED.execution_time_ms,
    success = true,
    error_message = NULL;
EOF
    else
        psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME <<EOF
INSERT INTO schema_migrations (filename, checksum, execution_time_ms, success, error_message)
VALUES ('$filename', '$checksum', $execution_time, false, '$error_message')
ON CONFLICT (filename) DO UPDATE
SET applied_at = CURRENT_TIMESTAMP,
    checksum = EXCLUDED.checksum,
    execution_time_ms = EXCLUDED.execution_time_ms,
    success = false,
    error_message = EXCLUDED.error_message;
EOF
    fi
}

# Function to run a single migration
run_migration() {
    local migration_file=$1
    local filename=$(basename "$migration_file")
    
    if is_migration_applied "$filename"; then
        print_color $YELLOW "⊙ Skipping $filename (already applied)"
        return 0
    fi
    
    print_color $BLUE "→ Applying $filename..."
    
    local checksum=$(calculate_checksum "$migration_file")
    local start_time=$(date +%s%N)
    
    # Create temporary file for error capture
    local error_file=$(mktemp)
    
    # Run migration
    if psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f "$migration_file" 2>"$error_file"; then
        local end_time=$(date +%s%N)
        local execution_time=$(( ($end_time - $start_time) / 1000000 ))
        
        record_migration "$filename" "$checksum" "$execution_time" "true"
        print_color $GREEN "✓ Applied $filename (${execution_time}ms)"
        rm -f "$error_file"
        return 0
    else
        local end_time=$(date +%s%N)
        local execution_time=$(( ($end_time - $start_time) / 1000000 ))
        local error_message=$(cat "$error_file" | tr '\n' ' ' | sed "s/'/''/g")
        
        record_migration "$filename" "$checksum" "$execution_time" "false" "$error_message"
        print_color $RED "✗ Failed to apply $filename"
        print_color $RED "Error: $(cat "$error_file")"
        rm -f "$error_file"
        return 1
    fi
}

# Function to run all migrations
run_all_migrations() {
    print_color $BLUE "Running database migrations..."
    
    local migration_count=0
    local success_count=0
    local skip_count=0
    local fail_count=0
    
    # Get all SQL files in migrations directory, sorted by name
    for migration_file in $(ls -1 "$MIGRATIONS_DIR"/*.sql 2>/dev/null | sort); do
        ((migration_count++))
        
        if run_migration "$migration_file"; then
            local filename=$(basename "$migration_file")
            if is_migration_applied "$filename"; then
                ((success_count++))
            else
                ((skip_count++))
            fi
        else
            ((fail_count++))
            # Stop on first failure
            break
        fi
    done
    
    print_color $BLUE ""
    print_color $BLUE "Migration Summary:"
    print_color $BLUE "  Total files: $migration_count"
    print_color $GREEN "  Applied: $success_count"
    print_color $YELLOW "  Skipped: $skip_count"
    
    if [ $fail_count -gt 0 ]; then
        print_color $RED "  Failed: $fail_count"
        return 1
    fi
    
    return 0
}

# Function to rollback last migration
rollback_last_migration() {
    print_color $BLUE "Rolling back last migration..."
    
    local last_migration=$(psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -tAc \
        "SELECT filename FROM schema_migrations WHERE success = true ORDER BY applied_at DESC LIMIT 1")
    
    if [ -z "$last_migration" ]; then
        print_color $YELLOW "No migrations to rollback"
        return 0
    fi
    
    local rollback_file="$MIGRATIONS_DIR/rollback_${last_migration}"
    
    if [ ! -f "$rollback_file" ]; then
        print_color $RED "✗ Rollback file not found: $rollback_file"
        print_color $YELLOW "Manual rollback may be required"
        return 1
    fi
    
    print_color $BLUE "→ Rolling back $last_migration..."
    
    if psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f "$rollback_file"; then
        psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c \
            "DELETE FROM schema_migrations WHERE filename = '$last_migration'"
        print_color $GREEN "✓ Rolled back $last_migration"
        return 0
    else
        print_color $RED "✗ Failed to rollback $last_migration"
        return 1
    fi
}

# Function to show migration status
show_status() {
    print_color $BLUE "Migration Status:"
    
    psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME <<EOF
SELECT 
    filename,
    applied_at,
    execution_time_ms as exec_ms,
    CASE WHEN success THEN '✓' ELSE '✗' END as status
FROM schema_migrations
ORDER BY applied_at DESC
LIMIT 20;
EOF
}

# Function to validate migrations
validate_migrations() {
    print_color $BLUE "Validating migrations..."
    
    local issues=0
    
    # Check for migrations in DB that don't exist on disk
    while IFS= read -r filename; do
        if [ ! -f "$MIGRATIONS_DIR/$filename" ]; then
            print_color $YELLOW "⚠ Migration in DB but not on disk: $filename"
            ((issues++))
        fi
    done < <(psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -tAc \
        "SELECT filename FROM schema_migrations WHERE success = true")
    
    # Check for migrations on disk that haven't been applied
    for migration_file in $(ls -1 "$MIGRATIONS_DIR"/*.sql 2>/dev/null | sort); do
        local filename=$(basename "$migration_file")
        if ! is_migration_applied "$filename"; then
            print_color $YELLOW "⚠ Migration not applied: $filename"
            ((issues++))
        fi
    done
    
    if [ $issues -eq 0 ]; then
        print_color $GREEN "✓ All migrations are valid"
        return 0
    else
        print_color $YELLOW "Found $issues validation issues"
        return 1
    fi
}

# Main execution
main() {
    print_color $BLUE "╔════════════════════════════════════════╗"
    print_color $BLUE "║   Neo Service Layer Migration Runner   ║"
    print_color $BLUE "╚════════════════════════════════════════╝"
    echo ""
    
    # Check PostgreSQL connection
    if ! check_postgres; then
        print_color $RED "Cannot connect to PostgreSQL"
        print_color $YELLOW "Please check your connection settings:"
        print_color $YELLOW "  Host: $DB_HOST"
        print_color $YELLOW "  Port: $DB_PORT"
        print_color $YELLOW "  User: $DB_USER"
        exit 1
    fi
    
    # Create database if needed
    create_database
    
    # Create migrations table
    create_migrations_table
    
    # Execute action
    case "$ACTION" in
        migrate|up)
            run_all_migrations
            ;;
        rollback|down)
            rollback_last_migration
            ;;
        status)
            show_status
            ;;
        validate)
            validate_migrations
            ;;
        *)
            print_color $RED "Unknown action: $ACTION"
            print_color $YELLOW "Usage: $0 [migrate|rollback|status|validate]"
            exit 1
            ;;
    esac
}

# Run main function
main