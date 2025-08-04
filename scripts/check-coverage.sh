#!/bin/bash

# Bash script to check code coverage locally
set -e

# Default values
MIN_COVERAGE=75
MIN_BRANCH_COVERAGE=70
OUTPUT_PATH="TestResults"
FAIL_ON_LOW_COVERAGE=true

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --min-coverage)
      MIN_COVERAGE="$2"
      shift 2
      ;;
    --min-branch-coverage)
      MIN_BRANCH_COVERAGE="$2"
      shift 2
      ;;
    --output-path)
      OUTPUT_PATH="$2"
      shift 2
      ;;
    --no-fail)
      FAIL_ON_LOW_COVERAGE=false
      shift
      ;;
    --help)
      echo "Usage: $0 [options]"
      echo "Options:"
      echo "  --min-coverage N          Minimum line coverage percentage (default: 75)"
      echo "  --min-branch-coverage N   Minimum branch coverage percentage (default: 70)"
      echo "  --output-path PATH        Output directory for test results (default: TestResults)"
      echo "  --no-fail                 Don't fail on low coverage"
      echo "  --help                    Show this help message"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      exit 1
      ;;
  esac
done

echo "🧪 Running tests with code coverage..."

# Clean previous results
if [ -d "$OUTPUT_PATH" ]; then
    rm -rf "$OUTPUT_PATH"
fi

# Run tests with coverage
echo "Running: dotnet test --collect:'XPlat Code Coverage' --settings tests/codecoverage.runsettings --results-directory $OUTPUT_PATH"

if ! dotnet test --collect:"XPlat Code Coverage" --settings tests/codecoverage.runsettings --results-directory "$OUTPUT_PATH" --verbosity minimal; then
    echo "❌ Tests failed!"
    exit 1
fi

# Find coverage files
coverage_files=$(find "$OUTPUT_PATH" -name "coverage.cobertura.xml" -type f)

if [ -z "$coverage_files" ]; then
    echo "⚠️ No coverage files found!"
    exit 1
fi

echo "📊 Generating coverage report..."

# Check if ReportGenerator is installed
if ! command -v reportgenerator &> /dev/null; then
    echo "Installing ReportGenerator..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
fi

# Generate report
coverage_pattern="$OUTPUT_PATH/**/coverage.cobertura.xml"
report_path="$OUTPUT_PATH/CoverageReport"

if ! reportgenerator -reports:"$coverage_pattern" -targetdir:"$report_path" -reporttypes:"Html;JsonSummary;Badges" -verbosity:Warning; then
    echo "❌ Failed to generate coverage report!"
    exit 1
fi

# Read coverage summary
summary_file="$report_path/Summary.json"

if [ ! -f "$summary_file" ]; then
    echo "⚠️ Coverage summary not found!"
    exit 1
fi

# Extract coverage percentages using jq
if command -v jq &> /dev/null; then
    line_coverage=$(jq -r '.coverage.linecoverage // 0' "$summary_file")
    branch_coverage=$(jq -r '.coverage.branchcoverage // 0' "$summary_file")
else
    echo "⚠️ jq not found, using basic parsing"
    line_coverage=$(grep -o '"linecoverage":[0-9.]*' "$summary_file" | cut -d: -f2)
    branch_coverage=$(grep -o '"branchcoverage":[0-9.]*' "$summary_file" | cut -d: -f2)
fi

echo ""
echo "📈 Coverage Results:"
echo "  Line Coverage:   ${line_coverage}% (minimum: ${MIN_COVERAGE}%)"
echo "  Branch Coverage: ${branch_coverage}% (minimum: ${MIN_BRANCH_COVERAGE}%)"

# Check thresholds
line_passed=false
branch_passed=false

if (( $(echo "$line_coverage >= $MIN_COVERAGE" | bc -l) )); then
    echo "  ✅ Line coverage meets threshold"
    line_passed=true
else
    echo "  ❌ Line coverage below threshold"
fi

if (( $(echo "$branch_coverage >= $MIN_BRANCH_COVERAGE" | bc -l) )); then
    echo "  ✅ Branch coverage meets threshold"
    branch_passed=true
else
    echo "  ❌ Branch coverage below threshold"
fi

echo ""
echo "📂 Reports generated:"
echo "  HTML Report: $report_path/index.html"
echo "  Summary:     $summary_file"

# Open report if running in WSL or Linux desktop
if command -v xdg-open &> /dev/null; then
    echo ""
    echo "🌐 Opening coverage report..."
    xdg-open "$report_path/index.html" &
fi

# Exit with appropriate code
if [ "$FAIL_ON_LOW_COVERAGE" = true ] && ([ "$line_passed" = false ] || [ "$branch_passed" = false ]); then
    echo ""
    echo "❌ Code coverage check failed!"
    exit 1
else
    echo ""
    echo "✅ Code coverage check passed!"
    exit 0
fi