#!/bin/bash

# Setup script for running SGX SDK tests in simulation mode
# This script configures the environment to use Intel SGX SDK and Occlum LibOS

echo "=== SGX SDK Simulation Mode Test Setup ==="
echo

# Set SGX mode to simulation
export SGX_MODE=SIM
export SGX_DEBUG=1
export SGX_PRERELEASE=1

# Set Occlum-specific environment variables
export OCCLUM_RELEASE=1
export OCCLUM_LOG_LEVEL=info

# Check if Intel SGX SDK is installed
if [ -d "/opt/intel/sgxsdk" ]; then
    echo "✓ Intel SGX SDK found at /opt/intel/sgxsdk"
    source /opt/intel/sgxsdk/environment
elif [ -d "$HOME/sgxsdk" ]; then
    echo "✓ Intel SGX SDK found at $HOME/sgxsdk"
    source $HOME/sgxsdk/environment
else
    echo "✗ Intel SGX SDK not found!"
    echo "Please install Intel SGX SDK first:"
    echo "  wget https://download.01.org/intel-sgx/latest/linux-latest/distro/ubuntu20.04-server/sgx_linux_x64_sdk.bin"
    echo "  chmod +x sgx_linux_x64_sdk.bin"
    echo "  ./sgx_linux_x64_sdk.bin --prefix=/opt/intel"
    exit 1
fi

# Check if Occlum is installed
if command -v occlum &> /dev/null; then
    echo "✓ Occlum found in PATH"
    OCCLUM_PATH=$(which occlum)
    export OCCLUM_PREFIX=$(dirname $(dirname $OCCLUM_PATH))
elif [ -d "/opt/occlum" ]; then
    echo "✓ Occlum found at /opt/occlum"
    export OCCLUM_PREFIX=/opt/occlum
    export PATH=$OCCLUM_PREFIX/bin:$PATH
else
    echo "✗ Occlum not found!"
    echo "Please install Occlum first:"
    echo "  See: https://github.com/occlum/occlum"
    exit 1
fi

# Set library paths for Occlum
export LD_LIBRARY_PATH=$OCCLUM_PREFIX/lib:$LD_LIBRARY_PATH

# Create Occlum instance directory if needed
OCCLUM_INSTANCE_DIR="./occlum_instance"
if [ ! -d "$OCCLUM_INSTANCE_DIR" ]; then
    echo "Creating Occlum instance directory..."
    mkdir -p $OCCLUM_INSTANCE_DIR
    cd $OCCLUM_INSTANCE_DIR
    
    # Initialize Occlum instance
    occlum init
    
    # Configure Occlum for Neo Service Layer
    cat > Occlum.json <<EOF
{
    "resource_limits": {
        "user_space_size": "1GB",
        "kernel_space_heap_size": "256MB",
        "max_num_of_threads": 32
    },
    "process": {
        "default_stack_size": "8MB",
        "default_heap_size": "32MB",
        "default_mmap_size": "256MB"
    },
    "entry_points": [
        "/bin/dotnet"
    ],
    "env": {
        "default": [
            "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1",
            "SGX_MODE=SIM"
        ]
    },
    "metadata": {
        "product_id": 1,
        "version_number": 1
    }
}
EOF
    
    cd ..
fi

# Build the native enclave library if source exists
if [ -d "src/Tee/NeoServiceLayer.Tee.Enclave/Enclave" ]; then
    echo "Building native enclave library..."
    cd src/Tee/NeoServiceLayer.Tee.Enclave/Enclave
    
    if [ -f "Makefile.sgx" ]; then
        make -f Makefile.sgx SGX_MODE=SIM clean
        make -f Makefile.sgx SGX_MODE=SIM
        
        # Copy built libraries to test directory
        cp *.so ../../../tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/ 2>/dev/null || true
    fi
    
    cd ../../../../
fi

# Create a wrapper script for running tests
cat > run-sgx-tests.sh <<'EOF'
#!/bin/bash

# Ensure environment is set
export SGX_MODE=SIM
export LD_LIBRARY_PATH=$OCCLUM_PREFIX/lib:$LD_LIBRARY_PATH

echo "Running SGX SDK tests in simulation mode..."
echo "SGX_MODE=$SGX_MODE"
echo "LD_LIBRARY_PATH=$LD_LIBRARY_PATH"
echo

# Run the specific SGX tests
dotnet test --filter "Category=SGXIntegration" --logger "console;verbosity=detailed"
EOF

chmod +x run-sgx-tests.sh

echo
echo "=== Setup Complete ==="
echo
echo "Environment variables set:"
echo "  SGX_MODE=$SGX_MODE"
echo "  LD_LIBRARY_PATH=$LD_LIBRARY_PATH"
echo "  OCCLUM_PREFIX=$OCCLUM_PREFIX"
echo
echo "To run SGX SDK tests in simulation mode:"
echo "  ./run-sgx-tests.sh"
echo
echo "Or run directly with:"
echo "  dotnet test --filter \"Category=SGXIntegration\""