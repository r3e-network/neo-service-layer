# PowerShell script to run the Occlum enclave
# This script should be run after building the enclave with build_occlum.ps1

# Stop on first error
$ErrorActionPreference = "Stop"

Write-Host "Running Occlum Enclave..." -ForegroundColor Green

# Check if Occlum is installed
$occlumPath = $env:OCCLUM_PATH
if (-not $occlumPath) {
    $occlumPath = "/opt/occlum"
    if (-not (Test-Path $occlumPath)) {
        Write-Host "Occlum not found. Please install it or set OCCLUM_PATH environment variable." -ForegroundColor Red
        exit 1
    }
}

# Check if we're in simulation mode
$simulationMode = $env:OCCLUM_SIMULATION -eq "1"
if ($simulationMode) {
    Write-Host "Running in simulation mode." -ForegroundColor Yellow
    $env:OCCLUM_SIMULATION_MODE = "1"
}

# Set paths
$buildDir = Join-Path $PWD "build"
$occlumInstanceDir = Join-Path $buildDir "occlum_instance"

# Check if Occlum instance exists
if (-not (Test-Path $occlumInstanceDir)) {
    Write-Host "Occlum instance not found. Please build the enclave first with build_occlum.ps1." -ForegroundColor Red
    exit 1
}

# Run the enclave
Write-Host "Starting the enclave..." -ForegroundColor Cyan
Push-Location $occlumInstanceDir
& occlum run /bin/node /app/enclave_main.js
$exitCode = $LASTEXITCODE
Pop-Location

if ($exitCode -ne 0) {
    Write-Host "Enclave exited with error code: $exitCode" -ForegroundColor Red
    exit $exitCode
}

Write-Host "Enclave exited successfully." -ForegroundColor Green
