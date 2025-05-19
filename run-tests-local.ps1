# Run the Core tests
Write-Host "Running Core tests..." -ForegroundColor Green
dotnet test tests/NeoServiceLayer.Core.Tests

# Run the Infrastructure tests
Write-Host "Running Infrastructure tests..." -ForegroundColor Green
dotnet test tests/NeoServiceLayer.Infrastructure.Tests

# Run the Tee.Enclave tests
Write-Host "Running Tee.Enclave tests..." -ForegroundColor Green
dotnet test tests/NeoServiceLayer.Tee.Enclave.Tests

# Run the Tee.Host tests
Write-Host "Running Tee.Host tests..." -ForegroundColor Green
dotnet test tests/NeoServiceLayer.Tee.Host.Tests
