#!/bin/bash

# Neo Service Layer - Unit Tests Only (Excludes Performance Tests)
# This script builds the solution and runs unit tests excluding performance tests

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
CONFIGURATION=${1:-"Release"}
VERBOSITY=${2:-"normal"}
RESULTS_DIR="./TestResults"
COVERAGE_DIR="./CoverageReport"

echo -e "${CYAN}🧪 Neo Service Layer - Unit Tests Suite${NC}"
echo -e "${CYAN}=======================================${NC}"
echo ""
echo -e "${BLUE}Configuration: ${CONFIGURATION}${NC}"
echo -e "${BLUE}Verbosity: ${VERBOSITY}${NC}"
echo ""

# Clean previous results
echo -e "${YELLOW}🧹 Cleaning previous test results...${NC}"
if [ -d "$RESULTS_DIR" ]; then
    rm -rf "$RESULTS_DIR" 2>/dev/null || sudo rm -rf "$RESULTS_DIR" 2>/dev/null || {
        echo -e "${YELLOW}⚠️  Could not remove existing TestResults, using alternative directory${NC}"
        RESULTS_DIR="./TestResults_$(date +%s)"
    }
fi
if [ -d "$COVERAGE_DIR" ]; then
    rm -rf "$COVERAGE_DIR" 2>/dev/null || sudo rm -rf "$COVERAGE_DIR" 2>/dev/null || {
        echo -e "${YELLOW}⚠️  Could not remove existing CoverageReport, using alternative directory${NC}"
        COVERAGE_DIR="./CoverageReport_$(date +%s)"
    }
fi
mkdir -p "$RESULTS_DIR" "$COVERAGE_DIR"

# Check .NET installation
echo -e "${YELLOW}🔍 Checking .NET installation...${NC}"
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ .NET CLI not found. Please install .NET 9.0 SDK.${NC}"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo -e "${GREEN}✅ .NET version: ${DOTNET_VERSION}${NC}"

# Restore dependencies
echo -e "${YELLOW}📦 Restoring NuGet packages...${NC}"
dotnet restore NeoServiceLayer.sln --verbosity minimal
if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Failed to restore packages${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Packages restored successfully${NC}"

# Build solution
echo -e "${YELLOW}🔨 Building solution...${NC}"
dotnet build NeoServiceLayer.sln \
    --configuration "$CONFIGURATION" \
    --no-restore \
    --verbosity minimal

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Build failed${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Build completed successfully${NC}"

# List all test projects (excluding performance tests)
echo -e "${YELLOW}🔍 Discovering unit test projects...${NC}"
TEST_PROJECTS=$(find tests -name "*.csproj" -type f | grep -v "Performance" | sort)
TEST_COUNT=$(echo "$TEST_PROJECTS" | wc -l)

echo -e "${BLUE}Found ${TEST_COUNT} unit test projects:${NC}"
for project in $TEST_PROJECTS; do
    echo -e "  ${CYAN}• $(basename "$project" .csproj)${NC}"
done
echo ""

# Run unit tests with coverage (excluding performance tests)
echo -e "${YELLOW}🧪 Running unit tests with coverage...${NC}"
dotnet test NeoServiceLayer.sln \
    --configuration "$CONFIGURATION" \
    --no-build \
    --verbosity "$VERBOSITY" \
    --logger "trx;LogFileName=TestResults.trx" \
    --logger "console;verbosity=$VERBOSITY" \
    --collect:"XPlat Code Coverage" \
    --settings coverlet.runsettings \
    --results-directory "$RESULTS_DIR" \
    --filter "FullyQualifiedName!~Performance"

TEST_EXIT_CODE=$?

# Parse test results
echo ""
echo -e "${CYAN}📊 Analyzing test results...${NC}"

# Count TRX files
TRX_FILES=$(find "$RESULTS_DIR" -name "*.trx" 2>/dev/null | wc -l)
COVERAGE_FILES=$(find "$RESULTS_DIR" -name "coverage.cobertura.xml" 2>/dev/null | wc -l)

echo -e "${BLUE}Test result files: ${TRX_FILES}${NC}"
echo -e "${BLUE}Coverage files: ${COVERAGE_FILES}${NC}"

# Generate coverage report if coverage files exist
if [ $COVERAGE_FILES -gt 0 ]; then
    echo -e "${YELLOW}📈 Generating coverage report...${NC}"
    
    # Install reportgenerator if not available
    if ! command -v reportgenerator &> /dev/null; then
        echo -e "${YELLOW}Installing ReportGenerator...${NC}"
        dotnet tool install -g dotnet-reportgenerator-globaltool
    fi
    
    # Generate HTML coverage report
    reportgenerator \
        -reports:"$RESULTS_DIR/**/coverage.cobertura.xml" \
        -targetdir:"$COVERAGE_DIR" \
        -reporttypes:"Html;Badges;TextSummary;MarkdownSummaryGithub" \
        -verbosity:Warning
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Coverage report generated: ${COVERAGE_DIR}/index.html${NC}"
        
        # Display coverage summary if available
        if [ -f "$COVERAGE_DIR/Summary.txt" ]; then
            echo ""
            echo -e "${CYAN}📋 Coverage Summary:${NC}"
            cat "$COVERAGE_DIR/Summary.txt"
        fi
    else
        echo -e "${RED}❌ Failed to generate coverage report${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  No coverage files found${NC}"
fi

# Run individual test projects for detailed reporting (excluding performance tests)
echo ""
echo -e "${CYAN}🔍 Individual Test Project Results:${NC}"
echo -e "${CYAN}===================================${NC}"

TOTAL_PROJECTS=0
PASSED_PROJECTS=0
FAILED_PROJECTS=0

for project in $TEST_PROJECTS; do
    PROJECT_NAME=$(basename "$project" .csproj)
    TOTAL_PROJECTS=$((TOTAL_PROJECTS + 1))
    
    echo -e "${BLUE}Testing: ${PROJECT_NAME}${NC}"
    
    # Run test for individual project
    dotnet test "$project" \
        --configuration "$CONFIGURATION" \
        --no-build \
        --verbosity minimal \
        --logger "console;verbosity=minimal" > /tmp/test_output.log 2>&1
    
    PROJECT_EXIT_CODE=$?
    
    if [ $PROJECT_EXIT_CODE -eq 0 ]; then
        PASSED_PROJECTS=$((PASSED_PROJECTS + 1))
        echo -e "  ${GREEN}✅ PASSED${NC}"
    else
        FAILED_PROJECTS=$((FAILED_PROJECTS + 1))
        echo -e "  ${RED}❌ FAILED${NC}"
        
        # Show error details
        if [ -f /tmp/test_output.log ]; then
            echo -e "  ${RED}Error details:${NC}"
            tail -10 /tmp/test_output.log | sed 's/^/    /'
        fi
    fi
done

# Final summary
echo ""
echo -e "${CYAN}📋 Final Test Summary${NC}"
echo -e "${CYAN}=====================${NC}"
echo -e "${BLUE}Total Projects: ${TOTAL_PROJECTS}${NC}"
echo -e "${GREEN}Passed Projects: ${PASSED_PROJECTS}${NC}"
echo -e "${RED}Failed Projects: ${FAILED_PROJECTS}${NC}"

if [ $FAILED_PROJECTS -eq 0 ]; then
    echo -e "${GREEN}🎉 All unit test projects passed!${NC}"
else
    echo -e "${RED}💥 ${FAILED_PROJECTS} test project(s) failed${NC}"
fi

# Quality gates
echo ""
echo -e "${CYAN}🎯 Quality Gates:${NC}"

# Gate 1: All tests must pass
if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo -e "  ${GREEN}✅ All unit tests pass${NC}"
else
    echo -e "  ${RED}❌ Some unit tests failed${NC}"
fi

# Gate 2: Coverage collection
if [ $COVERAGE_FILES -gt 0 ]; then
    echo -e "  ${GREEN}✅ Coverage data collected${NC}"
else
    echo -e "  ${YELLOW}⚠️  No coverage data collected${NC}"
fi

# Gate 3: All projects build and run
if [ $FAILED_PROJECTS -eq 0 ]; then
    echo -e "  ${GREEN}✅ All test projects executable${NC}"
else
    echo -e "  ${RED}❌ Some test projects failed to run${NC}"
fi

# Results location
echo ""
echo -e "${CYAN}📁 Results Location:${NC}"
echo -e "  Test Results: ${RESULTS_DIR}"
echo -e "  Coverage Report: ${COVERAGE_DIR}/index.html"

# Final exit code
if [ $TEST_EXIT_CODE -eq 0 ] && [ $FAILED_PROJECTS -eq 0 ]; then
    echo -e "${GREEN}🎉 Unit test suite completed successfully!${NC}"
    exit 0
else
    echo -e "${RED}❌ Unit test suite completed with failures${NC}"
    exit 1
fi