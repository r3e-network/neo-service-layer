#!/bin/bash

# Neo Service Layer - Secure Credentials Generator
# This script generates secure passwords and keys for production deployment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Output file
ENV_FILE=".env.production"
BACKUP_FILE=".env.production.backup.$(date +%Y%m%d_%H%M%S)"

# Functions
print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Generate secure random string
generate_password() {
    local length=${1:-32}
    openssl rand -base64 $length | tr -d "=+/" | cut -c1-$length
}

# Generate base64 encoded key
generate_base64_key() {
    local bytes=${1:-16}
    openssl rand -base64 $bytes
}

# Generate JWT secret
generate_jwt_secret() {
    # Generate 64-character secret for JWT
    openssl rand -base64 64 | tr -d "\n"
}

# Check if .env.production already exists
if [ -f "$ENV_FILE" ]; then
    print_warning "Existing $ENV_FILE found. Creating backup at $BACKUP_FILE"
    cp "$ENV_FILE" "$BACKUP_FILE"
fi

print_warning "Generating secure credentials for Neo Service Layer production deployment..."

# Start with the template
if [ -f ".env.production.template" ]; then
    cp .env.production.template "$ENV_FILE"
else
    print_error ".env.production.template not found!"
    exit 1
fi

# Database credentials
DB_PASSWORD=$(generate_password 32)
print_success "Generated database password"

# Redis password
REDIS_PASSWORD=$(generate_password 32)
print_success "Generated Redis password"

# RabbitMQ credentials
RABBITMQ_PASSWORD=$(generate_password 32)
RABBITMQ_ERLANG_COOKIE=$(generate_password 20)
print_success "Generated RabbitMQ credentials"

# JWT secret
JWT_SECRET_KEY=$(generate_jwt_secret)
print_success "Generated JWT secret key"

# Consul encryption key (16-byte base64)
CONSUL_ENCRYPT_KEY=$(generate_base64_key 16)
CONSUL_HTTP_TOKEN=$(generate_password 32)
print_success "Generated Consul security keys"

# Grafana admin password
GRAFANA_ADMIN_PASSWORD=$(generate_password 24)
print_success "Generated Grafana admin password"

# Certificate password
CERTIFICATE_PASSWORD=$(generate_password 32)
print_success "Generated certificate password"

# Update the .env.production file
print_warning "Updating $ENV_FILE with generated credentials..."

# Use sed to replace placeholders
sed -i.bak \
    -e "s|DB_PASSWORD=.*|DB_PASSWORD=$DB_PASSWORD|" \
    -e "s|REDIS_PASSWORD=.*|REDIS_PASSWORD=$REDIS_PASSWORD|" \
    -e "s|RABBITMQ_PASSWORD=.*|RABBITMQ_PASSWORD=$RABBITMQ_PASSWORD|" \
    -e "s|RABBITMQ_ERLANG_COOKIE=.*|RABBITMQ_ERLANG_COOKIE=$RABBITMQ_ERLANG_COOKIE|" \
    -e "s|JWT_SECRET_KEY=.*|JWT_SECRET_KEY=$JWT_SECRET_KEY|" \
    -e "s|CONSUL_ENCRYPT_KEY=.*|CONSUL_ENCRYPT_KEY=$CONSUL_ENCRYPT_KEY|" \
    -e "s|CONSUL_HTTP_TOKEN=.*|CONSUL_HTTP_TOKEN=$CONSUL_HTTP_TOKEN|" \
    -e "s|GRAFANA_ADMIN_PASSWORD=.*|GRAFANA_ADMIN_PASSWORD=$GRAFANA_ADMIN_PASSWORD|" \
    -e "s|CERTIFICATE_PASSWORD=.*|CERTIFICATE_PASSWORD=$CERTIFICATE_PASSWORD|" \
    "$ENV_FILE"

# Remove backup created by sed
rm -f "$ENV_FILE.bak"

# Set appropriate permissions
chmod 600 "$ENV_FILE"
print_success "Set secure permissions on $ENV_FILE"

# Generate SSL certificate for HTTPS (self-signed for testing)
if [ ! -f "certificates/certificate.pfx" ]; then
    print_warning "Generating self-signed SSL certificate..."
    mkdir -p certificates
    
    openssl req -x509 -newkey rsa:4096 -keyout certificates/key.pem -out certificates/cert.pem -days 365 -nodes \
        -subj "/C=US/ST=State/L=City/O=NeoServiceLayer/CN=localhost"
    
    openssl pkcs12 -export -out certificates/certificate.pfx -inkey certificates/key.pem -in certificates/cert.pem \
        -password pass:$CERTIFICATE_PASSWORD
    
    rm -f certificates/key.pem certificates/cert.pem
    chmod 600 certificates/certificate.pfx
    print_success "Generated SSL certificate"
fi

# Summary
echo ""
print_success "=== Secure credentials generated successfully ==="
echo ""
echo "Generated credentials have been saved to: $ENV_FILE"
echo ""
print_warning "IMPORTANT REMINDERS:"
echo "1. Never commit $ENV_FILE to version control"
echo "2. Update placeholders with actual values for:"
echo "   - Email service credentials"
echo "   - Intel SGX API keys"
echo "   - Cloud provider credentials"
echo "3. For production, use a valid SSL certificate"
echo ""

# Optionally display credentials
if [ "$1" == "--show" ]; then
    print_warning "Generated credentials (DO NOT SHARE):"
    echo "JWT Secret (first 20 chars): ${JWT_SECRET_KEY:0:20}..."
fi