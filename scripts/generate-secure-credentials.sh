#!/bin/bash

# Generate secure credentials for Neo Service Layer
# This script generates secure random passwords and keys for production use

echo "Neo Service Layer - Secure Credential Generator"
echo "=============================================="
echo ""
echo "This script will generate secure credentials for your .env file."
echo "Copy these values to your .env file before running the services."
echo ""

# Function to generate secure random password
generate_password() {
    openssl rand -base64 32 | tr -d "=+/" | cut -c1-25
}

# Function to generate secure JWT key
generate_jwt_key() {
    openssl rand -base64 64 | tr -d "\n"
}

echo "# Generated Secure Credentials"
echo "# Generated on: $(date)"
echo ""
echo "# JWT Secret Key (64 bytes base64)"
echo "JWT_SECRET_KEY=$(generate_jwt_key)"
echo ""
echo "# Database Passwords"
echo "POSTGRES_PASSWORD=$(generate_password)"
echo "REDIS_PASSWORD=$(generate_password)"
echo ""
echo "# Service Passwords"
echo "RABBITMQ_USER=neouser"
echo "RABBITMQ_PASSWORD=$(generate_password)"
echo "GRAFANA_PASSWORD=$(generate_password)"
echo ""
echo "# Configuration Encryption Key"
echo "CONFIG_ENCRYPTION_KEY=$(generate_jwt_key)"
echo ""
echo "=============================================="
echo "IMPORTANT: Save these credentials securely!"
echo "Never commit them to version control."
echo "=============================================="