#\!/bin/bash

# Test runner script to bypass MSBuild response file issue
echo "Neo Service Layer Test Execution Script"
echo "======================================="
echo ""

# Set working directory
cd /home/ubuntu/neo-service-layer

# Clean previous test results
rm -rf TestResults 2>/dev/null
mkdir -p TestResults

echo "Executing tests..."
echo ""

# Track results
TOTAL_PROJECTS=0
PASSED_PROJECTS=0

# Find and execute all test projects
for PROJECT in tests/Core/NeoServiceLayer.Core.Tests/NeoServiceLayer.Core.Tests.csproj \
               tests/Services/NeoServiceLayer.Services.Authentication.Tests/NeoServiceLayer.Services.Authentication.Tests.csproj \
               tests/Services/NeoServiceLayer.Services.Storage.Tests/NeoServiceLayer.Services.Storage.Tests.csproj; do
    if [ -f "$PROJECT" ]; then
        TOTAL_PROJECTS=$((TOTAL_PROJECTS + 1))
        PROJECT_NAME=$(basename $PROJECT .csproj)
        echo "Testing: $PROJECT_NAME"
        
        # Use vstest directly
        dotnet vstest "$PROJECT" --logger:"console;verbosity=normal" 2>&1 | head -20
        
        if [ $? -eq 0 ]; then
            PASSED_PROJECTS=$((PASSED_PROJECTS + 1))
            echo "✅ $PROJECT_NAME completed"
        else
            echo "⚠️ $PROJECT_NAME had issues"
        fi
        echo ""
    fi
done

echo "======================================="
echo "Test Execution Summary"
echo "======================================="
echo "Total Projects Attempted: $TOTAL_PROJECTS"
echo "Successfully Processed: $PASSED_PROJECTS"
