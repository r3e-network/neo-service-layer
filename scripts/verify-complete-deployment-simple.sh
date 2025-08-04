#!/bin/bash

# Simple Complete Neo Service Layer Deployment Verification

set -e

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m'

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║            Neo Service Layer - Complete Deployment           ║${NC}"
echo -e "${BLUE}║                    System Verification                       ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Simple health check function
check_service() {
    local name=$1
    local port=$2
    
    if curl -s -f "http://localhost:$port/health" >/dev/null 2>&1; then
        echo -e "${GREEN}✓${NC} $name (port $port)"
        return 0
    else
        echo -e "${RED}✗${NC} $name (port $port) - FAILED"
        return 1
    fi
}

# Infrastructure checks (simplified)
check_infra() {
    local name=$1
    local port=$2
    
    if nc -z localhost $port 2>/dev/null; then
        echo -e "${GREEN}✓${NC} $name (port $port)"
        return 0
    else
        echo -e "${RED}✗${NC} $name (port $port) - FAILED"
        return 1
    fi
}

# Phase 1 - Infrastructure & Core Services
echo -e "${PURPLE}█ Phase 1 - Infrastructure & Core Services${NC}"
echo -e "${BLUE}├─ Infrastructure Services${NC}"
phase1_infra_failed=0
check_infra "PostgreSQL" "15432" || ((phase1_infra_failed++))
check_infra "Redis" "16379" || ((phase1_infra_failed++))
check_infra "Consul" "18500" || ((phase1_infra_failed++))
check_infra "Prometheus" "19090" || ((phase1_infra_failed++))
check_infra "Grafana" "13000" || ((phase1_infra_failed++))

echo -e "${BLUE}├─ Core Application Services${NC}"
phase1_app_failed=0
check_service "API Gateway" "8080" || ((phase1_app_failed++))
check_service "Smart Contracts" "8081" || ((phase1_app_failed++))

# Phase 2 - Management & AI Services
echo -e "${PURPLE}█ Phase 2 - Management & AI Services${NC}"
echo -e "${BLUE}├─ Management Services${NC}"
phase2_mgmt_failed=0
check_service "Key Management" "8090" || ((phase2_mgmt_failed++))
check_service "Notification" "8091" || ((phase2_mgmt_failed++))
check_service "Monitoring" "8092" || ((phase2_mgmt_failed++))
check_service "Health" "8093" || ((phase2_mgmt_failed++))

echo -e "${BLUE}├─ AI Services${NC}"
phase2_ai_failed=0
check_service "Pattern Recognition" "8100" || ((phase2_ai_failed++))
check_service "Prediction" "8101" || ((phase2_ai_failed++))

# Phase 3 - Advanced Services
echo -e "${PURPLE}█ Phase 3 - Advanced Services${NC}"
echo -e "${BLUE}├─ Core Services${NC}"
phase3_core_failed=0
check_service "Oracle" "8110" || ((phase3_core_failed++))
check_service "Storage" "8111" || ((phase3_core_failed++))
check_service "CrossChain" "8112" || ((phase3_core_failed++))
check_service "Proof of Reserve" "8113" || ((phase3_core_failed++))
check_service "Randomness" "8114" || ((phase3_core_failed++))

echo -e "${BLUE}├─ Advanced Services${NC}"
phase3_adv_failed=0
check_service "Fair Ordering" "8120" || ((phase3_adv_failed++))
check_service "TEE Host" "8130" || ((phase3_adv_failed++))

# Phase 4 - Security & Governance
echo -e "${PURPLE}█ Phase 4 - Security & Governance${NC}"
echo -e "${BLUE}├─ Security Services${NC}"
phase4_sec_failed=0
check_service "Voting" "8140" || ((phase4_sec_failed++))
check_service "Zero Knowledge" "8141" || ((phase4_sec_failed++))
check_service "Secrets Management" "8142" || ((phase4_sec_failed++))
check_service "Social Recovery" "8143" || ((phase4_sec_failed++))
check_service "Enclave Storage" "8144" || ((phase4_sec_failed++))
check_service "Network Security" "8145" || ((phase4_sec_failed++))

echo -e "${BLUE}├─ User Interface${NC}"
phase4_ui_failed=0
check_service "Web Interface" "8200" || ((phase4_ui_failed++))

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
    echo -e "   Main Dashboard: http://localhost:8200"
    echo -e "   API Gateway: http://localhost:8080"
    echo -e "   Grafana Monitoring: http://localhost:13000"
    echo -e "   Prometheus Metrics: http://localhost:19090"
    echo -e "   Consul Service Discovery: http://localhost:18500"
    echo ""
    echo -e "${GREEN}🚀 Neo Service Layer is ready for production use!${NC}"
else
    echo -e "${RED}⚠️  DEPLOYMENT ISSUES DETECTED ⚠️${NC}"
    echo -e "${RED}❌ $total_failed/$total_services services failed health checks${NC}"
fi

exit $total_failed