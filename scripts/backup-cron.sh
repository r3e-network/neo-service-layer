#!/bin/bash

# Cron wrapper for backup automation
# Add to crontab: 0 2 * * * /path/to/backup-cron.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_DIR="/var/log/neo-service-layer"
LOG_FILE="$LOG_DIR/backup-$(date +%Y%m%d).log"

# Create log directory
mkdir -p "$LOG_DIR"

# Change to project directory
cd "$SCRIPT_DIR/.."

# Run backup with logging
{
    echo "=== Backup started at $(date) ==="
    ./scripts/backup-automation.sh
    echo "=== Backup completed at $(date) ==="
} >> "$LOG_FILE" 2>&1

# Rotate logs (keep last 30 days)
find "$LOG_DIR" -name "backup-*.log" -mtime +30 -delete

# Check if backup succeeded
if tail -n 100 "$LOG_FILE" | grep -q "Backup process completed successfully"; then
    exit 0
else
    # Send alert if backup failed
    if [ -n "$ALERT_EMAIL" ]; then
        echo "Neo Service Layer backup failed. Check $LOG_FILE for details." | \
            mail -s "Backup Failed - Neo Service Layer" "$ALERT_EMAIL"
    fi
    exit 1
fi