#!/bin/bash
set -e

echo "🚀 Starting Neo Service Layer in Development Mode..."
echo "===================================================="

# Configuration
CONTAINER_NAME="neo-service-layer"
IMAGE_NAME="jinghuiliao/neo-service-layer:latest"
HOST_PORT="8080"
CONTAINER_PORT="5000"
JWT_SECRET="development-jwt-secret-key-for-testing-only-32chars"

# Pull latest image from Docker Hub
echo "📥 Pulling latest image from Docker Hub..."
if ! sudo docker pull $IMAGE_NAME; then
    echo "❌ Failed to pull Docker image '$IMAGE_NAME' from Docker Hub!"
    echo "🔨 Please check your internet connection or build locally:"
    echo "   ./scripts/build-docker.sh"
    exit 1
fi

# Stop and remove existing container if it exists
echo "🧹 Cleaning up existing container..."
sudo docker stop $CONTAINER_NAME 2>/dev/null || true
sudo docker rm $CONTAINER_NAME 2>/dev/null || true

# Check if port is available
if sudo netstat -tlnp | grep ":$HOST_PORT " >/dev/null 2>&1; then
    echo "⚠️  Warning: Port $HOST_PORT is already in use"
    echo "   The container may fail to start if another service is using this port"
fi

echo "🐳 Starting container..."
sudo docker run -d \
    --name $CONTAINER_NAME \
    -p $HOST_PORT:$CONTAINER_PORT \
    -e ASPNETCORE_ENVIRONMENT=Development \
    -e ASPNETCORE_URLS="http://+:$CONTAINER_PORT" \
    -e JWT_SECRET_KEY="$JWT_SECRET" \
    $IMAGE_NAME

if [ $? -ne 0 ]; then
    echo "❌ Failed to start container"
    exit 1
fi

echo "⏳ Waiting for application to start..."
sleep 5

# Health check
echo "🏥 Performing health check..."
for i in {1..12}; do
    if curl -s -f http://localhost:$HOST_PORT/health >/dev/null 2>&1; then
        echo "✅ Application is healthy!"
        break
    fi
    if [ $i -eq 12 ]; then
        echo "❌ Health check failed after 60 seconds"
        echo "📋 Container logs:"
        sudo docker logs $CONTAINER_NAME
        exit 1
    fi
    echo "   Attempt $i/12 - waiting..."
    sleep 5
done

echo ""
echo "🎉 Neo Service Layer is running successfully!"
echo "=============================================="
echo "📍 Container: $CONTAINER_NAME"
echo "🌐 Base URL:  http://localhost:$HOST_PORT"
echo ""
echo "📋 Available endpoints:"
echo "   Health Check: http://localhost:$HOST_PORT/health"
echo "   API Info:     http://localhost:$HOST_PORT/api/info"
echo "   Swagger UI:   http://localhost:$HOST_PORT"
echo ""
echo "🔧 Management commands:"
echo "   View logs:    sudo docker logs $CONTAINER_NAME"
echo "   Follow logs:  sudo docker logs -f $CONTAINER_NAME"
echo "   Stop:         sudo docker stop $CONTAINER_NAME"
echo "   Restart:      sudo docker restart $CONTAINER_NAME"
echo "   Remove:       sudo docker rm $CONTAINER_NAME"
echo ""
echo "🧪 Quick tests:"
echo "   curl http://localhost:$HOST_PORT/health"
echo "   curl http://localhost:$HOST_PORT/api/info"

# Test the endpoints
echo ""
echo "🧪 Testing endpoints..."
echo "Health: $(curl -s http://localhost:$HOST_PORT/health)"
echo "Info: $(curl -s http://localhost:$HOST_PORT/api/info | jq -r '.name + " v" + .version + " (" + .environment + ")"' 2>/dev/null || curl -s http://localhost:$HOST_PORT/api/info)"