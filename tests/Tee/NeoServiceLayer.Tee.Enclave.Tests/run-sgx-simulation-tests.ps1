# SGX Simulation Tests Runner
# This script runs the SGX enclave simulation tests with proper environment configuration

param(
    [switch]$Verbose,
    [switch]$Coverage,
    [string]$Filter = ""
)

Write-Host "Neo Service Layer - SGX Simulation Tests" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Set SGX simulation environment variables
$env:SGX_MODE = "SIM"
$env:SGX_SIMULATION = "1"
$env:TEE_MODE = "SIMULATION"

Write-Host "Environment Configuration:" -ForegroundColor Yellow
Write-Host "   SGX_MODE: $env:SGX_MODE"
Write-Host "   SGX_SIMULATION: $env:SGX_SIMULATION"
Write-Host "   TEE_MODE: $env:TEE_MODE"
Write-Host "   Platform: $([System.Runtime.InteropServices.RuntimeInformation]::OSDescription)"
Write-Host ""

# Build the test project
Write-Host "Building test project..." -ForegroundColor Green
$buildResult = dotnet build --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

# Prepare test command
$testCommand = "dotnet test"
if ($Coverage) {
    $testCommand += " --collect:`"XPlat Code Coverage`""
}
if ($Filter) {
    $testCommand += " --filter `"$Filter`""
}
if ($Verbose) {
    $testCommand += " --verbosity detailed"
} else {
    $testCommand += " --verbosity normal"
}

Write-Host "Running SGX simulation tests..." -ForegroundColor Green
Write-Host "Command: $testCommand" -ForegroundColor Gray
Write-Host ""

# Execute tests
$testStartTime = Get-Date
Invoke-Expression $testCommand
$testResult = $LASTEXITCODE
$testEndTime = Get-Date
$testDuration = $testEndTime - $testStartTime

Write-Host ""
Write-Host "Test Results Summary:" -ForegroundColor Yellow
$durationStr = $testDuration.TotalSeconds.ToString("F2")
Write-Host "   Duration: $durationStr seconds"
Write-Host "   Exit Code: $testResult"

if ($testResult -eq 0) {
    Write-Host "   Status: All tests passed!" -ForegroundColor Green
    Write-Host "   Result: Production-ready SGX simulation verified!" -ForegroundColor Green
} else {
    Write-Host "   Status: Some tests failed!" -ForegroundColor Red
}

Write-Host ""
Write-Host "SGX simulation testing completed!" -ForegroundColor Cyan

exit $testResult 