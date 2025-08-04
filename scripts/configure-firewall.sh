#!/bin/bash

# Configure UFW firewall for Neo Service Layer
# This provides basic security for public deployments

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║         Neo Service Layer - Firewall Configuration           ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo -e "${RED}Please run with sudo: sudo $0${NC}"
    exit 1
fi

# Check if ufw is installed
if ! command -v ufw &> /dev/null; then
    echo -e "${YELLOW}UFW not installed. Installing...${NC}"
    apt-get update && apt-get install -y ufw
fi

echo -e "${BLUE}Current firewall status:${NC}"
ufw status

echo ""
echo -e "${YELLOW}This will configure the firewall with the following rules:${NC}"
echo -e "  - Allow SSH (port 22)"
echo -e "  - Allow HTTP (port 80)"
echo -e "  - Allow HTTPS (port 443)"
echo -e "  - Allow Main Dashboard (port 8200)"
echo -e "  - Allow API Gateway (port 8080)"
echo -e "  - Allow Grafana (port 13000) - restricted to your IP"
echo -e "  - Block direct database access from public"
echo -e "  - Block other admin interfaces from public"
echo ""

read -p "Configure firewall rules? (y/n): " confirm
if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
    echo -e "${YELLOW}Firewall configuration cancelled${NC}"
    exit 0
fi

# Get current SSH connection IP for admin access
ADMIN_IP=$(echo $SSH_CONNECTION | awk '{print $1}')
if [ -z "$ADMIN_IP" ]; then
    read -p "Enter your admin IP address for restricted access: " ADMIN_IP
fi

echo -e "${BLUE}Configuring firewall rules...${NC}"

# Reset firewall to defaults
ufw --force reset

# Default policies
ufw default deny incoming
ufw default allow outgoing

# Allow SSH (important!)
ufw allow 22/tcp comment "SSH"

# Allow public web services
ufw allow 80/tcp comment "HTTP"
ufw allow 443/tcp comment "HTTPS"
ufw allow 8200/tcp comment "Neo Dashboard"
ufw allow 8080/tcp comment "Neo API Gateway"

# Allow health check endpoints for all services
for port in 8081 8090 8091 8092 8093 8100 8101 8110 8111 8112 8113 8114 8120 8130 8140 8141 8142 8143 8144 8145; do
    ufw allow $port/tcp comment "Neo Service"
done

# Restrict admin interfaces to specific IP
if [ ! -z "$ADMIN_IP" ]; then
    echo -e "${BLUE}Restricting admin interfaces to IP: $ADMIN_IP${NC}"
    
    # Admin interfaces - restricted
    ufw allow from $ADMIN_IP to any port 13000 comment "Grafana Admin"
    ufw allow from $ADMIN_IP to any port 19090 comment "Prometheus Admin"
    ufw allow from $ADMIN_IP to any port 18500 comment "Consul Admin"
    ufw allow from $ADMIN_IP to any port 15432 comment "PostgreSQL Admin"
    ufw allow from $ADMIN_IP to any port 16379 comment "Redis Admin"
else
    echo -e "${YELLOW}⚠️  No admin IP specified - admin interfaces will be blocked!${NC}"
fi

# Enable firewall
echo ""
echo -e "${YELLOW}⚠️  WARNING: Enabling firewall. Make sure SSH is allowed!${NC}"
echo -e "${YELLOW}Current SSH connection from: $ADMIN_IP${NC}"
echo ""
read -p "Enable firewall now? (y/n): " enable

if [ "$enable" == "y" ] || [ "$enable" == "Y" ]; then
    ufw --force enable
    echo -e "${GREEN}✓ Firewall enabled${NC}"
else
    echo -e "${YELLOW}Firewall configured but not enabled. Run 'sudo ufw enable' to activate.${NC}"
fi

# Show status
echo ""
echo -e "${BLUE}Firewall status:${NC}"
ufw status verbose

# Create iptables rules for additional protection
echo ""
echo -e "${BLUE}Adding additional security rules...${NC}"

# Rate limiting for API endpoints
iptables -A INPUT -p tcp --dport 8080 -m conntrack --ctstate NEW -m limit --limit 100/min --limit-burst 200 -j ACCEPT
iptables -A INPUT -p tcp --dport 8080 -m conntrack --ctstate NEW -j DROP

# Save iptables rules
if command -v netfilter-persistent &> /dev/null; then
    netfilter-persistent save
else
    iptables-save > /etc/iptables/rules.v4
fi

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║             FIREWALL CONFIGURATION COMPLETE                  ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BLUE}Security Summary:${NC}"
echo -e "  ✓ Public access: Dashboard (8200), API (8080)"
echo -e "  ✓ Admin access restricted to: $ADMIN_IP"
echo -e "  ✓ Database ports blocked from public"
echo -e "  ✓ Rate limiting enabled on API"
echo ""
echo -e "${YELLOW}Additional recommendations:${NC}"
echo -e "  1. Set up SSL certificates for HTTPS"
echo -e "  2. Configure application-level authentication"
echo -e "  3. Enable service mesh security"
echo -e "  4. Set up monitoring and alerts"
echo -e "  5. Regular security updates"
echo ""
echo -e "${BLUE}Useful commands:${NC}"
echo -e "  View rules: ${GREEN}sudo ufw status verbose${NC}"
echo -e "  Add IP whitelist: ${GREEN}sudo ufw allow from YOUR_IP${NC}"
echo -e "  Check logs: ${GREEN}sudo tail -f /var/log/ufw.log${NC}"