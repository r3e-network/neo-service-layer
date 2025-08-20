#!/bin/bash
# Neo Service Layer - Production Secrets Setup Script

set -e

echo "🔐 Neo Service Layer - Production Secrets Setup"
echo "=============================================="
echo ""

# Check if running as root
if [ "$EUID" -eq 0 ]; then 
   echo "❌ Please do not run this script as root"
   exit 1
fi

# Function to generate secure random string
generate_secret() {
    openssl rand -base64 32 | tr -d "=+/" | cut -c1-32
}

# Function to prompt for secret with validation
prompt_secret() {
    local var_name=$1
    local description=$2
    local default_value=$3
    local is_password=${4:-false}
    
    echo -n "Enter $description"
    if [ -n "$default_value" ] && [ "$is_password" = "false" ]; then
        echo -n " [$default_value]"
    fi
    echo -n ": "
    
    if [ "$is_password" = "true" ]; then
        read -s value
        echo ""
    else
        read value
    fi
    
    if [ -z "$value" ] && [ -n "$default_value" ]; then
        value=$default_value
    fi
    
    if [ -z "$value" ]; then
        echo "❌ $description cannot be empty"
        exit 1
    fi
    
    echo "export $var_name='$value'" >> .env.production
}

# Create .env.production file
echo "# Neo Service Layer Production Environment Variables" > .env.production
echo "# Generated on $(date)" >> .env.production
echo "" >> .env.production

echo "🔑 Generating secure secrets..."
echo ""

# JWT Secret
JWT_SECRET=$(generate_secret)
echo "export JWT_SECRET_KEY='$JWT_SECRET'" >> .env.production
echo "✅ Generated JWT secret key"

# Database Configuration
echo ""
echo "📊 Database Configuration"
prompt_secret "DB_HOST" "Database host" "localhost"
prompt_secret "DB_PORT" "Database port" "5432"
prompt_secret "DB_NAME" "Database name" "neoservicelayer"
prompt_secret "DB_USER" "Database user" "neoservice_app"
prompt_secret "DB_PASSWORD" "Database password" "" true

# Redis Configuration
echo ""
echo "💾 Redis Configuration"
prompt_secret "REDIS_HOST" "Redis host" "localhost"
prompt_secret "REDIS_PORT" "Redis port" "6379"
REDIS_PASSWORD=$(generate_secret)
echo "export REDIS_PASSWORD='$REDIS_PASSWORD'" >> .env.production
echo "✅ Generated Redis password"

# Certificate Configuration
echo ""
echo "🔐 SSL/TLS Configuration"
CERT_PASSWORD=$(generate_secret)
echo "export CERT_PASSWORD='$CERT_PASSWORD'" >> .env.production
echo "✅ Generated certificate password"

# AWS Configuration (optional)
echo ""
echo "☁️  AWS Configuration (press Enter to skip if not using AWS)"
read -p "Configure AWS S3 storage? (y/N): " configure_aws
if [[ $configure_aws =~ ^[Yy]$ ]]; then
    prompt_secret "AWS_REGION" "AWS Region" "us-east-1"
    prompt_secret "S3_BUCKET_NAME" "S3 Bucket name" "neo-service-layer-production"
    prompt_secret "AWS_ACCESS_KEY_ID" "AWS Access Key ID" ""
    prompt_secret "AWS_SECRET_ACCESS_KEY" "AWS Secret Access Key" "" true
fi

# Email Configuration (optional)
echo ""
echo "📧 Email Configuration (press Enter to skip if not using email)"
read -p "Configure email notifications? (y/N): " configure_email
if [[ $configure_email =~ ^[Yy]$ ]]; then
    prompt_secret "SMTP_SERVER" "SMTP Server" "smtp.sendgrid.net"
    prompt_secret "SMTP_PORT" "SMTP Port" "587"
    prompt_secret "SMTP_USERNAME" "SMTP Username" "apikey"
    prompt_secret "SMTP_PASSWORD" "SMTP Password (API Key)" "" true
    prompt_secret "NOTIFICATION_FROM_EMAIL" "From Email" "noreply@neo-service-layer.io"
fi

# Monitoring Configuration (optional)
echo ""
echo "📊 Monitoring Configuration (press Enter to skip)"
read -p "Configure OpenTelemetry monitoring? (y/N): " configure_monitoring
if [[ $configure_monitoring =~ ^[Yy]$ ]]; then
    prompt_secret "OTEL_EXPORTER_ENDPOINT" "OpenTelemetry Endpoint" "http://localhost:4317"
    prompt_secret "OTEL_API_KEY" "OpenTelemetry API Key" "" true
fi

# Additional configurations
echo "" >> .env.production
echo "# Additional Configuration" >> .env.production
echo "export JWT_ISSUER='https://neo-service-layer.io'" >> .env.production
echo "export JWT_AUDIENCE='https://neo-service-layer.io/api'" >> .env.production
echo "export CORS_ALLOWED_ORIGINS='https://app.neo-service-layer.io'" >> .env.production
echo "export NEO_N3_RPC='https://seed1.neo.org:10331'" >> .env.production
echo "export NEO_X_RPC='https://mainnet.neo-x.org'" >> .env.production

# Set file permissions
chmod 600 .env.production

echo ""
echo "✅ Production secrets have been generated and saved to .env.production"
echo ""
echo "⚠️  IMPORTANT SECURITY REMINDERS:"
echo "   1. Never commit .env.production to source control"
echo "   2. Store a backup of this file in a secure location"
echo "   3. Use a proper secret management system in production (Azure Key Vault, AWS Secrets Manager, etc.)"
echo "   4. Rotate secrets regularly"
echo ""
echo "🚀 To use these secrets:"
echo "   - Docker: docker-compose --env-file .env.production up"
echo "   - Kubernetes: Use the apply-secrets.sh script to create K8s secrets"
echo "   - Local: source .env.production"
echo ""

# Optionally create Kubernetes secrets
read -p "Generate Kubernetes secrets YAML? (y/N): " generate_k8s
if [[ $generate_k8s =~ ^[Yy]$ ]]; then
    # Source the environment file
    source .env.production
    
    # Replace variables in template
    envsubst < k8s/secret-production.yaml > k8s/secret-production-filled.yaml
    
    echo "✅ Kubernetes secrets YAML generated at k8s/secret-production-filled.yaml"
    echo "   Apply with: kubectl apply -f k8s/secret-production-filled.yaml"
fi

echo ""
echo "🎉 Setup complete!"