#!/bin/bash

# Script to run Occlum tests for the Neo Service Layer

# Set environment variables
export Tee__SimulationMode="true"
export Tee__OcclumSupport="true"
export Tee__OcclumInstanceDir="/occlum_instance"
export Tee__OcclumLogLevel="info"

# Create results directory
RESULTS_DIR="./test-results/occlum-tests-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$RESULTS_DIR"

echo -e "\e[33mStarting Occlum tests...\e[0m"
echo -e "\e[33mResults will be saved to: $RESULTS_DIR\e[0m"
echo ""

# Function to run tests and collect results
run_tests() {
    PROJECT_PATH=$1
    CATEGORY=$2
    OUTPUT_PREFIX=$3

    echo -e "\e[36mRunning $CATEGORY tests from $PROJECT_PATH...\e[0m"

    # Run tests with detailed logging and collect code coverage
    dotnet test "$PROJECT_PATH" \
        --filter "Category=$CATEGORY" \
        --logger "console;verbosity=detailed" \
        --logger "trx;LogFileName=$RESULTS_DIR/$OUTPUT_PREFIX-$CATEGORY.trx" \
        --collect:"XPlat Code Coverage" \
        --results-directory:"$RESULTS_DIR"

    if [ $? -eq 0 ]; then
        echo -e "\e[32m✅ $CATEGORY tests passed!\e[0m"
    else
        echo -e "\e[31m❌ $CATEGORY tests failed!\e[0m"
    fi

    echo ""
}

# Build the projects first
echo -e "\e[36mBuilding projects...\e[0m"
dotnet build ../src/NeoServiceLayer.Tee.Enclave/NeoServiceLayer.Tee.Enclave.csproj -c Debug
dotnet build ../src/NeoServiceLayer.Tee.Host/NeoServiceLayer.Tee.Host.csproj -c Debug
dotnet build ../src/NeoServiceLayer.Shared/NeoServiceLayer.Shared.csproj -c Debug

# Run Occlum tests
run_tests "./NeoServiceLayer.Occlum.Tests/NeoServiceLayer.Occlum.Tests.csproj" "Occlum" "Occlum"

# Run API tests with Occlum
run_tests "./NeoServiceLayer.Api.Tests/NeoServiceLayer.Api.Tests.csproj" "Occlum" "Api-Occlum"

# Run integration tests with Occlum
run_tests "./NeoServiceLayer.IntegrationTests/NeoServiceLayer.IntegrationTests.csproj" "Occlum" "Integration-Occlum"

echo -e "\e[33mAll Occlum tests completed!\e[0m"
echo -e "\e[33mResults saved to: $RESULTS_DIR\e[0m"
