#!/bin/bash

# Simple deployment test using the production compose file

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() {
    echo "[$(date +'%H:%M:%S')] $1"
}

log_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

log_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Check if .env.production exists
if [ ! -f ".env.production" ]; then
    log_error ".env.production file not found"
    exit 1
fi

# Load environment variables
set -a
source .env.production
set +a

log_info "Environment variables loaded"

# Test with just the infrastructure services first
log_info "Starting infrastructure services..."

# Start only postgres and redis first
docker compose -f docker-compose.production.yml up -d postgres redis

# Wait for them to be ready
log_info "Waiting for PostgreSQL..."
until docker exec $(docker ps -q -f name=postgres) pg_isready -U ${DB_USER:-neo_service_user} >/dev/null 2>&1; do
    sleep 1
done
log_success "PostgreSQL is ready"

log_info "Waiting for Redis..."
until docker exec $(docker ps -q -f name=redis) redis-cli -a ${REDIS_PASSWORD} ping >/dev/null 2>&1; do
    sleep 1
done
log_success "Redis is ready"

# Now start consul
docker compose -f docker-compose.production.yml up -d consul

log_info "Waiting for Consul..."
until curl -s http://localhost:8500/v1/status/leader >/dev/null 2>&1; do
    sleep 1
done
log_success "Consul is ready"

# Finally start API service
log_info "Starting API service..."
docker compose -f docker-compose.production.yml up -d api

log_info "Deployment test completed"
docker compose -f docker-compose.production.yml ps