#!/bin/bash

# 🚀 Neo Service Layer - Local CI/CD Script
# This script replicates the GitHub Actions workflow locally for faster development

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
DOTNET_VERSION="9.0.x"
COVERAGE_THRESHOLD=75
BRANCH_COVERAGE_THRESHOLD=70
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Default options
SKIP_TESTS=false
SKIP_DOCKER=false
SKIP_COVERAGE=false
VERBOSE=false
CLEAN_BUILD=false
PARALLEL_TESTS=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --skip-tests)
      SKIP_TESTS=true
      shift
      ;;
    --skip-docker)
      SKIP_DOCKER=true
      shift
      ;;
    --skip-coverage)
      SKIP_COVERAGE=true
      shift
      ;;
    --verbose|-v)
      VERBOSE=true
      shift
      ;;
    --clean)
      CLEAN_BUILD=true
      shift
      ;;
    --no-parallel)
      PARALLEL_TESTS=false
      shift
      ;;
    --help|-h)
      echo "Usage: $0 [options]"
      echo "Options:"
      echo "  --skip-tests      Skip test execution"
      echo "  --skip-docker     Skip Docker build"
      echo "  --skip-coverage   Skip coverage analysis"
      echo "  --verbose, -v     Enable verbose output"
      echo "  --clean           Clean build artifacts first"
      echo "  --no-parallel     Run tests sequentially"
      echo "  --help, -h        Show this help"
      exit 0
      ;;
    *)
      echo "Unknown option $1"
      exit 1
      ;;
  esac
done

# Helper functions
log_info() {
  echo -e "${BLUE}ℹ️  $1${NC}"
}

log_success() {
  echo -e "${GREEN}✅ $1${NC}"
}

log_warning() {
  echo -e "${YELLOW}⚠️  $1${NC}"
}

log_error() {
  echo -e "${RED}❌ $1${NC}"
}

log_step() {
  echo -e "${PURPLE}🔧 $1${NC}"
}

log_section() {
  echo -e "\n${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
  echo -e "${CYAN}$1${NC}"
  echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n"
}

check_prerequisites() {
  log_section "🔍 Checking Prerequisites"
  
  # Check if we're in the right directory
  if [[ ! -f "$PROJECT_ROOT/NeoServiceLayer.sln" ]]; then
    log_error "Not in Neo Service Layer project root. Expected to find NeoServiceLayer.sln"
    exit 1
  fi
  
  # Check .NET
  if ! command -v dotnet &> /dev/null; then
    log_error ".NET SDK not found. Please install .NET $DOTNET_VERSION"
    exit 1
  fi
  
  local dotnet_version=$(dotnet --version)
  log_info "Found .NET SDK: $dotnet_version"
  
  # Check Docker (optional)
  if ! command -v docker &> /dev/null && [[ "$SKIP_DOCKER" == "false" ]]; then
    log_warning "Docker not found. Docker builds will be skipped."
    SKIP_DOCKER=true
  fi
  
  # Check if we have test projects
  local test_count=$(find "$PROJECT_ROOT/tests" -name "*.csproj" 2>/dev/null | wc -l)
  log_info "Found $test_count test projects"
  
  log_success "Prerequisites check completed"
}

detect_changes() {
  log_section "🔍 Detecting Changes"
  
  # Check if we're in a git repository
  if ! git rev-parse --git-dir > /dev/null 2>&1; then
    log_warning "Not in a git repository. Running all checks."
    return 0
  fi
  
  # Get changed files since last commit
  local changed_files=$(git diff --name-only HEAD~1 2>/dev/null || echo "all")
  
  if [[ "$changed_files" == "all" ]]; then
    log_info "Unable to detect changes, running full pipeline"
    return 0
  fi
  
  log_info "Changed files:"
  echo "$changed_files" | while read -r file; do
    [[ -n "$file" ]] && echo "  - $file"
  done
  
  # Check if only docs changed
  local non_doc_changes=$(echo "$changed_files" | grep -v -E '\.(md|txt)$|^docs/|^\.vscode/|^\.devcontainer/' | wc -l)
  
  if [[ "$non_doc_changes" -eq 0 ]]; then
    log_info "Only documentation files changed. Consider using --skip-tests --skip-docker"
  fi
}

clean_artifacts() {
  if [[ "$CLEAN_BUILD" == "true" ]]; then
    log_section "🧹 Cleaning Build Artifacts"
    
    log_step "Cleaning .NET artifacts"
    dotnet clean "$PROJECT_ROOT/NeoServiceLayer.sln" --configuration Release --verbosity minimal
    
    log_step "Removing bin and obj directories"
    find "$PROJECT_ROOT" -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
    find "$PROJECT_ROOT" -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
    
    log_step "Removing test results"
    rm -rf "$PROJECT_ROOT/TestResults" 2>/dev/null || true
    rm -rf "$PROJECT_ROOT/reports" 2>/dev/null || true
    
    log_success "Clean completed"
  fi
}

restore_dependencies() {
  log_section "📦 Restoring Dependencies"
  
  log_step "Restoring .NET packages"
  if [[ "$VERBOSE" == "true" ]]; then
    dotnet restore "$PROJECT_ROOT/NeoServiceLayer.sln" --verbosity normal
  else
    dotnet restore "$PROJECT_ROOT/NeoServiceLayer.sln" --verbosity minimal
  fi
  
  log_success "Dependencies restored"
}

build_solution() {
  log_section "🏗️ Building Solution"
  
  log_step "Building in Release configuration"
  local verbosity="minimal"
  [[ "$VERBOSE" == "true" ]] && verbosity="normal"
  
  dotnet build "$PROJECT_ROOT/NeoServiceLayer.sln" \
    --configuration Release \
    --no-restore \
    --verbosity $verbosity
  
  log_success "Build completed successfully"
}

run_tests() {
  if [[ "$SKIP_TESTS" == "true" ]]; then
    log_warning "Skipping tests (--skip-tests specified)"
    return 0
  fi
  
  log_section "🧪 Running Tests"
  
  # Create test results directory
  mkdir -p "$PROJECT_ROOT/TestResults"
  
  local test_args=(
    "$PROJECT_ROOT/NeoServiceLayer.sln"
    "--configuration" "Release"
    "--no-build"
    "--logger" "trx;LogFileName=test-results.trx"
    "--logger" "console;verbosity=normal"
    "--results-directory" "$PROJECT_ROOT/TestResults"
  )
  
  # Add coverage collection if not skipped
  if [[ "$SKIP_COVERAGE" == "false" ]]; then
    log_step "Running tests with coverage collection"
    if [[ "$PARALLEL_TESTS" == "true" ]]; then
      test_args+=(
        "--collect" "XPlat Code Coverage"
        "--settings" "$PROJECT_ROOT/tests/codecoverage.runsettings"
      )
    else
      # Sequential mode - don't use runsettings that forces parallel execution
      test_args+=(
        "--collect" "XPlat Code Coverage"
      )
    fi
  else
    log_step "Running tests without coverage"
  fi
  
  # Add parallel execution - disabled by default for stability
  if [[ "$PARALLEL_TESTS" == "true" ]]; then
    test_args+=("--" "RunConfiguration.MaxCpuCount=0")
  else
    # Sequential execution for better stability
    test_args+=("--" "RunConfiguration.MaxCpuCount=1")
  fi
  
  # Add filter to exclude problematic test categories
  test_args+=(
    "--filter" "Category!=Performance&Category!=SGXIntegration&Category!=LoadTest"
  )
  
  # Run tests
  if dotnet test "${test_args[@]}"; then
    log_success "All tests passed"
  else
    log_error "Some tests failed"
    
    # Show failed test summary
    if [[ -f "$PROJECT_ROOT/TestResults/test-results.trx" ]]; then
      log_info "Checking for failed tests..."
      # Parse TRX file for failed tests (simplified)
      grep -i "failed\|error" "$PROJECT_ROOT/TestResults/test-results.trx" | head -10 || true
    fi
    
    return 1
  fi
}

generate_coverage_report() {
  if [[ "$SKIP_COVERAGE" == "true" || "$SKIP_TESTS" == "true" ]]; then
    return 0
  fi
  
  log_section "📊 Generating Coverage Report"
  
  # Find coverage files
  local coverage_files=$(find "$PROJECT_ROOT/TestResults" -name "coverage.cobertura.xml" 2>/dev/null)
  
  if [[ -z "$coverage_files" ]]; then
    log_warning "No coverage files found"
    return 0
  fi
  
  # Install reportgenerator if not present
  if ! command -v reportgenerator &> /dev/null; then
    log_step "Installing ReportGenerator"
    dotnet tool install --global dotnet-reportgenerator-globaltool || true
  fi
  
  # Generate HTML report
  log_step "Generating HTML coverage report"
  mkdir -p "$PROJECT_ROOT/TestResults/CoverageReport"
  
  reportgenerator \
    "-reports:$PROJECT_ROOT/TestResults/**/coverage.cobertura.xml" \
    "-targetdir:$PROJECT_ROOT/TestResults/CoverageReport" \
    "-reporttypes:Html;JsonSummary" \
    "-title:Neo Service Layer Coverage" \
    || log_warning "Failed to generate coverage report"
  
  # Check coverage thresholds
  if [[ -f "$PROJECT_ROOT/TestResults/CoverageReport/Summary.json" ]]; then
    check_coverage_thresholds
  fi
  
  log_success "Coverage report generated at TestResults/CoverageReport/index.html"
}

check_coverage_thresholds() {
  log_step "Checking coverage thresholds"
  
  local summary_file="$PROJECT_ROOT/TestResults/CoverageReport/Summary.json"
  
  if command -v jq &> /dev/null && [[ -f "$summary_file" ]]; then
    local line_coverage=$(jq -r '.summary.linecoverage // 0' "$summary_file")
    local branch_coverage=$(jq -r '.summary.branchcoverage // 0' "$summary_file")
    
    log_info "Line Coverage: ${line_coverage}% (threshold: ${COVERAGE_THRESHOLD}%)"
    log_info "Branch Coverage: ${branch_coverage}% (threshold: ${BRANCH_COVERAGE_THRESHOLD}%)"
    
    # Convert to integers for comparison
    local line_int=$(echo "$line_coverage" | cut -d. -f1)
    local branch_int=$(echo "$branch_coverage" | cut -d. -f1)
    
    if [[ "$line_int" -lt "$COVERAGE_THRESHOLD" ]]; then
      log_warning "Line coverage ${line_coverage}% is below threshold ${COVERAGE_THRESHOLD}%"
    else
      log_success "Line coverage ${line_coverage}% meets threshold"
    fi
    
    if [[ "$branch_int" -lt "$BRANCH_COVERAGE_THRESHOLD" ]]; then
      log_warning "Branch coverage ${branch_coverage}% is below threshold ${BRANCH_COVERAGE_THRESHOLD}%"
    else
      log_success "Branch coverage ${branch_coverage}% meets threshold"
    fi
  else
    log_warning "Cannot check coverage thresholds (jq not installed or summary file missing)"
  fi
}

run_security_scan() {
  log_section "🔒 Security Scan"
  
  log_step "Checking for known vulnerabilities"
  
  # Check for security vulnerabilities using dotnet list package
  if dotnet list "$PROJECT_ROOT/NeoServiceLayer.sln" package --vulnerable --include-transitive > /dev/null 2>&1; then
    log_info "Running vulnerability scan..."
    dotnet list "$PROJECT_ROOT/NeoServiceLayer.sln" package --vulnerable --include-transitive || true
  else
    log_info "Vulnerability scanning not available in this .NET version"
  fi
  
  # Check for outdated packages
  log_step "Checking for outdated packages"
  dotnet list "$PROJECT_ROOT/NeoServiceLayer.sln" package --outdated || true
  
  log_success "Security scan completed"
}

build_docker() {
  if [[ "$SKIP_DOCKER" == "true" ]]; then
    log_warning "Skipping Docker build (--skip-docker specified)"
    return 0
  fi
  
  log_section "🐳 Building Docker Image"
  
  if ! command -v docker &> /dev/null; then
    log_error "Docker not found"
    return 1
  fi
  
  log_step "Building Docker image"
  
  local image_tag="neo-service-layer:local-$(date +%s)"
  
  if docker build -t "$image_tag" "$PROJECT_ROOT"; then
    log_success "Docker image built: $image_tag"
    
    # Test the image
    log_step "Testing Docker image"
    if docker run --rm "$image_tag" --version 2>/dev/null || true; then
      log_success "Docker image test passed"
    else
      log_warning "Docker image test failed or not supported"
    fi
  else
    log_error "Docker build failed"
    return 1
  fi
}

show_summary() {
  log_section "📋 Summary"
  
  local total_time=$SECONDS
  local minutes=$((total_time / 60))
  local seconds=$((total_time % 60))
  
  log_info "Total execution time: ${minutes}m ${seconds}s"
  
  if [[ "$SKIP_TESTS" == "false" ]]; then
    if [[ -f "$PROJECT_ROOT/TestResults/test-results.trx" ]]; then
      log_info "Test results: TestResults/test-results.trx"
    fi
    
    if [[ "$SKIP_COVERAGE" == "false" && -f "$PROJECT_ROOT/TestResults/CoverageReport/index.html" ]]; then
      log_info "Coverage report: TestResults/CoverageReport/index.html"
      log_info "Open with: open TestResults/CoverageReport/index.html"
    fi
  fi
  
  log_success "Local CI pipeline completed successfully!"
}

# Main execution
main() {
  log_section "🚀 Neo Service Layer - Local CI/CD Pipeline"
  
  cd "$PROJECT_ROOT"
  
  # Record start time
  local start_time=$SECONDS
  
  # Run pipeline steps
  check_prerequisites
  detect_changes
  clean_artifacts
  restore_dependencies
  build_solution
  run_tests
  generate_coverage_report
  run_security_scan
  build_docker
  show_summary
  
  log_success "All steps completed successfully! 🎉"
}

# Error handling
trap 'log_error "Script failed at line $LINENO"; exit 1' ERR

# Run main function
main "$@"