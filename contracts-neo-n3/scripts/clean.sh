#!/bin/bash

# Neo Service Layer Clean Script
# Cleans build artifacts, temporary files, and generated content

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

# Clean build artifacts
clean_build_artifacts() {
    log_info "Cleaning build artifacts..."
    
    # Navigate to project root
    cd "$(dirname "$0")/.."
    
    # Remove .NET build directories
    if [ -d "bin" ]; then
        rm -rf bin
        log_info "Removed bin directory"
    fi
    
    if [ -d "obj" ]; then
        rm -rf obj
        log_info "Removed obj directory"
    fi
    
    # Remove contract compilation outputs
    if [ -d "bin/contracts" ]; then
        rm -rf bin/contracts
        log_info "Removed compiled contracts"
    fi
    
    if [ -d "manifests" ]; then
        rm -rf manifests
        log_info "Removed contract manifests"
    fi
    
    log_success "Build artifacts cleaned"
}

# Clean temporary files
clean_temp_files() {
    log_info "Cleaning temporary files..."
    
    # Remove temporary directories
    find . -name "temp_*" -type d -exec rm -rf {} + 2>/dev/null || true
    find . -name "tmp_*" -type d -exec rm -rf {} + 2>/dev/null || true
    
    # Remove temporary files
    find . -name "*.tmp" -type f -delete 2>/dev/null || true
    find . -name "*.temp" -type f -delete 2>/dev/null || true
    find . -name ".DS_Store" -type f -delete 2>/dev/null || true
    
    # Remove backup files
    find . -name "*~" -type f -delete 2>/dev/null || true
    find . -name "*.bak" -type f -delete 2>/dev/null || true
    find . -name "*.orig" -type f -delete 2>/dev/null || true
    
    log_success "Temporary files cleaned"
}

# Clean test outputs
clean_test_outputs() {
    log_info "Cleaning test outputs..."
    
    # Remove test result directories
    if [ -d "test-results" ]; then
        rm -rf test-results
        log_info "Removed test results"
    fi
    
    if [ -d "TestResults" ]; then
        rm -rf TestResults
        log_info "Removed TestResults directory"
    fi
    
    if [ -d "coverage" ]; then
        rm -rf coverage
        log_info "Removed coverage reports"
    fi
    
    # Remove test output files
    find . -name "*.trx" -type f -delete 2>/dev/null || true
    find . -name "coverage.*.xml" -type f -delete 2>/dev/null || true
    
    log_success "Test outputs cleaned"
}

# Clean logs
clean_logs() {
    log_info "Cleaning log files..."
    
    # Remove log directories
    if [ -d "logs" ]; then
        rm -rf logs
        log_info "Removed logs directory"
    fi
    
    # Remove log files
    find . -name "*.log" -type f -delete 2>/dev/null || true
    find . -name "*.log.*" -type f -delete 2>/dev/null || true
    
    log_success "Log files cleaned"
}

# Clean reports
clean_reports() {
    log_info "Cleaning generated reports..."
    
    # Remove report files
    find . -name "*-report-*.json" -type f -delete 2>/dev/null || true
    find . -name "*-report-*.html" -type f -delete 2>/dev/null || true
    find . -name "*-report-*.xml" -type f -delete 2>/dev/null || true
    
    log_success "Generated reports cleaned"
}

# Clean NuGet packages (cache)
clean_nuget_cache() {
    log_info "Cleaning NuGet cache..."
    
    # Clear local NuGet cache
    dotnet nuget locals all --clear > /dev/null 2>&1 || true
    
    log_success "NuGet cache cleaned"
}

# Clean Docker artifacts
clean_docker_artifacts() {
    log_info "Cleaning Docker artifacts..."
    
    # Remove Docker build cache (if Docker is available)
    if command -v docker &> /dev/null; then
        # Remove dangling images
        docker image prune -f > /dev/null 2>&1 || true
        
        # Remove build cache
        docker builder prune -f > /dev/null 2>&1 || true
        
        log_success "Docker artifacts cleaned"
    else
        log_info "Docker not available, skipping Docker cleanup"
    fi
}

# Clean IDE files
clean_ide_files() {
    log_info "Cleaning IDE files..."
    
    # Remove Visual Studio files
    find . -name "*.suo" -type f -delete 2>/dev/null || true
    find . -name "*.user" -type f -delete 2>/dev/null || true
    find . -name "*.userosscache" -type f -delete 2>/dev/null || true
    find . -name "*.sln.docstates" -type f -delete 2>/dev/null || true
    
    # Remove VS Code files (optional)
    if [ "$1" = "all" ]; then
        if [ -d ".vscode" ]; then
            rm -rf .vscode
            log_info "Removed .vscode directory"
        fi
    fi
    
    # Remove JetBrains files
    if [ -d ".idea" ]; then
        rm -rf .idea
        log_info "Removed .idea directory"
    fi
    
    log_success "IDE files cleaned"
}

# Reset to clean state
reset_to_clean_state() {
    log_info "Resetting to clean state..."
    
    clean_build_artifacts
    clean_temp_files
    clean_test_outputs
    clean_logs
    clean_reports
    clean_nuget_cache
    clean_docker_artifacts
    clean_ide_files "$1"
    
    # Recreate necessary directories
    mkdir -p bin/contracts
    mkdir -p manifests
    mkdir -p logs
    
    log_success "Reset to clean state completed"
}

# Show disk space saved
show_disk_space_info() {
    log_info "Disk space information:"
    
    # Show current directory size
    if command -v du &> /dev/null; then
        local dir_size=$(du -sh . 2>/dev/null | cut -f1)
        log_info "Current directory size: $dir_size"
    fi
    
    # Show available disk space
    if command -v df &> /dev/null; then
        local available_space=$(df -h . | tail -1 | awk '{print $4}')
        log_info "Available disk space: $available_space"
    fi
}

# Main clean function
main() {
    echo "=================================================="
    echo "Neo Service Layer Clean"
    echo "Cleaning build artifacts and temporary files"
    echo "=================================================="
    
    # Navigate to project root
    cd "$(dirname "$0")/.."
    
    # Show initial disk space
    show_disk_space_info
    
    # Perform cleaning
    clean_build_artifacts
    clean_temp_files
    clean_test_outputs
    clean_logs
    clean_reports
    
    echo "=================================================="
    log_success "Cleaning completed successfully!"
    echo "=================================================="
    
    # Show final disk space
    show_disk_space_info
    
    log_info "Next steps:"
    log_info "1. Restore packages: dotnet restore"
    log_info "2. Compile contracts: ./scripts/compile.sh"
    log_info "3. Run tests: ./scripts/test.sh"
}

# Handle script arguments
case "${1:-}" in
    "build")
        cd "$(dirname "$0")/.."
        clean_build_artifacts
        ;;
    "temp")
        cd "$(dirname "$0")/.."
        clean_temp_files
        ;;
    "test")
        cd "$(dirname "$0")/.."
        clean_test_outputs
        ;;
    "logs")
        cd "$(dirname "$0")/.."
        clean_logs
        ;;
    "reports")
        cd "$(dirname "$0")/.."
        clean_reports
        ;;
    "nuget")
        cd "$(dirname "$0")/.."
        clean_nuget_cache
        ;;
    "docker")
        cd "$(dirname "$0")/.."
        clean_docker_artifacts
        ;;
    "ide")
        cd "$(dirname "$0")/.."
        clean_ide_files
        ;;
    "all")
        cd "$(dirname "$0")/.."
        reset_to_clean_state "all"
        ;;
    "reset")
        cd "$(dirname "$0")/.."
        reset_to_clean_state
        ;;
    *)
        main
        ;;
esac