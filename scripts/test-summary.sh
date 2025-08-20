#!/bin/bash

echo "========================================="
echo "   NeoServiceLayer Test Summary Report  "
echo "========================================="
echo ""

# Initialize counters
total_tests=0
passed_tests=0
failed_tests=0

# Run Core Tests
echo "Running Core Tests..."
result=$(dotnet vstest \
    tests/Core/NeoServiceLayer.Core.Tests/bin/Debug/net9.0/NeoServiceLayer.Core.Tests.dll \
    tests/Core/NeoServiceLayer.Shared.Tests/bin/Debug/net9.0/NeoServiceLayer.Shared.Tests.dll \
    tests/Core/NeoServiceLayer.ServiceFramework.Tests/bin/Debug/net9.0/NeoServiceLayer.ServiceFramework.Tests.dll \
    --logger:"console;verbosity=quiet" 2>&1 | tail -1)
echo "Core: $result"

# Run Infrastructure Tests
echo "Running Infrastructure Tests..."
result=$(dotnet vstest \
    tests/Infrastructure/NeoServiceLayer.Infrastructure.Tests/bin/Debug/net9.0/NeoServiceLayer.Infrastructure.Tests.dll \
    --logger:"console;verbosity=quiet" 2>&1 | tail -1)
echo "Infrastructure: $result"

# Run Service Tests
echo "Running Service Tests..."
result=$(dotnet vstest \
    tests/Services/NeoServiceLayer.Services.Backup.Tests/bin/Debug/net9.0/NeoServiceLayer.Services.Backup.Tests.dll \
    tests/Services/NeoServiceLayer.Services.Storage.Tests/bin/Debug/net9.0/NeoServiceLayer.Services.Storage.Tests.dll \
    tests/Services/NeoServiceLayer.Services.EventSubscription.Tests/bin/Debug/net9.0/NeoServiceLayer.Services.EventSubscription.Tests.dll \
    tests/Services/NeoServiceLayer.Services.Monitoring.Tests/bin/Debug/net9.0/NeoServiceLayer.Services.Monitoring.Tests.dll \
    tests/Services/NeoServiceLayer.Services.Compliance.Tests/bin/Debug/net9.0/NeoServiceLayer.Services.Compliance.Tests.dll \
    tests/Services/NeoServiceLayer.Services.NetworkSecurity.Tests/bin/Debug/net9.0/NeoServiceLayer.Services.NetworkSecurity.Tests.dll \
    tests/Services/NeoServiceLayer.Services.CrossChain.Tests/bin/Debug/net9.0/NeoServiceLayer.Services.CrossChain.Tests.dll \
    --logger:"console;verbosity=quiet" 2>&1 | tail -1)
echo "Services: $result"

# Run Blockchain Tests
echo "Running Blockchain Tests..."
result=$(dotnet vstest \
    tests/Blockchain/NeoServiceLayer.Neo.N3.Tests/bin/Debug/net9.0/NeoServiceLayer.Neo.N3.Tests.dll \
    tests/Blockchain/NeoServiceLayer.Neo.X.Tests/bin/Debug/net9.0/NeoServiceLayer.Neo.X.Tests.dll \
    --logger:"console;verbosity=quiet" 2>&1 | tail -1)
echo "Blockchain: $result"

# Run TEE Tests
echo "Running TEE/Enclave Tests..."
result=$(dotnet vstest \
    tests/Tee/NeoServiceLayer.Tee.Host.Tests/bin/Debug/net9.0/NeoServiceLayer.Tee.Host.Tests.dll \
    --logger:"console;verbosity=quiet" 2>&1 | tail -1)
echo "TEE: $result"

# Run AI Tests
echo "Running AI Tests..."
result=$(dotnet vstest \
    tests/AI/NeoServiceLayer.AI.Prediction.Tests/bin/Debug/net9.0/NeoServiceLayer.AI.Prediction.Tests.dll \
    --logger:"console;verbosity=quiet" 2>&1 | tail -1)
echo "AI: $result"

# Run Performance Tests
echo "Running Performance Tests..."
if [ -f "tests/Performance/NeoServiceLayer.Performance.Tests/bin/Debug/net9.0/NeoServiceLayer.Performance.Tests.dll" ]; then
    result=$(dotnet vstest \
        tests/Performance/NeoServiceLayer.Performance.Tests/bin/Debug/net9.0/NeoServiceLayer.Performance.Tests.dll \
        --logger:"console;verbosity=quiet" 2>&1 | tail -1)
    echo "Performance: $result"
else
    echo "Performance: Tests not built"
fi

echo ""
echo "========================================="
echo "         FINAL TEST SUMMARY             "
echo "========================================="
echo ""
echo "Test Categories Executed:"
echo "✅ Core Tests (Core, Shared, ServiceFramework)"
echo "✅ Infrastructure Tests"
echo "✅ Service Tests (7 services)"
echo "✅ Blockchain Tests (Neo N3, Neo X)"
echo "✅ TEE/Enclave Tests"
echo "✅ AI Tests"
echo "⚠️  Performance Tests (if available)"
echo ""
echo "All available tests have been executed!"