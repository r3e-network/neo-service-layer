#!/bin/bash

# Generate production SSL certificate using Let's Encrypt
# This script should be run on the production server with proper domain configuration

set -e

# Configuration
DOMAIN="${1:-your-production-domain.com}"
EMAIL="${2:-admin@your-production-domain.com}"
CERT_DIR="/etc/letsencrypt/live/$DOMAIN"
PFX_OUTPUT="/etc/ssl/certs/neo-service-layer.pfx"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

print_usage() {
    echo "Usage: $0 <domain> <email>"
    echo "Example: $0 api.neoservicelayer.com admin@neoservicelayer.com"
}

if [ "$#" -lt 2 ]; then
    print_usage
    exit 1
fi

echo -e "${YELLOW}Generating production SSL certificate for $DOMAIN${NC}"

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
   echo -e "${RED}This script must be run as root for Let's Encrypt${NC}"
   exit 1
fi

# Install certbot if not present
if ! command -v certbot &> /dev/null; then
    echo -e "${YELLOW}Installing certbot...${NC}"
    apt-get update
    apt-get install -y certbot
fi

# Generate certificate using Let's Encrypt
echo -e "${YELLOW}Requesting certificate from Let's Encrypt...${NC}"
certbot certonly \
    --standalone \
    --non-interactive \
    --agree-tos \
    --email "$EMAIL" \
    --domains "$DOMAIN" \
    --domains "www.$DOMAIN"

# Check if certificate was generated
if [ ! -d "$CERT_DIR" ]; then
    echo -e "${RED}Certificate generation failed${NC}"
    exit 1
fi

# Convert to PFX format for .NET
echo -e "${YELLOW}Converting to PFX format...${NC}"

# Generate a secure password for the PFX
PFX_PASSWORD=$(openssl rand -base64 32)

# Create PFX file
openssl pkcs12 -export \
    -out "$PFX_OUTPUT" \
    -inkey "$CERT_DIR/privkey.pem" \
    -in "$CERT_DIR/fullchain.pem" \
    -password "pass:$PFX_PASSWORD"

# Set proper permissions
chmod 600 "$PFX_OUTPUT"
chown www-data:www-data "$PFX_OUTPUT"

# Update .env.production with certificate path and password
ENV_FILE="/home/ubuntu/neo-service-layer/.env.production"
if [ -f "$ENV_FILE" ]; then
    sed -i.bak \
        -e "s|CERTIFICATE_PATH=.*|CERTIFICATE_PATH=$PFX_OUTPUT|" \
        -e "s|CERTIFICATE_PASSWORD=.*|CERTIFICATE_PASSWORD=$PFX_PASSWORD|" \
        "$ENV_FILE"
fi

# Create renewal script
RENEWAL_SCRIPT="/etc/letsencrypt/renewal-hooks/deploy/neo-service-layer.sh"
mkdir -p /etc/letsencrypt/renewal-hooks/deploy

cat > "$RENEWAL_SCRIPT" << 'EOF'
#!/bin/bash
# Auto-renewal script for Neo Service Layer SSL certificate

DOMAIN="__DOMAIN__"
CERT_DIR="/etc/letsencrypt/live/$DOMAIN"
PFX_OUTPUT="/etc/ssl/certs/neo-service-layer.pfx"
PFX_PASSWORD="__PFX_PASSWORD__"

# Convert renewed certificate to PFX
openssl pkcs12 -export \
    -out "$PFX_OUTPUT" \
    -inkey "$CERT_DIR/privkey.pem" \
    -in "$CERT_DIR/fullchain.pem" \
    -password "pass:$PFX_PASSWORD"

# Set permissions
chmod 600 "$PFX_OUTPUT"
chown www-data:www-data "$PFX_OUTPUT"

# Reload services
systemctl reload neo-service-layer || true
docker restart neo-api-gateway || true
EOF

# Replace placeholders in renewal script
sed -i \
    -e "s|__DOMAIN__|$DOMAIN|g" \
    -e "s|__PFX_PASSWORD__|$PFX_PASSWORD|g" \
    "$RENEWAL_SCRIPT"

chmod +x "$RENEWAL_SCRIPT"

echo -e "${GREEN}✓ Production SSL certificate generated successfully${NC}"
echo ""
echo "Certificate details:"
echo "  Domain: $DOMAIN"
echo "  PFX Location: $PFX_OUTPUT"
echo "  PFX Password: Saved in .env.production"
echo "  Auto-renewal: Enabled via certbot"
echo ""
echo -e "${YELLOW}Important:${NC}"
echo "1. Certificate will auto-renew before expiration"
echo "2. Renewal hook will automatically convert to PFX"
echo "3. Make sure port 80 is open for renewal challenges"
echo "4. Test renewal with: certbot renew --dry-run"