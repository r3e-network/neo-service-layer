#!/bin/bash
# 🚀 Neo Service Layer - DevContainer Verification & Setup
# Run this script immediately after devcontainer loads

echo "🔍 Verifying DevContainer Environment..."

# 1. Verify we're in the container
echo "📍 Current environment:"
cat /etc/os-release | grep "Ubuntu 24.04" && echo "✅ Ubuntu 24.04 detected" || echo "❌ Not Ubuntu 24.04"

# 2. Check working directory
echo "📁 Working directory: $(pwd)"
if [[ "$(pwd)" == *"workspaces/neo-service-layer"* ]]; then
    echo "✅ Correct working directory"
else
    echo "⚠️  Expected /workspaces/neo-service-layer, got $(pwd)"
fi

# 3. Verify .NET installation
echo "🔧 .NET SDK version:"
dotnet --info | head -5

# 4. Verify SGX SDK
echo "🔒 Intel SGX SDK:"
if source /opt/intel/sgxsdk/environment 2>/dev/null; then
    echo "✅ SGX SDK Ready - Simulation mode available"
else
    echo "⚠️  SGX SDK not properly configured"
fi

# 5. Verify Rust installation
echo "🦀 Rust version:"
rustc --version && echo "✅ Rust ready" || echo "❌ Rust not found"

# 6. Check project structure
echo "📂 Project structure:"
ls -la src/Web/NeoServiceLayer.Web/ | head -5

# 7. Test basic build
echo "🔨 Testing basic build..."
if dotnet build src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj --verbosity quiet; then
    echo "✅ Build successful!"
else
    echo "❌ Build failed - checking errors..."
    dotnet build src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj --verbosity normal
fi

# 8. List available disabled services
echo "📋 Available services to enable:"
ls src/Web/NeoServiceLayer.Web/Controllers/*.disabled 2>/dev/null | sed 's/.*\///g' | sed 's/Controller.cs.disabled//g' | sort | head -10

# 9. Create logs directory
mkdir -p logs
echo "📝 Logs directory created"

echo ""
echo "🎉 DevContainer verification complete!"
echo ""
echo "🚀 Next steps:"
echo "1. Run: ./start-dev.sh                    # Start the web application"
echo "2. Open: http://localhost:5000            # Test the web interface"
echo "3. Run: ./enable-service.sh Health        # Enable first service"
echo "4. Open: http://localhost:5000/swagger    # Check API documentation"
echo "" 