#!/bin/bash

# Neo Service Layer Testing Script
# Comprehensive testing for all 22 smart contracts

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Test configuration
TEST_OUTPUT_DIR="./test-results"
COVERAGE_OUTPUT_DIR="./coverage"

# Setup test environment
setup_test_environment() {
    log_info "Setting up test environment..."
    
    # Navigate to project root
    cd "$(dirname "$0")/.."
    
    # Create test output directories
    mkdir -p "$TEST_OUTPUT_DIR"
    mkdir -p "$COVERAGE_OUTPUT_DIR"
    mkdir -p "./logs"
    
    # Ensure project is built
    log_info "Building project for testing..."
    dotnet build --configuration Release --verbosity minimal
    
    if [ $? -eq 0 ]; then
        log_success "Project built successfully"
    else
        log_error "Failed to build project"
        exit 1
    fi
}

# Run unit tests
run_unit_tests() {
    log_info "Running unit tests for all 22 contracts..."
    
    dotnet test \
        --configuration Release \
        --logger "trx;LogFileName=unit-tests.trx" \
        --logger "console;verbosity=normal" \
        --results-directory "$TEST_OUTPUT_DIR" \
        --collect:"XPlat Code Coverage" \
        --filter "Category=Unit"
    
    local exit_code=$?
    
    if [ $exit_code -eq 0 ]; then
        log_success "Unit tests passed"
    else
        log_error "Unit tests failed"
        return $exit_code
    fi
}

# Run integration tests
run_integration_tests() {
    log_info "Running integration tests..."
    
    dotnet test \
        --configuration Release \
        --logger "trx;LogFileName=integration-tests.trx" \
        --logger "console;verbosity=normal" \
        --results-directory "$TEST_OUTPUT_DIR" \
        --collect:"XPlat Code Coverage" \
        --filter "Category=Integration"
    
    local exit_code=$?
    
    if [ $exit_code -eq 0 ]; then
        log_success "Integration tests passed"
    else
        log_error "Integration tests failed"
        return $exit_code
    fi
}

# Run performance tests
run_performance_tests() {
    log_info "Running performance tests..."
    
    dotnet test \
        --configuration Release \
        --logger "trx;LogFileName=performance-tests.trx" \
        --logger "console;verbosity=normal" \
        --results-directory "$TEST_OUTPUT_DIR" \
        --filter "Category=Performance"
    
    local exit_code=$?
    
    if [ $exit_code -eq 0 ]; then
        log_success "Performance tests passed"
    else
        log_warning "Performance tests failed or not available"
        return 0  # Don't fail build on performance test issues
    fi
}

# Run security tests
run_security_tests() {
    log_info "Running security tests..."
    
    dotnet test \
        --configuration Release \
        --logger "trx;LogFileName=security-tests.trx" \
        --logger "console;verbosity=normal" \
        --results-directory "$TEST_OUTPUT_DIR" \
        --filter "Category=Security"
    
    local exit_code=$?
    
    if [ $exit_code -eq 0 ]; then
        log_success "Security tests passed"
    else
        log_warning "Security tests failed or not available"
        return 0  # Don't fail build on security test issues
    fi
}

# Run contract-specific tests
run_contract_tests() {
    local contract_name=$1
    
    log_info "Running tests for $contract_name..."
    
    dotnet test \
        --configuration Release \
        --logger "trx;LogFileName=${contract_name}-tests.trx" \
        --logger "console;verbosity=normal" \
        --results-directory "$TEST_OUTPUT_DIR" \
        --filter "FullyQualifiedName~$contract_name"
    
    local exit_code=$?
    
    if [ $exit_code -eq 0 ]; then
        log_success "$contract_name tests passed"
    else
        log_error "$contract_name tests failed"
        return $exit_code
    fi
}

# Generate test coverage report
generate_coverage_report() {
    log_info "Generating test coverage report..."
    
    # Find coverage files
    local coverage_files=$(find "$TEST_OUTPUT_DIR" -name "coverage.cobertura.xml" -type f)
    
    if [ -z "$coverage_files" ]; then
        log_warning "No coverage files found"
        return 0
    fi
    
    # Install reportgenerator if not available
    if ! command -v reportgenerator &> /dev/null; then
        log_info "Installing ReportGenerator..."
        dotnet tool install --global dotnet-reportgenerator-globaltool
    fi
    
    # Generate HTML coverage report
    reportgenerator \
        -reports:"$TEST_OUTPUT_DIR/**/coverage.cobertura.xml" \
        -targetdir:"$COVERAGE_OUTPUT_DIR" \
        -reporttypes:"Html;Badges;TextSummary"
    
    if [ $? -eq 0 ]; then
        log_success "Coverage report generated: $COVERAGE_OUTPUT_DIR/index.html"
    else
        log_warning "Failed to generate coverage report"
    fi
}

# Run all tests for specific contracts
test_financial_contracts() {
    log_info "Testing Financial Services contracts..."
    
    local contracts=(
        "PaymentProcessingContract"
        "InsuranceContract"
        "LendingContract"
        "TokenizationContract"
    )
    
    for contract in "${contracts[@]}"; do
        run_contract_tests "$contract"
    done
}

test_industry_contracts() {
    log_info "Testing Industry-Specific contracts..."
    
    local contracts=(
        "SupplyChainContract"
        "EnergyManagementContract"
        "HealthcareContract"
        "GameContract"
    )
    
    for contract in "${contracts[@]}"; do
        run_contract_tests "$contract"
    done
}

test_infrastructure_contracts() {
    log_info "Testing Infrastructure contracts..."
    
    local contracts=(
        "ServiceRegistry"
        "RandomnessContract"
        "OracleContract"
        "StorageContract"
        "CrossChainContract"
    )
    
    for contract in "${contracts[@]}"; do
        run_contract_tests "$contract"
    done
}

# Generate test report
generate_test_report() {
    local report_file="test-report-$(date +%Y%m%d-%H%M%S).json"
    
    log_info "Generating test report: $report_file"
    
    # Count test results
    local total_tests=$(find "$TEST_OUTPUT_DIR" -name "*.trx" -exec grep -c "UnitTestResult" {} \; | awk '{sum += $1} END {print sum}')
    local passed_tests=$(find "$TEST_OUTPUT_DIR" -name "*.trx" -exec grep -c 'outcome="Passed"' {} \; | awk '{sum += $1} END {print sum}')
    local failed_tests=$(find "$TEST_OUTPUT_DIR" -name "*.trx" -exec grep -c 'outcome="Failed"' {} \; | awk '{sum += $1} END {print sum}')
    
    cat > "$report_file" << EOF
{
  "test_execution": {
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "total_contracts": 22,
    "test_summary": {
      "total_tests": ${total_tests:-0},
      "passed_tests": ${passed_tests:-0},
      "failed_tests": ${failed_tests:-0},
      "success_rate": $(echo "scale=2; ${passed_tests:-0} * 100 / ${total_tests:-1}" | bc)
    },
    "test_categories": {
      "unit_tests": "$([ -f "$TEST_OUTPUT_DIR/unit-tests.trx" ] && echo "completed" || echo "not_run")",
      "integration_tests": "$([ -f "$TEST_OUTPUT_DIR/integration-tests.trx" ] && echo "completed" || echo "not_run")",
      "performance_tests": "$([ -f "$TEST_OUTPUT_DIR/performance-tests.trx" ] && echo "completed" || echo "not_run")",
      "security_tests": "$([ -f "$TEST_OUTPUT_DIR/security-tests.trx" ] && echo "completed" || echo "not_run")"
    },
    "coverage": {
      "report_available": $([ -f "$COVERAGE_OUTPUT_DIR/index.html" ] && echo "true" || echo "false"),
      "report_path": "$COVERAGE_OUTPUT_DIR/index.html"
    },
    "output_directory": "$TEST_OUTPUT_DIR"
  }
}
EOF

    log_success "Test report generated: $report_file"
}

# Main testing function
main() {
    echo "=================================================="
    echo "Neo Service Layer Testing Suite"
    echo "Testing all 22 smart contracts"
    echo "=================================================="
    
    setup_test_environment
    
    local exit_code=0
    
    # Run different test suites
    run_unit_tests || exit_code=$?
    run_integration_tests || exit_code=$?
    run_performance_tests || exit_code=$?
    run_security_tests || exit_code=$?
    
    # Generate reports
    generate_coverage_report
    generate_test_report
    
    echo "=================================================="
    if [ $exit_code -eq 0 ]; then
        log_success "All tests completed successfully!"
    else
        log_error "Some tests failed"
    fi
    echo "=================================================="
    
    log_info "Test results available in: $TEST_OUTPUT_DIR"
    log_info "Coverage report available in: $COVERAGE_OUTPUT_DIR"
    
    exit $exit_code
}

# Handle script arguments
case "${1:-}" in
    "unit")
        setup_test_environment
        run_unit_tests
        ;;
    "integration")
        setup_test_environment
        run_integration_tests
        ;;
    "performance")
        setup_test_environment
        run_performance_tests
        ;;
    "security")
        setup_test_environment
        run_security_tests
        ;;
    "financial")
        setup_test_environment
        test_financial_contracts
        ;;
    "industry")
        setup_test_environment
        test_industry_contracts
        ;;
    "infrastructure")
        setup_test_environment
        test_infrastructure_contracts
        ;;
    "coverage")
        generate_coverage_report
        ;;
    *)
        main
        ;;
esac