#!/bin/bash

# Generate SSL Certificate for Local Development/Testing
# For production, use Let's Encrypt or your SSL provider

set -e

echo "🔐 Generating SSL Certificate for Neo Service Layer"
echo "================================================="

# Create SSL directory
mkdir -p ssl

# Generate private key
openssl genrsa -out ssl/key.pem 2048

# Generate certificate signing request
openssl req -new -key ssl/key.pem -out ssl/csr.pem \
    -subj "/C=US/ST=State/L=City/O=Neo Service Layer/CN=service.neoservicelayer.com"

# Generate self-signed certificate (valid for 365 days)
openssl x509 -req -days 365 -in ssl/csr.pem -signkey ssl/key.pem -out ssl/cert.pem

# Generate DH parameters for enhanced security
openssl dhparam -out ssl/dhparam.pem 2048

# Clean up CSR
rm ssl/csr.pem

# Set appropriate permissions
chmod 600 ssl/key.pem
chmod 644 ssl/cert.pem
chmod 644 ssl/dhparam.pem

echo ""
echo "✅ SSL certificate generated successfully!"
echo ""
echo "Files created:"
echo "  - ssl/cert.pem (Certificate)"
echo "  - ssl/key.pem (Private Key)"
echo "  - ssl/dhparam.pem (DH Parameters)"
echo ""
echo "⚠️  This is a self-signed certificate for development/testing."
echo "For production, use Let's Encrypt or a trusted CA."
echo ""
echo "To use Let's Encrypt in production:"
echo "  certbot certonly --webroot -w /var/www/html -d service.neoservicelayer.com"