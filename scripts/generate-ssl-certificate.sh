#!/bin/bash
# Generate SSL certificate for Neo Service Layer

set -e

CERT_DIR="./certificates"
DOMAIN="${1:-localhost}"
CERT_VALIDITY_DAYS="${2:-365}"

echo "🔐 Generating SSL certificate for domain: $DOMAIN"

# Create certificates directory
mkdir -p "$CERT_DIR"

# Generate private key
openssl genrsa -out "$CERT_DIR/neo-service-layer.key" 4096

# Generate certificate signing request
openssl req -new -key "$CERT_DIR/neo-service-layer.key" -out "$CERT_DIR/neo-service-layer.csr" \
    -subj "/C=US/ST=State/L=City/O=Neo Service Layer/CN=$DOMAIN"

# Generate self-signed certificate (for development/testing)
openssl x509 -req -days "$CERT_VALIDITY_DAYS" -in "$CERT_DIR/neo-service-layer.csr" \
    -signkey "$CERT_DIR/neo-service-layer.key" -out "$CERT_DIR/neo-service-layer.crt"

# Create PFX file for .NET
openssl pkcs12 -export -out "$CERT_DIR/neo-service-layer.pfx" \
    -inkey "$CERT_DIR/neo-service-layer.key" -in "$CERT_DIR/neo-service-layer.crt" \
    -passout pass:${CERT_PASSWORD:-changeme}

# Set appropriate permissions
chmod 600 "$CERT_DIR"/*.key
chmod 644 "$CERT_DIR"/*.crt
chmod 644 "$CERT_DIR"/*.pfx

echo "✅ SSL certificate generated successfully!"
echo "📁 Certificate files:"
echo "   - Private Key: $CERT_DIR/neo-service-layer.key"
echo "   - Certificate: $CERT_DIR/neo-service-layer.crt"
echo "   - PFX Bundle: $CERT_DIR/neo-service-layer.pfx"
echo ""
echo "⚠️  Note: This is a self-signed certificate. For production, use a certificate from a trusted CA."
echo "🚀 You can obtain a free certificate from Let's Encrypt using Certbot."