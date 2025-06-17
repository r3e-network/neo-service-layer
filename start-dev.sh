#!/bin/bash
# 🚀 Neo Service Layer - Development Startup Script
# Works in both devcontainer and local environments

set -e

echo "🚀 Starting Neo Service Layer Web Application..."

# Check if we're in devcontainer or local environment
if [ -f "/.dockerenv" ]; then
    echo "📦 Running in DevContainer"
    ENVIRONMENT="devcontainer"
else
    echo "💻 Running on Local Machine"
    ENVIRONMENT="local"
fi

# Function to check if port is available
check_port() {
    local port=$1
    if command -v netstat >/dev/null 2>&1; then
        if netstat -tulpn 2>/dev/null | grep ":$port " >/dev/null; then
            return 1
        fi
    elif command -v lsof >/dev/null 2>&1; then
        if lsof -i :$port >/dev/null 2>&1; then
            return 1
        fi
    fi
    return 0
}

# Check and kill existing processes on required ports
for port in 5000 5001; do
    if ! check_port $port; then
        echo "⚠️  Port $port is in use. Attempting to free it..."
        if [ "$ENVIRONMENT" = "devcontainer" ]; then
            pkill -f "dotnet.*NeoServiceLayer.Web" || true
            sleep 2
        else
            echo "❌ Port $port is busy. Please stop the conflicting process manually."
            exit 1
        fi
    fi
done

# Restore packages if needed
if [ ! -d "src/Web/NeoServiceLayer.Web/bin" ] || [ ! -d "src/Web/NeoServiceLayer.Web/obj" ]; then
    echo "📦 Restoring NuGet packages..."
    dotnet restore src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj
fi

# Build the application
echo "🔨 Building application..."
if ! dotnet build src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj --configuration Debug --no-restore; then
    echo "❌ Build failed! Please check the errors above."
    exit 1
fi

# Create logs directory if it doesn't exist
mkdir -p logs

# Set environment variables
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="http://localhost:5000;https://localhost:5001"

# Start the application
echo "🌐 Starting web application at:"
echo "   HTTP:  http://localhost:5000"
echo "   HTTPS: https://localhost:5001"
echo ""
echo "📊 Available endpoints:"
echo "   🏠 Home:           http://localhost:5000"
echo "   🔧 Services:       http://localhost:5000/Services"
echo "   📊 Dashboard:      http://localhost:5000/Dashboard"
echo "   🧪 Demo:           http://localhost:5000/Demo"
echo "   🔍 Health Check:   http://localhost:5000/health"
echo "   📚 API Docs:       http://localhost:5000/swagger"
echo "   🔑 Demo Token:     http://localhost:5000/api/auth/demo-token"
echo "   ℹ️  Info:          http://localhost:5000/api/info"
echo ""
echo "⌨️  Press Ctrl+C to stop the server"
echo ""

# Start the application with appropriate settings
if [ "$ENVIRONMENT" = "devcontainer" ]; then
    # In devcontainer, run with specific configuration
    dotnet run --project src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj \
        --configuration Debug \
        --no-build \
        --urls "http://0.0.0.0:5000;https://0.0.0.0:5001" \
        --environment Development
else
    # On local machine, use standard localhost
    dotnet run --project src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj \
        --configuration Debug \
        --no-build \
        --environment Development
fi 