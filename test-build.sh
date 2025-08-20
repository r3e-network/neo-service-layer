#!/bin/bash
# Direct build test script to work around MSBuild issue

echo "=== Testing Build Status ==="
echo ""

# Test Core projects
echo "Testing Core.Tests..."
/usr/bin/dotnet test tests/Core/NeoServiceLayer.Core.Tests/NeoServiceLayer.Core.Tests.csproj --no-restore 2>/dev/null | grep -E "Passed!|Failed:" || echo "  Build/test failed"

echo "Testing Shared.Tests..."
/usr/bin/dotnet test tests/Core/NeoServiceLayer.Shared.Tests/NeoServiceLayer.Shared.Tests.csproj --no-restore 2>/dev/null | grep -E "Passed!|Failed:" || echo "  Build/test failed"

echo "Testing Performance.Tests..."
/usr/bin/dotnet test tests/Performance/NeoServiceLayer.Performance.Tests/NeoServiceLayer.Performance.Tests.csproj --no-restore 2>/dev/null | grep -E "Passed!|Failed:" || echo "  Build/test failed"

echo ""
echo "=== Checking for compilation errors ==="
# Try to build and capture errors
/usr/bin/dotnet build src/Core/NeoServiceLayer.Core/NeoServiceLayer.Core.csproj --no-restore 2>&1 | grep -E "error CS" | head -10 || echo "No errors in Core"