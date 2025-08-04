#!/bin/bash

# Deploy Neo Service Layer with public access enabled
# WARNING: This exposes all services to the internet!

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m'

# Get public IP
PUBLIC_IP=$(curl -s ifconfig.me || curl -s icanhazip.com || echo "YOUR_PUBLIC_IP")

echo -e "${RED}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${RED}║              PUBLIC DEPLOYMENT - SECURITY WARNING            ║${NC}"
echo -e "${RED}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${YELLOW}This will deploy all services with PUBLIC INTERNET ACCESS!${NC}"
echo -e "${YELLOW}Your public IP: ${RED}$PUBLIC_IP${NC}"
echo ""
echo -e "${RED}Services that will be exposed:${NC}"
echo -e "  - Databases (PostgreSQL, Redis)"
echo -e "  - Admin interfaces (Grafana, Prometheus, Consul)"
echo -e "  - All API endpoints"
echo -e "  - Web interface"
echo ""
echo -e "${YELLOW}Continue only if you have:${NC}"
echo -e "  ✓ Configured firewall rules"
echo -e "  ✓ Set strong passwords"
echo -e "  ✓ Understood the security implications"
echo ""

# First enable public access
echo -e "${BLUE}Configuring public access...${NC}"
./scripts/enable-public-access.sh

# Create secure environment file with public binding
cat > .env.public << EOF
# Public deployment configuration
ASPNETCORE_URLS=http://0.0.0.0:80
ALLOWED_ORIGINS=http://$PUBLIC_IP:8080,http://$PUBLIC_IP:8200,https://$PUBLIC_IP
CORS_ALLOW_ANY_ORIGIN=false
ENABLE_SWAGGER=false
REQUIRE_HTTPS=false
PUBLIC_IP=$PUBLIC_IP
EOF

# Merge with existing .env
if [ -f ".env" ]; then
    cat .env >> .env.public
    mv .env.public .env
fi

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║           PUBLIC DEPLOYMENT COMPLETE                         ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BLUE}Your services are accessible at:${NC}"
echo ""
echo -e "${PURPLE}Main Dashboard:${NC} ${YELLOW}http://$PUBLIC_IP:8200${NC}"
echo -e "${PURPLE}API Gateway:${NC} ${YELLOW}http://$PUBLIC_IP:8080${NC}"
echo -e "${PURPLE}Grafana:${NC} ${YELLOW}http://$PUBLIC_IP:13000${NC} (admin/admin)"
echo -e "${PURPLE}Prometheus:${NC} ${YELLOW}http://$PUBLIC_IP:19090${NC}"
echo -e "${PURPLE}Consul:${NC} ${YELLOW}http://$PUBLIC_IP:18500${NC}"
echo ""
echo -e "${RED}⚠️  CRITICAL: Your services are exposed to the internet!${NC}"
echo -e "${YELLOW}Secure them immediately using the recommendations in public-access-info.txt${NC}"
echo ""
echo -e "${BLUE}To disable public access later:${NC}"
echo -e "  ${GREEN}./scripts/disable-public-access.sh${NC}"