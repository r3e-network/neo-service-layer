# Script to run Occlum tests for the Neo Service Layer

# Set environment variables
$env:Tee__SimulationMode = "true"
$env:Tee__OcclumSupport = "true"
$env:Tee__OcclumInstanceDir = "/occlum_instance"
$env:Tee__OcclumLogLevel = "info"

# Create results directory
$resultsDir = ".\test-results\occlum-tests-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null

Write-Host "Starting Occlum tests..." -ForegroundColor Yellow
Write-Host "Results will be saved to: $resultsDir" -ForegroundColor Yellow
Write-Host ""

# Function to run tests and collect results
function Run-Tests {
    param (
        [string]$ProjectPath,
        [string]$Category,
        [string]$OutputPrefix
    )

    Write-Host "Running $Category tests from $ProjectPath..." -ForegroundColor Cyan

    # Run tests with detailed logging and collect code coverage
    dotnet test $ProjectPath `
        --filter "Category=$Category" `
        --logger "console;verbosity=detailed" `
        --logger "trx;LogFileName=$resultsDir\$OutputPrefix-$Category.trx" `
        --collect:"XPlat Code Coverage" `
        --results-directory:"$resultsDir"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ $Category tests passed!" -ForegroundColor Green
    } else {
        Write-Host "❌ $Category tests failed!" -ForegroundColor Red
    }

    Write-Host ""
}

# Build the projects first
Write-Host "Building projects..." -ForegroundColor Cyan
dotnet build ..\src\NeoServiceLayer.Tee.Enclave\NeoServiceLayer.Tee.Enclave.csproj -c Debug
dotnet build ..\src\NeoServiceLayer.Tee.Host\NeoServiceLayer.Tee.Host.csproj -c Debug
dotnet build ..\src\NeoServiceLayer.Shared\NeoServiceLayer.Shared.csproj -c Debug

# Run Occlum tests
Run-Tests -ProjectPath ".\NeoServiceLayer.Occlum.Tests\NeoServiceLayer.Occlum.Tests.csproj" -Category "Occlum" -OutputPrefix "Occlum"

# Run API tests with Occlum
Run-Tests -ProjectPath ".\NeoServiceLayer.Api.Tests\NeoServiceLayer.Api.Tests.csproj" -Category "Occlum" -OutputPrefix "Api-Occlum"

# Run integration tests with Occlum
Run-Tests -ProjectPath ".\NeoServiceLayer.IntegrationTests\NeoServiceLayer.IntegrationTests.csproj" -Category "Occlum" -OutputPrefix "Integration-Occlum"

Write-Host "All Occlum tests completed!" -ForegroundColor Yellow
Write-Host "Results saved to: $resultsDir" -ForegroundColor Yellow
