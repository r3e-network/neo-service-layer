#!/bin/bash
# Neo Service Layer - Secure Secrets Generation Script
# This script generates secure random secrets for Kubernetes deployments

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}Neo Service Layer - Secrets Generation Script${NC}"
echo "============================================="

# Function to generate secure random string
generate_secret() {
    local length=$1
    openssl rand -base64 $length | tr -d "=+/" | cut -c1-$length
}

# Function to generate hex string
generate_hex() {
    local length=$1
    openssl rand -hex $((length/2))
}

# Check if openssl is installed
if ! command -v openssl &> /dev/null; then
    echo -e "${RED}Error: openssl is required but not installed${NC}"
    exit 1
fi

# Create secrets directory if not exists
mkdir -p k8s/secrets/generated

# Generate secrets
echo -e "${YELLOW}Generating secure secrets...${NC}"

# Database password (32 chars)
DB_PASSWORD=$(generate_secret 32)
echo "Database password generated"

# JWT Secret (64 chars for production)
JWT_SECRET_PROD=$(generate_secret 64)
JWT_SECRET_STAGING=$(generate_secret 32)
echo "JWT secrets generated"

# Azure credentials (UUID format)
AZURE_CLIENT_ID_PROD=$(uuidgen || generate_hex 32)
AZURE_CLIENT_SECRET_PROD=$(generate_secret 32)
AZURE_CLIENT_ID_STAGING=$(uuidgen || generate_hex 32)
AZURE_CLIENT_SECRET_STAGING=$(generate_secret 32)
echo "Azure credentials generated"

# Neo private keys (64 hex chars)
NEO_PRIVATE_KEY_PROD=$(generate_hex 64)
NEO_PRIVATE_KEY_STAGING=$(generate_hex 64)
echo "Neo private keys generated"

# Monitoring passwords
GRAFANA_PASSWORD_PROD=$(generate_secret 16)
GRAFANA_PASSWORD_STAGING=$(generate_secret 16)
PROMETHEUS_AUTH_PROD=$(echo -n "admin:$(generate_secret 16)" | base64)
PROMETHEUS_AUTH_STAGING=$(echo -n "admin:$(generate_secret 16)" | base64)
echo "Monitoring credentials generated"

# Internal API keys
INTERNAL_API_KEY_PROD=$(generate_secret 48)
INTERNAL_API_KEY_STAGING=$(generate_secret 48)
echo "Internal API keys generated"

# Generate TLS certificates (self-signed for now, replace with proper CA in production)
echo -e "${YELLOW}Generating TLS certificates...${NC}"
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout k8s/secrets/generated/tls.key \
    -out k8s/secrets/generated/tls.crt \
    -subj "/C=US/ST=State/L=City/O=NeoServiceLayer/CN=*.neo-service-layer.local" \
    2>/dev/null

TLS_CERT=$(base64 -w 0 < k8s/secrets/generated/tls.crt)
TLS_KEY=$(base64 -w 0 < k8s/secrets/generated/tls.key)

# Generate service mesh CA
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout k8s/secrets/generated/ca.key \
    -out k8s/secrets/generated/ca.crt \
    -subj "/C=US/ST=State/L=City/O=NeoServiceLayer/CN=ServiceMeshCA" \
    2>/dev/null

SERVICE_MESH_CA=$(base64 -w 0 < k8s/secrets/generated/ca.crt)

# Create production secrets file
cat > k8s/secrets/generated/production-secrets.yaml <<EOF
apiVersion: v1
kind: Secret
metadata:
  name: neo-secrets
  namespace: neo-service-layer
type: Opaque
stringData:
  # Database connections
  db-connection: "Host=postgres-prod;Database=neo_service_layer;Username=neo_user;Password=${DB_PASSWORD}"
  redis-connection: "redis-prod:6379,password=${DB_PASSWORD}"
  
  # JWT configuration
  jwt-secret: "${JWT_SECRET_PROD}"
  jwt-issuer: "neo-service-layer-production"
  jwt-audience: "neo-service-layer-api"
  
  # External service keys
  azure-key-vault-uri: "https://neo-prod-kv.vault.azure.net/"
  azure-client-id: "${AZURE_CLIENT_ID_PROD}"
  azure-client-secret: "${AZURE_CLIENT_SECRET_PROD}"
  
  # Neo blockchain configuration (MainNet)
  neo-rpc-endpoint: "https://mainnet1-seed.neo.org:10332"
  neo-network-magic: "860833102"
  neo-private-key: "${NEO_PRIVATE_KEY_PROD}"
  
  # Monitoring and observability
  grafana-admin-password: "${GRAFANA_PASSWORD_PROD}"
  prometheus-basic-auth: "${PROMETHEUS_AUTH_PROD}"
  
  # Service-to-service communication
  internal-api-key: "${INTERNAL_API_KEY_PROD}"
  service-mesh-ca-cert: "${SERVICE_MESH_CA}"
  
  # TLS certificates
  tls-cert: "${TLS_CERT}"
  tls-key: "${TLS_KEY}"
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: neo-config
  namespace: neo-service-layer
data:
  appsettings.json: |
    {
      "Logging": {
        "LogLevel": {
          "Default": "Warning",
          "Microsoft.AspNetCore": "Error",
          "Neo.ServiceLayer": "Information"
        }
      },
      "Neo": {
        "Network": "MainNet",
        "MaxConnections": 50,
        "BlockchainSyncEnabled": true,
        "TransactionPoolSize": 1000
      },
      "RateLimiting": {
        "RequestsPerMinute": 10000,
        "BurstAllowance": 1000,
        "StrictMode": true
      },
      "HealthChecks": {
        "CheckIntervalSeconds": 15,
        "TimeoutSeconds": 5
      },
      "ServiceDiscovery": {
        "ConsulEnabled": true,
        "ConsulAddress": "http://consul:8500"
      },
      "Security": {
        "RequireHttps": true,
        "HstsMaxAge": 31536000,
        "ContentSecurityPolicy": "default-src 'self'"
      }
    }
EOF

# Create staging secrets file
cat > k8s/secrets/generated/staging-secrets.yaml <<EOF
apiVersion: v1
kind: Secret
metadata:
  name: neo-secrets
  namespace: neo-service-layer-staging
type: Opaque
stringData:
  # Database connections
  db-connection: "Host=postgres-staging;Database=neo_service_layer;Username=neo_user;Password=${DB_PASSWORD}"
  redis-connection: "redis-staging:6379,password=${DB_PASSWORD}"
  
  # JWT configuration
  jwt-secret: "${JWT_SECRET_STAGING}"
  jwt-issuer: "neo-service-layer-staging"
  jwt-audience: "neo-service-layer-api"
  
  # External service keys
  azure-key-vault-uri: "https://neo-staging-kv.vault.azure.net/"
  azure-client-id: "${AZURE_CLIENT_ID_STAGING}"
  azure-client-secret: "${AZURE_CLIENT_SECRET_STAGING}"
  
  # Neo blockchain configuration
  neo-rpc-endpoint: "https://testnet1-seed.neo.org:20332"
  neo-network-magic: "827601742"
  neo-private-key: "${NEO_PRIVATE_KEY_STAGING}"
  
  # Monitoring and observability
  grafana-admin-password: "${GRAFANA_PASSWORD_STAGING}"
  prometheus-basic-auth: "${PROMETHEUS_AUTH_STAGING}"
  
  # Service-to-service communication
  internal-api-key: "${INTERNAL_API_KEY_STAGING}"
  service-mesh-ca-cert: "${SERVICE_MESH_CA}"
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: neo-config
  namespace: neo-service-layer-staging
data:
  appsettings.json: |
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "Neo": {
        "Network": "TestNet",
        "MaxConnections": 10,
        "BlockchainSyncEnabled": true
      },
      "RateLimiting": {
        "RequestsPerMinute": 1000,
        "BurstAllowance": 100
      },
      "HealthChecks": {
        "CheckIntervalSeconds": 30,
        "TimeoutSeconds": 10
      },
      "ServiceDiscovery": {
        "ConsulEnabled": true,
        "ConsulAddress": "http://consul:8500"
      }
    }
EOF

# Save secrets to secure file (for backup - protect this file!)
cat > k8s/secrets/generated/.secrets-backup <<EOF
# Neo Service Layer - Generated Secrets Backup
# PROTECT THIS FILE - DO NOT COMMIT TO GIT
# Generated on: $(date)

## Production Secrets
DB_PASSWORD_PROD=${DB_PASSWORD}
JWT_SECRET_PROD=${JWT_SECRET_PROD}
AZURE_CLIENT_ID_PROD=${AZURE_CLIENT_ID_PROD}
AZURE_CLIENT_SECRET_PROD=${AZURE_CLIENT_SECRET_PROD}
NEO_PRIVATE_KEY_PROD=${NEO_PRIVATE_KEY_PROD}
GRAFANA_PASSWORD_PROD=${GRAFANA_PASSWORD_PROD}
INTERNAL_API_KEY_PROD=${INTERNAL_API_KEY_PROD}

## Staging Secrets
JWT_SECRET_STAGING=${JWT_SECRET_STAGING}
AZURE_CLIENT_ID_STAGING=${AZURE_CLIENT_ID_STAGING}
AZURE_CLIENT_SECRET_STAGING=${AZURE_CLIENT_SECRET_STAGING}
NEO_PRIVATE_KEY_STAGING=${NEO_PRIVATE_KEY_STAGING}
GRAFANA_PASSWORD_STAGING=${GRAFANA_PASSWORD_STAGING}
INTERNAL_API_KEY_STAGING=${INTERNAL_API_KEY_STAGING}
EOF

chmod 600 k8s/secrets/generated/.secrets-backup

# Add to .gitignore
echo -e "\n# Generated secrets - DO NOT COMMIT" >> .gitignore
echo "k8s/secrets/generated/" >> .gitignore
echo ".secrets-backup" >> .gitignore

echo -e "${GREEN}✓ Secrets generated successfully!${NC}"
echo -e "${YELLOW}Files created:${NC}"
echo "  - k8s/secrets/generated/production-secrets.yaml"
echo "  - k8s/secrets/generated/staging-secrets.yaml"
echo "  - k8s/secrets/generated/.secrets-backup (KEEP SECURE!)"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Review the generated secrets"
echo "2. Apply to Kubernetes: kubectl apply -f k8s/secrets/generated/production-secrets.yaml"
echo "3. Store the .secrets-backup file in a secure location"
echo "4. Delete local copies after applying to cluster"