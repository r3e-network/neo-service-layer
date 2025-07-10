#!/bin/bash

# Neo Service Layer Microservices Integration Tests Runner
# This script sets up the test environment and runs integration tests

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$SCRIPT_DIR/../../../.."

echo "=========================================="
echo "Neo Service Layer Integration Tests"
echo "=========================================="

# Function to cleanup on exit
cleanup() {
    echo "Cleaning up test environment..."
    cd "$SCRIPT_DIR"
    docker-compose -f docker-compose.test.yml down -v
}

# Set trap to cleanup on script exit
trap cleanup EXIT

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker and try again."
    exit 1
fi

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "Error: docker-compose is not installed. Please install it and try again."
    exit 1
fi

# Parse command line arguments
RUN_SPECIFIC_TEST=""
SKIP_BUILD=false
KEEP_RUNNING=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --test)
            RUN_SPECIFIC_TEST="$2"
            shift 2
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --keep-running)
            KEEP_RUNNING=true
            shift
            ;;
        --help)
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  --test <test-name>    Run specific test class or method"
            echo "  --skip-build          Skip building Docker images"
            echo "  --keep-running        Keep containers running after tests"
            echo "  --help                Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Build base images if needed
if [ "$SKIP_BUILD" = false ]; then
    echo "Building base images..."
    cd "$PROJECT_ROOT"
    
    # Build base image first
    docker build -t neoservicelayer/base:latest -f docker/base/Dockerfile .
    
    echo "Building service images..."
    # Build services in parallel for faster builds
    cd "$SCRIPT_DIR"
    docker-compose -f docker-compose.test.yml build --parallel
fi

# Start test infrastructure
echo "Starting test infrastructure..."
cd "$SCRIPT_DIR"
docker-compose -f docker-compose.test.yml up -d

# Wait for services to be ready
echo "Waiting for services to be ready..."
MAX_WAIT=120
WAIT_COUNT=0

# Function to check if a service is healthy
check_service() {
    local service=$1
    local port=$2
    nc -z localhost $port > /dev/null 2>&1
}

# Wait for core services
while [ $WAIT_COUNT -lt $MAX_WAIT ]; do
    if check_service "consul" 8500 && \
       check_service "gateway" 7000 && \
       check_service "rabbitmq" 5672 && \
       check_service "postgres" 5432; then
        echo "Core services are ready!"
        break
    fi
    
    echo -n "."
    sleep 1
    WAIT_COUNT=$((WAIT_COUNT + 1))
done

if [ $WAIT_COUNT -eq $MAX_WAIT ]; then
    echo "Error: Services failed to start within $MAX_WAIT seconds"
    docker-compose -f docker-compose.test.yml logs
    exit 1
fi

# Additional wait for service registration
echo "Waiting for service registration..."
sleep 10

# Check service discovery
echo "Checking service discovery..."
SERVICES=$(curl -s http://localhost:8500/v1/catalog/services | jq -r 'keys[]' 2>/dev/null || echo "")
echo "Registered services: $SERVICES"

# Run the integration tests
echo "Running integration tests..."
cd "$PROJECT_ROOT"

TEST_FILTER=""
if [ -n "$RUN_SPECIFIC_TEST" ]; then
    TEST_FILTER="--filter FullyQualifiedName~$RUN_SPECIFIC_TEST"
fi

# Run tests with detailed output
dotnet test tests/Integration/NeoServiceLayer.Integration.Tests/NeoServiceLayer.Integration.Tests.csproj \
    --configuration Release \
    --logger "console;verbosity=detailed" \
    --logger "trx;LogFileName=microservices-integration-test-results.trx" \
    $TEST_FILTER \
    -- xunit.skipnontestassemblies=true

TEST_RESULT=$?

# Show container logs if tests failed
if [ $TEST_RESULT -ne 0 ]; then
    echo "Tests failed! Showing container logs..."
    cd "$SCRIPT_DIR"
    docker-compose -f docker-compose.test.yml logs --tail=50
fi

# Keep containers running if requested
if [ "$KEEP_RUNNING" = true ]; then
    echo "Keeping containers running. Press Ctrl+C to stop..."
    read -r -d '' _ </dev/tty
fi

exit $TEST_RESULT