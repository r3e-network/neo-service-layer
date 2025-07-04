#!/bin/bash

# Initialize counters
total_passed=0
total_failed=0
total_skipped=0
total_tests=0

# Create results directory
mkdir -p test-results-summary

echo "Running all test projects..."
echo "================================"

# Find all test projects
test_projects=$(find /home/ubuntu/neo-service-layer/tests -name "*.csproj" -path "*/tests/*" | grep -E "\.Tests\.csproj$" | sort)

project_count=0
for project in $test_projects; do
    project_count=$((project_count + 1))
    project_name=$(basename "$project" .csproj)
    echo ""
    echo "[$project_count/36] Running tests for: $project_name"
    echo "----------------------------------------"
    
    # Run tests and capture output
    output=$(dotnet test "$project" --logger "console;verbosity=minimal" --no-build 2>&1 || true)
    
    # Extract test results from output
    if echo "$output" | grep -q "Passed!"; then
        # Extract numbers from the summary line
        summary_line=$(echo "$output" | grep -E "(Failed|Passed|Skipped|Total|Duration)" | tail -1)
        
        # Parse the results
        failed=$(echo "$summary_line" | grep -oP 'Failed:\s*\K\d+' || echo "0")
        passed=$(echo "$summary_line" | grep -oP 'Passed:\s*\K\d+' || echo "0")
        skipped=$(echo "$summary_line" | grep -oP 'Skipped:\s*\K\d+' || echo "0")
        total=$(echo "$summary_line" | grep -oP 'Total:\s*\K\d+' || echo "0")
        
        # Add to totals
        total_failed=$((total_failed + failed))
        total_passed=$((total_passed + passed))
        total_skipped=$((total_skipped + skipped))
        total_tests=$((total_tests + total))
        
        echo "Results: Failed: $failed, Passed: $passed, Skipped: $skipped, Total: $total"
    else
        echo "Could not parse test results for $project_name"
        # Try alternative parsing
        if echo "$output" | grep -q "Test Run"; then
            passed=$(echo "$output" | grep -c "Passed" || echo "0")
            failed=$(echo "$output" | grep -c "Failed" || echo "0")
            skipped=$(echo "$output" | grep -c "Skipped" || echo "0")
            
            total_passed=$((total_passed + passed))
            total_failed=$((total_failed + failed))
            total_skipped=$((total_skipped + skipped))
            total_tests=$((total_tests + passed + failed + skipped))
        fi
    fi
done

echo ""
echo "================================"
echo "FINAL TEST SUMMARY"
echo "================================"
echo "Total Test Projects: $project_count"
echo "Total Tests Run: $total_tests"
echo "Total Passed: $total_passed"
echo "Total Failed: $total_failed"
echo "Total Skipped: $total_skipped"
echo ""
echo "Success Rate: $(awk "BEGIN {if ($total_tests > 0) printf \"%.2f%%\", ($total_passed / $total_tests) * 100; else print \"0%\"}")"
echo "================================"