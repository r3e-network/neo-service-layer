#!/bin/bash

# Neo Service Layer - Automated Coverage Tracking Script
# Tracks test coverage over time and generates reports

set -e

# Configuration
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COVERAGE_DIR="${PROJECT_ROOT}/coverage-reports"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
REPORT_FILE="${COVERAGE_DIR}/coverage_${TIMESTAMP}.json"
SUMMARY_FILE="${COVERAGE_DIR}/coverage_summary.md"
BASELINE_FILE="${COVERAGE_DIR}/coverage_baseline.json"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Create coverage directory if it doesn't exist
mkdir -p "${COVERAGE_DIR}"

echo "========================================="
echo "Neo Service Layer - Coverage Tracker"
echo "========================================="
echo ""

# Function to run tests with coverage
run_tests_with_coverage() {
    echo "📊 Running tests with coverage collection..."
    
    dotnet test "${PROJECT_ROOT}/NeoServiceLayer.sln" \
        --configuration Release \
        --collect:"XPlat Code Coverage" \
        --results-directory "${COVERAGE_DIR}/TestResults_${TIMESTAMP}" \
        --logger "json;LogFileName=testresults_${TIMESTAMP}.json" \
        --logger "console;verbosity=minimal" \
        /p:CollectCoverage=true \
        /p:CoverletOutputFormat="opencover,cobertura,json" \
        /p:CoverletOutput="${COVERAGE_DIR}/coverage_${TIMESTAMP}" \
        /p:ExcludeByAttribute="Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute" \
        /p:ExcludeByFile="**/Migrations/*.cs" \
        /p:Threshold=0 \
        /p:ThresholdType=line \
        /p:ThresholdStat=total 2>&1 | tee "${COVERAGE_DIR}/test_run_${TIMESTAMP}.log"
    
    return ${PIPESTATUS[0]}
}

# Function to parse coverage results
parse_coverage_results() {
    echo ""
    echo "📈 Parsing coverage results..."
    
    # Find the latest coverage file
    LATEST_COVERAGE=$(find "${COVERAGE_DIR}" -name "coverage_${TIMESTAMP}.json" -type f | head -1)
    
    if [ -z "$LATEST_COVERAGE" ]; then
        echo -e "${RED}❌ No coverage file found${NC}"
        return 1
    fi
    
    # Extract coverage metrics using jq (or python if jq not available)
    if command -v jq &> /dev/null; then
        LINE_COVERAGE=$(jq '.Summary.LineCoverage' "$LATEST_COVERAGE" 2>/dev/null || echo "0")
        BRANCH_COVERAGE=$(jq '.Summary.BranchCoverage' "$LATEST_COVERAGE" 2>/dev/null || echo "0")
        METHOD_COVERAGE=$(jq '.Summary.MethodCoverage' "$LATEST_COVERAGE" 2>/dev/null || echo "0")
    else
        # Fallback to Python if jq is not available
        LINE_COVERAGE=$(python3 -c "import json; print(json.load(open('$LATEST_COVERAGE'))['Summary']['LineCoverage'])" 2>/dev/null || echo "0")
        BRANCH_COVERAGE=$(python3 -c "import json; print(json.load(open('$LATEST_COVERAGE'))['Summary']['BranchCoverage'])" 2>/dev/null || echo "0")
        METHOD_COVERAGE=$(python3 -c "import json; print(json.load(open('$LATEST_COVERAGE'))['Summary']['MethodCoverage'])" 2>/dev/null || echo "0")
    fi
    
    echo "Line Coverage: ${LINE_COVERAGE}%"
    echo "Branch Coverage: ${BRANCH_COVERAGE}%"
    echo "Method Coverage: ${METHOD_COVERAGE}%"
    
    # Save to JSON report
    cat > "$REPORT_FILE" <<EOF
{
    "timestamp": "${TIMESTAMP}",
    "coverage": {
        "line": ${LINE_COVERAGE:-0},
        "branch": ${BRANCH_COVERAGE:-0},
        "method": ${METHOD_COVERAGE:-0}
    }
}
EOF
}

# Function to compare with baseline
compare_with_baseline() {
    echo ""
    echo "📊 Comparing with baseline..."
    
    if [ ! -f "$BASELINE_FILE" ]; then
        echo "No baseline found. Setting current results as baseline."
        cp "$REPORT_FILE" "$BASELINE_FILE"
        return 0
    fi
    
    # Extract baseline values
    if command -v jq &> /dev/null; then
        BASELINE_LINE=$(jq '.coverage.line' "$BASELINE_FILE" 2>/dev/null || echo "0")
        CURRENT_LINE=$(jq '.coverage.line' "$REPORT_FILE" 2>/dev/null || echo "0")
    else
        BASELINE_LINE=$(python3 -c "import json; print(json.load(open('$BASELINE_FILE'))['coverage']['line'])" 2>/dev/null || echo "0")
        CURRENT_LINE=$(python3 -c "import json; print(json.load(open('$REPORT_FILE'))['coverage']['line'])" 2>/dev/null || echo "0")
    fi
    
    # Calculate difference
    DIFF=$(echo "$CURRENT_LINE - $BASELINE_LINE" | bc 2>/dev/null || echo "0")
    
    if (( $(echo "$DIFF > 0" | bc -l) )); then
        echo -e "${GREEN}✅ Coverage improved by ${DIFF}%${NC}"
    elif (( $(echo "$DIFF < 0" | bc -l) )); then
        echo -e "${RED}⚠️  Coverage decreased by ${DIFF#-}%${NC}"
    else
        echo -e "${YELLOW}➡️  Coverage unchanged${NC}"
    fi
}

# Function to generate summary report
generate_summary_report() {
    echo ""
    echo "📝 Generating summary report..."
    
    # Read current values
    if [ -f "$REPORT_FILE" ]; then
        if command -v jq &> /dev/null; then
            LINE_COV=$(jq '.coverage.line' "$REPORT_FILE" 2>/dev/null || echo "0")
            BRANCH_COV=$(jq '.coverage.branch' "$REPORT_FILE" 2>/dev/null || echo "0")
            METHOD_COV=$(jq '.coverage.method' "$REPORT_FILE" 2>/dev/null || echo "0")
        else
            LINE_COV=$(python3 -c "import json; print(json.load(open('$REPORT_FILE'))['coverage']['line'])" 2>/dev/null || echo "0")
            BRANCH_COV=$(python3 -c "import json; print(json.load(open('$REPORT_FILE'))['coverage']['branch'])" 2>/dev/null || echo "0")
            METHOD_COV=$(python3 -c "import json; print(json.load(open('$REPORT_FILE'))['coverage']['method'])" 2>/dev/null || echo "0")
        fi
    else
        LINE_COV="0"
        BRANCH_COV="0"
        METHOD_COV="0"
    fi
    
    # Generate markdown summary
    cat > "$SUMMARY_FILE" <<EOF
# Coverage Report Summary

**Generated**: $(date +"%Y-%m-%d %H:%M:%S")

## Current Coverage

| Metric | Coverage | Target | Status |
|--------|----------|--------|--------|
| Line Coverage | ${LINE_COV}% | 80% | $([ $(echo "$LINE_COV >= 80" | bc) -eq 1 ] && echo "✅" || echo "❌") |
| Branch Coverage | ${BRANCH_COV}% | 70% | $([ $(echo "$BRANCH_COV >= 70" | bc) -eq 1 ] && echo "✅" || echo "❌") |
| Method Coverage | ${METHOD_COV}% | 75% | $([ $(echo "$METHOD_COV >= 75" | bc) -eq 1 ] && echo "✅" || echo "❌") |

## Coverage Trend

\`\`\`
Last 5 runs:
EOF
    
    # Add last 5 coverage results
    ls -t "${COVERAGE_DIR}"/coverage_*.json 2>/dev/null | head -5 | while read -r file; do
        if [ -f "$file" ]; then
            timestamp=$(basename "$file" | sed 's/coverage_\(.*\)\.json/\1/')
            if command -v jq &> /dev/null; then
                line=$(jq '.coverage.line' "$file" 2>/dev/null || echo "0")
            else
                line=$(python3 -c "import json; print(json.load(open('$file'))['coverage']['line'])" 2>/dev/null || echo "0")
            fi
            echo "${timestamp}: ${line}%" >> "$SUMMARY_FILE"
        fi
    done
    
    echo "\`\`\`" >> "$SUMMARY_FILE"
    echo "" >> "$SUMMARY_FILE"
    echo "Full report: ${COVERAGE_DIR}/TestResults_${TIMESTAMP}" >> "$SUMMARY_FILE"
    
    echo "Summary report saved to: ${SUMMARY_FILE}"
}

# Function to check coverage gates
check_coverage_gates() {
    echo ""
    echo "🚦 Checking coverage gates..."
    
    # Define thresholds
    LINE_THRESHOLD=80
    BRANCH_THRESHOLD=70
    METHOD_THRESHOLD=75
    
    # Read current values
    if [ -f "$REPORT_FILE" ]; then
        if command -v jq &> /dev/null; then
            LINE_COV=$(jq '.coverage.line' "$REPORT_FILE" 2>/dev/null || echo "0")
            BRANCH_COV=$(jq '.coverage.branch' "$REPORT_FILE" 2>/dev/null || echo "0")
            METHOD_COV=$(jq '.coverage.method' "$REPORT_FILE" 2>/dev/null || echo "0")
        else
            LINE_COV=$(python3 -c "import json; print(json.load(open('$REPORT_FILE'))['coverage']['line'])" 2>/dev/null || echo "0")
            BRANCH_COV=$(python3 -c "import json; print(json.load(open('$REPORT_FILE'))['coverage']['branch'])" 2>/dev/null || echo "0")
            METHOD_COV=$(python3 -c "import json; print(json.load(open('$REPORT_FILE'))['coverage']['method'])" 2>/dev/null || echo "0")
        fi
    else
        LINE_COV="0"
        BRANCH_COV="0"
        METHOD_COV="0"
    fi
    
    GATE_PASSED=true
    
    if (( $(echo "$LINE_COV < $LINE_THRESHOLD" | bc -l) )); then
        echo -e "${RED}❌ Line coverage (${LINE_COV}%) below threshold (${LINE_THRESHOLD}%)${NC}"
        GATE_PASSED=false
    else
        echo -e "${GREEN}✅ Line coverage (${LINE_COV}%) meets threshold${NC}"
    fi
    
    if (( $(echo "$BRANCH_COV < $BRANCH_THRESHOLD" | bc -l) )); then
        echo -e "${RED}❌ Branch coverage (${BRANCH_COV}%) below threshold (${BRANCH_THRESHOLD}%)${NC}"
        GATE_PASSED=false
    else
        echo -e "${GREEN}✅ Branch coverage (${BRANCH_COV}%) meets threshold${NC}"
    fi
    
    if (( $(echo "$METHOD_COV < $METHOD_THRESHOLD" | bc -l) )); then
        echo -e "${RED}❌ Method coverage (${METHOD_COV}%) below threshold (${METHOD_THRESHOLD}%)${NC}"
        GATE_PASSED=false
    else
        echo -e "${GREEN}✅ Method coverage (${METHOD_COV}%) meets threshold${NC}"
    fi
    
    if [ "$GATE_PASSED" = false ]; then
        echo ""
        echo -e "${RED}⚠️  Coverage gates failed!${NC}"
        return 1
    else
        echo ""
        echo -e "${GREEN}✅ All coverage gates passed!${NC}"
        return 0
    fi
}

# Main execution
main() {
    # Parse command line arguments
    SKIP_TESTS=false
    UPDATE_BASELINE=false
    
    while [[ $# -gt 0 ]]; do
        case $1 in
            --skip-tests)
                SKIP_TESTS=true
                shift
                ;;
            --update-baseline)
                UPDATE_BASELINE=true
                shift
                ;;
            --help)
                echo "Usage: $0 [options]"
                echo "Options:"
                echo "  --skip-tests      Skip running tests, use existing coverage data"
                echo "  --update-baseline Update the baseline with current results"
                echo "  --help           Show this help message"
                exit 0
                ;;
            *)
                echo "Unknown option: $1"
                exit 1
                ;;
        esac
    done
    
    # Run tests if not skipped
    if [ "$SKIP_TESTS" = false ]; then
        if ! run_tests_with_coverage; then
            echo -e "${RED}❌ Test execution failed${NC}"
            exit 1
        fi
    fi
    
    # Parse and analyze results
    parse_coverage_results
    compare_with_baseline
    generate_summary_report
    
    # Update baseline if requested
    if [ "$UPDATE_BASELINE" = true ]; then
        echo ""
        echo "📌 Updating baseline..."
        cp "$REPORT_FILE" "$BASELINE_FILE"
        echo "Baseline updated successfully"
    fi
    
    # Check coverage gates
    if ! check_coverage_gates; then
        exit 1
    fi
    
    echo ""
    echo "========================================="
    echo "Coverage tracking completed successfully!"
    echo "========================================="
}

# Run main function
main "$@"