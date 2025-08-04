#!/bin/bash

# Enable public access for Neo Service Layer services
# WARNING: This exposes all services to the internet - use with caution!

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${RED}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${RED}║                    SECURITY WARNING                          ║${NC}"
echo -e "${RED}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${YELLOW}This script will expose ALL services to the public internet!${NC}"
echo -e "${YELLOW}This includes databases, admin interfaces, and sensitive services.${NC}"
echo ""
echo -e "${RED}Only use this in controlled environments with proper security measures:${NC}"
echo -e "  - Firewall rules to restrict access"
echo -e "  - Strong passwords for all services"
echo -e "  - VPN or IP whitelisting"
echo -e "  - SSL/TLS certificates for encryption"
echo ""
read -p "Are you SURE you want to continue? (type 'yes-expose-services' to confirm): " confirm

if [ "$confirm" != "yes-expose-services" ]; then
    echo -e "${GREEN}Cancelled. Services remain on localhost only.${NC}"
    exit 0
fi

# Get public IP
PUBLIC_IP=$(curl -s ifconfig.me || curl -s icanhazip.com || echo "YOUR_PUBLIC_IP")
echo ""
echo -e "${BLUE}Detected public IP: ${YELLOW}$PUBLIC_IP${NC}"
echo ""

# Backup original files
echo -e "${BLUE}Creating backups...${NC}"
for file in docker-compose.phase*.yml; do
    if [ -f "$file" ]; then
        cp "$file" "${file}.localhost-backup"
        echo -e "${GREEN}✓ Backed up $file${NC}"
    fi
done

# Update all phase files to use 0.0.0.0 binding
echo -e "${BLUE}Updating docker-compose files...${NC}"

# Phase 1 - Infrastructure
if [ -f "docker-compose.phase1-minimal.yml" ]; then
    sed -i 's/"15432:5432"/"0.0.0.0:15432:5432"/g' docker-compose.phase1-minimal.yml
    sed -i 's/"16379:6379"/"0.0.0.0:16379:6379"/g' docker-compose.phase1-minimal.yml
    sed -i 's/"18500:8500"/"0.0.0.0:18500:8500"/g' docker-compose.phase1-minimal.yml
    sed -i 's/"19090:9090"/"0.0.0.0:19090:9090"/g' docker-compose.phase1-minimal.yml
    sed -i 's/"13000:3000"/"0.0.0.0:13000:3000"/g' docker-compose.phase1-minimal.yml
    sed -i 's/"8080:80"/"0.0.0.0:8080:80"/g' docker-compose.phase1-minimal.yml
    sed -i 's/"8081:80"/"0.0.0.0:8081:80"/g' docker-compose.phase1-minimal.yml
    echo -e "${GREEN}✓ Updated Phase 1 services${NC}"
fi

# Phase 2 - Management & AI
if [ -f "docker-compose.phase2-minimal.yml" ]; then
    for port in 8090 8091 8092 8093 8100 8101; do
        sed -i "s/\"$port:80\"/\"0.0.0.0:$port:80\"/g" docker-compose.phase2-minimal.yml
    done
    echo -e "${GREEN}✓ Updated Phase 2 services${NC}"
fi

# Phase 3 - Advanced Services
if [ -f "docker-compose.phase3-minimal.yml" ]; then
    for port in 8110 8111 8112 8113 8114 8120 8130; do
        sed -i "s/\"$port:80\"/\"0.0.0.0:$port:80\"/g" docker-compose.phase3-minimal.yml
    done
    echo -e "${GREEN}✓ Updated Phase 3 services${NC}"
fi

# Phase 4 - Security & Web
if [ -f "docker-compose.phase4-minimal.yml" ]; then
    for port in 8140 8141 8142 8143 8144 8145 8200; do
        sed -i "s/\"$port:80\"/\"0.0.0.0:$port:80\"/g" docker-compose.phase4-minimal.yml
    done
    echo -e "${GREEN}✓ Updated Phase 4 services${NC}"
fi

# Create public access information file
cat > public-access-info.txt << EOF
Neo Service Layer - Public Access Configuration
==============================================
Generated: $(date)
Public IP: $PUBLIC_IP

WARNING: All services are now exposed to the public internet!

Service Access URLs:
-------------------

Infrastructure Services:
- PostgreSQL: $PUBLIC_IP:15432
- Redis: $PUBLIC_IP:16379
- Consul UI: http://$PUBLIC_IP:18500
- Prometheus: http://$PUBLIC_IP:19090
- Grafana: http://$PUBLIC_IP:13000

Core Services:
- API Gateway: http://$PUBLIC_IP:8080
- Smart Contracts: http://$PUBLIC_IP:8081

Management Services:
- Key Management: http://$PUBLIC_IP:8090
- Notification: http://$PUBLIC_IP:8091
- Monitoring: http://$PUBLIC_IP:8092
- Health: http://$PUBLIC_IP:8093

AI Services:
- Pattern Recognition: http://$PUBLIC_IP:8100
- Prediction: http://$PUBLIC_IP:8101

Advanced Services:
- Oracle: http://$PUBLIC_IP:8110
- Storage: http://$PUBLIC_IP:8111
- CrossChain: http://$PUBLIC_IP:8112
- Proof of Reserve: http://$PUBLIC_IP:8113
- Randomness: http://$PUBLIC_IP:8114
- Fair Ordering: http://$PUBLIC_IP:8120
- TEE Host: http://$PUBLIC_IP:8130

Security Services:
- Voting: http://$PUBLIC_IP:8140
- Zero Knowledge: http://$PUBLIC_IP:8141
- Secrets Management: http://$PUBLIC_IP:8142
- Social Recovery: http://$PUBLIC_IP:8143
- Enclave Storage: http://$PUBLIC_IP:8144
- Network Security: http://$PUBLIC_IP:8145

User Interface:
- Main Dashboard: http://$PUBLIC_IP:8200

Security Recommendations:
------------------------
1. Configure firewall rules to restrict access
2. Use strong passwords for all services
3. Enable SSL/TLS certificates
4. Implement IP whitelisting
5. Monitor access logs regularly
6. Use VPN for administrative access

To revert to localhost-only:
./scripts/disable-public-access.sh
EOF

echo ""
echo -e "${GREEN}✓ Public access configuration saved to public-access-info.txt${NC}"

# Restart services to apply changes
echo ""
echo -e "${YELLOW}Services need to be restarted for changes to take effect.${NC}"
read -p "Restart all services now? (y/n): " restart

if [ "$restart" == "y" ] || [ "$restart" == "Y" ]; then
    echo -e "${BLUE}Restarting services...${NC}"
    ./scripts/stop-all-services.sh
    ./scripts/deploy-automatic.sh
fi

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║               PUBLIC ACCESS ENABLED                          ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${YELLOW}Services are now accessible at:${NC}"
echo -e "${BLUE}Main Dashboard:${NC} http://$PUBLIC_IP:8200"
echo -e "${BLUE}API Gateway:${NC} http://$PUBLIC_IP:8080"
echo -e "${BLUE}Grafana:${NC} http://$PUBLIC_IP:13000"
echo ""
echo -e "${RED}⚠️  IMPORTANT: Secure your services immediately!${NC}"
echo -e "See ${YELLOW}public-access-info.txt${NC} for all URLs and security recommendations."