#!/bin/bash

# Temporarily allow current IP address for admin access
# Use this when you need quick admin access from your current location

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo -e "${RED}Please run with sudo: sudo $0${NC}"
    exit 1
fi

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║              Allow Current IP for Admin Access               ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Try to detect current IP from SSH connection
CURRENT_IP=""

# Method 1: SSH_CONNECTION environment variable
if [ ! -z "$SSH_CONNECTION" ]; then
    CURRENT_IP=$(echo $SSH_CONNECTION | awk '{print $1}')
    echo -e "${GREEN}Detected IP from SSH connection: $CURRENT_IP${NC}"
fi

# Method 2: Ask user to provide IP
if [ -z "$CURRENT_IP" ]; then
    echo -e "${YELLOW}Could not detect your IP automatically.${NC}"
    echo -e "${BLUE}You can find your IP at: https://whatismyipaddress.com${NC}"
    echo ""
    read -p "Enter your current IP address: " CURRENT_IP
fi

if [ -z "$CURRENT_IP" ]; then
    echo -e "${RED}No IP address provided. Exiting.${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}Current firewall rules:${NC}"
ufw status numbered

echo ""
echo -e "${YELLOW}This will add temporary rules to allow your IP ($CURRENT_IP) access to:${NC}"
echo -e "  - Grafana (port 13000)"
echo -e "  - Prometheus (port 19090)"
echo -e "  - Consul (port 18500)"
echo -e "  - PostgreSQL (port 15432)"
echo -e "  - Redis (port 16379)"
echo ""

read -p "Add these rules for IP $CURRENT_IP? (y/n): " confirm

if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
    echo -e "${YELLOW}Cancelled.${NC}"
    exit 0
fi

echo -e "${BLUE}Adding firewall rules for $CURRENT_IP...${NC}"

# Add rules for admin access
ufw allow from $CURRENT_IP to any port 13000 comment "Temp: Grafana for $CURRENT_IP"
ufw allow from $CURRENT_IP to any port 19090 comment "Temp: Prometheus for $CURRENT_IP"
ufw allow from $CURRENT_IP to any port 18500 comment "Temp: Consul for $CURRENT_IP"
ufw allow from $CURRENT_IP to any port 15432 comment "Temp: PostgreSQL for $CURRENT_IP"
ufw allow from $CURRENT_IP to any port 16379 comment "Temp: Redis for $CURRENT_IP"

echo -e "${GREEN}✓ Added temporary admin access rules for $CURRENT_IP${NC}"

# Show updated rules
echo ""
echo -e "${BLUE}Updated firewall status:${NC}"
ufw status numbered

echo ""
echo -e "${GREEN}You can now access admin interfaces:${NC}"
echo -e "  Grafana: ${YELLOW}http://198.244.215.132:13000${NC}"
echo -e "  Prometheus: ${YELLOW}http://198.244.215.132:19090${NC}"
echo -e "  Consul: ${YELLOW}http://198.244.215.132:18500${NC}"
echo ""

# Create removal script
cat > /tmp/remove-ip-$CURRENT_IP.sh << EOF
#!/bin/bash
echo "Removing temporary access rules for $CURRENT_IP..."
ufw status numbered | grep "$CURRENT_IP" | awk '{print \$1}' | tr -d '[]' | sort -nr | while read rule_num; do
    if [ ! -z "\$rule_num" ]; then
        echo "Removing rule \$rule_num"
        ufw --force delete \$rule_num
    fi
done
echo "Temporary rules removed for $CURRENT_IP"
rm -f /tmp/remove-ip-$CURRENT_IP.sh
EOF

chmod +x /tmp/remove-ip-$CURRENT_IP.sh

echo -e "${BLUE}Security Notes:${NC}"
echo -e "  1. These rules are temporary - consider removing when done"
echo -e "  2. To remove these rules later: ${GREEN}sudo /tmp/remove-ip-$CURRENT_IP.sh${NC}"
echo -e "  3. Rules will persist until manually removed"
echo -e "  4. Your IP may change if you disconnect/reconnect"
echo ""
echo -e "${YELLOW}⚠️  Always use strong passwords and enable 2FA when available!${NC}"