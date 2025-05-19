#!/bin/bash
# Bash script to run all tests in simulation mode

# Set environment variables for testing
export OCCLUM_SIMULATION=1
export OCCLUM_ENCLAVE_PATH="$(pwd)/../src/NeoServiceLayer.Tee.Enclave/build/lib/libenclave.so"
export OCCLUM_INSTANCE_DIR="$(pwd)/../occlum_instance"
export DOTNET_ENVIRONMENT="Testing"
export ASPNETCORE_ENVIRONMENT="Testing"
export TEST_CONFIG_PATH="$(pwd)/simulation-test-config.json"

# Create results directory
RESULTS_DIR="$(pwd)/TestResults/$(date +%Y-%m-%d_%H-%M-%S)"
mkdir -p "$RESULTS_DIR"

# Function to run tests and collect results
run_tests() {
    PROJECT_PATH=$1
    CATEGORY=$2
    OUTPUT_PREFIX=$3

    echo -e "\033[0;36mRunning $CATEGORY tests from $PROJECT_PATH...\033[0m"

    # Run tests with detailed logging and collect code coverage
    dotnet test "$PROJECT_PATH" \
        --filter "Category=$CATEGORY" \
        --logger "console;verbosity=detailed" \
        --logger "trx;LogFileName=$RESULTS_DIR/$OUTPUT_PREFIX-$CATEGORY.trx" \
        --collect:"XPlat Code Coverage" \
        --results-directory:"$RESULTS_DIR"

    if [ $? -eq 0 ]; then
        echo -e "\033[0;32m✅ $CATEGORY tests passed!\033[0m"
    else
        echo -e "\033[0;31m❌ $CATEGORY tests failed!\033[0m"
    fi

    echo ""
}

# Build the projects first
echo -e "\033[0;36mBuilding projects...\033[0m"
dotnet build ../src/NeoServiceLayer.Tee.Enclave/NeoServiceLayer.Tee.Enclave.csproj -c Debug
dotnet build ../src/NeoServiceLayer.Tee.Host/NeoServiceLayer.Tee.Host.csproj -c Debug
dotnet build ../src/NeoServiceLayer.Shared/NeoServiceLayer.Shared.csproj -c Debug

# Run all the test categories
echo -e "\033[0;33mStarting test execution in simulation mode...\033[0m"
echo -e "\033[0;33mResults will be saved to: $RESULTS_DIR\033[0m"
echo ""

# Run basic tests
run_tests "./NeoServiceLayer.BasicTests/NeoServiceLayer.BasicTests.csproj" "Basic" "Basic"

# Run mock tests
run_tests "./NeoServiceLayer.MockTests/NeoServiceLayer.MockTests.csproj" "Mock" "Mock"
run_tests "./NeoServiceLayer.MockTests/NeoServiceLayer.MockTests.csproj" "Security" "Security"
run_tests "./NeoServiceLayer.MockTests/NeoServiceLayer.MockTests.csproj" "Performance" "Performance"
run_tests "./NeoServiceLayer.MockTests/NeoServiceLayer.MockTests.csproj" "ErrorHandling" "ErrorHandling"

# Run simulation mode tests if the enclave is available
if [ -f "$OCCLUM_ENCLAVE_PATH" ]; then
    run_tests "./NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj" "Occlum" "Occlum"
    run_tests "./NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj" "Attestation" "Attestation"
    run_tests "./NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj" "JavaScriptEngine" "JavaScriptEngine"
    run_tests "./NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj" "GasAccounting" "GasAccounting"
    run_tests "./NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj" "UserSecrets" "UserSecrets"
    run_tests "./NeoServiceLayer.Occlum.Tests/NeoServiceLayer.Occlum.Tests.csproj" "Occlum" "Occlum"
    run_tests "./NeoServiceLayer.IntegrationTests/NeoServiceLayer.IntegrationTests.csproj" "Integration" "Integration"

    # Check if the API is running
    if curl -s -f http://localhost:5000/api/health > /dev/null; then
        echo -e "\033[0;36mAPI is running. Running API integration tests...\033[0m"
        run_tests "./NeoServiceLayer.IntegrationTests/NeoServiceLayer.IntegrationTests.csproj" "ApiIntegration" "ApiIntegration"
    else
        echo -e "\033[0;33m⚠️ API is not running. Skipping API integration tests.\033[0m"
    fi
else
    echo -e "\033[0;33m⚠️ Enclave binary not found at $OE_ENCLAVE_PATH. Skipping simulation mode tests.\033[0m"
fi

# Generate test summary
echo -e "\033[0;36mGenerating test summary...\033[0m"
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0
SKIPPED_TESTS=0

for TRX_FILE in "$RESULTS_DIR"/*.trx; do
    if [ -f "$TRX_FILE" ]; then
        # Extract test counts using grep and sed
        TOTAL=$(grep -oP 'total="\K[0-9]+' "$TRX_FILE")
        PASSED=$(grep -oP 'passed="\K[0-9]+' "$TRX_FILE")
        FAILED=$(grep -oP 'failed="\K[0-9]+' "$TRX_FILE")
        SKIPPED=$(grep -oP 'notExecuted="\K[0-9]+' "$TRX_FILE")

        TOTAL_TESTS=$((TOTAL_TESTS + TOTAL))
        PASSED_TESTS=$((PASSED_TESTS + PASSED))
        FAILED_TESTS=$((FAILED_TESTS + FAILED))
        SKIPPED_TESTS=$((SKIPPED_TESTS + SKIPPED))
    fi
done

echo ""
echo -e "\033[0;33mTest Summary:\033[0m"
echo -e "\033[0;36m  Total Tests: $TOTAL_TESTS\033[0m"
echo -e "\033[0;32m  Passed: $PASSED_TESTS\033[0m"
echo -e "\033[0;31m  Failed: $FAILED_TESTS\033[0m"
echo -e "\033[0;33m  Skipped: $SKIPPED_TESTS\033[0m"
echo ""

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "\033[0;32m✅ All tests passed!\033[0m"
    exit 0
else
    echo -e "\033[0;31m❌ Some tests failed!\033[0m"
    exit 1
fi
