#!/bin/bash

# Complete Neo Service Layer Deployment Verification

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m'

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║            Neo Service Layer - Complete Deployment           ║${NC}"
echo -e "${BLUE}║                    System Verification                       ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Verification functions
check_service() {
    local name=$1
    local url=$2
    local port=$3
    
    if curl -s -f "$url" >/dev/null 2>&1; then
        echo -e "${GREEN}✓${NC} $name ($port)"
        return 0
    else
        echo -e "${RED}✗${NC} $name ($port) - FAILED"
        return 1
    fi
}

# Phase 1 - Infrastructure & Core Services
echo -e "${PURPLE}█ Phase 1 - Infrastructure & Core Services${NC}"
echo -e "${BLUE}├─ Infrastructure Services${NC}"
phase1_infra_failed=0
check_service "PostgreSQL" "http://localhost:15432" "15432" || ((phase1_infra_failed++))
check_service "Redis" "http://localhost:16379" "16379" || ((phase1_infra_failed++))
check_service "Consul" "http://localhost:18500/v1/status/leader" "18500" || ((phase1_infra_failed++))
check_service "Prometheus" "http://localhost:19090/-/ready" "19090" || ((phase1_infra_failed++))
check_service "Grafana" "http://localhost:13000/api/health" "13000" || ((phase1_infra_failed++))

echo -e "${BLUE}├─ Core Application Services${NC}"
phase1_app_failed=0
check_service "API Gateway" "http://localhost:8080/health" "8080" || ((phase1_app_failed++))
check_service "Smart Contracts" "http://localhost:8081/health" "8081" || ((phase1_app_failed++))

# Phase 2 - Management & AI Services
echo -e "${PURPLE}█ Phase 2 - Management & AI Services${NC}"
echo -e "${BLUE}├─ Management Services${NC}"
phase2_mgmt_failed=0
check_service "Key Management" "http://localhost:8090/health" "8090" || ((phase2_mgmt_failed++))
check_service "Notification" "http://localhost:8091/health" "8091" || ((phase2_mgmt_failed++))
check_service "Monitoring" "http://localhost:8092/health" "8092" || ((phase2_mgmt_failed++))
check_service "Health" "http://localhost:8093/health" "8093" || ((phase2_mgmt_failed++))

echo -e "${BLUE}├─ AI Services${NC}"
phase2_ai_failed=0
check_service "Pattern Recognition" "http://localhost:8100/health" "8100" || ((phase2_ai_failed++))
check_service "Prediction" "http://localhost:8101/health" "8101" || ((phase2_ai_failed++))

# Phase 3 - Advanced Services
echo -e "${PURPLE}█ Phase 3 - Advanced Services${NC}"
echo -e "${BLUE}├─ Core Services${NC}"
phase3_core_failed=0
check_service "Oracle" "http://localhost:8110/health" "8110" || ((phase3_core_failed++))
check_service "Storage" "http://localhost:8111/health" "8111" || ((phase3_core_failed++))
check_service "CrossChain" "http://localhost:8112/health" "8112" || ((phase3_core_failed++))
check_service "Proof of Reserve" "http://localhost:8113/health" "8113" || ((phase3_core_failed++))
check_service "Randomness" "http://localhost:8114/health" "8114" || ((phase3_core_failed++))

echo -e "${BLUE}├─ Advanced Services${NC}"
phase3_adv_failed=0
check_service "Fair Ordering" "http://localhost:8120/health" "8120" || ((phase3_adv_failed++))
check_service "TEE Host" "http://localhost:8130/health" "8130" || ((phase3_adv_failed++))

# Phase 4 - Security & Governance
echo -e "${PURPLE}█ Phase 4 - Security & Governance${NC}"
echo -e "${BLUE}├─ Security Services${NC}"
phase4_sec_failed=0
check_service "Voting" "http://localhost:8140/health" "8140" || ((phase4_sec_failed++))
check_service "Zero Knowledge" "http://localhost:8141/health" "8141" || ((phase4_sec_failed++))
check_service "Secrets Management" "http://localhost:8142/health" "8142" || ((phase4_sec_failed++))
check_service "Social Recovery" "http://localhost:8143/health" "8143" || ((phase4_sec_failed++))
check_service "Enclave Storage" "http://localhost:8144/health" "8144" || ((phase4_sec_failed++))
check_service "Network Security" "http://localhost:8145/health" "8145" || ((phase4_sec_failed++))

echo -e "${BLUE}├─ User Interface${NC}"
phase4_ui_failed=0
check_service "Web Interface" "http://localhost:8200/health" "8200" || ((phase4_ui_failed++))

echo ""
echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                        DEPLOYMENT SUMMARY                    ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"

# Calculate totals
total_failed=$((phase1_infra_failed + phase1_app_failed + phase2_mgmt_failed + phase2_ai_failed + phase3_core_failed + phase3_adv_failed + phase4_sec_failed + phase4_ui_failed))
total_services=26

if [ $total_failed -eq 0 ]; then
    echo -e "${GREEN}🎉 ALL SYSTEMS OPERATIONAL! 🎉${NC}"
    echo -e "${GREEN}✅ $total_services/$total_services services are healthy${NC}"
    echo ""
    echo -e "${BLUE}📊 Service Statistics:${NC}"
    echo -e "   Infrastructure Services: 5/5 ✅"
    echo -e "   Core Application Services: 2/2 ✅"
    echo -e "   Management Services: 4/4 ✅"
    echo -e "   AI Services: 2/2 ✅"
    echo -e "   Advanced Core Services: 5/5 ✅"
    echo -e "   Advanced Services: 2/2 ✅"
    echo -e "   Security Services: 6/6 ✅"
    echo -e "   User Interface: 1/1 ✅"
    echo ""
    echo -e "${BLUE}🌐 Access Points:${NC}"
    echo -e "   Main Dashboard: ${YELLOW}http://localhost:8200${NC}"
    echo -e "   API Gateway: ${YELLOW}http://localhost:8080${NC}"
    echo -e "   Grafana Monitoring: ${YELLOW}http://localhost:13000${NC}"
    echo -e "   Prometheus Metrics: ${YELLOW}http://localhost:19090${NC}"
    echo -e "   Consul Service Discovery: ${YELLOW}http://localhost:18500${NC}"
    echo ""
    echo -e "${GREEN}🚀 Neo Service Layer is ready for production use!${NC}"
else
    echo -e "${RED}⚠️  DEPLOYMENT ISSUES DETECTED ⚠️${NC}"
    echo -e "${RED}❌ $total_failed/$total_services services failed health checks${NC}"
    echo ""
    echo -e "${YELLOW}Please check the logs for failed services:${NC}"
    echo -e "   docker compose logs [service-name]"
fi

echo ""
echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                     MANAGEMENT COMMANDS                      ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo -e "${BLUE}Stop All Services:${NC}"
echo "   docker compose -f docker-compose.phase1-minimal.yml down"
echo "   docker compose -f docker-compose.phase2-minimal.yml down"
echo "   docker compose -f docker-compose.phase3-minimal.yml down"
echo "   docker compose -f docker-compose.phase4-minimal.yml down"
echo ""
echo -e "${BLUE}View Service Logs:${NC}"
echo "   docker compose -f docker-compose.phase[1-4]-minimal.yml logs -f [service-name]"
echo ""
echo -e "${BLUE}Service Status:${NC}"
echo "   docker compose -f docker-compose.phase[1-4]-minimal.yml ps"
echo ""

exit $total_failed