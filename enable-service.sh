#!/bin/bash
# 🚀 Neo Service Layer - Service Enablement Script
# Usage: ./enable-service.sh <ServiceName>

set -e  # Exit on any error

SERVICE_NAME=$1
if [ -z "$SERVICE_NAME" ]; then
  echo "❌ Usage: ./enable-service.sh <ServiceName>"
  echo "📋 Available services:"
  ls src/Web/NeoServiceLayer.Web/Controllers/*.disabled 2>/dev/null | sed 's/.*\///g' | sed 's/Controller.cs.disabled//g' | sort
  exit 1
fi

echo "🔧 Enabling $SERVICE_NAME service..."

# Check if disabled controller exists
CONTROLLER_PATH="src/Web/NeoServiceLayer.Web/Controllers/${SERVICE_NAME}Controller.cs"
DISABLED_PATH="${CONTROLLER_PATH}.disabled"

if [ ! -f "$DISABLED_PATH" ]; then
  echo "⚠️  $DISABLED_PATH not found"
  echo "📋 Available disabled services:"
  ls src/Web/NeoServiceLayer.Web/Controllers/*.disabled 2>/dev/null | sed 's/.*\///g' | sed 's/Controller.cs.disabled//g' | sort
  exit 1
fi

# Backup current state
echo "💾 Creating backup..."
cp "$DISABLED_PATH" "${DISABLED_PATH}.backup"

# Enable controller
echo "✅ Enabling ${SERVICE_NAME}Controller..."
mv "$DISABLED_PATH" "$CONTROLLER_PATH"

# Test build
echo "🔨 Building project..."
if dotnet build src/Web/NeoServiceLayer.Web/ --no-restore --verbosity quiet; then
  echo "✅ Build successful for $SERVICE_NAME"
  
  # Test run (quick check)
  echo "🧪 Testing application startup..."
  timeout 30s dotnet run --project src/Web/NeoServiceLayer.Web/ --no-build --urls "http://localhost:5001" > /dev/null 2>&1 &
  PID=$!
  
  sleep 5
  if kill -0 $PID 2>/dev/null; then
    echo "✅ Application startup successful"
    kill $PID 2>/dev/null || true
    wait $PID 2>/dev/null || true
    
    echo "🎉 $SERVICE_NAME service enabled successfully!"
    echo "📝 Next steps:"
    echo "   1. Uncomment service registration in Program.cs if needed"
    echo "   2. Test the service at: http://localhost:5000/api/v1/${SERVICE_NAME,,}/health"
    echo "   3. Check the web interface: http://localhost:5000"
    
  else
    echo "❌ Application failed to start"
    echo "🔄 Rolling back changes..."
    mv "$CONTROLLER_PATH" "$DISABLED_PATH"
    mv "${DISABLED_PATH}.backup" "${DISABLED_PATH}.temp" 2>/dev/null || true
    echo "❌ $SERVICE_NAME service enablement failed - rolled back"
    exit 1
  fi
  
else
  echo "❌ Build failed for $SERVICE_NAME"
  echo "🔄 Rolling back changes..."
  mv "$CONTROLLER_PATH" "$DISABLED_PATH"
  mv "${DISABLED_PATH}.backup" "${DISABLED_PATH}.temp" 2>/dev/null || true
  
  echo "📋 Build errors:"
  dotnet build src/Web/NeoServiceLayer.Web/ --verbosity normal | grep -E "(error|Error)" || echo "Check detailed logs above"
  
  echo "❌ $SERVICE_NAME service enablement failed - rolled back"
  exit 1
fi 