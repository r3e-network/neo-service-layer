#!/bin/bash
set -e

echo "🔍 Neo Service Layer DevContainer Validation"
echo "============================================="
echo ""

# Initialize counters
PASSED=0
FAILED=0
WARNINGS=0

# Function to test and report
test_component() {
    local name="$1"
    local command="$2"
    local expected="$3"
    
    echo -n "Testing $name... "
    
    if eval "$command" >/dev/null 2>&1; then
        echo "✅ PASSED"
        ((PASSED++))
    else
        echo "❌ FAILED"
        ((FAILED++))
        if [ ! -z "$expected" ]; then
            echo "   Expected: $expected"
        fi
    fi
}

test_warning() {
    local name="$1"
    local command="$2"
    local message="$3"
    
    echo -n "Checking $name... "
    
    if eval "$command" >/dev/null 2>&1; then
        echo "✅ OK"
        ((PASSED++))
    else
        echo "⚠️  WARNING"
        ((WARNINGS++))
        if [ ! -z "$message" ]; then
            echo "   Note: $message"
        fi
    fi
}

echo "🔧 Core System Components"
echo "------------------------"

# Test .NET installation
test_component ".NET 9.0 SDK" "dotnet --version | grep -q '9\.0'"

# Test Rust installation
test_component "Rust compiler" "rustc --version"

# Test Cargo
test_component "Cargo package manager" "cargo --version"

# Test Node.js
test_component "Node.js runtime" "node --version"

# Test Git
test_component "Git version control" "git --version"

echo ""
echo "🔐 SGX and Security Components"
echo "------------------------------"

# Test SGX SDK
test_warning "SGX SDK installation" "test -d /opt/intel/sgxsdk" "SGX SDK may not be fully installed"

# Test SGX environment script
test_warning "SGX environment script" "test -f /opt/intel/sgxsdk/environment" "SGX environment setup may be incomplete"

# Test Occlum installation
test_warning "Occlum LibOS" "test -d /opt/occlum" "Occlum may not be installed"

# Test Occlum bashrc
test_warning "Occlum environment" "test -f /opt/occlum/build/bin/occlum_bashrc" "Occlum environment may be incomplete"

echo ""
echo "📦 Neo Service Layer Components"
echo "-------------------------------"

# Test project structure
test_component "Source directory" "test -d /workspace/src"

# Test web project
test_component "Web project" "test -f /workspace/src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj"

# Test services directory
test_component "Services directory" "test -d /workspace/src/Services"

# Test TEE directory
test_component "TEE directory" "test -d /workspace/src/Tee"

# Test startup script
test_component "Startup script" "test -x /workspace/start-dev.sh"

# Test service helper script
test_component "Service helper script" "test -x /workspace/.devcontainer/enable-services.sh"

echo ""
echo "🛠️ Development Tools"
echo "-------------------"

# Test VS Code extensions directory
test_component "VS Code extensions mount" "test -d /home/vscode/.vscode-server"

# Test Cargo cache mount
test_component "Cargo cache mount" "test -d /home/vscode/.cargo"

# Test NuGet cache mount
test_component "NuGet cache mount" "test -d /home/vscode/.nuget"

# Test Protocol Buffers compiler
test_component "Protocol Buffers compiler" "protoc --version"

# Test build tools
test_component "C++ build tools" "gcc --version"
test_component "CMake build system" "cmake --version"

echo ""
echo "🌐 Network and Environment"
echo "-------------------------"

# Test environment variables
test_component "ASPNETCORE_ENVIRONMENT" "test \"\$ASPNETCORE_ENVIRONMENT\" = \"Development\""
test_component "SGX_MODE setting" "test \"\$SGX_MODE\" = \"SIM\""

# Test workspace directory
test_component "Workspace directory" "test \"\$PWD\" = \"/workspace\""

# Test user context
test_component "VS Code user context" "test \"\$(whoami)\" = \"vscode\""

echo ""
echo "🧪 Build and Runtime Tests"
echo "-------------------------"

# Test NuGet restore
echo -n "Testing NuGet restore... "
if cd /workspace && dotnet restore >/dev/null 2>&1; then
    echo "✅ PASSED"
    ((PASSED++))
else
    echo "⚠️  WARNING - Some packages may be missing"
    ((WARNINGS++))
fi

# Test basic build
echo -n "Testing basic project build... "
if cd /workspace && dotnet build src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj >/dev/null 2>&1; then
    echo "✅ PASSED"
    ((PASSED++))
else
    echo "⚠️  WARNING - Build issues detected"
    ((WARNINGS++))
    echo "   This is expected if service dependencies are not fully configured"
fi

# Test Rust build environment
echo -n "Testing Rust build environment... "
if source /home/vscode/.cargo/env && cargo --version >/dev/null 2>&1; then
    echo "✅ PASSED"
    ((PASSED++))
else
    echo "❌ FAILED"
    ((FAILED++))
fi

echo ""
echo "📋 Validation Summary"
echo "===================="
echo "✅ Passed: $PASSED"
echo "⚠️  Warnings: $WARNINGS"
echo "❌ Failed: $FAILED"
echo ""

if [ $FAILED -eq 0 ]; then
    if [ $WARNINGS -eq 0 ]; then
        echo "🎉 DevContainer validation PASSED! Everything is working perfectly."
        echo ""
        echo "🚀 Ready to start development:"
        echo "   ./start-dev.sh"
    else
        echo "✅ DevContainer validation PASSED with warnings."
        echo ""
        echo "⚠️  Some optional components may not be fully configured."
        echo "This is normal and won't prevent development."
        echo ""
        echo "🚀 Ready to start development:"
        echo "   ./start-dev.sh"
    fi
    exit 0
else
    echo "❌ DevContainer validation FAILED!"
    echo ""
    echo "🔧 Please check the failed components above."
    echo "Some critical dependencies may be missing."
    echo ""
    echo "💡 Try running the troubleshooting script:"
    echo "   ./enable-services.sh"
    exit 1
fi 