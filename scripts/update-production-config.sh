#!/bin/bash

# Update production configuration with real blockchain endpoints
# This script updates the .env.production file with production blockchain endpoints

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

ENV_FILE=".env.production"

if [ ! -f "$ENV_FILE" ]; then
    echo "Error: $ENV_FILE not found. Run generate-secure-credentials.sh first."
    exit 1
fi

echo -e "${YELLOW}Updating production blockchain endpoints...${NC}"

# Production Neo N3 endpoints (mainnet)
NEO_N3_MAINNET_URLS=(
    "https://mainnet1.neo.coz.io:443"
    "https://mainnet2.neo.coz.io:443"
    "https://mainnet3.neo.coz.io:443"
    "https://seed1.neo.org:443"
    "https://seed2.neo.org:443"
)

# Production Neo X endpoint
NEO_X_MAINNET_URL="https://mainnet.neox.org:443"

# Select primary Neo N3 endpoint (you can rotate these for load balancing)
PRIMARY_NEO_N3_URL="${NEO_N3_MAINNET_URLS[0]}"

# Update blockchain endpoints
sed -i.bak \
    -e "s|NEO_N3_RPC_URL=.*|NEO_N3_RPC_URL=$PRIMARY_NEO_N3_URL|" \
    -e "s|NEO_X_RPC_URL=.*|NEO_X_RPC_URL=$NEO_X_MAINNET_URL|" \
    "$ENV_FILE"

# Add production domain configuration
if ! grep -q "ALLOWED_ORIGINS" "$ENV_FILE"; then
    echo "" >> "$ENV_FILE"
    echo "# Production Domain Configuration" >> "$ENV_FILE"
    echo "ALLOWED_ORIGINS=https://your-production-domain.com,https://api.your-production-domain.com" >> "$ENV_FILE"
fi

# Update certificate paths for production
if ! grep -q "CERTIFICATE_PATH" "$ENV_FILE"; then
    echo "CERTIFICATE_PATH=/etc/ssl/certs/your-production-cert.pfx" >> "$ENV_FILE"
fi

# Add production-specific settings
if ! grep -q "PRODUCTION_MODE" "$ENV_FILE"; then
    echo "" >> "$ENV_FILE"
    echo "# Production Mode Settings" >> "$ENV_FILE"
    echo "PRODUCTION_MODE=true" >> "$ENV_FILE"
    echo "ENABLE_DEBUG_LOGGING=false" >> "$ENV_FILE"
    echo "ENABLE_SWAGGER=false" >> "$ENV_FILE"
fi

echo -e "${GREEN}✓ Updated blockchain endpoints to production${NC}"
echo ""
echo "Production Neo N3 endpoints configured:"
for url in "${NEO_N3_MAINNET_URLS[@]}"; do
    echo "  - $url"
done
echo ""
echo "Production Neo X endpoint: $NEO_X_MAINNET_URL"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Update ALLOWED_ORIGINS with your actual domain"
echo "2. Update CERTIFICATE_PATH with your SSL certificate location"
echo "3. Configure external service credentials (Intel SGX, Email, etc.)"
echo "4. Deploy smart contracts and update contract hashes"