#!/bin/bash

# Generate code coverage report for Neo Service Layer
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "Generating code coverage report..."

# Clean previous test results
echo "Cleaning previous test results..."
find "$PROJECT_ROOT" -name "TestResults" -type d -exec rm -rf {} + 2>/dev/null || true

# Run tests with coverage
echo "Running tests with coverage collection..."
dotnet test "$PROJECT_ROOT/NeoServiceLayer.sln" \
    --configuration Release \
    --no-build \
    --logger "trx;LogFileName=test-results.trx" \
    --logger "console;verbosity=normal" \
    --collect:"XPlat Code Coverage" \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:CoverletOutput="$PROJECT_ROOT/TestResults/"

# Find all coverage files
echo "Finding coverage files..."
COVERAGE_FILES=$(find "$PROJECT_ROOT" -name "coverage.opencover.xml" -type f)

if [ -z "$COVERAGE_FILES" ]; then
    echo "Warning: No coverage files found!"
    exit 1
fi

echo "Found coverage files:"
echo "$COVERAGE_FILES"

# Install ReportGenerator if not present
if ! command -v reportgenerator &> /dev/null; then
    echo "Installing ReportGenerator..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
fi

# Generate HTML report
REPORT_DIR="$PROJECT_ROOT/TestResults/CoverageReport"
echo "Generating HTML coverage report in $REPORT_DIR..."

reportgenerator \
    -reports:"$PROJECT_ROOT/**/coverage.opencover.xml" \
    -targetdir:"$REPORT_DIR" \
    -reporttypes:"Html;Cobertura;Badges" \
    -sourcedirs:"$PROJECT_ROOT/src" \
    -historydir:"$PROJECT_ROOT/TestResults/CoverageHistory" \
    -title:"Neo Service Layer Coverage Report" \
    -verbosity:"Info"

echo "Coverage report generated successfully!"
echo "Open $REPORT_DIR/index.html to view the report"

# Display coverage summary
if [ -f "$REPORT_DIR/Summary.txt" ]; then
    echo ""
    echo "Coverage Summary:"
    cat "$REPORT_DIR/Summary.txt"
fi