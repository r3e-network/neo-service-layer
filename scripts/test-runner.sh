#!/bin/bash

# Simple test runner that avoids MSBuild response file issues

echo "=== Neo Service Layer Test Execution ==="
echo ""

# First, let's directly compile and run a test assembly
TEST_PROJECT="/home/ubuntu/neo-service-layer/tests/Core/NeoServiceLayer.Core.Tests/NeoServiceLayer.Core.Tests.csproj"

echo "1. Testing Core functionality..."
cd /home/ubuntu/neo-service-layer/tests/Core/NeoServiceLayer.Core.Tests/
/usr/lib/dotnet/dotnet build --configuration Release --verbosity quiet 2>&1 | head -20

echo ""
echo "2. Running Core tests..."
/usr/lib/dotnet/dotnet test --no-build --configuration Release --verbosity minimal 2>&1 | head -50

echo ""
echo "3. Testing Infrastructure Security..."
cd /home/ubuntu/neo-service-layer/tests/Infrastructure/NeoServiceLayer.Infrastructure.Security.Tests/
/usr/lib/dotnet/dotnet build --configuration Release --verbosity quiet 2>&1 | head -20

echo ""
echo "4. Running Security tests..."
/usr/lib/dotnet/dotnet test --no-build --configuration Release --verbosity minimal 2>&1 | head -50

echo ""
echo "5. Testing Performance benchmarks..."
cd /home/ubuntu/neo-service-layer/tests/Performance/NeoServiceLayer.Performance.Tests/
/usr/lib/dotnet/dotnet build --configuration Release --verbosity quiet 2>&1 | head -20

echo ""
echo "6. Listing available test assemblies..."
find /home/ubuntu/neo-service-layer -name "*Tests.dll" -path "*/bin/Release/*" 2>/dev/null | head -10

echo ""
echo "Test execution completed."