#!/bin/bash

echo "================================================"
echo "Running Enclave Tests with Real SGX SDK"
echo "================================================"

# Pull and run Occlum Docker image with tests
sudo docker run --rm \
    -v "$(pwd)":/workspace \
    -w /workspace \
    -e SGX_MODE=SIM \
    -e SGX_DEBUG=1 \
    --privileged \
    occlum/occlum:0.30.1-ubuntu20.04 \
    bash -c '
        echo "Setting up environment..."
        
        # Install .NET 9.0 SDK
        wget -q https://dot.net/v1/dotnet-install.sh
        chmod +x dotnet-install.sh
        ./dotnet-install.sh --channel 9.0 --install-dir /dotnet
        export PATH="/dotnet:$PATH"
        
        # Remove global.json to avoid version conflicts
        rm -f global.json
        
        echo "dotnet version: $(dotnet --version)"
        
        # Build test project
        echo "Building test project..."
        dotnet build tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj -c Release
        
        # Run the SGX integration tests
        echo "Running SGX integration tests..."
        dotnet test tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj \
            -c Release \
            --no-build \
            --filter "Category=SGXIntegration" \
            -v normal
    '