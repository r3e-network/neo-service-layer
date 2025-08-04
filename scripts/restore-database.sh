#!/bin/bash

# Database restoration script
set -e

BACKUP_FILE="${1}"
BACKUP_DIR="/var/backups/neo-service-layer"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Load environment
if [ -f ".env.production" ]; then
    export $(cat .env.production | grep -v '^#' | xargs)
fi

echo -e "${YELLOW}Neo Service Layer - Database Restoration${NC}"

# Find backup file if not specified
if [ -z "$BACKUP_FILE" ] || [ "$BACKUP_FILE" == "latest" ]; then
    BACKUP_FILE=$(ls -t $BACKUP_DIR/*-database.sql.gz 2>/dev/null | head -1)
    if [ -z "$BACKUP_FILE" ]; then
        echo -e "${RED}No backup files found in $BACKUP_DIR${NC}"
        exit 1
    fi
fi

if [ ! -f "$BACKUP_FILE" ]; then
    echo -e "${RED}Backup file not found: $BACKUP_FILE${NC}"
    exit 1
fi

echo "Using backup file: $BACKUP_FILE"
echo ""

# Confirm restoration
read -p "This will replace the current database. Are you sure? (yes/no): " confirm
if [ "$confirm" != "yes" ]; then
    echo "Restoration cancelled"
    exit 0
fi

# Stop dependent services
echo -e "${YELLOW}Stopping dependent services...${NC}"
docker compose -f docker compose.production.yml stop api-gateway notification-service

# Create restore point
echo -e "${YELLOW}Creating restore point...${NC}"
RESTORE_POINT="$BACKUP_DIR/restore-point-$(date +%Y%m%d-%H%M%S).sql.gz"
PGPASSWORD="$DB_PASSWORD" pg_dump \
    -h "$DB_HOST" \
    -p "$DB_PORT" \
    -U "$DB_USER" \
    -d "$DB_NAME" \
    | gzip -9 > "$RESTORE_POINT"

echo "Restore point created: $RESTORE_POINT"

# Restore database
echo -e "${YELLOW}Restoring database...${NC}"
gunzip -c "$BACKUP_FILE" | PGPASSWORD="$DB_PASSWORD" psql \
    -h "$DB_HOST" \
    -p "$DB_PORT" \
    -U "$DB_USER" \
    -d "$DB_NAME" \
    -v ON_ERROR_STOP=1

# Run migrations if needed
echo -e "${YELLOW}Running database migrations...${NC}"
docker compose -f docker compose.production.yml run --rm api-gateway \
    dotnet ef database update --project src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence || true

# Restart services
echo -e "${YELLOW}Restarting services...${NC}"
docker compose -f docker compose.production.yml start api-gateway notification-service

# Verify restoration
echo -e "${YELLOW}Verifying restoration...${NC}"
TABLES=$(PGPASSWORD="$DB_PASSWORD" psql \
    -h "$DB_HOST" \
    -p "$DB_PORT" \
    -U "$DB_USER" \
    -d "$DB_NAME" \
    -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';")

echo -e "${GREEN}✓ Database restored successfully${NC}"
echo "Tables in database: $TABLES"
echo ""
echo "If you need to rollback this restoration:"
echo "  ./scripts/restore-database.sh $RESTORE_POINT"