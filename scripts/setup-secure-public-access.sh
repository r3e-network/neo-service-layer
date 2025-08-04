#!/bin/bash

# Complete setup for secure public access on fixed IP server
# Server IP: 198.244.215.132

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
BOLD='\033[1m'
NC='\033[0m'

SERVER_IP="198.244.215.132"

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║${BOLD}     Neo Service Layer - Complete Public Setup${NC}${BLUE}               ║${NC}"
echo -e "${BLUE}║${BOLD}     Server IP: 198.244.215.132${NC}${BLUE}                              ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

echo -e "${GREEN}This script will:${NC}"
echo -e "  ✓ Configure all services for public access"
echo -e "  ✓ Set up secure firewall rules"
echo -e "  ✓ Provide admin access options"
echo -e "  ✓ Display all access URLs"
echo ""

# Check if services are already public
if grep -q "0.0.0.0:" docker-compose.phase1-minimal.yml 2>/dev/null; then
    echo -e "${GREEN}✓ Services already configured for public access${NC}"
else
    echo -e "${BLUE}Step 1: Configuring services for public access...${NC}"
    
    # Enable public access
    ./scripts/enable-public-access.sh << 'EOF'
yes-expose-services
y
EOF

    echo -e "${GREEN}✓ Services configured for public access${NC}"
fi

echo ""
echo -e "${BLUE}Step 2: Security Configuration${NC}"
echo ""
echo -e "${PURPLE}Choose your security level:${NC}"
echo ""
echo -e "${GREEN}1) Production Ready${NC} - Recommended for live deployments"
echo -e "   ✓ Only web services publicly accessible"
echo -e "   ✓ Admin interfaces restricted to your IP"
echo -e "   ✓ Database ports blocked from public"
echo -e "   ✓ Rate limiting enabled"
echo ""
echo -e "${YELLOW}2) Development Mode${NC} - For testing and development"
echo -e "   ✓ All services publicly accessible"
echo -e "   ✓ Admin interfaces open (with strong passwords)"
echo -e "   ✓ Easy access for testing"
echo ""
echo -e "${BLUE}3) API Only${NC} - Maximum security"
echo -e "   ✓ Only API Gateway and Dashboard public"
echo -e "   ✓ All admin access via SSH tunnel"
echo -e "   ✓ Minimal attack surface"
echo ""

read -p "Select security level (1-3): " security_level

case $security_level in
    1)
        echo -e "${GREEN}Setting up Production Security...${NC}"
        
        # Get current admin IP
        echo ""
        echo -e "${BLUE}For admin interface access, we need your current IP address.${NC}"
        
        # Try to detect from SSH
        ADMIN_IP=""
        if [ ! -z "$SSH_CONNECTION" ]; then
            ADMIN_IP=$(echo $SSH_CONNECTION | awk '{print $1}')
            echo -e "${GREEN}Detected your IP: $ADMIN_IP${NC}"
            read -p "Use this IP for admin access? (y/n): " use_detected
            if [ "$use_detected" != "y" ] && [ "$use_detected" != "Y" ]; then
                ADMIN_IP=""
            fi
        fi
        
        if [ -z "$ADMIN_IP" ]; then
            echo -e "${YELLOW}Find your IP at: https://whatismyipaddress.com${NC}"
            read -p "Enter your admin IP address: " ADMIN_IP
        fi
        
        # Configure production firewall
        sudo ./scripts/configure-firewall.sh << EOF
y
$ADMIN_IP
y
EOF
        ;;
        
    2)
        echo -e "${YELLOW}Setting up Development Mode...${NC}"
        
        # Use dynamic firewall with basic security
        sudo ./scripts/configure-firewall-dynamic.sh << 'EOF'
1
y
EOF
        ;;
        
    3)
        echo -e "${BLUE}Setting up API Only Mode...${NC}"
        
        # Use dynamic firewall with API only
        sudo ./scripts/configure-firewall-dynamic.sh << 'EOF'
3
y
EOF
        ;;
esac

echo ""
echo -e "${GREEN}✓ Security configuration complete${NC}"

# Update environment for public access
echo ""
echo -e "${BLUE}Step 3: Updating environment configuration...${NC}"

# Update .env with public settings
if ! grep -q "PUBLIC_IP=" .env 2>/dev/null; then
    cat >> .env << EOF

# Public Access Configuration
PUBLIC_IP=$SERVER_IP
ALLOWED_ORIGINS=http://$SERVER_IP:8080,http://$SERVER_IP:8200,https://$SERVER_IP
ASPNETCORE_URLS=http://0.0.0.0:80
ENABLE_CORS=true
EOF
    echo -e "${GREEN}✓ Environment updated for public access${NC}"
fi

# Restart services to apply all changes
echo ""
echo -e "${BLUE}Step 4: Restarting services with new configuration...${NC}"
./scripts/stop-all-services.sh
./scripts/deploy-automatic.sh

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║${BOLD}              SETUP COMPLETE - SERVICES ONLINE${NC}${GREEN}               ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Display access information based on security level
echo -e "${PURPLE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}                        ACCESS INFORMATION                     ${NC}"
echo -e "${PURPLE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

echo -e "${BLUE}🌐 Public Services (accessible from anywhere):${NC}"
echo -e "   Main Dashboard: ${YELLOW}http://$SERVER_IP:8200${NC}"
echo -e "   API Gateway: ${YELLOW}http://$SERVER_IP:8080${NC}"

case $security_level in
    1)
        echo ""
        echo -e "${GREEN}🔒 Admin Services (restricted to your IP: $ADMIN_IP):${NC}"
        echo -e "   Grafana: ${YELLOW}http://$SERVER_IP:13000${NC} (admin/admin)"
        echo -e "   Prometheus: ${YELLOW}http://$SERVER_IP:19090${NC}"
        echo -e "   Consul: ${YELLOW}http://$SERVER_IP:18500${NC}"
        echo ""
        echo -e "${BLUE}📝 Admin Notes:${NC}"
        echo -e "   • Change Grafana password immediately!"
        echo -e "   • Update database passwords in .env"
        echo -e "   • If your IP changes: ${GREEN}sudo ./scripts/allow-my-ip.sh${NC}"
        ;;
        
    2)
        echo ""
        echo -e "${YELLOW}⚠️  Development Mode - Admin interfaces are public:${NC}"
        echo -e "   Grafana: ${YELLOW}http://$SERVER_IP:13000${NC} (admin/admin)"
        echo -e "   Prometheus: ${YELLOW}http://$SERVER_IP:19090${NC}"
        echo -e "   Consul: ${YELLOW}http://$SERVER_IP:18500${NC}"
        echo ""
        echo -e "${RED}🚨 CRITICAL: Change default passwords immediately!${NC}"
        ;;
        
    3)
        echo ""
        echo -e "${GREEN}🔒 Maximum Security - Admin access via SSH tunnel only:${NC}"
        echo -e "   Run from your computer: ${YELLOW}./admin-tunnel.sh${NC}"
        echo -e "   Then access: http://localhost:13000"
        ;;
esac

echo ""
echo -e "${BLUE}📊 All Service Endpoints:${NC}"
./scripts/show-public-urls.sh

echo ""
echo -e "${PURPLE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}                       SECURITY REMINDERS                     ${NC}"
echo -e "${PURPLE}═══════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${YELLOW}Important Security Tasks:${NC}"
echo -e "  1. ${RED}Change Grafana password:${NC} Visit Grafana → User → Change Password"
echo -e "  2. ${RED}Update .env passwords:${NC} Edit DB_PASSWORD, REDIS_PASSWORD"
echo -e "  3. ${BLUE}Set up SSL:${NC} Use Let's Encrypt or CloudFlare"
echo -e "  4. ${BLUE}Enable monitoring:${NC} Set up alerts in Grafana"
echo -e "  5. ${BLUE}Regular updates:${NC} sudo apt update && sudo apt upgrade"
echo ""
echo -e "${GREEN}Useful Commands:${NC}"
echo -e "  Show URLs: ${YELLOW}./scripts/show-public-urls.sh${NC}"
echo -e "  Allow your IP: ${YELLOW}sudo ./scripts/allow-my-ip.sh${NC}"
echo -e "  Firewall status: ${YELLOW}sudo ufw status${NC}"
echo -e "  Service logs: ${YELLOW}docker compose -f docker-compose.phase1-minimal.yml logs -f${NC}"
echo ""
echo -e "${BLUE}🎉 Your Neo Service Layer is now publicly accessible and secured!${NC}"