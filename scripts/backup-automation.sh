#!/bin/bash

# Automated backup script for Neo Service Layer
# Supports local and cloud backups (AWS S3, Azure Blob)

set -e

# Configuration
BACKUP_DIR="/var/backups/neo-service-layer"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_PREFIX="neo-backup-$TIMESTAMP"
RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"

# Load environment
if [ -f ".env.production" ]; then
    export $(cat .env.production | grep -v '^#' | xargs)
fi

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Create backup directory
mkdir -p "$BACKUP_DIR"

# Helper functions
log_info() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')] INFO: $1${NC}"
}

log_warn() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] WARN: $1${NC}"
}

log_error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] ERROR: $1${NC}"
}

# Backup database
backup_database() {
    log_info "Starting database backup..."
    
    local db_backup_file="$BACKUP_DIR/${BACKUP_PREFIX}-database.sql.gz"
    
    # Use pg_dump with compression
    PGPASSWORD="$DB_PASSWORD" pg_dump \
        -h "$DB_HOST" \
        -p "$DB_PORT" \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        --verbose \
        --no-owner \
        --no-privileges \
        --clean \
        --if-exists \
        | gzip -9 > "$db_backup_file"
    
    log_info "Database backup completed: $db_backup_file"
    echo "$db_backup_file"
}

# Backup Redis
backup_redis() {
    log_info "Starting Redis backup..."
    
    local redis_backup_file="$BACKUP_DIR/${BACKUP_PREFIX}-redis.rdb"
    
    # Trigger Redis BGSAVE
    redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" -a "$REDIS_PASSWORD" BGSAVE
    
    # Wait for backup to complete
    while [ $(redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" -a "$REDIS_PASSWORD" LASTSAVE) -eq $(redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" -a "$REDIS_PASSWORD" LASTSAVE) ]; do
        sleep 1
    done
    
    # Copy RDB file
    docker cp neo-redis:/data/dump.rdb "$redis_backup_file" 2>/dev/null || \
        cp /var/lib/redis/dump.rdb "$redis_backup_file"
    
    log_info "Redis backup completed: $redis_backup_file"
    echo "$redis_backup_file"
}

# Backup configuration files
backup_configs() {
    log_info "Starting configuration backup..."
    
    local config_backup_file="$BACKUP_DIR/${BACKUP_PREFIX}-configs.tar.gz"
    
    tar -czf "$config_backup_file" \
        --exclude='.env.production' \
        --exclude='certificates/*.pfx' \
        .env.production.template \
        docker compose.production.yml \
        config/ \
        monitoring/ \
        scripts/ \
        2>/dev/null || true
    
    log_info "Configuration backup completed: $config_backup_file"
    echo "$config_backup_file"
}

# Backup smart contracts
backup_contracts() {
    log_info "Starting smart contracts backup..."
    
    local contracts_backup_file="$BACKUP_DIR/${BACKUP_PREFIX}-contracts.tar.gz"
    
    tar -czf "$contracts_backup_file" \
        contracts-neo-n3/src/ \
        contracts-neo-n3/*.csproj \
        2>/dev/null || true
    
    log_info "Smart contracts backup completed: $contracts_backup_file"
    echo "$contracts_backup_file"
}

# Backup encryption keys (encrypted)
backup_keys() {
    log_info "Starting secure keys backup..."
    
    local keys_backup_file="$BACKUP_DIR/${BACKUP_PREFIX}-keys.enc"
    
    # Create temporary directory for sensitive files
    local temp_dir=$(mktemp -d)
    
    # Copy sensitive files
    cp .env.production "$temp_dir/" 2>/dev/null || true
    cp certificates/*.pfx "$temp_dir/" 2>/dev/null || true
    
    # Encrypt with GPG
    tar -czf - -C "$temp_dir" . | \
        gpg --symmetric --cipher-algo AES256 --output "$keys_backup_file"
    
    # Clean up
    rm -rf "$temp_dir"
    
    log_info "Secure keys backup completed: $keys_backup_file"
    echo "$keys_backup_file"
}

# Upload to S3
upload_to_s3() {
    local file=$1
    
    if [ -z "$AWS_ACCESS_KEY_ID" ] || [ -z "$AWS_SECRET_ACCESS_KEY" ]; then
        log_warn "AWS credentials not configured, skipping S3 upload"
        return
    fi
    
    log_info "Uploading to S3: $(basename $file)"
    
    aws s3 cp "$file" "s3://$BACKUP_S3_BUCKET/neo-service-layer/$(basename $file)" \
        --storage-class GLACIER_IR \
        --metadata "timestamp=$TIMESTAMP,retention=$RETENTION_DAYS"
}

# Upload to Azure
upload_to_azure() {
    local file=$1
    
    if [ -z "$AZURE_STORAGE_CONNECTION_STRING" ]; then
        log_warn "Azure credentials not configured, skipping Azure upload"
        return
    fi
    
    log_info "Uploading to Azure: $(basename $file)"
    
    az storage blob upload \
        --container-name "neo-backups" \
        --name "neo-service-layer/$(basename $file)" \
        --file "$file" \
        --tier "Cool"
}

# Clean old backups
cleanup_old_backups() {
    log_info "Cleaning up old backups..."
    
    # Local cleanup
    find "$BACKUP_DIR" -name "neo-backup-*" -mtime +$RETENTION_DAYS -delete
    
    # S3 cleanup
    if [ -n "$AWS_ACCESS_KEY_ID" ]; then
        aws s3 ls "s3://$BACKUP_S3_BUCKET/neo-service-layer/" | \
            awk '{print $4}' | \
            while read file; do
                file_date=$(echo $file | grep -oP '\d{8}-\d{6}' | head -1)
                if [ -n "$file_date" ]; then
                    file_timestamp=$(date -d "${file_date:0:8} ${file_date:9:2}:${file_date:11:2}:${file_date:13:2}" +%s)
                    current_timestamp=$(date +%s)
                    age_days=$(( ($current_timestamp - $file_timestamp) / 86400 ))
                    
                    if [ $age_days -gt $RETENTION_DAYS ]; then
                        log_info "Deleting old S3 backup: $file"
                        aws s3 rm "s3://$BACKUP_S3_BUCKET/neo-service-layer/$file"
                    fi
                fi
            done
    fi
}

# Create backup manifest
create_manifest() {
    local manifest_file="$BACKUP_DIR/${BACKUP_PREFIX}-manifest.json"
    
    cat > "$manifest_file" << EOF
{
  "timestamp": "$TIMESTAMP",
  "version": "$(git describe --tags --always 2>/dev/null || echo 'unknown')",
  "files": [
    $(printf '"%s",' "$@" | sed 's/,$//')
  ],
  "checksums": {
$(for file in "$@"; do
    if [ -f "$file" ]; then
        echo "    \"$(basename $file)\": \"$(sha256sum $file | awk '{print $1}')\","
    fi
done | sed '$ s/,$//')
  },
  "environment": {
    "hostname": "$(hostname)",
    "backup_host": "$(hostname -f)",
    "backup_user": "$(whoami)"
  }
}
EOF
    
    echo "$manifest_file"
}

# Send notification
send_notification() {
    local status=$1
    local message=$2
    
    # Slack notification
    if [ -n "$SLACK_WEBHOOK_URL" ]; then
        curl -X POST "$SLACK_WEBHOOK_URL" \
            -H 'Content-Type: application/json' \
            -d "{
                \"text\": \"Backup $status\",
                \"attachments\": [{
                    \"color\": \"$([ "$status" == "SUCCESS" ] && echo "good" || echo "danger")\",
                    \"fields\": [{
                        \"title\": \"Environment\",
                        \"value\": \"Production\",
                        \"short\": true
                    }, {
                        \"title\": \"Timestamp\",
                        \"value\": \"$TIMESTAMP\",
                        \"short\": true
                    }, {
                        \"title\": \"Message\",
                        \"value\": \"$message\",
                        \"short\": false
                    }]
                }]
            }" 2>/dev/null || true
    fi
}

# Main backup process
main() {
    log_info "Starting Neo Service Layer backup process..."
    
    local backup_files=()
    local failed=0
    
    # Perform backups
    if db_file=$(backup_database); then
        backup_files+=("$db_file")
    else
        log_error "Database backup failed"
        failed=1
    fi
    
    if redis_file=$(backup_redis); then
        backup_files+=("$redis_file")
    else
        log_error "Redis backup failed"
        failed=1
    fi
    
    if config_file=$(backup_configs); then
        backup_files+=("$config_file")
    else
        log_error "Configuration backup failed"
        failed=1
    fi
    
    if contracts_file=$(backup_contracts); then
        backup_files+=("$contracts_file")
    else
        log_error "Contracts backup failed"
        failed=1
    fi
    
    if keys_file=$(backup_keys); then
        backup_files+=("$keys_file")
    else
        log_error "Keys backup failed"
        failed=1
    fi
    
    # Create manifest
    manifest_file=$(create_manifest "${backup_files[@]}")
    backup_files+=("$manifest_file")
    
    # Upload to cloud storage
    for file in "${backup_files[@]}"; do
        upload_to_s3 "$file"
        upload_to_azure "$file"
    done
    
    # Cleanup
    cleanup_old_backups
    
    # Send notification
    if [ $failed -eq 0 ]; then
        send_notification "SUCCESS" "All backups completed successfully"
        log_info "Backup process completed successfully"
    else
        send_notification "PARTIAL" "Some backups failed, check logs"
        log_warn "Backup process completed with errors"
    fi
    
    return $failed
}

# Run main function
main