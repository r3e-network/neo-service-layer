#!/bin/bash

# Configure UFW firewall for Neo Service Layer with dynamic IP support
# This version provides options for users without fixed IPs

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m'

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    Neo Service Layer - Dynamic IP Firewall Configuration     ║${NC}"
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
echo -e "${PURPLE}══════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}                    Configuration Options                     ${NC}"
echo -e "${PURPLE}══════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${GREEN}Since you don't have a fixed admin IP, choose an option:${NC}"
echo ""
echo -e "${BLUE}1) Basic Security${NC} - Block database ports, allow web services"
echo -e "   ✓ Blocks direct database access (PostgreSQL, Redis)"
echo -e "   ✓ Allows web services and APIs"
echo -e "   ✓ Keeps admin panels accessible (use strong passwords!)"
echo ""
echo -e "${BLUE}2) Restricted Admin${NC} - Require VPN or SSH tunnel for admin access"
echo -e "   ✓ Blocks ALL admin interfaces from public"
echo -e "   ✓ Only web dashboard and APIs publicly accessible"
echo -e "   ✓ Access admin panels via SSH tunnel"
echo ""
echo -e "${BLUE}3) API Only${NC} - Only expose API endpoints"
echo -e "   ✓ Only API Gateway (8080) and Dashboard (8200) public"
echo -e "   ✓ All other services blocked"
echo -e "   ✓ Maximum security, limited functionality"
echo ""
echo -e "${BLUE}4) Custom${NC} - Configure specific rules"
echo ""

read -p "Select option (1-4): " option

echo -e "${BLUE}Configuring firewall rules...${NC}"

# Reset firewall to defaults
ufw --force reset

# Default policies
ufw default deny incoming
ufw default allow outgoing

# Always allow SSH (critical!)
ufw allow 22/tcp comment "SSH"

case $option in
    1)
        echo -e "${GREEN}Configuring Basic Security...${NC}"
        
        # Allow public web services
        ufw allow 80/tcp comment "HTTP"
        ufw allow 443/tcp comment "HTTPS"
        ufw allow 8200/tcp comment "Neo Dashboard"
        ufw allow 8080/tcp comment "Neo API Gateway"
        
        # Allow all service endpoints
        for port in 8081 8090 8091 8092 8093 8100 8101 8110 8111 8112 8113 8114 8120 8130 8140 8141 8142 8143 8144 8145; do
            ufw allow $port/tcp comment "Neo Service"
        done
        
        # Allow admin interfaces (with warning)
        ufw allow 13000/tcp comment "Grafana (CHANGE DEFAULT PASSWORD!)"
        ufw allow 19090/tcp comment "Prometheus"
        ufw allow 18500/tcp comment "Consul"
        
        # Block database ports from public
        # (No need to explicitly deny - default policy handles this)
        
        echo -e "${YELLOW}⚠️  Admin interfaces are publicly accessible!${NC}"
        echo -e "${YELLOW}   IMMEDIATELY change default passwords:${NC}"
        echo -e "${YELLOW}   - Grafana: admin/admin → Change in UI${NC}"
        echo -e "${YELLOW}   - Database passwords in .env file${NC}"
        ;;
        
    2)
        echo -e "${GREEN}Configuring Restricted Admin Access...${NC}"
        
        # Only allow essential public services
        ufw allow 80/tcp comment "HTTP"
        ufw allow 443/tcp comment "HTTPS"
        ufw allow 8200/tcp comment "Neo Dashboard"
        ufw allow 8080/tcp comment "Neo API Gateway"
        
        # Allow API endpoints
        for port in 8081 8090 8091 8092 8093 8100 8101 8110 8111 8112 8113 8114 8120 8130 8140 8141 8142 8143 8144 8145; do
            ufw allow $port/tcp comment "Neo Service API"
        done
        
        # Admin interfaces blocked by default deny policy
        
        echo -e "${GREEN}Admin interfaces blocked from public access.${NC}"
        echo -e "${BLUE}To access admin panels, use SSH tunnel:${NC}"
        echo -e "${YELLOW}ssh -L 13000:localhost:13000 -L 19090:localhost:19090 -L 18500:localhost:18500 ubuntu@198.244.215.132${NC}"
        echo -e "Then access locally: ${GREEN}http://localhost:13000${NC}"
        ;;
        
    3)
        echo -e "${GREEN}Configuring API Only Access...${NC}"
        
        # Minimal public exposure
        ufw allow 80/tcp comment "HTTP"
        ufw allow 443/tcp comment "HTTPS"
        ufw allow 8200/tcp comment "Neo Dashboard"
        ufw allow 8080/tcp comment "Neo API Gateway"
        
        echo -e "${GREEN}Only API Gateway and Dashboard are publicly accessible.${NC}"
        echo -e "${BLUE}All other services require SSH tunnel or VPN access.${NC}"
        ;;
        
    4)
        echo -e "${GREEN}Custom Configuration${NC}"
        echo -e "${BLUE}Current plan: Allow SSH, HTTP, HTTPS by default${NC}"
        
        ufw allow 80/tcp comment "HTTP"
        ufw allow 443/tcp comment "HTTPS"
        
        echo -e "${YELLOW}Add custom rules:${NC}"
        while true; do
            read -p "Enter port to allow (or 'done' to finish): " port
            if [ "$port" = "done" ]; then
                break
            fi
            read -p "Enter comment for port $port: " comment
            ufw allow $port/tcp comment "$comment"
            echo -e "${GREEN}✓ Added rule for port $port${NC}"
        done
        ;;
esac

# Add rate limiting for API endpoints
echo -e "${BLUE}Adding rate limiting for API protection...${NC}"
iptables -A INPUT -p tcp --dport 8080 -m conntrack --ctstate NEW -m limit --limit 100/min --limit-burst 200 -j ACCEPT
iptables -A INPUT -p tcp --dport 8080 -m conntrack --ctstate NEW -j DROP

# Enable firewall
echo ""
echo -e "${YELLOW}⚠️  WARNING: Enabling firewall. Make sure SSH (port 22) is allowed!${NC}"
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

# Create SSH tunnel helper script
cat > /home/ubuntu/neo-service-layer/admin-tunnel.sh << 'EOF'
#!/bin/bash
# SSH tunnel for admin access
echo "Creating SSH tunnel for admin interfaces..."
echo "After connecting, access services at:"
echo "  Grafana: http://localhost:13000"
echo "  Prometheus: http://localhost:19090"
echo "  Consul: http://localhost:18500"
echo "  PostgreSQL: localhost:15432"
echo ""
echo "Press Ctrl+C to close tunnel"
ssh -L 13000:localhost:13000 \
    -L 19090:localhost:19090 \
    -L 18500:localhost:18500 \
    -L 15432:localhost:15432 \
    -L 16379:localhost:16379 \
    -N ubuntu@198.244.215.132
EOF

chmod +x /home/ubuntu/neo-service-layer/admin-tunnel.sh
chown ubuntu:ubuntu /home/ubuntu/neo-service-layer/admin-tunnel.sh

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║             FIREWALL CONFIGURATION COMPLETE                  ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Option-specific advice
case $option in
    1)
        echo -e "${RED}⚠️  CRITICAL SECURITY TASKS:${NC}"
        echo -e "  1. ${YELLOW}Change Grafana password immediately!${NC}"
        echo -e "     Visit http://198.244.215.132:13000 → admin/admin"
        echo -e "  2. ${YELLOW}Update database passwords in .env${NC}"
        echo -e "  3. ${YELLOW}Set up monitoring alerts${NC}"
        ;;
    2|3)
        echo -e "${GREEN}✓ Admin interfaces protected${NC}"
        echo -e "${BLUE}To access admin panels from your computer:${NC}"
        echo -e "  ${YELLOW}./admin-tunnel.sh${NC}"
        echo -e "  (Run this on your local machine)"
        ;;
esac

echo ""
echo -e "${BLUE}Additional Security Recommendations:${NC}"
echo -e "  1. Set up fail2ban: ${GREEN}sudo apt-get install fail2ban${NC}"
echo -e "  2. Enable CloudFlare/CDN for DDoS protection"
echo -e "  3. Set up SSL certificates with Let's Encrypt"
echo -e "  4. Configure application-level authentication"
echo -e "  5. Regular security updates: ${GREEN}sudo apt update && sudo apt upgrade${NC}"
echo ""
echo -e "${BLUE}Useful commands:${NC}"
echo -e "  View rules: ${GREEN}sudo ufw status numbered${NC}"
echo -e "  Delete rule: ${GREEN}sudo ufw delete [number]${NC}"
echo -e "  Check logs: ${GREEN}sudo tail -f /var/log/ufw.log${NC}"
echo -e "  Admin tunnel: ${GREEN}./admin-tunnel.sh${NC}"