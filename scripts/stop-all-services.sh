#!/bin/bash

# Stop all Neo Service Layer services

set -e

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}Stopping all Neo Service Layer services...${NC}"

# List of all possible compose files
compose_files=(
    "docker-compose.phase1-minimal.yml"
    "docker-compose.phase2-minimal.yml"
    "docker-compose.phase3-minimal.yml"
    "docker-compose.phase4-minimal.yml"
    "docker-compose.phase1.yml"
    "docker-compose.phase2.yml"
    "docker-compose.phase3.yml"
    "docker-compose.phase4.yml"
    "docker-compose.production.yml"
    "docker-compose.yml"
)

# Stop services from each compose file
for file in "${compose_files[@]}"; do
    if [ -f "$file" ]; then
        echo -e "${BLUE}Stopping services from $file...${NC}"
        docker compose -f "$file" down --remove-orphans 2>/dev/null || true
    fi
done

# Stop any remaining neo- containers
echo -e "${BLUE}Stopping any remaining Neo containers...${NC}"
docker ps -a --format "{{.Names}}" | grep -E "^neo-" | while read container; do
    echo "Stopping $container..."
    docker stop "$container" 2>/dev/null || true
    docker rm "$container" 2>/dev/null || true
done

# Remove the network
echo -e "${BLUE}Removing Neo network...${NC}"
docker network rm neo-network 2>/dev/null || true
docker network rm neo-service-layer_neo-network 2>/dev/null || true

echo -e "${GREEN}✓ All services stopped successfully${NC}"