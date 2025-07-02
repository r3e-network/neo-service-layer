#\!/bin/bash

echo "Running all unit tests for Neo Service Layer..."
echo "=============================================="

total_passed=0
total_failed=0
total_skipped=0
total_tests=0
failed_projects=""

# Find all test projects
for project in $(find tests -name "*.csproj" -type f  < /dev/null |  sort); do
    echo -n "Testing $(basename $project .csproj)... "
    
    # Run tests and capture output
    output=$(dotnet test "$project" --configuration Release --no-build --verbosity minimal 2>&1 | tail -5)
    
    # Extract results
    if echo "$output" | grep -q "Passed\!"; then
        results=$(echo "$output" | grep "Passed\!" | sed -E 's/.*Failed: *([0-9]+), *Passed: *([0-9]+), *Skipped: *([0-9]+), *Total: *([0-9]+).*/\1 \2 \3 \4/')
        failed=$(echo $results | cut -d' ' -f1)
        passed=$(echo $results | cut -d' ' -f2)
        skipped=$(echo $results | cut -d' ' -f3)
        total=$(echo $results | cut -d' ' -f4)
        
        total_failed=$((total_failed + failed))
        total_passed=$((total_passed + passed))
        total_skipped=$((total_skipped + skipped))
        total_tests=$((total_tests + total))
        
        if [ "$failed" -gt 0 ]; then
            echo "❌ FAILED: $failed failed, $passed passed, $skipped skipped"
            failed_projects="$failed_projects\n  - $(basename $project .csproj): $failed failed"
        else
            echo "✅ PASSED: $passed passed, $skipped skipped"
        fi
    else
        echo "❓ ERROR or NO TESTS"
    fi
done

echo ""
echo "=============================================="
echo "FINAL TEST SUMMARY"
echo "=============================================="
echo "Total Test Projects: $(find tests -name "*.csproj" -type f | wc -l)"
echo "Total Tests Run: $total_tests"
echo "✅ Passed: $total_passed"
echo "❌ Failed: $total_failed"
echo "⏭️  Skipped: $total_skipped"

if [ "$total_failed" -gt 0 ]; then
    echo ""
    echo "Failed Projects:$failed_projects"
    echo ""
    echo "STATUS: ❌ TESTS FAILED"
    exit 1
else
    echo ""
    echo "STATUS: ✅ ALL TESTS PASSED"
    exit 0
fi
