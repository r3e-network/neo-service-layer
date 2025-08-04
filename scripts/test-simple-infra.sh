#!/bin/bash

# Test simple infrastructure deployment

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

echo "Testing infrastructure deployment..."

# First, let's check what's running
echo "Current Docker containers:"
docker ps -a

# Stop any existing containers
echo "Stopping existing containers..."
docker compose -f docker-compose.production.yml down 2>/dev/null || true

# Create a minimal test compose file
cat > docker-compose.test.yml << 'EOF'
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    container_name: neo-postgres-test
    environment:
      - POSTGRES_DB=neo_service_layer
      - POSTGRES_USER=neo_service_user
      - POSTGRES_PASSWORD=SecurePass123!
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U neo_service_user"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: neo-redis-test
    command: redis-server --requirepass RedisPass123!
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "--raw", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
EOF

echo "Starting test infrastructure..."
docker compose -f docker-compose.test.yml up -d

# Wait for services
echo "Waiting for services to be ready..."
sleep 10

# Check status
echo -e "\n${GREEN}Service Status:${NC}"
docker compose -f docker-compose.test.yml ps

# Test connections
echo -e "\n${GREEN}Testing connections:${NC}"

# Test PostgreSQL
if docker exec neo-postgres-test pg_isready -U neo_service_user >/dev/null 2>&1; then
    echo -e "${GREEN}✓ PostgreSQL is ready${NC}"
else
    echo -e "${RED}✗ PostgreSQL is not ready${NC}"
fi

# Test Redis
if docker exec neo-redis-test redis-cli -a RedisPass123! ping >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Redis is ready${NC}"
else
    echo -e "${RED}✗ Redis is not ready${NC}"
fi

echo -e "\nTest complete! Use 'docker compose -f docker-compose.test.yml down' to stop."