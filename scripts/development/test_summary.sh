#\!/bin/bash
echo "=== UNIT TEST SUMMARY ==="
echo ""

# Core tests
echo "CORE COMPONENTS:"
echo "- Core.Tests: $(dotnet test tests/Core/NeoServiceLayer.Core.Tests --no-build --no-restore -v q 2>&1  < /dev/null |  grep -E "(Passed!|Failed!)" | head -1)"
echo "- ServiceFramework.Tests: $(dotnet test tests/Core/NeoServiceLayer.ServiceFramework.Tests --no-build --no-restore -v q 2>&1 | grep -E "(Passed!|Failed!)" | head -1)"
echo "- Shared.Tests: $(dotnet test tests/Core/NeoServiceLayer.Shared.Tests --no-build --no-restore -v q 2>&1 | grep -E "(Passed!|Failed!)" | head -1)"
echo ""

echo "AI COMPONENTS:"
echo "- AI.PatternRecognition.Tests: $(dotnet test tests/AI/NeoServiceLayer.AI.PatternRecognition.Tests --no-build --no-restore -v q 2>&1 | grep -E "(Passed!|Failed!)" | head -1)"
echo "- AI.Prediction.Tests: $(dotnet test tests/AI/NeoServiceLayer.AI.Prediction.Tests --no-build --no-restore -v q 2>&1 | grep -E "(Passed!|Failed!)" | head -1)"
echo ""

echo "BLOCKCHAIN COMPONENTS:"
echo "- Neo.N3.Tests: $(dotnet test tests/Blockchain/NeoServiceLayer.Neo.N3.Tests --no-build --no-restore -v q 2>&1 | grep -E "(Passed!|Failed!)" | head -1)"
echo "- Neo.X.Tests: $(dotnet test tests/Blockchain/NeoServiceLayer.Neo.X.Tests --no-build --no-restore -v q 2>&1 | grep -E "(Passed!|Failed!)" | head -1)"
echo ""

echo "OTHER COMPONENTS:"
echo "- TEE.Host.Tests: $(dotnet test tests/Tee/NeoServiceLayer.Tee.Host.Tests --no-build --no-restore -v q 2>&1 | grep -E "(Passed!|Failed!)" | head -1)"
echo "- Api.Tests: $(dotnet test tests/Api/NeoServiceLayer.Api.Tests --no-build --no-restore -v q 2>&1 | grep -E "(Passed!|Failed!)" | head -1)"
echo "- Advanced.FairOrdering.Tests: $(dotnet test tests/Advanced/NeoServiceLayer.Advanced.FairOrdering.Tests --no-build --no-restore -v q 2>&1 | grep -E "(Passed!|Failed!)" | head -1)"
