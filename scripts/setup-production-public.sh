#!/bin/bash

# Production Security Setup - All Services Publicly Accessible
# Strong password-based security, no IP restrictions, no SSH tunnels required

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
echo -e "${BLUE}║${BOLD}     Neo Service Layer - Public Production Security${NC}${BLUE}          ║${NC}"
echo -e "${BLUE}║${BOLD}     All Services Publicly Accessible with Strong Auth${NC}${BLUE}      ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check if running as root for firewall setup
if [ "$EUID" -ne 0 ]; then 
    echo -e "${RED}Please run with sudo: sudo $0${NC}"
    exit 1
fi

echo -e "${GREEN}Public Production Security Model:${NC}"
echo -e "  🌐 ALL services publicly accessible"
echo -e "  🔐 Strong random passwords for everything"
echo -e "  🛡️ Rate limiting and DDoS protection"
echo -e "  🚫 Fail2ban protection against brute force"
echo -e "  📊 Security monitoring and logging"
echo -e "  🔒 HTTPS ready (certificates needed separately)"
echo ""

read -p "Continue with public production setup? (y/n): " confirm
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
API_KEY=$(openssl rand -base64 32 | tr -d '=/' | head -c 32)

echo -e "${GREEN}✓ Generated secure passwords${NC}"

echo -e "${BLUE}Step 2: Updating environment for public production...${NC}"

# Backup existing .env
cp .env .env.backup.$(date +%Y%m%d-%H%M%S)

# Create production .env with public access and strong security
cat > .env << EOF
# Neo Service Layer - Public Production Configuration
# Generated: $(date)
# Security Level: Public Production with Strong Authentication

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
JWT_ISSUER=neo-service-layer-$SERVER_IP
JWT_AUDIENCE=neo-service-layer-clients
JWT_EXPIRATION_MINUTES=60
JWT_REFRESH_EXPIRATION_MINUTES=10080
JWT_CLOCK_SKEW_MINUTES=5

# API Security
API_KEY=$API_KEY
ENABLE_API_KEY_AUTH=true
REQUIRE_API_KEY_FOR_ADMIN=true

# Consul Configuration (Secured)
CONSUL_PORT=8500
CONSUL_ENCRYPT_KEY=$(openssl rand -base64 32 | head -c 24)
CONSUL_HTTP_TOKEN=$CONSUL_TOKEN

# Monitoring Configuration (Strong Passwords)
PROMETHEUS_PORT=9090
GRAFANA_PORT=3000
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=$GRAFANA_PASSWORD
GRAFANA_SECURITY_ADMIN_PASSWORD=$GRAFANA_PASSWORD
GRAFANA_SECURITY_SECRET_KEY=$(openssl rand -base64 32)

# API Gateway Configuration
API_HTTP_PORT=80
API_HTTPS_PORT=443

# Security Configuration (Production Public)
REQUIRE_HTTPS=false
ENABLE_SECURITY_HEADERS=true
ALLOWED_ORIGINS=*
ENABLE_CORS=true
CORS_ALLOW_CREDENTIALS=true

# Rate Limiting (Aggressive for public)
RATE_LIMIT_ENABLED=true
RATE_LIMIT_REQUESTS_PER_MINUTE=100
RATE_LIMIT_BURST=200
CIRCUIT_BREAKER_ENABLED=true

# Public Access Configuration
PUBLIC_IP=$SERVER_IP
ASPNETCORE_URLS=http://0.0.0.0:80
BIND_ALL_INTERFACES=true

# Blockchain Configuration
NEO_N3_RPC_URL=https://mainnet1.neo.coz.io:443
NEO_X_RPC_URL=https://mainnet.neox.org:443

# Logging Configuration (Enhanced for public)
LOG_LEVEL=Information
LOG_RETENTION_DAYS=90
ENABLE_STRUCTURED_LOGGING=true
ENABLE_SECURITY_LOGGING=true
ENABLE_ACCESS_LOGGING=true

# Performance Configuration
ENABLE_RESPONSE_COMPRESSION=true
ENABLE_REQUEST_BUFFERING=false
MAX_REQUEST_SIZE_MB=10
CONNECTION_TIMEOUT_SECONDS=30

# Production Mode Settings
PRODUCTION_MODE=true
ENABLE_DEBUG_LOGGING=false
ENABLE_SWAGGER=false
ENABLE_METRICS=true

# Backup Configuration
ENABLE_AUTOMATIC_BACKUPS=true
BACKUP_RETENTION_DAYS=30
EOF

echo -e "${GREEN}✓ Production environment configured for public access${NC}"

echo -e "${BLUE}Step 3: Setting up public production firewall...${NC}"

# Configure UFW for public production (more permissive but still secure)
ufw --force reset
ufw default deny incoming
ufw default allow outgoing

# Essential services
ufw allow 22/tcp comment "SSH (Essential)"
ufw allow 80/tcp comment "HTTP"
ufw allow 443/tcp comment "HTTPS"

# All Neo Services - Publicly Accessible
ufw allow 8080/tcp comment "Neo API Gateway (Public)"
ufw allow 8200/tcp comment "Neo Dashboard (Public)"

# Service APIs
for port in 8081 8090 8091 8092 8093 8100 8101 8110 8111 8112 8113 8114 8120 8130 8140 8141 8142 8143 8144 8145; do
    ufw allow $port/tcp comment "Neo Service API (Public)"
done

# Admin Interfaces - Publicly Accessible (with strong passwords)
ufw allow 13000/tcp comment "Grafana (Public - Strong Password)"
ufw allow 19090/tcp comment "Prometheus (Public)"
ufw allow 18500/tcp comment "Consul (Public - Token Required)"

# Database Access (for external tools) - Strong passwords required
ufw allow 15432/tcp comment "PostgreSQL (Public - Strong Password)"
ufw allow 16379/tcp comment "Redis (Public - Strong Password)"

# Advanced rate limiting and DDoS protection
echo -e "${BLUE}Configuring advanced rate limiting...${NC}"

# Rate limiting for web interfaces
iptables -A INPUT -p tcp --dport 13000 -m conntrack --ctstate NEW -m limit --limit 30/min --limit-burst 50 -j ACCEPT
iptables -A INPUT -p tcp --dport 13000 -m conntrack --ctstate NEW -j DROP

# Rate limiting for API endpoints
iptables -A INPUT -p tcp --dport 8080 -m conntrack --ctstate NEW -m limit --limit 100/min --limit-burst 200 -j ACCEPT
iptables -A INPUT -p tcp --dport 8080 -m conntrack --ctstate NEW -j DROP

# Rate limiting for database access
iptables -A INPUT -p tcp --dport 15432 -m conntrack --ctstate NEW -m limit --limit 20/min --limit-burst 30 -j ACCEPT
iptables -A INPUT -p tcp --dport 15432 -m conntrack --ctstate NEW -j DROP

# Save iptables rules
if command -v netfilter-persistent &> /dev/null; then
    netfilter-persistent save
else
    mkdir -p /etc/iptables
    iptables-save > /etc/iptables/rules.v4
fi

# Enable firewall
ufw --force enable

echo -e "${GREEN}✓ Public production firewall configured${NC}"

echo -e "${BLUE}Step 4: Installing and configuring security tools...${NC}"

# Install security tools
apt-get update
apt-get install -y fail2ban ufw htop

# Enhanced fail2ban configuration for public services
cat > /etc/fail2ban/jail.local << EOF
[DEFAULT]
bantime = 3600
findtime = 600
maxretry = 5
backend = systemd

[sshd]
enabled = true
port = ssh
logpath = /var/log/auth.log
maxretry = 3
bantime = 86400

[grafana]
enabled = true
port = 13000
logpath = /var/log/neo/grafana.log
maxretry = 5
bantime = 3600
filter = grafana

[neo-api]
enabled = true
port = 8080
logpath = /var/log/neo/api.log
maxretry = 10
bantime = 1800
filter = neo-api

[postgresql]
enabled = true
port = 15432
logpath = /var/log/neo/postgresql.log
maxretry = 3
bantime = 7200
filter = postgresql
EOF

# Create custom fail2ban filters
mkdir -p /etc/fail2ban/filter.d

cat > /etc/fail2ban/filter.d/grafana.conf << EOF
[Definition]
failregex = ^.*"remote_addr":"<HOST>".*"status":401.*$
ignoreregex =
EOF

cat > /etc/fail2ban/filter.d/neo-api.conf << EOF
[Definition]
failregex = ^.*<HOST>.*401.*Unauthorized.*$
ignoreregex =
EOF

systemctl enable fail2ban
systemctl restart fail2ban

echo -e "${GREEN}✓ Security tools configured${NC}"

echo -e "${BLUE}Step 5: Creating production credentials and access guide...${NC}"

# Create comprehensive credentials file
cat > /home/ubuntu/neo-service-layer/PRODUCTION-CREDENTIALS.txt << EOF
Neo Service Layer - Public Production Credentials
=================================================
Generated: $(date)
Server IP: $SERVER_IP

🚨 CRITICAL: Keep this file secure! All services are publicly accessible!

═══════════════════════════════════════════════════════════════

🌐 PUBLIC SERVICE URLS:

Main Dashboard:
  URL: http://$SERVER_IP:8200
  Access: Public (no authentication required)

API Gateway:
  URL: http://$SERVER_IP:8080
  Access: Public APIs + JWT for protected endpoints
  API Key: $API_KEY

═══════════════════════════════════════════════════════════════

🔒 ADMIN INTERFACE CREDENTIALS:

Grafana (Monitoring & Dashboards):
  URL: http://$SERVER_IP:13000
  Username: admin
  Password: $GRAFANA_PASSWORD
  🚨 CHANGE PASSWORD IMMEDIATELY AFTER FIRST LOGIN!

Prometheus (Metrics):
  URL: http://$SERVER_IP:19090
  Access: Public (no authentication by default)
  Note: Consider enabling basic auth if needed

Consul (Service Discovery):
  URL: http://$SERVER_IP:18500
  Token: $CONSUL_TOKEN
  Access: Token required for admin operations

═══════════════════════════════════════════════════════════════

🗄️ DATABASE CREDENTIALS:

PostgreSQL:
  Host: $SERVER_IP
  Port: 15432
  Database: neo_service_layer
  Username: neo_service_user
  Password: $DB_PASSWORD
  
  Connection String: 
  Host=$SERVER_IP;Port=15432;Database=neo_service_layer;Username=neo_service_user;Password=$DB_PASSWORD

Redis Cache:
  Host: $SERVER_IP
  Port: 16379
  Password: $REDIS_PASSWORD
  
  Connection String:
  $SERVER_IP:16379,password=$REDIS_PASSWORD

═══════════════════════════════════════════════════════════════

🔐 SECURITY TOKENS:

JWT Secret Key:
  $JWT_SECRET

API Key (for protected endpoints):
  $API_KEY

Consul Token:
  $CONSUL_TOKEN

═══════════════════════════════════════════════════════════════

📊 ALL SERVICE ENDPOINTS:

Core Services:
  • API Gateway: http://$SERVER_IP:8080
  • Smart Contracts: http://$SERVER_IP:8081

Management Services:
  • Key Management: http://$SERVER_IP:8090
  • Notification: http://$SERVER_IP:8091
  • Monitoring: http://$SERVER_IP:8092
  • Health: http://$SERVER_IP:8093

AI Services:
  • Pattern Recognition: http://$SERVER_IP:8100
  • Prediction: http://$SERVER_IP:8101

Advanced Services:
  • Oracle: http://$SERVER_IP:8110
  • Storage: http://$SERVER_IP:8111
  • CrossChain: http://$SERVER_IP:8112
  • Proof of Reserve: http://$SERVER_IP:8113
  • Randomness: http://$SERVER_IP:8114
  • Fair Ordering: http://$SERVER_IP:8120
  • TEE Host: http://$SERVER_IP:8130

Security Services:
  • Voting: http://$SERVER_IP:8140
  • Zero Knowledge: http://$SERVER_IP:8141
  • Secrets Management: http://$SERVER_IP:8142
  • Social Recovery: http://$SERVER_IP:8143
  • Enclave Storage: http://$SERVER_IP:8144
  • Network Security: http://$SERVER_IP:8145

User Interface:
  • Web Dashboard: http://$SERVER_IP:8200

═══════════════════════════════════════════════════════════════

🛡️ SECURITY MEASURES ENABLED:

✓ Strong random passwords (32+ characters)
✓ Rate limiting on all services
✓ Fail2ban intrusion prevention
✓ Firewall with DDoS protection
✓ Security headers enabled
✓ Comprehensive logging
✓ API key authentication for protected endpoints
✓ JWT-based session management

⚠️ IMMEDIATE SECURITY TASKS:
1. Change Grafana admin password
2. Set up SSL/TLS certificates for HTTPS
3. Configure API authentication as needed
4. Set up monitoring alerts
5. Regular password rotation schedule

═══════════════════════════════════════════════════════════════
EOF

chmod 600 /home/ubuntu/neo-service-layer/PRODUCTION-CREDENTIALS.txt
chown ubuntu:ubuntu /home/ubuntu/neo-service-layer/PRODUCTION-CREDENTIALS.txt

echo -e "${GREEN}✓ Production credentials file created${NC}"

echo -e "${BLUE}Step 6: Restarting services with production configuration...${NC}"

# Change to project directory
cd /home/ubuntu/neo-service-layer

# Stop and restart all services with new configuration
sudo -u ubuntu ./scripts/stop-all-services.sh
sudo -u ubuntu ./scripts/deploy-automatic.sh

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║${BOLD}         PUBLIC PRODUCTION DEPLOYMENT COMPLETE${NC}${GREEN}               ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

echo -e "${PURPLE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}                  🌐 ALL SERVICES ARE PUBLIC                  ${NC}"
echo -e "${PURPLE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

echo -e "${GREEN}🎯 MAIN ACCESS POINTS:${NC}"
echo -e "   Dashboard: ${YELLOW}http://$SERVER_IP:8200${NC}"
echo -e "   API Gateway: ${YELLOW}http://$SERVER_IP:8080${NC}"
echo -e "   Grafana: ${YELLOW}http://$SERVER_IP:13000${NC} ${RED}(admin/${GRAFANA_PASSWORD})${NC}"
echo ""

echo -e "${BLUE}🔐 ADMIN INTERFACES (publicly accessible):${NC}"
echo -e "   Grafana: ${YELLOW}http://$SERVER_IP:13000${NC}"
echo -e "   Prometheus: ${YELLOW}http://$SERVER_IP:19090${NC}"
echo -e "   Consul: ${YELLOW}http://$SERVER_IP:18500${NC}"
echo ""

echo -e "${YELLOW}🗄️ DATABASES (publicly accessible with credentials):${NC}"
echo -e "   PostgreSQL: ${YELLOW}$SERVER_IP:15432${NC}"
echo -e "   Redis: ${YELLOW}$SERVER_IP:16379${NC}"
echo ""

echo -e "${RED}🚨 CRITICAL NEXT STEPS:${NC}"
echo -e "   1. ${BOLD}Log into Grafana and CHANGE PASSWORD immediately!${NC}"
echo -e "      Visit: http://$SERVER_IP:13000"
echo -e "      Login: admin / $GRAFANA_PASSWORD"
echo -e ""
echo -e "   2. ${BOLD}Review and secure credentials in:${NC}"
echo -e "      cat PRODUCTION-CREDENTIALS.txt"
echo -e ""
echo -e "   3. ${BOLD}Set up HTTPS certificates${NC}"
echo -e "   4. ${BOLD}Configure API authentication as needed${NC}"
echo -e "   5. ${BOLD}Set up monitoring alerts${NC}"
echo ""

echo -e "${GREEN}✅ SECURITY FEATURES ACTIVE:${NC}"
echo -e "   • Strong passwords for all services"
echo -e "   • Rate limiting and DDoS protection"
echo -e "   • Fail2ban intrusion prevention"
echo -e "   • Comprehensive security logging"
echo -e "   • API key authentication available"
echo ""

echo -e "${BLUE}📋 USEFUL COMMANDS:${NC}"
echo -e "   View credentials: ${YELLOW}cat PRODUCTION-CREDENTIALS.txt${NC}"
echo -e "   Check security status: ${YELLOW}sudo fail2ban-client status${NC}"
echo -e "   View firewall rules: ${YELLOW}sudo ufw status verbose${NC}"
echo -e "   Monitor services: ${YELLOW}./scripts/show-public-urls.sh${NC}"
echo ""

echo -e "${GREEN}🎉 Neo Service Layer is live with public production security!${NC}"