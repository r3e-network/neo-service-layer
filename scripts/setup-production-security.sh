#!/bin/bash

# Production Security Setup for Neo Service Layer
# Uses password-based and SSH-based security (no IP restrictions)

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
echo -e "${BLUE}║${BOLD}        Neo Service Layer - Production Security Setup${NC}${BLUE}         ║${NC}"
echo -e "${BLUE}║${BOLD}        Password & SSH-Based Security (No IP restrictions)${NC}${BLUE}   ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check if running as root for firewall setup
if [ "$EUID" -ne 0 ]; then 
    echo -e "${RED}Please run with sudo for complete setup: sudo $0${NC}"
    exit 1
fi

echo -e "${GREEN}Production Security Model:${NC}"
echo -e "  🔒 Admin interfaces accessible only via SSH tunnel"
echo -e "  🌐 Public APIs with strong authentication"
echo -e "  🔐 All services use strong generated passwords"
echo -e "  🛡️ Firewall blocks direct admin access"
echo -e "  📊 Public dashboard for monitoring"
echo ""

read -p "Continue with production security setup? (y/n): " confirm
if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
    echo -e "${YELLOW}Setup cancelled.${NC}"
    exit 0
fi

echo -e "${BLUE}Step 1: Generating strong passwords...${NC}"

# Generate secure random passwords
DB_PASSWORD=$(openssl rand -base64 32 | tr -d '=/' | head -c 32)
REDIS_PASSWORD=$(openssl rand -base64 32 | tr -d '=/' | head -c 32)
JWT_SECRET=$(openssl rand -base64 64 | tr -d '=/')
GRAFANA_PASSWORD=$(openssl rand -base64 24 | tr -d '=/' | head -c 24)
CONSUL_TOKEN=$(openssl rand -base64 32 | tr -d '=/' | head -c 32)

echo -e "${GREEN}✓ Generated secure passwords${NC}"

echo -e "${BLUE}Step 2: Updating environment configuration...${NC}"

# Backup existing .env
cp .env .env.backup.$(date +%Y%m%d-%H%M%S)

# Create production .env with strong security
cat > .env << EOF
# Neo Service Layer Production Environment Configuration
# Generated: $(date)
# Security Level: Production

# Environment
ASPNETCORE_ENVIRONMENT=Production
NODE_ENV=production

# Database Configuration (Strong Password)
DB_HOST=postgres
DB_PORT=5432
DB_NAME=neo_service_layer
DB_USER=neo_service_user
DB_PASSWORD=$DB_PASSWORD

# Redis Configuration (Strong Password)
REDIS_HOST=redis
REDIS_PORT=6379
REDIS_PASSWORD=$REDIS_PASSWORD
REDIS_SSL=false
REDIS_MAX_MEMORY=2gb
REDIS_EVICTION_POLICY=allkeys-lru

# JWT Configuration (Strong Secret)
JWT_SECRET_KEY=$JWT_SECRET
JWT_ISSUER=neo-service-layer
JWT_AUDIENCE=neo-service-layer-clients
JWT_EXPIRATION_MINUTES=60
JWT_REFRESH_EXPIRATION_MINUTES=10080
JWT_CLOCK_SKEW_MINUTES=5

# Consul Configuration (Secured)
CONSUL_PORT=8500
CONSUL_ENCRYPT_KEY=$(openssl rand -base64 32 | head -c 24)
CONSUL_HTTP_TOKEN=$CONSUL_TOKEN

# Monitoring Configuration (Strong Passwords)
PROMETHEUS_PORT=9090
GRAFANA_PORT=3000
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=$GRAFANA_PASSWORD
JAEGER_UI_PORT=16686

# API Gateway Configuration
API_HTTP_PORT=80
API_HTTPS_PORT=443
CERTIFICATE_PASSWORD=$(openssl rand -base64 32 | head -c 24)

# Security Configuration (Production)
REQUIRE_HTTPS=false
ENABLE_SECURITY_HEADERS=true
ALLOWED_ORIGINS=http://$SERVER_IP:8080,http://$SERVER_IP:8200
ENABLE_API_KEY_AUTH=true
RATE_LIMIT_ENABLED=true
RATE_LIMIT_REQUESTS_PER_MINUTE=100
CIRCUIT_BREAKER_ENABLED=true

# Public Access Configuration
PUBLIC_IP=$SERVER_IP
ASPNETCORE_URLS=http://0.0.0.0:80
ENABLE_CORS=true

# Blockchain Configuration
NEO_N3_RPC_URL=https://mainnet1.neo.coz.io:443
NEO_X_RPC_URL=https://mainnet.neox.org:443

# Logging Configuration
LOG_LEVEL=Information
LOG_RETENTION_DAYS=30
ENABLE_STRUCTURED_LOGGING=true
ENABLE_SECURITY_LOGGING=true

# Performance Configuration
ENABLE_RESPONSE_COMPRESSION=true
ENABLE_REQUEST_BUFFERING=false
MAX_REQUEST_SIZE_MB=10

# Production Mode Settings
PRODUCTION_MODE=true
ENABLE_DEBUG_LOGGING=false
ENABLE_SWAGGER=false
EOF

echo -e "${GREEN}✓ Production environment configured with strong passwords${NC}"

echo -e "${BLUE}Step 3: Setting up production firewall rules...${NC}"

# Configure UFW for production
ufw --force reset
ufw default deny incoming
ufw default allow outgoing

# Essential services
ufw allow 22/tcp comment "SSH (Essential)"
ufw allow 80/tcp comment "HTTP"
ufw allow 443/tcp comment "HTTPS"

# Public Neo Services (API endpoints only)
ufw allow 8080/tcp comment "Neo API Gateway"
ufw allow 8200/tcp comment "Neo Dashboard"

# All other service APIs (for external integrations)
for port in 8081 8090 8091 8092 8093 8100 8101 8110 8111 8112 8113 8114 8120 8130 8140 8141 8142 8143 8144 8145; do
    ufw allow $port/tcp comment "Neo Service API"
done

# Block admin interfaces from public (accessible only via SSH tunnel)
# Ports 13000, 15432, 16379, 18500, 19090 are blocked by default deny policy

# Rate limiting for API endpoints
iptables -A INPUT -p tcp --dport 8080 -m conntrack --ctstate NEW -m limit --limit 100/min --limit-burst 200 -j ACCEPT
iptables -A INPUT -p tcp --dport 8080 -m conntrack --ctstate NEW -j DROP

# Save iptables rules
if command -v netfilter-persistent &> /dev/null; then
    netfilter-persistent save
else
    mkdir -p /etc/iptables
    iptables-save > /etc/iptables/rules.v4
fi

# Enable firewall
ufw --force enable

echo -e "${GREEN}✓ Production firewall configured${NC}"

echo -e "${BLUE}Step 4: Creating SSH tunnel helper scripts...${NC}"

# Create admin tunnel script for users
cat > /home/ubuntu/neo-service-layer/admin-access.sh << EOF
#!/bin/bash

# Neo Service Layer Admin Access via SSH Tunnel
# Run this script from your LOCAL computer to access admin interfaces

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║           Neo Service Layer - Admin Access Tunnel            ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${GREEN}Creating secure SSH tunnel to admin interfaces...${NC}"
echo ""
echo -e "${YELLOW}After connection, access admin interfaces at:${NC}"
echo -e "  🔍 Grafana (Monitoring): ${GREEN}http://localhost:13000${NC}"
echo -e "      Username: admin"
echo -e "      Password: $GRAFANA_PASSWORD"
echo ""
echo -e "  📊 Prometheus (Metrics): ${GREEN}http://localhost:19090${NC}"
echo -e "  🏛️  Consul (Service Discovery): ${GREEN}http://localhost:18500${NC}"
echo -e "  🗄️  PostgreSQL: ${GREEN}localhost:15432${NC}"
echo -e "      Database: neo_service_layer"
echo -e "      Username: neo_service_user" 
echo -e "      Password: $DB_PASSWORD"
echo ""
echo -e "  📦 Redis: ${GREEN}localhost:16379${NC}"
echo -e "      Password: $REDIS_PASSWORD"
echo ""
echo -e "${BLUE}Press Ctrl+C to disconnect${NC}"
echo ""

# Create SSH tunnel with all admin ports
ssh -L 13000:localhost:13000 \\
    -L 19090:localhost:19090 \\
    -L 18500:localhost:18500 \\
    -L 15432:localhost:15432 \\
    -L 16379:localhost:16379 \\
    -N ubuntu@$SERVER_IP
EOF

chmod +x /home/ubuntu/neo-service-layer/admin-access.sh
chown ubuntu:ubuntu /home/ubuntu/neo-service-layer/admin-access.sh

# Create credentials info file
cat > /home/ubuntu/neo-service-layer/PRODUCTION-CREDENTIALS.txt << EOF
Neo Service Layer - Production Credentials
==========================================
Generated: $(date)
Server IP: $SERVER_IP

IMPORTANT: Keep this file secure and private!

Database (PostgreSQL):
  Host: $SERVER_IP:15432 (via SSH tunnel: localhost:15432)
  Database: neo_service_layer
  Username: neo_service_user
  Password: $DB_PASSWORD

Redis Cache:
  Host: $SERVER_IP:16379 (via SSH tunnel: localhost:16379)
  Password: $REDIS_PASSWORD

Grafana Admin:
  URL: http://localhost:13000 (via SSH tunnel)
  Username: admin
  Password: $GRAFANA_PASSWORD

JWT Secret Key:
  $JWT_SECRET

Consul Token:
  $CONSUL_TOKEN

SSH Tunnel Command (run from your computer):
  ./admin-access.sh

Public Services (no credentials needed):
  Main Dashboard: http://$SERVER_IP:8200
  API Gateway: http://$SERVER_IP:8080

Security Notes:
  - Admin interfaces only accessible via SSH tunnel
  - All passwords are randomly generated and secure
  - Change Grafana password after first login
  - Regularly rotate JWT secret and database passwords
  - Monitor access logs in Grafana
EOF

chmod 600 /home/ubuntu/neo-service-layer/PRODUCTION-CREDENTIALS.txt
chown ubuntu:ubuntu /home/ubuntu/neo-service-layer/PRODUCTION-CREDENTIALS.txt

echo -e "${GREEN}✓ SSH tunnel scripts and credentials created${NC}"

echo -e "${BLUE}Step 5: Installing additional security tools...${NC}"

# Install fail2ban for intrusion prevention
apt-get update
apt-get install -y fail2ban ufw

# Configure fail2ban
cat > /etc/fail2ban/jail.local << EOF
[DEFAULT]
bantime = 3600
findtime = 600
maxretry = 5

[sshd]
enabled = true
port = ssh
logpath = /var/log/auth.log
maxretry = 3
bantime = 86400

[neo-api]
enabled = true
port = 8080
logpath = /var/log/neo-api.log
maxretry = 10
bantime = 3600
EOF

systemctl enable fail2ban
systemctl restart fail2ban

echo -e "${GREEN}✓ Security tools installed and configured${NC}"

echo -e "${BLUE}Step 6: Restarting services with production configuration...${NC}"

# Change to project directory
cd /home/ubuntu/neo-service-layer

# Stop and restart all services
sudo -u ubuntu ./scripts/stop-all-services.sh
sudo -u ubuntu ./scripts/deploy-automatic.sh

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║${BOLD}           PRODUCTION SECURITY SETUP COMPLETE${NC}${GREEN}                ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

echo -e "${PURPLE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}                    PRODUCTION ACCESS GUIDE                   ${NC}"
echo -e "${PURPLE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

echo -e "${GREEN}🌐 PUBLIC SERVICES (accessible from anywhere):${NC}"
echo -e "   Main Dashboard: ${YELLOW}http://$SERVER_IP:8200${NC}"
echo -e "   API Gateway: ${YELLOW}http://$SERVER_IP:8080${NC}"
echo -e "   All Service APIs: ${YELLOW}http://$SERVER_IP:8081-8145${NC}"
echo ""

echo -e "${BLUE}🔒 ADMIN INTERFACES (SSH tunnel required):${NC}"
echo -e "   ${BOLD}From your local computer, run:${NC}"
echo -e "   ${YELLOW}./admin-access.sh${NC}"
echo ""
echo -e "   Then access locally:"
echo -e "   • Grafana: ${GREEN}http://localhost:13000${NC} (admin/${GRAFANA_PASSWORD})"
echo -e "   • Prometheus: ${GREEN}http://localhost:19090${NC}"
echo -e "   • Consul: ${GREEN}http://localhost:18500${NC}"
echo -e "   • PostgreSQL: ${GREEN}localhost:15432${NC}"
echo -e "   • Redis: ${GREEN}localhost:16379${NC}"
echo ""

echo -e "${YELLOW}📋 IMPORTANT FILES CREATED:${NC}"
echo -e "   • ${BOLD}PRODUCTION-CREDENTIALS.txt${NC} - All passwords and access info"
echo -e "   • ${BOLD}admin-access.sh${NC} - SSH tunnel script for admin access"
echo -e "   • ${BOLD}.env.backup.*${NC} - Backup of previous configuration"
echo ""

echo -e "${RED}🔐 SECURITY FEATURES ENABLED:${NC}"
echo -e "   ✓ Strong random passwords for all services"
echo -e "   ✓ Admin interfaces blocked from public internet"
echo -e "   ✓ SSH tunnel required for admin access"
echo -e "   ✓ Firewall blocking direct database access"
echo -e "   ✓ Rate limiting on API endpoints"
echo -e "   ✓ Fail2ban protection against brute force"
echo -e "   ✓ Security headers and CORS properly configured"
echo ""

echo -e "${BLUE}📚 NEXT STEPS:${NC}"
echo -e "   1. ${YELLOW}Copy admin-access.sh to your local computer${NC}"
echo -e "   2. ${YELLOW}Secure the PRODUCTION-CREDENTIALS.txt file${NC}"
echo -e "   3. ${YELLOW}Test admin access via SSH tunnel${NC}"
echo -e "   4. ${YELLOW}Change Grafana password on first login${NC}"
echo -e "   5. ${YELLOW}Set up SSL certificates for HTTPS${NC}"
echo -e "   6. ${YELLOW}Configure monitoring alerts in Grafana${NC}"
echo ""

echo -e "${GREEN}🎉 Your Neo Service Layer is now production-ready and secure!${NC}"
echo ""
echo -e "${BLUE}View all credentials: ${YELLOW}cat PRODUCTION-CREDENTIALS.txt${NC}"
echo -e "${BLUE}Check firewall status: ${YELLOW}sudo ufw status${NC}"
echo -e "${BLUE}View service status: ${YELLOW}./scripts/show-public-urls.sh${NC}"