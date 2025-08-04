#!/bin/bash
set -e

echo "🔨 Building Neo Service Layer Docker Image..."
echo "=============================================="

# Check if we're in the right directory
if [ ! -f "src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj" ]; then
    echo "❌ Error: Please run this script from the project root directory"
    exit 1
fi

echo "📦 Building .NET application..."
dotnet publish src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj \
    -c Release \
    -o src/Api/NeoServiceLayer.Api/bin/Release/net9.0/publish/ \
    --verbosity minimal

if [ $? -ne 0 ]; then
    echo "❌ Failed to build .NET application"
    exit 1
fi

echo "🐳 Building Docker image..."
sudo docker build -f docker/Dockerfile.minimal -t neo-service-layer:latest .

if [ $? -ne 0 ]; then
    echo "❌ Failed to build Docker image"
    exit 1
fi

echo "✅ Docker image built successfully!"
echo ""
echo "🚀 To run the container:"
echo "   Development: ./scripts/run-docker-dev.sh"
echo "   Manual:      sudo docker run -d -p 8080:5000 \\"
echo "                  -e ASPNETCORE_ENVIRONMENT=Development \\"
echo "                  -e ASPNETCORE_URLS=\"http://+:5000\" \\"
echo "                  -e JWT_SECRET_KEY=\"your-secure-jwt-secret-key-at-least-32-characters-long\" \\"
echo "                  --name neo-service-layer \\"
echo "                  neo-service-layer:latest"
echo ""
echo "📋 Available endpoints after running:"
echo "   Health:     http://localhost:8080/health"
echo "   API Info:   http://localhost:8080/api/info"
echo "   Swagger UI: http://localhost:8080 (Development mode only)"