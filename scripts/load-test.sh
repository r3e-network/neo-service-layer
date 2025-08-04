#!/bin/bash

# Load testing script for Neo Service Layer
# Uses Apache Bench (ab) and custom scripts

set -e

BASE_URL="${1:-http://localhost}"
DURATION="${2:-60}"  # seconds
CONCURRENCY="${3:-100}"
REQUESTS="${4:-10000}"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Results directory
RESULTS_DIR="./load-test-results-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$RESULTS_DIR"

echo -e "${YELLOW}Neo Service Layer Load Testing${NC}"
echo "Base URL: $BASE_URL"
echo "Duration: $DURATION seconds"
echo "Concurrency: $CONCURRENCY"
echo "Total Requests: $REQUESTS"
echo "Results: $RESULTS_DIR"
echo ""

# Check if ab is installed
if ! command -v ab &> /dev/null; then
    echo -e "${RED}Apache Bench (ab) not found. Installing...${NC}"
    sudo apt-get update && sudo apt-get install -y apache2-utils
fi

# Function to run load test
run_load_test() {
    local endpoint=$1
    local name=$2
    local output_file="$RESULTS_DIR/${name}.txt"
    
    echo -e "${YELLOW}Testing: $name${NC}"
    
    ab -n $REQUESTS \
       -c $CONCURRENCY \
       -t $DURATION \
       -g "$RESULTS_DIR/${name}.tsv" \
       "$BASE_URL$endpoint" > "$output_file" 2>&1
    
    # Extract key metrics
    local requests_per_sec=$(grep "Requests per second" "$output_file" | awk '{print $4}')
    local time_per_request=$(grep "Time per request" "$output_file" | head -1 | awk '{print $4}')
    local failed_requests=$(grep "Failed requests" "$output_file" | awk '{print $3}')
    
    echo "  Requests/sec: $requests_per_sec"
    echo "  Time/request: $time_per_request ms"
    echo "  Failed: $failed_requests"
    echo ""
}

# Test endpoints
echo -e "${GREEN}Starting load tests...${NC}"
echo ""

# Health endpoint (lightweight)
run_load_test "/health" "health"

# API info endpoint
run_load_test "/api/info" "api-info"

# Generate summary report
cat > "$RESULTS_DIR/summary.md" << EOF
# Load Test Summary

**Date**: $(date)
**Base URL**: $BASE_URL
**Duration**: $DURATION seconds
**Concurrency**: $CONCURRENCY
**Total Requests**: $REQUESTS

## Results

| Endpoint | Requests/sec | Avg Response Time (ms) | Failed Requests |
|----------|--------------|------------------------|-----------------|
EOF

# Parse results and add to summary
for file in "$RESULTS_DIR"/*.txt; do
    if [ -f "$file" ]; then
        name=$(basename "$file" .txt)
        rps=$(grep "Requests per second" "$file" | awk '{print $4}')
        tpr=$(grep "Time per request" "$file" | head -1 | awk '{print $4}')
        failed=$(grep "Failed requests" "$file" | awk '{print $3}')
        
        echo "| $name | $rps | $tpr | $failed |" >> "$RESULTS_DIR/summary.md"
    fi
done

echo "" >> "$RESULTS_DIR/summary.md"
echo "## Recommendations" >> "$RESULTS_DIR/summary.md"

# Analyze results and provide recommendations
total_failed=0
for file in "$RESULTS_DIR"/*.txt; do
    if [ -f "$file" ]; then
        failed=$(grep "Failed requests" "$file" | awk '{print $3}')
        total_failed=$((total_failed + failed))
    fi
done

if [ $total_failed -eq 0 ]; then
    echo "✅ **No failed requests** - System handled load well" >> "$RESULTS_DIR/summary.md"
else
    echo "⚠️ **$total_failed failed requests** - Consider scaling or optimization" >> "$RESULTS_DIR/summary.md"
fi

# Advanced load test with JMeter (if available)
if command -v jmeter &> /dev/null; then
    echo -e "${YELLOW}Running advanced JMeter tests...${NC}"
    
    # Create JMeter test plan
    cat > "$RESULTS_DIR/test-plan.jmx" << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<jmeterTestPlan version="1.2" properties="5.0" jmeter="5.5">
  <hashTree>
    <TestPlan guiclass="TestPlanGui" testclass="TestPlan" testname="Neo Service Layer Load Test" enabled="true">
      <stringProp name="TestPlan.comments"></stringProp>
      <boolProp name="TestPlan.functional_mode">false</boolProp>
      <boolProp name="TestPlan.tearDown_on_shutdown">true</boolProp>
      <boolProp name="TestPlan.serialize_threadgroups">false</boolProp>
      <elementProp name="TestPlan.user_defined_variables" elementType="Arguments" guiclass="ArgumentsPanel" testclass="Arguments" testname="User Defined Variables" enabled="true">
        <collectionProp name="Arguments.arguments"/>
      </elementProp>
      <stringProp name="TestPlan.user_define_classpath"></stringProp>
    </TestPlan>
    <hashTree>
      <ThreadGroup guiclass="ThreadGroupGui" testclass="ThreadGroup" testname="API Users" enabled="true">
        <stringProp name="ThreadGroup.on_sample_error">continue</stringProp>
        <elementProp name="ThreadGroup.main_controller" elementType="LoopController" guiclass="LoopControlPanel" testclass="LoopController" testname="Loop Controller" enabled="true">
          <boolProp name="LoopController.continue_forever">false</boolProp>
          <intProp name="LoopController.loops">-1</intProp>
        </elementProp>
        <stringProp name="ThreadGroup.num_threads">100</stringProp>
        <stringProp name="ThreadGroup.ramp_time">30</stringProp>
        <boolProp name="ThreadGroup.scheduler">true</boolProp>
        <stringProp name="ThreadGroup.duration">60</stringProp>
        <stringProp name="ThreadGroup.delay">0</stringProp>
        <boolProp name="ThreadGroup.same_user_on_next_iteration">true</boolProp>
      </ThreadGroup>
      <hashTree/>
    </hashTree>
  </hashTree>
</jmeterTestPlan>
EOF
    
    # Run JMeter test
    jmeter -n -t "$RESULTS_DIR/test-plan.jmx" -l "$RESULTS_DIR/jmeter-results.jtl" -e -o "$RESULTS_DIR/jmeter-report"
fi

# Display summary
echo ""
echo -e "${GREEN}Load testing complete!${NC}"
echo ""
cat "$RESULTS_DIR/summary.md"
echo ""
echo "Detailed results saved to: $RESULTS_DIR"