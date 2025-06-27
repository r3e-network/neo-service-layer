#!/bin/bash

# Neo Service Layer Build Script
# Complete build pipeline for all 22 smart contracts

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

# Configuration
BUILD_CONFIGURATION="Release"
SKIP_TESTS=${SKIP_TESTS:-false}
SKIP_VERIFICATION=${SKIP_VERIFICATION:-false}

# Build pipeline steps
setup_environment() {
    log_info "Setting up build environment..."
    
    # Navigate to project root
    cd "$(dirname "$0")/.."
    
    # Check prerequisites
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK not found"
        log_info "Run './scripts/setup.sh' to install prerequisites"
        exit 1
    fi
    
    # Make scripts executable
    chmod +x scripts/*.sh
    
    log_success "Build environment ready"
}

# Clean previous builds
clean_previous_builds() {
    log_info "Cleaning previous builds..."
    
    ./scripts/clean.sh build
    
    log_success "Previous builds cleaned"
}

# Restore dependencies
restore_dependencies() {
    log_info "Restoring NuGet packages..."
    
    dotnet restore --verbosity minimal
    
    if [ $? -eq 0 ]; then
        log_success "Dependencies restored successfully"
    else
        log_error "Failed to restore dependencies"
        exit 1
    fi
}

# Build .NET project
build_dotnet_project() {
    log_info "Building .NET project..."
    
    dotnet build \
        --configuration $BUILD_CONFIGURATION \
        --no-restore \
        --verbosity minimal
    
    if [ $? -eq 0 ]; then
        log_success ".NET project built successfully"
    else
        log_error "Failed to build .NET project"
        exit 1
    fi
}

# Compile smart contracts
compile_contracts() {
    log_info "Compiling smart contracts..."
    
    ./scripts/compile.sh compile
    
    if [ $? -eq 0 ]; then
        log_success "Smart contracts compiled successfully"
    else
        log_error "Failed to compile smart contracts"
        exit 1
    fi
}

# Run tests
run_tests() {
    if [ "$SKIP_TESTS" = "true" ]; then
        log_warning "Skipping tests (SKIP_TESTS=true)"
        return 0
    fi
    
    log_info "Running tests..."
    
    ./scripts/test.sh unit
    
    if [ $? -eq 0 ]; then
        log_success "Tests passed successfully"
    else
        log_error "Tests failed"
        exit 1
    fi
}

# Verify build
verify_build() {
    if [ "$SKIP_VERIFICATION" = "true" ]; then
        log_warning "Skipping verification (SKIP_VERIFICATION=true)"
        return 0
    fi
    
    log_info "Verifying build..."
    
    ./scripts/verify.sh
    
    if [ $? -eq 0 ]; then
        log_success "Build verification passed"
    else
        log_error "Build verification failed"
        exit 1
    fi
}

# Generate build report
generate_build_report() {
    local report_file="build-report-$(date +%Y%m%d-%H%M%S).json"
    
    log_info "Generating build report: $report_file"
    
    # Count compiled contracts
    local nef_count=$(find "./bin/contracts" -name "*.nef" 2>/dev/null | wc -l)
    local manifest_count=$(find "./manifests" -name "*.manifest.json" 2>/dev/null | wc -l)
    
    # Calculate total size
    local total_size=0
    if [ -d "./bin/contracts" ]; then
        total_size=$(find "./bin/contracts" -name "*.nef" -exec stat -f%z {} \; 2>/dev/null | awk '{sum += $1} END {print sum}' || find "./bin/contracts" -name "*.nef" -exec stat -c%s {} \; | awk '{sum += $1} END {print sum}')
    fi
    
    cat > "$report_file" << EOF
{
  "build": {
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "configuration": "$BUILD_CONFIGURATION",
    "status": "completed",
    "dotnet_version": "$(dotnet --version)",
    "neo_compiler_version": "3.6.3",
    "contracts": {
      "total_expected": 22,
      "compiled": $nef_count,
      "manifests": $manifest_count,
      "total_size_bytes": ${total_size:-0}
    },
    "pipeline": {
      "setup": "completed",
      "clean": "completed",
      "restore": "completed",
      "build": "completed",
      "compile": "completed",
      "test": "$([ "$SKIP_TESTS" = "true" ] && echo "skipped" || echo "completed")",
      "verify": "$([ "$SKIP_VERIFICATION" = "true" ] && echo "skipped" || echo "completed")"
    },
    "artifacts": {
      "contracts_directory": "./bin/contracts",
      "manifests_directory": "./manifests",
      "test_results": "./test-results",
      "coverage_reports": "./coverage"
    }
  }
}
EOF

    log_success "Build report generated: $report_file"
}

# Package artifacts
package_artifacts() {
    log_info "Packaging build artifacts..."
    
    local package_name="neo-service-layer-$(date +%Y%m%d-%H%M%S).tar.gz"
    
    # Create package with all artifacts
    tar -czf "$package_name" \
        bin/contracts/ \
        manifests/ \
        NeoServiceLayer.csproj \
        neo-compiler.config.json \
        README.md \
        scripts/ \
        --exclude="scripts/*.log" \
        --exclude="scripts/temp*" 2>/dev/null || true
    
    if [ -f "$package_name" ]; then
        local package_size=$(stat -f%z "$package_name" 2>/dev/null || stat -c%s "$package_name")
        local package_size_mb=$((package_size / 1024 / 1024))
        
        log_success "Build package created: $package_name (${package_size_mb}MB)"
    else
        log_warning "Failed to create build package"
    fi
}

# Show build summary
show_build_summary() {
    log_info "Build Summary:"
    
    # Contract count
    local nef_count=$(find "./bin/contracts" -name "*.nef" 2>/dev/null | wc -l)
    log_info "  Compiled contracts: $nef_count/22"
    
    # Total size
    if [ -d "./bin/contracts" ]; then
        local total_size=$(find "./bin/contracts" -name "*.nef" -exec stat -f%z {} \; 2>/dev/null | awk '{sum += $1} END {print sum}' || find "./bin/contracts" -name "*.nef" -exec stat -c%s {} \; | awk '{sum += $1} END {print sum}')
        local total_size_kb=$((total_size / 1024))
        log_info "  Total size: ${total_size_kb}KB"
    fi
    
    # Build configuration
    log_info "  Configuration: $BUILD_CONFIGURATION"
    log_info "  Tests: $([ "$SKIP_TESTS" = "true" ] && echo "skipped" || echo "passed")"
    log_info "  Verification: $([ "$SKIP_VERIFICATION" = "true" ] && echo "skipped" || echo "passed")"
    
    # Available artifacts
    log_info "  Artifacts:"
    log_info "    - NEF files: ./bin/contracts/"
    log_info "    - Manifests: ./manifests/"
    if [ -d "./test-results" ]; then
        log_info "    - Test results: ./test-results/"
    fi
    if [ -d "./coverage" ]; then
        log_info "    - Coverage: ./coverage/"
    fi
}

# Main build function
main() {
    echo "=================================================="
    echo "Neo Service Layer Build Pipeline"
    echo "Building all 22 smart contracts"
    echo "Configuration: $BUILD_CONFIGURATION"
    echo "=================================================="
    
    local start_time=$(date +%s)
    
    # Execute build pipeline
    setup_environment
    clean_previous_builds
    restore_dependencies
    build_dotnet_project
    compile_contracts
    run_tests
    verify_build
    
    # Generate outputs
    generate_build_report
    package_artifacts
    
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    
    echo "=================================================="
    log_success "Build completed successfully in ${duration}s!"
    echo "=================================================="
    
    show_build_summary
    
    log_info "Next steps:"
    log_info "1. Deploy contracts: ./scripts/deploy.sh deploy"
    log_info "2. Run integration tests: ./scripts/test.sh integration"
    log_info "3. View coverage: open ./coverage/index.html"
}

# Handle script arguments
case "${1:-}" in
    "clean")
        setup_environment
        clean_previous_builds
        ;;
    "restore")
        setup_environment
        restore_dependencies
        ;;
    "compile")
        setup_environment
        restore_dependencies
        build_dotnet_project
        compile_contracts
        ;;
    "test")
        setup_environment
        restore_dependencies
        build_dotnet_project
        compile_contracts
        run_tests
        ;;
    "verify")
        setup_environment
        verify_build
        ;;
    "package")
        setup_environment
        package_artifacts
        ;;
    "fast")
        SKIP_TESTS=true
        SKIP_VERIFICATION=true
        main
        ;;
    *)
        main
        ;;
esac