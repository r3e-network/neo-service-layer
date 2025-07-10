#!/bin/bash

echo "=== Testing Distributed Tracing with OpenTelemetry ==="
echo ""

# Get JWT token
echo "1. Getting JWT token..."
TOKEN=$(curl -s http://localhost:5200/api/auth/login -X POST -H "Content-Type: application/json" -d '{"username":"admin","password":"admin123"}' | jq -r .token)
echo "   ✓ Token obtained"
echo ""

# Store data
echo "2. Storing data in storage service..."
STORE_RESPONSE=$(curl -s http://localhost:5200/api/storage/store -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -X POST -d '{"name":"Distributed Trace Test","value":42,"timestamp":"'$(date -u +"%Y-%m-%dT%H:%M:%SZ")'"}')
echo "   Response: $STORE_RESPONSE"
echo ""

# Get stats
echo "3. Getting storage stats..."
STATS=$(curl -s http://localhost:5200/api/storage/stats -H "Authorization: Bearer $TOKEN")
echo "   Stats: $STATS"
echo ""

# Test trace endpoint
echo "4. Testing trace endpoint..."
TRACE_TEST=$(curl -s http://localhost:5200/api/storage/trace-test -H "Authorization: Bearer $TOKEN")
echo "   Trace test: $TRACE_TEST"
echo ""

# Wait for traces to be processed
echo "5. Waiting for traces to be processed..."
sleep 3

# Check Jaeger for traces
echo "6. Checking Jaeger for distributed traces..."
echo ""

# Get services
echo "   Services reporting to Jaeger:"
curl -s "http://localhost:16686/api/services" | jq -r '.data[]' | sed 's/^/   - /'
echo ""

# Get recent traces from API Gateway
echo "   Recent API Gateway traces:"
curl -s "http://localhost:16686/api/traces?service=api-gateway&limit=3" | jq -r '.data[].spans[0] | "   - \(.operationName) (TraceID: \(.traceID[0:16])...)"'
echo ""

# Get recent traces from Storage Service
echo "   Recent Storage Service traces:"
curl -s "http://localhost:16686/api/traces?service=storage-service&limit=3" | jq -r '.data[].spans[0] | "   - \(.operationName) (TraceID: \(.traceID[0:16])...)"'
echo ""

echo "=== Test Complete ==="
echo ""
echo "To view the distributed traces:"
echo "1. Open Jaeger UI: http://localhost:16686"
echo "2. Select 'api-gateway' or 'storage-service' from the Service dropdown"
echo "3. Click 'Find Traces' to see the distributed trace flow"
echo ""