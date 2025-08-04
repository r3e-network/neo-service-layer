#!/bin/bash

# Emergency shutdown script for security incidents
set -e

# Colors
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${RED}╔═══════════════════════════════════════╗${NC}"
echo -e "${RED}║   EMERGENCY SHUTDOWN INITIATED        ║${NC}"
echo -e "${RED}╚═══════════════════════════════════════╝${NC}"
echo ""

# Log the shutdown
echo "[$(date)] Emergency shutdown initiated by $(whoami)" >> /var/log/neo-service-layer/emergency.log

# 1. Block all incoming traffic (except SSH)
echo -e "${YELLOW}Blocking incoming traffic...${NC}"
sudo iptables -I INPUT 1 -p tcp --dport 22 -j ACCEPT
sudo iptables -I INPUT 2 -j DROP

# 2. Stop all services
echo -e "${YELLOW}Stopping all services...${NC}"
docker compose -f docker compose.production.yml down

# 3. Stop Docker daemon
echo -e "${YELLOW}Stopping Docker daemon...${NC}"
sudo systemctl stop docker

# 4. Capture current state
echo -e "${YELLOW}Capturing system state...${NC}"
INCIDENT_DIR="/var/log/neo-service-layer/incident-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$INCIDENT_DIR"

# Save network connections
sudo netstat -tulpn > "$INCIDENT_DIR/network-connections.txt"

# Save process list
ps auxf > "$INCIDENT_DIR/processes.txt"

# Save recent auth logs
sudo tail -n 10000 /var/log/auth.log > "$INCIDENT_DIR/auth.log"

# Save Docker logs
sudo journalctl -u docker --since "1 hour ago" > "$INCIDENT_DIR/docker.log"

# 5. Create incident report template
cat > "$INCIDENT_DIR/INCIDENT_REPORT.md" << EOF
# Security Incident Report

**Date**: $(date)
**Initiated by**: $(whoami)
**System**: $(hostname)

## Incident Description
[Describe what triggered the emergency shutdown]

## Timeline
- $(date): Emergency shutdown initiated
- [Add timeline entries]

## Affected Systems
- [ ] API Gateway
- [ ] Database
- [ ] Redis
- [ ] Smart Contracts
- [ ] User Data

## Initial Assessment
[Document initial findings]

## Actions Taken
1. Emergency shutdown executed
2. Network traffic blocked
3. System state captured
4. [Add additional actions]

## Next Steps
- [ ] Review captured logs
- [ ] Identify breach vector
- [ ] Assess data impact
- [ ] Prepare recovery plan
- [ ] Notify stakeholders

## Recovery Plan
[Document recovery steps]
EOF

echo ""
echo -e "${RED}════════════════════════════════════════${NC}"
echo -e "${RED}EMERGENCY SHUTDOWN COMPLETE${NC}"
echo -e "${RED}════════════════════════════════════════${NC}"
echo ""
echo "System state captured in: $INCIDENT_DIR"
echo ""
echo -e "${YELLOW}IMPORTANT NEXT STEPS:${NC}"
echo "1. Review logs in $INCIDENT_DIR"
echo "2. Fill out incident report: $INCIDENT_DIR/INCIDENT_REPORT.md"
echo "3. Run security audit: ./scripts/security-audit.sh"
echo "4. When ready to restore: ./scripts/secure-redeploy.sh"
echo ""
echo -e "${RED}DO NOT restart services until security review is complete!${NC}"