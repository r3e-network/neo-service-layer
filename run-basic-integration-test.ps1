# Run the BasicIntegrationTest directly

Write-Host "Running BasicIntegrationTest for Neo Service Layer..." -ForegroundColor Green

# Set environment variables
$env:ASPNETCORE_ENVIRONMENT = "Test"
$env:UseInMemoryDatabase = "true"
$env:ConnectionStrings__DefaultConnection = "InMemory"
$env:Tee__SimulationMode = "true"
$env:SGX_MODE = "SIM"
$env:SGX_SIMULATION = "1"
$env:DOTNET_ENVIRONMENT = "Test"

# Run the test
Write-Host "Running test..." -ForegroundColor Cyan
dotnet test tests/NeoServiceLayer.Integration.Tests/NeoServiceLayer.Integration.Tests.csproj --filter "FullyQualifiedName~BasicIntegrationTest" --logger "console;verbosity=detailed"

Write-Host "Test completed!" -ForegroundColor Green
