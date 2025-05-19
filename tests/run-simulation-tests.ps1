# PowerShell script to run all tests in simulation mode

# Set environment variables for testing
$env:OCCLUM_SIMULATION = "1"
$env:OCCLUM_ENCLAVE_PATH = "$PSScriptRoot\..\src\NeoServiceLayer.Tee.Enclave\build\lib\libenclave.so"
$env:OCCLUM_INSTANCE_DIR = "$PSScriptRoot\..\occlum_instance"
$env:DOTNET_ENVIRONMENT = "Testing"
$env:ASPNETCORE_ENVIRONMENT = "Testing"
$env:TEST_CONFIG_PATH = "$PSScriptRoot\simulation-test-config.json"

# Create results directory
$resultsDir = "$PSScriptRoot\TestResults\$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss')"
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null

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

# Run all the test categories
Write-Host "Starting test execution in simulation mode..." -ForegroundColor Yellow
Write-Host "Results will be saved to: $resultsDir" -ForegroundColor Yellow
Write-Host ""

# Run basic tests
Run-Tests -ProjectPath ".\NeoServiceLayer.BasicTests\NeoServiceLayer.BasicTests.csproj" -Category "Basic" -OutputPrefix "Basic"

# Run mock tests
Run-Tests -ProjectPath ".\NeoServiceLayer.MockTests\NeoServiceLayer.MockTests.csproj" -Category "Mock" -OutputPrefix "Mock"
Run-Tests -ProjectPath ".\NeoServiceLayer.MockTests\NeoServiceLayer.MockTests.csproj" -Category "Security" -OutputPrefix "Security"
Run-Tests -ProjectPath ".\NeoServiceLayer.MockTests\NeoServiceLayer.MockTests.csproj" -Category "Performance" -OutputPrefix "Performance"
Run-Tests -ProjectPath ".\NeoServiceLayer.MockTests\NeoServiceLayer.MockTests.csproj" -Category "ErrorHandling" -OutputPrefix "ErrorHandling"

# Run simulation mode tests if the enclave is available
if (Test-Path $env:OCCLUM_ENCLAVE_PATH) {
    Run-Tests -ProjectPath ".\NeoServiceLayer.Tee.Enclave.Tests\NeoServiceLayer.Tee.Enclave.Tests.csproj" -Category "Occlum" -OutputPrefix "Occlum"
    Run-Tests -ProjectPath ".\NeoServiceLayer.Tee.Enclave.Tests\NeoServiceLayer.Tee.Enclave.Tests.csproj" -Category "Attestation" -OutputPrefix "Attestation"
    Run-Tests -ProjectPath ".\NeoServiceLayer.Tee.Enclave.Tests\NeoServiceLayer.Tee.Enclave.Tests.csproj" -Category "JavaScriptEngine" -OutputPrefix "JavaScriptEngine"
    Run-Tests -ProjectPath ".\NeoServiceLayer.Tee.Enclave.Tests\NeoServiceLayer.Tee.Enclave.Tests.csproj" -Category "GasAccounting" -OutputPrefix "GasAccounting"
    Run-Tests -ProjectPath ".\NeoServiceLayer.Tee.Enclave.Tests\NeoServiceLayer.Tee.Enclave.Tests.csproj" -Category "UserSecrets" -OutputPrefix "UserSecrets"
    Run-Tests -ProjectPath ".\NeoServiceLayer.Occlum.Tests\NeoServiceLayer.Occlum.Tests.csproj" -Category "Occlum" -OutputPrefix "Occlum"
    Run-Tests -ProjectPath ".\NeoServiceLayer.IntegrationTests\NeoServiceLayer.IntegrationTests.csproj" -Category "Integration" -OutputPrefix "Integration"

    # Check if the API is running
    try {
        $apiRunning = (Invoke-WebRequest -Uri "http://localhost:5000/api/health" -Method GET -UseBasicParsing -ErrorAction SilentlyContinue).StatusCode -eq 200
    } catch {
        $apiRunning = $false
    }

    if ($apiRunning) {
        Write-Host "API is running. Running API integration tests..." -ForegroundColor Cyan
        Run-Tests -ProjectPath ".\NeoServiceLayer.IntegrationTests\NeoServiceLayer.IntegrationTests.csproj" -Category "ApiIntegration" -OutputPrefix "ApiIntegration"
    } else {
        Write-Host "⚠️ API is not running. Skipping API integration tests." -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️ Enclave binary not found at $env:OE_ENCLAVE_PATH. Skipping simulation mode tests." -ForegroundColor Yellow
}

# Generate test summary
Write-Host "Generating test summary..." -ForegroundColor Cyan
$trxFiles = Get-ChildItem -Path $resultsDir -Filter "*.trx"
$totalTests = 0
$passedTests = 0
$failedTests = 0
$skippedTests = 0

foreach ($trxFile in $trxFiles) {
    $xml = [xml](Get-Content $trxFile.FullName)
    $counters = $xml.TestRun.ResultSummary.Counters
    $totalTests += [int]$counters.total
    $passedTests += [int]$counters.passed
    $failedTests += [int]$counters.failed
    $skippedTests += [int]$counters.notExecuted
}

Write-Host ""
Write-Host "Test Summary:" -ForegroundColor Yellow
Write-Host "  Total Tests: $totalTests" -ForegroundColor Cyan
Write-Host "  Passed: $passedTests" -ForegroundColor Green
Write-Host "  Failed: $failedTests" -ForegroundColor Red
Write-Host "  Skipped: $skippedTests" -ForegroundColor Yellow
Write-Host ""

if ($failedTests -eq 0) {
    Write-Host "✅ All tests passed!" -ForegroundColor Green
} else {
    Write-Host "❌ Some tests failed!" -ForegroundColor Red
    exit 1
}

# Open the test results directory
Invoke-Item $resultsDir
