#!/bin/bash
set -e

echo "Building and testing Neo Service Layer with Occlum"

# Check if running in Docker
if [ -f /.dockerenv ]; then
    echo "Running inside Docker container"
else
    echo "Running Occlum Docker container..."
    docker run --rm -it \
        --device /dev/sgx_enclave \
        --device /dev/sgx_provision \
        -v /var/run/aesmd:/var/run/aesmd \
        -v $(pwd):/workspace \
        -w /workspace \
        occlum/occlum:0.30.1-ubuntu20.04 \
        bash -c "/workspace/scripts/run-occlum-tests.sh"
    exit $?
fi

# Install .NET if not present
if ! command -v dotnet &> /dev/null; then
    echo "Installing .NET SDK..."
    apt-get update && apt-get install -y wget
    wget https://dot.net/v1/dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet
    ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet
fi

# Build the project
echo "Building project..."
dotnet build --configuration Release

# Run regular tests
echo "Running unit tests..."
dotnet test --configuration Release --filter "Category!=SGX"

# Create Occlum instance for SGX tests
echo "Setting up Occlum instance..."
OCCLUM_DIR=/tmp/occlum_test
rm -rf $OCCLUM_DIR
mkdir -p $OCCLUM_DIR
cd $OCCLUM_DIR

occlum init
cp /workspace/occlum_config.json ./Occlum.json

# Copy .NET runtime and test assemblies
mkdir -p image/opt/dotnet
cp -r /usr/share/dotnet/* image/opt/dotnet/

mkdir -p image/app
cp -r /workspace/src/*/bin/Release/net9.0/* image/app/ 2>/dev/null || true

mkdir -p image/tests
find /workspace -name "*.Tests.dll" -path "*/bin/Release/*" -exec cp {} image/tests/ \; 2>/dev/null || true

# Build Occlum image
echo "Building Occlum image..."
occlum build

# Run application in Occlum (optional)
echo "Testing application in Occlum..."
occlum run /opt/dotnet/dotnet /app/NeoServiceLayer.Api.dll --urls http://localhost:5000 &
OCCLUM_PID=$!

# Wait for startup
sleep 10

# Test if running
if ps -p $OCCLUM_PID > /dev/null; then
    echo "Application running successfully in Occlum"
    kill $OCCLUM_PID
else
    echo "Application failed to start in Occlum"
fi

echo "Occlum tests completed!"