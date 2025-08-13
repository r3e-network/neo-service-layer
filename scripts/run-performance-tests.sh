#!/bin/bash

# Neo Service Layer - Performance Testing Script
# This script runs the complete performance testing suite

set -euo pipefail

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
PERFORMANCE_TEST_DIR="$PROJECT_DIR/tests/Performance/NeoServiceLayer.Performance.Tests"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
DEFAULT_TEST_TYPE="all"
DEFAULT_OUTPUT_DIR="$PROJECT_DIR/performance-results"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# Function to print colored output
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to show usage
show_usage() {
    cat << EOF
Usage: $0 [OPTIONS] [TEST_TYPE]

Run Neo Service Layer performance tests and benchmarks.

TEST_TYPE:
    all                Run complete test suite (default)
    benchmark          Run benchmarks only
    regression         Run regression tests only
    caching           Run caching benchmarks only
    patterns          Run pattern recognition benchmarks only
    automation        Run automation service benchmarks only

OPTIONS:
    -o, --output DIR   Output directory for results (default: $DEFAULT_OUTPUT_DIR)
    -c, --config FILE  Configuration file path
    -p, --parallel     Run tests in parallel where possible
    -v, --verbose      Enable verbose output
    -h, --help         Show this help message

EXAMPLES:
    $0                              # Run all tests
    $0 benchmark                    # Run benchmarks only
    $0 regression                   # Run regression tests only
    $0 caching -v                   # Run caching benchmarks with verbose output
    $0 all -o ./results             # Run all tests with custom output directory

EOF
}

# Function to check prerequisites
check_prerequisites() {
    print_info "Checking prerequisites..."

    # Check if .NET is installed
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK is not installed or not in PATH"
        exit 1
    fi

    # Check .NET version
    local dotnet_version=$(dotnet --version)
    print_info "Found .NET version: $dotnet_version"

    # Check if performance test project exists
    if [[ ! -d "$PERFORMANCE_TEST_DIR" ]]; then
        print_error "Performance test directory not found: $PERFORMANCE_TEST_DIR"
        exit 1
    fi

    # Check if project file exists
    if [[ ! -f "$PERFORMANCE_TEST_DIR/NeoServiceLayer.Performance.Tests.csproj" ]]; then
        print_error "Performance test project file not found"
        exit 1
    fi

    print_success "Prerequisites check passed"
}

# Function to setup output directory
setup_output_directory() {
    local output_dir="$1"
    
    print_info "Setting up output directory: $output_dir"
    
    mkdir -p "$output_dir"
    mkdir -p "$output_dir/benchmarks"
    mkdir -p "$output_dir/regression"
    mkdir -p "$output_dir/reports"
    
    print_success "Output directory created: $output_dir"
}

# Function to build the performance test project
build_performance_tests() {
    print_info "Building performance test project..."
    
    cd "$PERFORMANCE_TEST_DIR"
    
    if dotnet build --configuration Release --no-restore; then
        print_success "Performance test project built successfully"
    else
        print_error "Failed to build performance test project"
        exit 1
    fi
}

# Function to restore packages
restore_packages() {
    print_info "Restoring NuGet packages..."
    
    cd "$PERFORMANCE_TEST_DIR"
    
    if dotnet restore; then
        print_success "Packages restored successfully"
    else
        print_error "Failed to restore packages"
        exit 1
    fi
}

# Function to run benchmarks
run_benchmarks() {
    local test_type="$1"
    local output_dir="$2"
    local verbose="$3"
    
    print_info "Running benchmarks: $test_type"
    
    cd "$PERFORMANCE_TEST_DIR"
    
    local verbose_flag=""
    if [[ "$verbose" == "true" ]]; then
        verbose_flag="--verbosity normal"
    fi
    
    local benchmark_args="benchmark"
    if [[ "$test_type" != "benchmark" && "$test_type" != "all" ]]; then
        benchmark_args="benchmark $test_type"
    fi
    
    if dotnet run --configuration Release --no-build $verbose_flag -- $benchmark_args; then
        print_success "Benchmarks completed successfully"
        
        # Copy results to output directory
        if [[ -d "BenchmarkDotNet.Artifacts" ]]; then
            cp -r BenchmarkDotNet.Artifacts/* "$output_dir/benchmarks/" 2>/dev/null || true
            print_info "Benchmark results copied to $output_dir/benchmarks/"
        fi
    else
        print_error "Benchmarks failed"
        return 1
    fi
}

# Function to run regression tests
run_regression_tests() {
    local output_dir="$1"
    local verbose="$2"
    
    print_info "Running regression tests..."
    
    cd "$PERFORMANCE_TEST_DIR"
    
    local verbose_flag=""
    if [[ "$verbose" == "true" ]]; then
        verbose_flag="--verbosity normal"
    fi
    
    if dotnet test --configuration Release --no-build $verbose_flag --logger "trx;LogFileName=regression-results.trx"; then
        print_success "Regression tests completed successfully"
        
        # Copy test results to output directory
        if [[ -d "TestResults" ]]; then
            cp -r TestResults/* "$output_dir/regression/" 2>/dev/null || true
            print_info "Regression test results copied to $output_dir/regression/"
        fi
    else
        print_error "Regression tests failed"
        return 1
    fi
}

# Function to generate performance report
generate_performance_report() {
    local output_dir="$1"
    local test_type="$2"
    
    print_info "Generating performance report..."
    
    cd "$PERFORMANCE_TEST_DIR"
    
    if dotnet run --configuration Release --no-build -- all > "$output_dir/reports/performance-report-$TIMESTAMP.txt" 2>&1; then
        print_success "Performance report generated: $output_dir/reports/performance-report-$TIMESTAMP.txt"
    else
        print_warning "Performance report generation had issues, but continuing..."
    fi
    
    # Create summary report
    create_summary_report "$output_dir" "$test_type"
}

# Function to create summary report
create_summary_report() {
    local output_dir="$1"
    local test_type="$2"
    local summary_file="$output_dir/performance-summary-$TIMESTAMP.md"
    
    cat > "$summary_file" << EOF
# Neo Service Layer Performance Test Summary

**Test Run:** $(date)
**Test Type:** $test_type
**Output Directory:** $output_dir

## Test Execution Summary

### Environment Information
- **Operating System:** $(uname -s)
- **Architecture:** $(uname -m)
- **.NET Version:** $(dotnet --version)
- **Hostname:** $(hostname)

### Results Location
- **Benchmark Results:** \`$output_dir/benchmarks/\`
- **Regression Results:** \`$output_dir/regression/\`
- **Reports:** \`$output_dir/reports/\`

### Files Generated
EOF

    # List generated files
    if [[ -d "$output_dir/benchmarks" ]]; then
        echo "" >> "$summary_file"
        echo "#### Benchmark Files" >> "$summary_file"
        find "$output_dir/benchmarks" -type f -name "*.json" -o -name "*.md" -o -name "*.html" | sed 's/^/- /' >> "$summary_file"
    fi
    
    if [[ -d "$output_dir/regression" ]]; then
        echo "" >> "$summary_file"
        echo "#### Regression Test Files" >> "$summary_file"
        find "$output_dir/regression" -type f -name "*.trx" -o -name "*.xml" | sed 's/^/- /' >> "$summary_file"
    fi
    
    cat >> "$summary_file" << EOF

## Next Steps

1. Review benchmark results for performance trends
2. Analyze regression test outcomes
3. Update baseline metrics if needed
4. Address any performance regressions identified

## Notes

- Use the generated HTML reports for detailed performance analysis
- JSON files can be used for automated performance monitoring
- Markdown files provide human-readable summaries

---
*Generated by Neo Service Layer Performance Testing Suite*
EOF

    print_success "Summary report created: $summary_file"
}

# Function to cleanup
cleanup() {
    print_info "Cleaning up temporary files..."
    
    cd "$PERFORMANCE_TEST_DIR"
    
    # Remove temporary build artifacts (but keep results)
    rm -rf bin/obj 2>/dev/null || true
    
    print_success "Cleanup completed"
}

# Main function
main() {
    local test_type="$DEFAULT_TEST_TYPE"
    local output_dir="$DEFAULT_OUTPUT_DIR"
    local config_file=""
    local parallel="false"
    local verbose="false"
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            -o|--output)
                output_dir="$2"
                shift 2
                ;;
            -c|--config)
                config_file="$2"
                shift 2
                ;;
            -p|--parallel)
                parallel="true"
                shift
                ;;
            -v|--verbose)
                verbose="true"
                shift
                ;;
            -h|--help)
                show_usage
                exit 0
                ;;
            all|benchmark|regression|caching|patterns|automation)
                test_type="$1"
                shift
                ;;
            *)
                print_error "Unknown option: $1"
                show_usage
                exit 1
                ;;
        esac
    done
    
    # Add timestamp to output directory
    output_dir="$output_dir/$TIMESTAMP"
    
    print_info "Starting Neo Service Layer Performance Tests"
    print_info "Test Type: $test_type"
    print_info "Output Directory: $output_dir"
    print_info "Verbose Mode: $verbose"
    
    # Run the test pipeline
    check_prerequisites
    setup_output_directory "$output_dir"
    restore_packages
    build_performance_tests
    
    local exit_code=0
    
    case "$test_type" in
        "benchmark"|"caching"|"patterns"|"automation")
            run_benchmarks "$test_type" "$output_dir" "$verbose" || exit_code=$?
            ;;
        "regression")
            run_regression_tests "$output_dir" "$verbose" || exit_code=$?
            ;;
        "all")
            run_benchmarks "benchmark" "$output_dir" "$verbose" || exit_code=$?
            run_regression_tests "$output_dir" "$verbose" || exit_code=$?
            ;;
        *)
            print_error "Invalid test type: $test_type"
            exit 1
            ;;
    esac
    
    # Generate reports regardless of test outcome
    generate_performance_report "$output_dir" "$test_type"
    cleanup
    
    if [[ $exit_code -eq 0 ]]; then
        print_success "Performance testing completed successfully!"
        print_info "Results available in: $output_dir"
    else
        print_error "Performance testing completed with errors (exit code: $exit_code)"
        print_info "Check results in: $output_dir"
    fi
    
    exit $exit_code
}

# Run main function with all arguments
main "$@"