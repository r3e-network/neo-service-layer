#!/bin/bash

echo "==========================================="
echo "   NeoServiceLayer Test Suite Execution   "
echo "==========================================="
echo ""

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counters
total_tests=0
passed_tests=0
failed_tests=0
skipped_tests=0

# Function to run tests
run_test() {
    local project=$1
    local name=$(basename $project .csproj)
    
    echo -e "${YELLOW}Testing: $name${NC}"
    
    # Check if binary exists
    local dll_path="tests/${project#*/tests/}/bin/Debug/net9.0/${name}.dll"
    dll_path="${dll_path%.csproj*}.dll"
    
    if [ -f "$dll_path" ]; then
        result=$(dotnet vstest "$dll_path" --logger:"console;verbosity=minimal" 2>/dev/null | grep -E "Passed!|Failed:")
        if [ -n "$result" ]; then
            echo "$result"
            # Extract counts
            if [[ $result =~ Passed:\ +([0-9]+) ]]; then
                passed_tests=$((passed_tests + ${BASH_REMATCH[1]}))
            fi
            if [[ $result =~ Failed:\ +([0-9]+) ]]; then
                failed_tests=$((failed_tests + ${BASH_REMATCH[1]}))
            fi
            if [[ $result =~ Total:\ +([0-9]+) ]]; then
                total_tests=$((total_tests + ${BASH_REMATCH[1]}))
            fi
        else
            echo -e "${RED}  Build not available${NC}"
        fi
    else
        echo -e "${RED}  Binary not found${NC}"
    fi
    echo ""
}

# Core Tests
echo "=== CORE TESTS ==="
echo ""
run_test "Core/NeoServiceLayer.Core.Tests/NeoServiceLayer.Core.Tests"
run_test "Core/NeoServiceLayer.Shared.Tests/NeoServiceLayer.Shared.Tests"
run_test "Core/NeoServiceLayer.ServiceFramework.Tests/NeoServiceLayer.ServiceFramework.Tests"

# Infrastructure Tests
echo "=== INFRASTRUCTURE TESTS ==="
echo ""
run_test "Infrastructure/NeoServiceLayer.Infrastructure.Tests/NeoServiceLayer.Infrastructure.Tests"
run_test "Infrastructure/NeoServiceLayer.Infrastructure.Security.Tests/NeoServiceLayer.Infrastructure.Security.Tests"

# Performance Tests
echo "=== PERFORMANCE TESTS ==="
echo ""
run_test "Performance/NeoServiceLayer.Performance.Tests/NeoServiceLayer.Performance.Tests"

# Integration Tests
echo "=== INTEGRATION TESTS ==="
echo ""
run_test "Integration/NeoServiceLayer.Integration.Tests/NeoServiceLayer.Integration.Tests"

# Summary
echo "==========================================="
echo "            TEST SUMMARY                  "
echo "==========================================="
echo -e "Total Tests:   $total_tests"
echo -e "${GREEN}Passed Tests:  $passed_tests${NC}"
echo -e "${RED}Failed Tests:  $failed_tests${NC}"
echo -e "Skipped Tests: $skipped_tests"
echo ""

if [ $failed_tests -eq 0 ] && [ $total_tests -gt 0 ]; then
    echo -e "${GREEN}✅ All tests passed successfully!${NC}"
else
    echo -e "${RED}❌ Some tests failed or no tests found${NC}"
fi