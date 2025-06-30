#!/bin/bash

# Health Check Script for Neo Service Layer Website
# Monitors application health and sends alerts if needed

set -e

# Configuration
APP_URL="${APP_URL:-https://service.neoservicelayer.com}"
HEALTH_ENDPOINT="${HEALTH_ENDPOINT:-/api/health}"
TIMEOUT="${TIMEOUT:-10}"
MAX_RETRIES="${MAX_RETRIES:-3}"
ALERT_WEBHOOK="${ALERT_WEBHOOK:-}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Function to send alert
send_alert() {
    local message=$1
    local status=$2
    
    if [ -n "$ALERT_WEBHOOK" ]; then
        curl -X POST "$ALERT_WEBHOOK" \
            -H "Content-Type: application/json" \
            -d "{\"text\":\"🚨 Neo Service Layer Alert\",\"status\":\"$status\",\"message\":\"$message\",\"timestamp\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"}" \
            --silent --output /dev/null
    fi
}

# Function to check endpoint
check_endpoint() {
    local url=$1
    local expected_status=${2:-200}
    
    response=$(curl -s -o /dev/null -w "%{http_code}" --connect-timeout $TIMEOUT "$url" || echo "000")
    
    if [ "$response" = "$expected_status" ]; then
        return 0
    else
        return 1
    fi
}

# Main health check
echo "🏥 Neo Service Layer Health Check"
echo "================================="
echo "URL: $APP_URL"
echo "Timestamp: $(date)"
echo ""

# Check main application
echo -n "Checking application health... "
retry_count=0
while [ $retry_count -lt $MAX_RETRIES ]; do
    if check_endpoint "$APP_URL$HEALTH_ENDPOINT"; then
        echo -e "${GREEN}✓ Healthy${NC}"
        break
    else
        retry_count=$((retry_count + 1))
        if [ $retry_count -lt $MAX_RETRIES ]; then
            echo -n "."
            sleep 5
        else
            echo -e "${RED}✗ Unhealthy${NC}"
            send_alert "Application health check failed after $MAX_RETRIES attempts" "error"
            exit 1
        fi
    fi
done

# Check specific endpoints
endpoints=(
    "/:200"
    "/api/auth/providers:200"
    "/playground:200"
    "/docs:200"
)

echo ""
echo "Checking endpoints:"
for endpoint_config in "${endpoints[@]}"; do
    IFS=':' read -r endpoint expected_status <<< "$endpoint_config"
    echo -n "  $endpoint ... "
    
    if check_endpoint "$APP_URL$endpoint" "$expected_status"; then
        echo -e "${GREEN}✓${NC}"
    else
        echo -e "${YELLOW}⚠${NC} (non-critical)"
    fi
done

# Check database connection (if DATABASE_URL is set)
if [ -n "$DATABASE_URL" ]; then
    echo ""
    echo -n "Checking database connection... "
    if pg_isready -d "$DATABASE_URL" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Connected${NC}"
    else
        echo -e "${RED}✗ Failed${NC}"
        send_alert "Database connection check failed" "warning"
    fi
fi

# Check SSL certificate expiry
echo ""
echo -n "Checking SSL certificate... "
cert_expiry=$(echo | openssl s_client -servername "${APP_URL#https://}" -connect "${APP_URL#https://}:443" 2>/dev/null | openssl x509 -noout -dates 2>/dev/null | grep notAfter | cut -d= -f2)

if [ -n "$cert_expiry" ]; then
    expiry_epoch=$(date -d "$cert_expiry" +%s 2>/dev/null || date -j -f "%b %d %H:%M:%S %Y %Z" "$cert_expiry" +%s 2>/dev/null)
    current_epoch=$(date +%s)
    days_until_expiry=$(( (expiry_epoch - current_epoch) / 86400 ))
    
    if [ $days_until_expiry -lt 7 ]; then
        echo -e "${RED}✗ Expires in $days_until_expiry days${NC}"
        send_alert "SSL certificate expires in $days_until_expiry days" "warning"
    elif [ $days_until_expiry -lt 30 ]; then
        echo -e "${YELLOW}⚠ Expires in $days_until_expiry days${NC}"
    else
        echo -e "${GREEN}✓ Valid for $days_until_expiry days${NC}"
    fi
else
    echo -e "${YELLOW}⚠ Could not check${NC}"
fi

# Performance check
echo ""
echo -n "Checking response time... "
response_time=$(curl -o /dev/null -s -w '%{time_total}' "$APP_URL" || echo "0")
response_time_ms=$(echo "$response_time * 1000" | bc | cut -d. -f1)

if [ "$response_time_ms" -lt 500 ]; then
    echo -e "${GREEN}✓ ${response_time_ms}ms${NC}"
elif [ "$response_time_ms" -lt 1000 ]; then
    echo -e "${YELLOW}⚠ ${response_time_ms}ms${NC}"
else
    echo -e "${RED}✗ ${response_time_ms}ms${NC}"
    send_alert "High response time: ${response_time_ms}ms" "warning"
fi

echo ""
echo "✅ Health check completed"