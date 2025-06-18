#!/bin/bash

# Script to run all enclave-related tests using real SGX SDK in simulation mode
# This runs inside the Occlum Docker container

set -e

echo "============================================"
echo "Neo Service Layer - Enclave Tests Runner"
echo "Using Real SGX SDK in Simulation Mode"
echo "============================================"
echo

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[✓]${NC} $1"
}

print_error() {
    echo -e "${RED}[✗]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[!]${NC} $1"
}

print_info() {
    echo -e "${BLUE}[i]${NC} $1"
}

# Step 1: Verify SGX environment
print_info "Verifying SGX environment..."
if [ -z "$SGX_SDK" ]; then
    export SGX_SDK=/opt/intel/sgxsdk
fi

if [ -d "$SGX_SDK" ]; then
    print_status "SGX SDK found at: $SGX_SDK"
    source $SGX_SDK/environment
else
    print_error "SGX SDK not found at $SGX_SDK"
    exit 1
fi

# Verify Occlum installation
if command -v occlum &> /dev/null; then
    print_status "Occlum found: $(occlum version)"
else
    print_error "Occlum not found in PATH"
    exit 1
fi

# Step 2: Install .NET SDK if not present
print_info "Checking .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    print_warning ".NET SDK not found. Installing..."
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x ./dotnet-install.sh
    ./dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet
    ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet
    rm dotnet-install.sh
fi
print_status ".NET SDK installed: $(dotnet --version)"

# Step 3: Build native enclave components
print_info "Building native enclave components..."
ENCLAVE_DIR="/workspace/src/Tee/NeoServiceLayer.Tee.Enclave"

if [ -d "$ENCLAVE_DIR/Enclave" ]; then
    cd "$ENCLAVE_DIR/Enclave"
    
    # Build with SGX simulation mode
    if [ -f "Makefile.sgx" ]; then
        print_info "Building native SGX enclave..."
        make -f Makefile.sgx SGX_MODE=SIM clean
        make -f Makefile.sgx SGX_MODE=SIM
        print_status "Native enclave built successfully"
    fi
    
    # Build Rust components if present
    if [ -f "../Cargo.toml" ]; then
        print_info "Building Rust enclave components..."
        cd ..
        cargo build --release
        print_status "Rust components built successfully"
    fi
fi

# Step 4: Create Occlum instance for tests
print_info "Setting up Occlum instance..."
cd /workspace/tests/Tee/NeoServiceLayer.Tee.Enclave.Tests

OCCLUM_INSTANCE_DIR="occlum_instance"
if [ -d "$OCCLUM_INSTANCE_DIR" ]; then
    rm -rf "$OCCLUM_INSTANCE_DIR"
fi

mkdir -p "$OCCLUM_INSTANCE_DIR"
cd "$OCCLUM_INSTANCE_DIR"

# Initialize Occlum
occlum init

# Configure Occlum for .NET and Neo Service Layer
cat > Occlum.json <<'EOF'
{
    "resource_limits": {
        "user_space_size": "2GB",
        "kernel_space_heap_size": "512MB",
        "kernel_space_stack_size": "16MB",
        "max_num_of_threads": 64
    },
    "process": {
        "default_stack_size": "16MB",
        "default_heap_size": "64MB",
        "default_mmap_size": "512MB"
    },
    "entry_points": [
        "/bin/dotnet"
    ],
    "env": {
        "default": [
            "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1",
            "SGX_MODE=SIM",
            "LD_LIBRARY_PATH=/opt/occlum/lib:/lib:/usr/lib"
        ],
        "untrusted": []
    },
    "metadata": {
        "product_id": 1,
        "version_number": 1,
        "debuggable": true,
        "enable_kss": false
    }
}
EOF

print_status "Occlum instance configured"

# Step 5: Restore and build .NET projects
print_info "Building .NET projects..."
cd /workspace

dotnet restore
dotnet build --configuration Release

print_status ".NET projects built successfully"

# Step 6: Modify test projects to use ProductionSGXEnclaveWrapper
print_info "Configuring tests to use real SGX SDK..."

# Find all test files that use SGXSimulationEnclaveWrapper
TEST_FILES=$(find tests -name "*.cs" -type f | xargs grep -l "SGXSimulationEnclaveWrapper" 2>/dev/null || true)

if [ ! -z "$TEST_FILES" ]; then
    print_info "Found test files using simulation wrapper. Creating real SGX versions..."
    
    for file in $TEST_FILES; do
        # Skip if it's already a real SGX test
        if [[ $file == *"RealSGX"* ]]; then
            continue
        fi
        
        # Create a new version that uses ProductionSGXEnclaveWrapper
        dir=$(dirname "$file")
        filename=$(basename "$file" .cs)
        newfile="$dir/RealSGX_$filename.cs"
        
        if [ ! -f "$newfile" ]; then
            print_info "Creating $newfile..."
            
            # Create modified version
            sed -e 's/SGXSimulationEnclaveWrapper/IEnclaveWrapper/g' \
                -e 's/new SGXSimulationEnclaveWrapper()/GetRealEnclaveWrapper()/g' \
                -e '/using NeoServiceLayer.Tee.Enclave.Tests;/a\
using Microsoft.Extensions.Logging;\
using Microsoft.Extensions.DependencyInjection;' \
                "$file" > "$newfile"
            
            # Add helper method to get real enclave wrapper
            cat >> "$newfile" <<'EOF'

    private static IEnclaveWrapper GetRealEnclaveWrapper()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddConsole());
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ProductionSGXEnclaveWrapper>();
        return new ProductionSGXEnclaveWrapper(logger);
    }
EOF
        fi
    done
fi

# Step 7: Run all enclave-related tests
print_info "Running enclave tests..."

# Create results directory
mkdir -p /workspace/TestResults

# Define test categories and projects
declare -A TEST_PROJECTS=(
    ["Enclave"]="tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj"
    ["Host"]="tests/Tee/NeoServiceLayer.Tee.Host.Tests/NeoServiceLayer.Tee.Host.Tests.csproj"
    ["Integration"]="tests/Integration/NeoServiceLayer.Integration.Tests/NeoServiceLayer.Integration.Tests.csproj"
)

# Track results
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Run tests for each project
for category in "${!TEST_PROJECTS[@]}"; do
    project="${TEST_PROJECTS[$category]}"
    
    if [ -f "$project" ]; then
        print_info "Running $category tests..."
        
        # Run tests with detailed output
        if dotnet test "$project" \
            --logger "console;verbosity=detailed" \
            --logger "trx;LogFileName=$category.trx" \
            --results-directory /workspace/TestResults \
            --filter "Category=SGXIntegration|Category=Unit|FullyQualifiedName~Enclave|FullyQualifiedName~SGX" \
            --no-build \
            --configuration Release; then
            
            print_status "$category tests completed"
            ((PASSED_TESTS++))
        else
            print_error "$category tests failed"
            ((FAILED_TESTS++))
        fi
        ((TOTAL_TESTS++))
    else
        print_warning "$category project not found: $project"
    fi
done

# Step 8: Run specific enclave service tests
print_info "Running enclave-dependent service tests..."

SERVICE_TEST_FILTER="FullyQualifiedName~Enclave|FullyQualifiedName~PatternRecognition|FullyQualifiedName~Randomness|FullyQualifiedName~Oracle|FullyQualifiedName~KeyManagement"

if dotnet test \
    --filter "$SERVICE_TEST_FILTER" \
    --logger "console;verbosity=normal" \
    --logger "trx;LogFileName=EnclaveServices.trx" \
    --results-directory /workspace/TestResults \
    --no-build \
    --configuration Release; then
    
    print_status "Enclave service tests completed"
else
    print_warning "Some enclave service tests failed"
fi

# Step 9: Generate test report
print_info "Generating test report..."

cat > /workspace/TestResults/test-summary.txt <<EOF
Neo Service Layer - Enclave Test Results
========================================
Date: $(date)
SGX Mode: $SGX_MODE
Occlum Version: $(occlum version)
.NET Version: $(dotnet --version)

Test Summary:
- Total test projects: $TOTAL_TESTS
- Passed: $PASSED_TESTS
- Failed: $FAILED_TESTS

Environment:
- SGX_SDK: $SGX_SDK
- LD_LIBRARY_PATH: $LD_LIBRARY_PATH
- OCCLUM_PREFIX: $OCCLUM_PREFIX

Test Results Location: /workspace/TestResults/
EOF

# Step 10: Display summary
echo
echo "============================================"
echo "Test Execution Complete"
echo "============================================"
cat /workspace/TestResults/test-summary.txt

# Exit with appropriate code
if [ $FAILED_TESTS -gt 0 ]; then
    exit 1
else
    exit 0
fi