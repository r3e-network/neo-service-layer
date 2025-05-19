#!/bin/bash
# Bash script to run enclave tests with the real SDK in simulation mode

# Set environment variables for testing
export OCCLUM_SIMULATION=1
export OCCLUM_ENCLAVE_PATH="$(pwd)/../src/NeoServiceLayer.Tee.Enclave/build/lib/libenclave.so"
export OCCLUM_INSTANCE_DIR="$(pwd)/../occlum_instance"

# Ensure the enclave is built
echo "Building enclave..."
dotnet build ../src/NeoServiceLayer.Tee.Enclave/NeoServiceLayer.Tee.Enclave.csproj -c Debug

# Run the tests
echo "Running Enclave Tests..."
dotnet test ./NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj --filter "Category=Occlum|Category=Attestation|Category=Performance|Category=ErrorHandling|Category=Security" --logger "console;verbosity=detailed"

echo "Running Occlum Tests..."
dotnet test ./NeoServiceLayer.Occlum.Tests/NeoServiceLayer.Occlum.Tests.csproj --filter "Category=Occlum" --logger "console;verbosity=detailed"

echo "Running Integration Tests..."
dotnet test ./NeoServiceLayer.IntegrationTests/NeoServiceLayer.IntegrationTests.csproj --filter "Category=Occlum" --logger "console;verbosity=detailed"

echo "All tests completed!"
