#!/bin/bash

# Quick deployment script for Neo Service Layer
# This script automates the production deployment process

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration
DEPLOYMENT_MODE="${1:-docker}"  # docker or k8s
ENVIRONMENT="${2:-production}"

echo -e "${YELLOW}Neo Service Layer - Quick Deploy${NC}"
echo "Deployment Mode: $DEPLOYMENT_MODE"
echo "Environment: $ENVIRONMENT"
echo ""

# Step 1: Verify prerequisites
echo -e "${YELLOW}Step 1: Verifying prerequisites...${NC}"

if [ ! -f ".env.production" ]; then
    echo -e "${RED}Error: .env.production not found${NC}"
    echo "Run: ./scripts/generate-secure-credentials.sh"
    exit 1
fi

if [ ! -f "certificates/certificate.pfx" ]; then
    echo -e "${YELLOW}Warning: SSL certificate not found${NC}"
    echo "Generating self-signed certificate for testing..."
    bash scripts/generate-self-signed-cert.sh
fi

# Step 2: Build services
echo -e "${YELLOW}Step 2: Building services...${NC}"

if [ "$DEPLOYMENT_MODE" == "docker" ]; then
    docker compose -f docker compose.production.yml build --parallel
elif [ "$DEPLOYMENT_MODE" == "k8s" ]; then
    # Build and push images for k8s
    echo "Building images for Kubernetes..."
    # Add your container registry and build commands here
fi

# Step 3: Database migrations
echo -e "${YELLOW}Step 3: Running database migrations...${NC}"

if [ "$DEPLOYMENT_MODE" == "docker" ]; then
    docker compose -f docker compose.production.yml run --rm api-gateway \
        dotnet ef database update --project src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence
fi

# Step 4: Deploy services
echo -e "${YELLOW}Step 4: Deploying services...${NC}"

if [ "$DEPLOYMENT_MODE" == "docker" ]; then
    docker compose -f docker compose.production.yml up -d
    
    # Wait for services to be healthy
    echo "Waiting for services to be healthy..."
    sleep 30
    
    # Check health
    docker compose -f docker compose.production.yml ps
    
elif [ "$DEPLOYMENT_MODE" == "k8s" ]; then
    kubectl apply -f k8s/namespace.yaml
    kubectl apply -f k8s/configmaps/
    kubectl apply -f k8s/secrets/
    kubectl apply -f k8s/services/
    kubectl apply -f k8s/deployments/
    kubectl apply -f k8s/ingress/
    
    # Wait for rollout
    kubectl rollout status deployment/api-gateway -n neo-service-layer
fi

# Step 5: Verify deployment
echo -e "${YELLOW}Step 5: Verifying deployment...${NC}"

# Check API health
if [ "$DEPLOYMENT_MODE" == "docker" ]; then
    API_URL="http://localhost"
else
    API_URL=$(kubectl get ingress -n neo-service-layer -o jsonpath='{.items[0].spec.rules[0].host}')
    API_URL="https://$API_URL"
fi

# Wait for API to be ready
MAX_ATTEMPTS=30
ATTEMPT=0

while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    if curl -s "$API_URL/health" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ API is healthy${NC}"
        break
    fi
    
    echo "Waiting for API to be ready... ($((ATTEMPT+1))/$MAX_ATTEMPTS)"
    sleep 5
    ATTEMPT=$((ATTEMPT+1))
done

if [ $ATTEMPT -eq $MAX_ATTEMPTS ]; then
    echo -e "${RED}✗ API health check failed${NC}"
    exit 1
fi

# Step 6: Post-deployment tasks
echo -e "${YELLOW}Step 6: Running post-deployment tasks...${NC}"

# Deploy smart contracts (if not already deployed)
if [ -f "contracts-neo-n3/deploy-contracts.sh" ]; then
    echo "Deploying smart contracts..."
    # bash contracts-neo-n3/deploy-contracts.sh
fi

# Summary
echo ""
echo -e "${GREEN}=== Deployment Complete ===${NC}"
echo ""
echo "Services deployed successfully!"
echo "API URL: $API_URL"
echo ""
echo "Next steps:"
echo "1. Update DNS records to point to your server"
echo "2. Configure external services in .env.production"
echo "3. Monitor services: docker compose -f docker compose.production.yml logs -f"
echo "4. Check metrics: http://localhost:3000 (Grafana)"
echo ""
echo -e "${YELLOW}Remember to:${NC}"
echo "- Set up backups"
echo "- Configure monitoring alerts"
echo "- Review security settings"
echo "- Test all functionality"