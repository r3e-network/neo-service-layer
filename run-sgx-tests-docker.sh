#!/bin/bash

# Script to run enclave tests using real SGX SDK via Docker with Occlum

set -e

echo "============================================"
echo "Running SGX Enclave Tests with Real SDK"
echo "Using Occlum LibOS Docker Container"
echo "============================================"
echo

# Create a simplified Docker run command
echo "Starting Occlum container with SGX simulation mode..."

# Run the tests in the Occlum container
sudo docker run --rm \
    -v $(pwd):/workspace \
    -w /workspace \
    -e SGX_MODE=SIM \
    -e SGX_DEBUG=1 \
    -e DOTNET_ENVIRONMENT=Development \
    -e DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    --privileged \
    occlum/occlum:0.30.1-ubuntu20.04 \
    /bin/bash -c '
        # Install .NET SDK
        echo "Installing .NET 9.0 SDK..."
        wget -q https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
        chmod +x ./dotnet-install.sh
        ./dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet
        ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet
        rm dotnet-install.sh
        
        # Verify installation
        echo ".NET SDK Version: $(dotnet --version)"
        
        # Set up SGX environment
        source /opt/intel/sgxsdk/environment
        
        # Build the test projects
        echo "Building test projects..."
        cd /workspace
        dotnet restore tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj
        dotnet build tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj --configuration Release
        
        # Run the existing SGX simulation tests
        echo "Running SGX simulation tests..."
        dotnet test tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj \
            --configuration Release \
            --no-build \
            --logger "console;verbosity=normal" \
            --filter "FullyQualifiedName~SGXSimulation|FullyQualifiedName~BasicSGX|FullyQualifiedName~Enclave" \
            -- RunConfiguration.TestSessionTimeout=600000
    '

echo
echo "Test execution completed!"