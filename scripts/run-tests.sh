#!/bin/bash

# Neo Service Layer Test Execution Script
# Executes tests systematically with build validation and coverage analysis

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
RESULTS_DIR="$PROJECT_ROOT/test-results"
LOGS_DIR="$RESULTS_DIR/logs"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Create results directories
mkdir -p "$RESULTS_DIR" "$LOGS_DIR"

echo -e "${BLUE}=== Neo Service Layer Test Execution ===${NC}"
echo "Project Root: $PROJECT_ROOT"
echo "Results Dir: $RESULTS_DIR"
echo ""

# Test categories based on successful builds
declare -a WORKING_PROJECTS=(
    "tests/Core/NeoServiceLayer.Shared.Tests"
    "tests/Core/NeoServiceLayer.ServiceFramework.Tests"
    "tests/Services/NeoServiceLayer.Services.Storage.Tests"
    "tests/Common/NeoServiceLayer.Tests.Common"
    "tests/TestInfrastructure/NeoServiceLayer.TestInfrastructure"
)

declare -a INFRASTRUCTURE_PROJECTS=(
    "tests/Infrastructure/NeoServiceLayer.Infrastructure.Persistence.Tests"
    "tests/Infrastructure/NeoServiceLayer.Infrastructure.Blockchain.Tests"
    "tests/Infrastructure/NeoServiceLayer.Infrastructure.Caching.Tests"
)

declare -a INTEGRATION_PROJECTS=(
    "tests/Integration/NeoServiceLayer.Integration.Tests"
    "tests/Integration/NeoServiceLayer.Authentication.Integration.Tests"
)

declare -a TEE_PROJECTS=(
    "tests/Tee/NeoServiceLayer.Tee.Host.Tests"
    "tests/Tee/NeoServiceLayer.Tee.Enclave.Tests"
)

# Function to test project build
test_build() {
    local project_path="$1"
    local project_name=$(basename "$project_path")
    
    echo -e "${BLUE}Building: $project_name${NC}"
    
    if dotnet build "$PROJECT_ROOT/$project_path" --no-restore > "$LOGS_DIR/${project_name}_build.log" 2>&1; then
        echo -e "${GREEN}✅ Build succeeded: $project_name${NC}"
        return 0
    else
        echo -e "${RED}❌ Build failed: $project_name${NC}"
        echo "  Log: $LOGS_DIR/${project_name}_build.log"
        return 1
    fi
}

# Function to run tests
run_tests() {
    local project_path="$1"
    local project_name=$(basename "$project_path")
    
    echo -e "${BLUE}Testing: $project_name${NC}"
    
    if dotnet test "$PROJECT_ROOT/$project_path" \
        --no-build \
        --verbosity normal \
        --collect:"XPlat Code Coverage" \
        --results-directory "$RESULTS_DIR/$project_name" \
        --logger "trx;LogFileName=${project_name}_results.trx" \
        > "$LOGS_DIR/${project_name}_test.log" 2>&1; then
        
        echo -e "${GREEN}✅ Tests passed: $project_name${NC}"
        
        # Extract test summary
        if grep -q "Passed!" "$LOGS_DIR/${project_name}_test.log"; then
            grep "Passed\|Failed\|Skipped" "$LOGS_DIR/${project_name}_test.log" | tail -1
        fi
        
        return 0
    else
        echo -e "${RED}❌ Tests failed: $project_name${NC}"
        echo "  Log: $LOGS_DIR/${project_name}_test.log"
        return 1
    fi
}

# Function to analyze coverage
analyze_coverage() {
    local project_path="$1"
    local project_name=$(basename "$project_path")
    
    local coverage_file=$(find "$RESULTS_DIR/$project_name" -name "coverage.cobertura.xml" 2>/dev/null | head -1)
    
    if [[ -f "$coverage_file" ]]; then
        echo -e "${BLUE}Coverage: $project_name${NC}"
        
        # Extract line coverage percentage
        local line_coverage=$(grep 'line-rate=' "$coverage_file" | head -1 | sed 's/.*line-rate="\([^"]*\)".*/\1/')
        local branch_coverage=$(grep 'branch-rate=' "$coverage_file" | head -1 | sed 's/.*branch-rate="\([^"]*\)".*/\1/')
        
        if [[ -n "$line_coverage" ]]; then
            local line_percent=$(echo "$line_coverage * 100" | bc -l | cut -d. -f1)
            local branch_percent=$(echo "$branch_coverage * 100" | bc -l | cut -d. -f1)
            
            echo "  Line Coverage: ${line_percent}%"
            echo "  Branch Coverage: ${branch_percent}%"
        else
            echo "  Coverage data not available"
        fi
    else
        echo -e "${YELLOW}⚠️  No coverage file found for $project_name${NC}"
    fi
}

# Test summary variables
declare -i passed_builds=0
declare -i failed_builds=0
declare -i passed_tests=0
declare -i failed_tests=0

# Execute tests by category
echo -e "\n${BLUE}=== Testing Working Projects ===${NC}"
for project in "${WORKING_PROJECTS[@]}"; do
    if test_build "$project"; then
        ((passed_builds++))
        if run_tests "$project"; then
            ((passed_tests++))
            analyze_coverage "$project"
        else
            ((failed_tests++))
        fi
    else
        ((failed_builds++))
    fi
    echo ""
done

echo -e "\n${BLUE}=== Testing Infrastructure Projects ===${NC}"
for project in "${INFRASTRUCTURE_PROJECTS[@]}"; do
    if test_build "$project"; then
        ((passed_builds++))
        if run_tests "$project"; then
            ((passed_tests++))
            analyze_coverage "$project"
        else
            ((failed_tests++))
        fi
    else
        ((failed_builds++))
    fi
    echo ""
done

echo -e "\n${BLUE}=== Testing Integration Projects ===${NC}"
for project in "${INTEGRATION_PROJECTS[@]}"; do
    if test_build "$project"; then
        ((passed_builds++))
        if run_tests "$project"; then
            ((passed_tests++))
            analyze_coverage "$project"
        else
            ((failed_tests++))
        fi
    else
        ((failed_builds++))
    fi
    echo ""
done

echo -e "\n${BLUE}=== Testing TEE Projects ===${NC}"
for project in "${TEE_PROJECTS[@]}"; do
    if test_build "$project"; then
        ((passed_builds++))
        if run_tests "$project"; then
            ((passed_tests++))
            analyze_coverage "$project"
        else
            ((failed_tests++))
        fi
    else
        ((failed_builds++))
    fi
    echo ""
done

# Generate summary report
echo -e "\n${BLUE}=== Test Execution Summary ===${NC}"
echo "Build Results:"
echo "  ✅ Successful builds: $passed_builds"
echo "  ❌ Failed builds: $failed_builds"
echo ""
echo "Test Results:"
echo "  ✅ Passed test suites: $passed_tests"
echo "  ❌ Failed test suites: $failed_tests"
echo ""
echo "Logs and results saved to: $RESULTS_DIR"
echo ""

# Generate coverage summary
echo -e "${BLUE}=== Coverage Summary ===${NC}"
find "$RESULTS_DIR" -name "coverage.cobertura.xml" | while read coverage_file; do
    local project_dir=$(dirname "$coverage_file")
    local project_name=$(basename "$project_dir")
    
    local line_coverage=$(grep 'line-rate=' "$coverage_file" | head -1 | sed 's/.*line-rate="\([^"]*\)".*/\1/' 2>/dev/null)
    
    if [[ -n "$line_coverage" && "$line_coverage" != "line-rate=" ]]; then
        local line_percent=$(echo "$line_coverage * 100" | bc -l 2>/dev/null | cut -d. -f1 2>/dev/null)
        echo "$project_name: ${line_percent}% line coverage"
    fi
done

# Exit with error if any tests failed
if [[ $failed_builds -gt 0 || $failed_tests -gt 0 ]]; then
    echo -e "\n${RED}❌ Some tests failed. Check logs for details.${NC}"
    exit 1
else
    echo -e "\n${GREEN}✅ All tests passed successfully!${NC}"
    exit 0
fi