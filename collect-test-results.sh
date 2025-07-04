#!/bin/bash

# Initialize counters
total_passed=0
total_failed=0
total_skipped=0
total_tests=0
failed_projects=()

# Create results file
results_file="test-results-summary.txt"
echo "Neo Service Layer - Comprehensive Test Results" > "$results_file"
echo "=============================================" >> "$results_file"
echo "Date: $(date)" >> "$results_file"
echo "" >> "$results_file"

# Find all test projects
test_projects=$(find /home/ubuntu/neo-service-layer/tests -name "*.csproj" -path "*/tests/*" | grep -E "\.Tests\.csproj$" | sort)

project_count=0
total_project_count=$(echo "$test_projects" | wc -l)

echo "Found $total_project_count test projects" | tee -a "$results_file"
echo "" | tee -a "$results_file"

for project in $test_projects; do
    project_count=$((project_count + 1))
    project_name=$(basename "$project" .csproj)
    project_dir=$(dirname "$project")
    
    echo "[$project_count/$total_project_count] Testing: $project_name" | tee -a "$results_file"
    
    # Run tests and capture output
    temp_output=$(mktemp)
    dotnet test "$project" --logger:"console;verbosity=normal" --no-restore > "$temp_output" 2>&1 || true
    
    # Extract test results
    if grep -q "Test Run Successful\|Test Run Failed\|Test Run Aborted" "$temp_output"; then
        # Get the summary section
        total_line=$(grep -A3 "Test Run" "$temp_output" | grep "Total tests:" | head -1)
        passed_line=$(grep -A3 "Test Run" "$temp_output" | grep "Passed:" | head -1)
        failed_line=$(grep -A3 "Test Run" "$temp_output" | grep "Failed:" | head -1)
        skipped_line=$(grep -A3 "Test Run" "$temp_output" | grep "Skipped:" | head -1)
        
        # Extract numbers
        tests=$(echo "$total_line" | grep -oE '[0-9]+' | head -1 || echo "0")
        passed=$(echo "$passed_line" | grep -oE '[0-9]+' | head -1 || echo "0")
        failed=$(echo "$failed_line" | grep -oE '[0-9]+' | head -1 || echo "0")
        skipped=$(echo "$skipped_line" | grep -oE '[0-9]+' | head -1 || echo "0")
        
        # If no explicit counts found, try the summary line format
        if [[ "$tests" == "0" ]] && grep -q "Passed!" "$temp_output"; then
            summary=$(grep -E "Failed:.*Passed:.*Skipped:.*Total:" "$temp_output" | tail -1)
            failed=$(echo "$summary" | grep -oP 'Failed:\s*\K\d+' || echo "0")
            passed=$(echo "$summary" | grep -oP 'Passed:\s*\K\d+' || echo "0")
            skipped=$(echo "$summary" | grep -oP 'Skipped:\s*\K\d+' || echo "0")
            tests=$(echo "$summary" | grep -oP 'Total:\s*\K\d+' || echo "0")
        fi
        
        # Update totals
        total_tests=$((total_tests + tests))
        total_passed=$((total_passed + passed))
        total_failed=$((total_failed + failed))
        total_skipped=$((total_skipped + skipped))
        
        # Record failed projects
        if [[ "$failed" -gt 0 ]]; then
            failed_projects+=("$project_name (Failed: $failed)")
        fi
        
        echo "  Tests: $tests, Passed: $passed, Failed: $failed, Skipped: $skipped" | tee -a "$results_file"
    else
        echo "  Could not parse results - build or test execution error" | tee -a "$results_file"
        # Check for build errors
        if grep -q "error CS\|error MSB" "$temp_output"; then
            echo "  Build errors detected" | tee -a "$results_file"
            failed_projects+=("$project_name (Build Error)")
        fi
    fi
    
    rm -f "$temp_output"
    echo "" | tee -a "$results_file"
done

# Print final summary
echo "=============================================" | tee -a "$results_file"
echo "FINAL TEST SUMMARY" | tee -a "$results_file"
echo "=============================================" | tee -a "$results_file"
echo "Total Test Projects: $project_count" | tee -a "$results_file"
echo "Total Tests: $total_tests" | tee -a "$results_file"
echo "Total Passed: $total_passed" | tee -a "$results_file"
echo "Total Failed: $total_failed" | tee -a "$results_file"
echo "Total Skipped: $total_skipped" | tee -a "$results_file"
echo "" | tee -a "$results_file"

if [[ $total_tests -gt 0 ]]; then
    success_rate=$(awk "BEGIN {printf \"%.2f\", ($total_passed / $total_tests) * 100}")
    echo "Success Rate: ${success_rate}%" | tee -a "$results_file"
else
    echo "Success Rate: N/A (No tests found)" | tee -a "$results_file"
fi

if [[ ${#failed_projects[@]} -gt 0 ]]; then
    echo "" | tee -a "$results_file"
    echo "Projects with failures:" | tee -a "$results_file"
    for proj in "${failed_projects[@]}"; do
        echo "  - $proj" | tee -a "$results_file"
    done
fi

echo "=============================================" | tee -a "$results_file"
echo "" | tee -a "$results_file"
echo "Full results saved to: $results_file"