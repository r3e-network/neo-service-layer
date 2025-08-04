#!/bin/bash

# Test building services locally
set -e

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

echo "Testing local build..."

# Build the API project directly
echo "Building API Gateway..."
if dotnet build src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj -c Release; then
    echo -e "${GREEN}✓ API Gateway build successful${NC}"
else
    echo -e "${RED}✗ API Gateway build failed${NC}"
    exit 1
fi

echo ""
echo "Building Smart Contracts Service..."
if dotnet build src/Services/NeoServiceLayer.Services.SmartContracts/NeoServiceLayer.Services.SmartContracts.csproj -c Release; then
    echo -e "${GREEN}✓ Smart Contracts Service build successful${NC}"
else
    echo -e "${RED}✗ Smart Contracts Service build failed${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}All builds successful!${NC}"