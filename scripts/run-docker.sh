#!/bin/bash

# Exit on error
set -e

# Set colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Building and Running Neo Service Layer with Docker${NC}"
echo "====================================================="

# Build and run the services
echo -e "${YELLOW}Building and running the services...${NC}"
docker-compose up -d --build

# Wait for the services to start
echo -e "${YELLOW}Waiting for the services to start...${NC}"
sleep 10

echo -e "${GREEN}Neo Service Layer is running!${NC}"
echo "API: http://localhost:5000"
echo "Swagger UI: http://localhost:5000/swagger"
echo "RabbitMQ Management: http://localhost:15672 (guest/guest)"
echo "Grafana: http://localhost:3000 (admin/admin)"
echo "Kibana: http://localhost:5601"
echo "Jaeger: http://localhost:16686"
echo ""
echo "To stop the services, run: docker-compose down"
