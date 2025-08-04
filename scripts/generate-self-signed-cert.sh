#!/bin/bash

# Generate self-signed certificate for development/testing
# For production, use generate-production-cert.sh with Let's Encrypt

set -e

# Configuration
DOMAIN="${1:-localhost}"
DAYS="${2:-365}"
CERT_DIR="./certificates"
KEY_FILE="$CERT_DIR/key.pem"
CERT_FILE="$CERT_DIR/cert.pem"
PFX_FILE="$CERT_DIR/certificate.pfx"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}Generating self-signed SSL certificate for $DOMAIN${NC}"

# Create certificate directory
mkdir -p "$CERT_DIR"

# Load certificate password from .env.production if exists
if [ -f ".env.production" ]; then
    CERT_PASSWORD=$(grep "^CERTIFICATE_PASSWORD=" .env.production | cut -d '=' -f2)
else
    CERT_PASSWORD=$(openssl rand -base64 32)
    echo -e "${YELLOW}No .env.production found. Generated password: $CERT_PASSWORD${NC}"
fi

# Generate private key
openssl genrsa -out "$KEY_FILE" 4096

# Generate certificate signing request with SAN
cat > "$CERT_DIR/cert.conf" << EOF
[req]
distinguished_name = req_distinguished_name
req_extensions = v3_req
prompt = no

[req_distinguished_name]
C = US
ST = State
L = City
O = Neo Service Layer
OU = Development
CN = $DOMAIN

[v3_req]
keyUsage = keyEncipherment, dataEncipherment
extendedKeyUsage = serverAuth
subjectAltName = @alt_names

[alt_names]
DNS.1 = $DOMAIN
DNS.2 = *.$DOMAIN
DNS.3 = localhost
IP.1 = 127.0.0.1
IP.2 = ::1
EOF

# Generate self-signed certificate
openssl req -new -x509 \
    -key "$KEY_FILE" \
    -out "$CERT_FILE" \
    -days "$DAYS" \
    -config "$CERT_DIR/cert.conf" \
    -extensions v3_req

# Convert to PFX format
openssl pkcs12 -export \
    -out "$PFX_FILE" \
    -inkey "$KEY_FILE" \
    -in "$CERT_FILE" \
    -password "pass:$CERT_PASSWORD"

# Clean up temporary files
rm -f "$CERT_DIR/cert.conf"
rm -f "$KEY_FILE" "$CERT_FILE"

# Set permissions
chmod 600 "$PFX_FILE"

echo -e "${GREEN}✓ Self-signed certificate generated successfully${NC}"
echo ""
echo "Certificate details:"
echo "  Domain: $DOMAIN"
echo "  Valid for: $DAYS days"
echo "  Location: $PFX_FILE"
echo ""
echo -e "${YELLOW}Note: This is a self-signed certificate for development/testing only.${NC}"
echo "For production, use generate-production-cert.sh with a real domain."