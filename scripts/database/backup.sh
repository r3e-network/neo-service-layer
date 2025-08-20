#!/bin/bash
# Neo Service Layer Database Backup Script

set -e

# Configuration
BACKUP_DIR="${BACKUP_DIR:-/var/backups/neo-service-layer}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-neoservicelayer}"
DB_USER="${DB_USER:-neoservice_app}"
RETENTION_DAYS="${RETENTION_DAYS:-30}"
S3_BUCKET="${S3_BUCKET:-}"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="neo_service_layer_backup_${TIMESTAMP}.sql.gz"

echo "🔒 Starting database backup at $(date)"

# Create backup directory
mkdir -p "$BACKUP_DIR"

# Perform backup
echo "📦 Creating backup: $BACKUP_FILE"
PGPASSWORD="${DB_PASSWORD}" pg_dump \
    -h "$DB_HOST" \
    -p "$DB_PORT" \
    -U "$DB_USER" \
    -d "$DB_NAME" \
    --verbose \
    --no-owner \
    --no-privileges \
    --clean \
    --if-exists \
    --create \
    --encoding=UTF8 \
    | gzip -9 > "$BACKUP_DIR/$BACKUP_FILE"

# Check backup size
BACKUP_SIZE=$(ls -lh "$BACKUP_DIR/$BACKUP_FILE" | awk '{print $5}')
echo "✅ Backup created successfully. Size: $BACKUP_SIZE"

# Calculate checksum
CHECKSUM=$(sha256sum "$BACKUP_DIR/$BACKUP_FILE" | awk '{print $1}')
echo "$CHECKSUM  $BACKUP_FILE" > "$BACKUP_DIR/${BACKUP_FILE}.sha256"
echo "🔐 Checksum: $CHECKSUM"

# Upload to S3 if configured
if [ -n "$S3_BUCKET" ]; then
    echo "☁️  Uploading backup to S3: s3://$S3_BUCKET/backups/$BACKUP_FILE"
    aws s3 cp "$BACKUP_DIR/$BACKUP_FILE" "s3://$S3_BUCKET/backups/$BACKUP_FILE" \
        --storage-class STANDARD_IA \
        --server-side-encryption AES256
    
    aws s3 cp "$BACKUP_DIR/${BACKUP_FILE}.sha256" "s3://$S3_BUCKET/backups/${BACKUP_FILE}.sha256"
    echo "✅ Backup uploaded to S3 successfully"
fi

# Clean up old local backups
echo "🧹 Cleaning up old backups (older than $RETENTION_DAYS days)"
find "$BACKUP_DIR" -name "neo_service_layer_backup_*.sql.gz" -mtime +$RETENTION_DAYS -delete
find "$BACKUP_DIR" -name "neo_service_layer_backup_*.sql.gz.sha256" -mtime +$RETENTION_DAYS -delete

# Clean up old S3 backups if configured
if [ -n "$S3_BUCKET" ]; then
    echo "🧹 Cleaning up old S3 backups"
    aws s3 ls "s3://$S3_BUCKET/backups/" | while read -r line; do
        createDate=$(echo "$line" | awk '{print $1" "$2}')
        createDateSeconds=$(date -d "$createDate" +%s)
        olderThanSeconds=$(date -d "$RETENTION_DAYS days ago" +%s)
        
        if [ "$createDateSeconds" -lt "$olderThanSeconds" ]; then
            fileName=$(echo "$line" | awk '{print $4}')
            if [[ "$fileName" == neo_service_layer_backup_* ]]; then
                echo "Deleting old backup: $fileName"
                aws s3 rm "s3://$S3_BUCKET/backups/$fileName"
            fi
        fi
    done
fi

# Generate backup report
BACKUP_COUNT=$(find "$BACKUP_DIR" -name "neo_service_layer_backup_*.sql.gz" | wc -l)
TOTAL_SIZE=$(du -sh "$BACKUP_DIR" | cut -f1)

echo ""
echo "📊 Backup Summary:"
echo "   - Backup File: $BACKUP_FILE"
echo "   - Size: $BACKUP_SIZE"
echo "   - Checksum: $CHECKSUM"
echo "   - Local Backups: $BACKUP_COUNT"
echo "   - Total Size: $TOTAL_SIZE"
echo "   - Timestamp: $(date)"
echo ""
echo "✅ Database backup completed successfully!"

# Exit with success
exit 0