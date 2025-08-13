#!/bin/bash

# Comprehensive test runner script for Neo Service Layer
# Addresses testing infrastructure gaps identified in code review

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
TEST_RESULTS_DIR="./TestResults"
COVERAGE_THRESHOLD=80
PARALLEL_JOBS=4

echo -e "${BLUE}🧪 Neo Service Layer - Comprehensive Test Suite${NC}"
echo "================================================="

# Create test results directory
mkdir -p "$TEST_RESULTS_DIR"

# Function to run unit tests
run_unit_tests() {
    echo -e "${YELLOW}📋 Running Unit Tests...${NC}"
    
    # Security Service Tests
    echo "Testing Security Service..."
    dotnet test tests/Infrastructure/NeoServiceLayer.Infrastructure.Security.Tests/ \
        --logger "trx;LogFileName=security-tests.trx" \
        --logger "console;verbosity=minimal" \
        --results-directory "$TEST_RESULTS_DIR" \
        --collect:"XPlat Code Coverage" \
        --configuration Release \
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
    
    echo -e "${GREEN}✅ Security Service Tests Completed${NC}"
}

# Function to run integration tests  
run_integration_tests() {
    echo -e "${YELLOW}🔗 Running Integration Tests...${NC}"
    
    # SGX Enclave Integration Tests
    echo "Testing SGX Enclave Integration..."
    dotnet test tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/ \
        --logger "trx;LogFileName=enclave-integration-tests.trx" \
        --logger "console;verbosity=minimal" \
        --results-directory "$TEST_RESULTS_DIR" \
        --collect:"XPlat Code Coverage" \
        --configuration Release \
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
        
    echo -e "${GREEN}✅ Integration Tests Completed${NC}"
}

# Function to run performance tests
run_performance_tests() {
    echo -e "${YELLOW}⚡ Running Performance Benchmarks...${NC}"
    
    # Performance benchmarks
    echo "Running Service Performance Benchmarks..."
    cd tests/Performance/NeoServiceLayer.Performance.Tests/
    dotnet run -c Release -- --filter "*" --memory --runtimes net8.0
    cd ../../../
    
    echo -e "${GREEN}✅ Performance Tests Completed${NC}"
}

# Function to generate coverage report
generate_coverage_report() {
    echo -e "${YELLOW}📊 Generating Coverage Report...${NC}"
    
    # Install reportgenerator tool if not already installed
    if ! dotnet tool list -g | grep -q reportgenerator; then
        echo "Installing ReportGenerator..."
        dotnet tool install -g dotnet-reportgenerator-globaltool
    fi
    
    # Generate HTML coverage report
    reportgenerator \
        "-reports:$TEST_RESULTS_DIR/**/coverage.opencover.xml" \
        "-targetdir:$TEST_RESULTS_DIR/CoverageReport" \
        "-reporttypes:Html;Badges;TextSummary" \
        "-verbosity:Warning"
    
    # Check coverage threshold
    if [ -f "$TEST_RESULTS_DIR/CoverageReport/Summary.txt" ]; then
        COVERAGE=$(grep "Line coverage:" "$TEST_RESULTS_DIR/CoverageReport/Summary.txt" | grep -o '[0-9.]*%' | grep -o '[0-9.]*')
        echo "Current code coverage: ${COVERAGE}%"
        
        if (( $(echo "$COVERAGE >= $COVERAGE_THRESHOLD" | bc -l) )); then
            echo -e "${GREEN}✅ Coverage threshold met (${COVERAGE}% >= ${COVERAGE_THRESHOLD}%)${NC}"
        else
            echo -e "${RED}❌ Coverage threshold not met (${COVERAGE}% < ${COVERAGE_THRESHOLD}%)${NC}"
            exit 1
        fi
    fi
}

# Function to run security validation tests
run_security_tests() {
    echo -e "${YELLOW}🔒 Running Security Validation Tests...${NC}"
    
    # Run security-specific test scenarios
    dotnet test tests/Infrastructure/NeoServiceLayer.Infrastructure.Security.Tests/ \
        --filter "Category=Security" \
        --logger "console;verbosity=detailed" \
        --results-directory "$TEST_RESULTS_DIR" \
        --configuration Release
        
    echo -e "${GREEN}✅ Security Tests Completed${NC}"
}

# Function to run resilience tests
run_resilience_tests() {
    echo -e "${YELLOW}🛡️ Running Resilience Pattern Tests...${NC}"
    
    # Test circuit breakers, retries, timeouts, bulkheads
    dotnet test tests/Infrastructure/NeoServiceLayer.Infrastructure.Resilience.Tests/ \
        --filter "Category=Resilience" \
        --logger "console;verbosity=detailed" \
        --results-directory "$TEST_RESULTS_DIR" \
        --configuration Release
        
    echo -e "${GREEN}✅ Resilience Tests Completed${NC}"
}

# Function to validate test results
validate_results() {
    echo -e "${YELLOW}📋 Validating Test Results...${NC}"
    
    # Check for test result files
    if ls "$TEST_RESULTS_DIR"/*.trx 1> /dev/null 2>&1; then
        echo -e "${GREEN}✅ Test result files found${NC}"
    else
        echo -e "${RED}❌ No test result files found${NC}"
        exit 1
    fi
    
    # Check for coverage files
    if ls "$TEST_RESULTS_DIR"/**/coverage.opencover.xml 1> /dev/null 2>&1; then
        echo -e "${GREEN}✅ Coverage files found${NC}"
    else
        echo -e "${YELLOW}⚠️ No coverage files found${NC}"
    fi
}

# Function to clean up previous results
cleanup() {
    echo -e "${YELLOW}🧹 Cleaning up previous test results...${NC}"
    if [ -d "$TEST_RESULTS_DIR" ]; then
        rm -rf "$TEST_RESULTS_DIR"
    fi
    mkdir -p "$TEST_RESULTS_DIR"
}

# Main execution
main() {
    echo "Starting comprehensive test suite..."
    echo "Test Results Directory: $TEST_RESULTS_DIR"
    echo "Coverage Threshold: $COVERAGE_THRESHOLD%"
    echo "Parallel Jobs: $PARALLEL_JOBS"
    echo ""
    
    # Parse command line arguments
    SKIP_CLEANUP=false
    SKIP_PERFORMANCE=false
    RUN_SECURITY_ONLY=false
    
    for arg in "$@"; do
        case $arg in
            --skip-cleanup)
                SKIP_CLEANUP=true
                ;;
            --skip-performance)
                SKIP_PERFORMANCE=true
                ;;
            --security-only)
                RUN_SECURITY_ONLY=true
                ;;
            --help)
                echo "Usage: $0 [options]"
                echo "Options:"
                echo "  --skip-cleanup      Skip cleaning previous results"
                echo "  --skip-performance  Skip performance benchmarks"
                echo "  --security-only     Run only security tests"
                echo "  --help              Show this help message"
                exit 0
                ;;
        esac
    done
    
    # Cleanup if not skipped
    if [ "$SKIP_CLEANUP" = false ]; then
        cleanup
    fi
    
    # Build solution first
    echo -e "${YELLOW}🔨 Building solution...${NC}"
    dotnet build --configuration Release --no-restore
    echo -e "${GREEN}✅ Build completed${NC}"
    
    if [ "$RUN_SECURITY_ONLY" = true ]; then
        run_security_tests
    else
        # Run all test suites
        run_unit_tests
        run_integration_tests
        run_security_tests
        run_resilience_tests
        
        # Run performance tests if not skipped
        if [ "$SKIP_PERFORMANCE" = false ]; then
            run_performance_tests
        fi
        
        # Generate coverage report
        generate_coverage_report
    fi
    
    # Validate results
    validate_results
    
    echo ""
    echo -e "${GREEN}🎉 All tests completed successfully!${NC}"
    echo "Results available in: $TEST_RESULTS_DIR"
    
    if [ -f "$TEST_RESULTS_DIR/CoverageReport/index.html" ]; then
        echo "Coverage report: $TEST_RESULTS_DIR/CoverageReport/index.html"
    fi
}

# Error handling
handle_error() {
    echo -e "${RED}❌ Test execution failed at line $1${NC}"
    echo "Check the logs above for details."
    exit 1
}

trap 'handle_error $LINENO' ERR

# Run main function
main "$@"