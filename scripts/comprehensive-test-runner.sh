#!/bin/bash

echo "================================================="
echo "    NeoServiceLayer Comprehensive Test Suite    "
echo "================================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Test counters
total_tests=0
passed_tests=0
failed_tests=0
skipped_tests=0
total_duration=0

# Arrays to store results
declare -a test_results
declare -a test_categories

# Function to categorize and run tests
run_test_category() {
    local category=$1
    local pattern=$2
    local description=$3
    
    echo -e "${BLUE}=== $description ===${NC}"
    echo ""
    
    local category_total=0
    local category_passed=0
    local category_failed=0
    local category_duration=0
    
    for dll in $pattern; do
        if [ -f "$dll" ]; then
            local test_name=$(basename "$dll" .dll)
            echo -e "${YELLOW}Testing: $test_name${NC}"
            
            # Run the test and capture output
            local result=$(dotnet vstest "$dll" --logger:"console;verbosity=minimal" 2>&1)
            
            # Parse results
            if echo "$result" | grep -q "Passed!"; then
                local summary=$(echo "$result" | grep -E "Passed!" | tail -1)
                echo "$summary"
                
                # Extract numbers using sed
                local test_count=$(echo "$summary" | sed -n 's/.*Total:\s*\([0-9]\+\).*/\1/p')
                local pass_count=$(echo "$summary" | sed -n 's/.*Passed:\s*\([0-9]\+\).*/\1/p')
                local fail_count=$(echo "$summary" | sed -n 's/.*Failed:\s*\([0-9]\+\).*/\1/p')
                local duration=$(echo "$summary" | sed -n 's/.*Duration:\s*\([0-9]\+\s*[a-z]*\).*/\1/p')
                
                # Convert to numbers (default to 0 if empty)
                test_count=${test_count:-0}
                pass_count=${pass_count:-0}
                fail_count=${fail_count:-0}
                
                category_total=$((category_total + test_count))
                category_passed=$((category_passed + pass_count))
                category_failed=$((category_failed + fail_count))
                
            elif echo "$result" | grep -q "Failed!"; then
                local summary=$(echo "$result" | grep -E "Failed!" | tail -1)
                echo -e "${RED}$summary${NC}"
                
                # Extract numbers for failed tests
                local test_count=$(echo "$summary" | sed -n 's/.*Total:\s*\([0-9]\+\).*/\1/p')
                local pass_count=$(echo "$summary" | sed -n 's/.*Passed:\s*\([0-9]\+\).*/\1/p')
                local fail_count=$(echo "$summary" | sed -n 's/.*Failed:\s*\([0-9]\+\).*/\1/p')
                
                test_count=${test_count:-0}
                pass_count=${pass_count:-0}
                fail_count=${fail_count:-0}
                
                category_total=$((category_total + test_count))
                category_passed=$((category_passed + pass_count))
                category_failed=$((category_failed + fail_count))
                
            else
                echo -e "${RED}  Test execution failed or no tests found${NC}"
            fi
            echo ""
        fi
    done
    
    # Store category results
    test_results+=("$category:$category_total:$category_passed:$category_failed")
    test_categories+=("$description")
    
    # Update totals
    total_tests=$((total_tests + category_total))
    passed_tests=$((passed_tests + category_passed))
    failed_tests=$((failed_tests + category_failed))
    
    echo -e "${CYAN}Category Summary - Total: $category_total, Passed: $category_passed, Failed: $category_failed${NC}"
    echo ""
}

# Execute test categories
run_test_category "CORE" "/home/ubuntu/neo-service-layer/tests/Core/*/bin/Debug/net9.0/*.Tests.dll" "CORE TESTS"
run_test_category "INFRASTRUCTURE" "/home/ubuntu/neo-service-layer/tests/Infrastructure/*/bin/Debug/net9.0/*.Tests.dll" "INFRASTRUCTURE TESTS"
run_test_category "SERVICES" "/home/ubuntu/neo-service-layer/tests/Services/*/bin/Debug/net9.0/*.Tests.dll" "SERVICE TESTS"
run_test_category "BLOCKCHAIN" "/home/ubuntu/neo-service-layer/tests/Blockchain/*/bin/Debug/net9.0/*.Tests.dll" "BLOCKCHAIN TESTS"
run_test_category "TEE" "/home/ubuntu/neo-service-layer/tests/Tee/*/bin/Debug/net9.0/*.Tests.dll" "TEE/ENCLAVE TESTS"
run_test_category "AI" "/home/ubuntu/neo-service-layer/tests/AI/*/bin/Debug/net9.0/*.Tests.dll" "AI/ML TESTS"
run_test_category "PERFORMANCE" "/home/ubuntu/neo-service-layer/tests/Performance/*/bin/Debug/net9.0/*.Tests.dll" "PERFORMANCE TESTS"

# Check for Integration Tests
if [ -f "/home/ubuntu/neo-service-layer/tests/Integration/NeoServiceLayer.Integration.Tests/bin/Debug/net9.0/NeoServiceLayer.Integration.Tests.dll" ]; then
    run_test_category "INTEGRATION" "/home/ubuntu/neo-service-layer/tests/Integration/*/bin/Debug/net9.0/*.Tests.dll" "INTEGRATION TESTS"
fi

# Generate Final Summary
echo ""
echo "================================================="
echo "              FINAL TEST SUMMARY                "
echo "================================================="
echo ""

# Category breakdown
echo -e "${BLUE}Test Results by Category:${NC}"
for i in "${!test_results[@]}"; do
    IFS=':' read -r category total passed failed <<< "${test_results[$i]}"
    if [ "$total" -gt 0 ]; then
        pass_rate=$(echo "scale=1; $passed * 100 / $total" | bc)
        if [ "$failed" -eq 0 ]; then
            echo -e "  ${GREEN}${test_categories[$i]}: $passed/$total (${pass_rate}%)${NC}"
        else
            echo -e "  ${YELLOW}${test_categories[$i]}: $passed/$total (${pass_rate}%) - $failed failed${NC}"
        fi
    else
        echo -e "  ${RED}${test_categories[$i]}: No tests found${NC}"
    fi
done

echo ""
echo -e "${BLUE}Overall Statistics:${NC}"
echo -e "Total Tests:   ${CYAN}$total_tests${NC}"
echo -e "Passed Tests:  ${GREEN}$passed_tests${NC}"
if [ $failed_tests -gt 0 ]; then
    echo -e "Failed Tests:  ${RED}$failed_tests${NC}"
else
    echo -e "Failed Tests:  ${GREEN}$failed_tests${NC}"
fi
echo -e "Skipped Tests: ${YELLOW}$skipped_tests${NC}"

# Calculate pass rate
if [ $total_tests -gt 0 ]; then
    pass_rate=$(echo "scale=2; $passed_tests * 100 / $total_tests" | bc)
    echo -e "Pass Rate:     ${GREEN}$pass_rate%${NC}"
    
    # Overall status
    if [ $failed_tests -eq 0 ]; then
        echo ""
        echo -e "${GREEN}✅ ALL TESTS PASSED SUCCESSFULLY!${NC}"
        exit 0
    else
        echo ""
        echo -e "${YELLOW}⚠️  SOME TESTS FAILED - REVIEW REQUIRED${NC}"
        exit 1
    fi
else
    echo -e "Pass Rate:     ${RED}N/A (No tests found)${NC}"
    echo ""
    echo -e "${RED}❌ NO TESTS WERE EXECUTED${NC}"
    exit 1
fi