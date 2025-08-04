#!/bin/bash

# Display public access URLs for Neo Service Layer

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m'

# Server has fixed public IP
PUBLIC_IPV4="198.244.215.132"
PUBLIC_IPV6=$(curl -s -6 ifconfig.me 2>/dev/null || echo "")

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║         Neo Service Layer - Public Access URLs               ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${GREEN}IPv4 Address:${NC} ${YELLOW}$PUBLIC_IPV4${NC}"
if [ ! -z "$PUBLIC_IPV6" ]; then
    echo -e "${GREEN}IPv6 Address:${NC} ${YELLOW}$PUBLIC_IPV6${NC}"
fi
echo ""

echo -e "${PURPLE}══════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}                    Main Access Points                        ${NC}"
echo -e "${PURPLE}══════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${BLUE}🌐 Main Dashboard:${NC} ${YELLOW}http://$PUBLIC_IPV4:8200${NC}"
echo -e "${BLUE}🔌 API Gateway:${NC} ${YELLOW}http://$PUBLIC_IPV4:8080${NC}"
echo -e "${BLUE}📊 Grafana Monitoring:${NC} ${YELLOW}http://$PUBLIC_IPV4:13000${NC} (admin/admin)"
echo -e "${BLUE}📈 Prometheus Metrics:${NC} ${YELLOW}http://$PUBLIC_IPV4:19090${NC}"
echo -e "${BLUE}🔍 Consul Service Discovery:${NC} ${YELLOW}http://$PUBLIC_IPV4:18500${NC}"
echo ""

echo -e "${PURPLE}══════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}                    All Service Endpoints                     ${NC}"
echo -e "${PURPLE}══════════════════════════════════════════════════════════════${NC}"
echo ""

echo -e "${GREEN}Infrastructure Services:${NC}"
echo -e "  PostgreSQL Database: ${YELLOW}$PUBLIC_IPV4:15432${NC}"
echo -e "  Redis Cache: ${YELLOW}$PUBLIC_IPV4:16379${NC}"
echo -e "  Consul: ${YELLOW}http://$PUBLIC_IPV4:18500${NC}"
echo -e "  Prometheus: ${YELLOW}http://$PUBLIC_IPV4:19090${NC}"
echo -e "  Grafana: ${YELLOW}http://$PUBLIC_IPV4:13000${NC}"
echo ""

echo -e "${GREEN}Core Services:${NC}"
echo -e "  API Gateway: ${YELLOW}http://$PUBLIC_IPV4:8080${NC}"
echo -e "  Smart Contracts: ${YELLOW}http://$PUBLIC_IPV4:8081${NC}"
echo ""

echo -e "${GREEN}Management Services:${NC}"
echo -e "  Key Management: ${YELLOW}http://$PUBLIC_IPV4:8090${NC}"
echo -e "  Notification: ${YELLOW}http://$PUBLIC_IPV4:8091${NC}"
echo -e "  Monitoring: ${YELLOW}http://$PUBLIC_IPV4:8092${NC}"
echo -e "  Health: ${YELLOW}http://$PUBLIC_IPV4:8093${NC}"
echo ""

echo -e "${GREEN}AI Services:${NC}"
echo -e "  Pattern Recognition: ${YELLOW}http://$PUBLIC_IPV4:8100${NC}"
echo -e "  Prediction: ${YELLOW}http://$PUBLIC_IPV4:8101${NC}"
echo ""

echo -e "${GREEN}Advanced Services:${NC}"
echo -e "  Oracle: ${YELLOW}http://$PUBLIC_IPV4:8110${NC}"
echo -e "  Storage: ${YELLOW}http://$PUBLIC_IPV4:8111${NC}"
echo -e "  CrossChain: ${YELLOW}http://$PUBLIC_IPV4:8112${NC}"
echo -e "  Proof of Reserve: ${YELLOW}http://$PUBLIC_IPV4:8113${NC}"
echo -e "  Randomness: ${YELLOW}http://$PUBLIC_IPV4:8114${NC}"
echo -e "  Fair Ordering: ${YELLOW}http://$PUBLIC_IPV4:8120${NC}"
echo -e "  TEE Host: ${YELLOW}http://$PUBLIC_IPV4:8130${NC}"
echo ""

echo -e "${GREEN}Security Services:${NC}"
echo -e "  Voting: ${YELLOW}http://$PUBLIC_IPV4:8140${NC}"
echo -e "  Zero Knowledge: ${YELLOW}http://$PUBLIC_IPV4:8141${NC}"
echo -e "  Secrets Management: ${YELLOW}http://$PUBLIC_IPV4:8142${NC}"
echo -e "  Social Recovery: ${YELLOW}http://$PUBLIC_IPV4:8143${NC}"
echo -e "  Enclave Storage: ${YELLOW}http://$PUBLIC_IPV4:8144${NC}"
echo -e "  Network Security: ${YELLOW}http://$PUBLIC_IPV4:8145${NC}"
echo ""

echo -e "${GREEN}User Interface:${NC}"
echo -e "  Web Dashboard: ${YELLOW}http://$PUBLIC_IPV4:8200${NC}"
echo ""

echo -e "${PURPLE}══════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}                    Security Notice                           ${NC}"
echo -e "${PURPLE}══════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${YELLOW}⚠️  WARNING: All services are publicly accessible!${NC}"
echo -e "${YELLOW}Please secure your deployment:${NC}"
echo -e "  1. Configure firewall rules: ${GREEN}sudo ./scripts/configure-firewall.sh${NC}"
echo -e "  2. Change default passwords"
echo -e "  3. Enable SSL/TLS certificates"
echo -e "  4. Implement authentication"
echo ""
echo -e "${BLUE}To disable public access:${NC} ${GREEN}./scripts/disable-public-access.sh${NC}"