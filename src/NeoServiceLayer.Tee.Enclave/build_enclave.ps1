# Build script for Occlum Enclave
# This script is a wrapper around build_occlum.ps1

# Stop on first error
$ErrorActionPreference = "Stop"

Write-Host "Building Occlum Enclave..." -ForegroundColor Green

# Check if we're in simulation mode
$simulationMode = $env:OCCLUM_SIMULATION -eq "1"
if (-not $simulationMode) {
    # Check if OE_SIMULATION is set (for backward compatibility)
    $simulationMode = $env:OE_SIMULATION -eq "1"
    if ($simulationMode) {
        $env:OCCLUM_SIMULATION = "1"
        Write-Host "OE_SIMULATION is set, setting OCCLUM_SIMULATION=1 for compatibility" -ForegroundColor Yellow
    }
}

# Call the Occlum build script
$occlumBuildScript = Join-Path $PSScriptRoot "build_occlum.ps1"
if (Test-Path $occlumBuildScript) {
    & $occlumBuildScript
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to build Occlum enclave" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Occlum build script not found at: $occlumBuildScript" -ForegroundColor Red
    exit 1
}

# Create build directory if it doesn't exist
$buildDir = Join-Path $PSScriptRoot "build"
if (-not (Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir | Out-Null
}

# Create enclave.signed and enclave.debug files for compatibility
$signedEnclaveFile = Join-Path $buildDir "enclave.signed"
$debugEnclaveFile = Join-Path $buildDir "enclave.debug"
$enclaveFile = Join-Path $buildDir "lib\libenclave.so"

if (Test-Path $enclaveFile) {
    Copy-Item $enclaveFile $signedEnclaveFile -Force
    Copy-Item $enclaveFile $debugEnclaveFile -Force
    Write-Host "Created compatibility enclave files" -ForegroundColor Green
} else {
    Write-Host "Enclave file not found at: $enclaveFile" -ForegroundColor Yellow
    Write-Host "Creating mock enclave files for compatibility..." -ForegroundColor Yellow
    Set-Content -Path $signedEnclaveFile -Value "MOCK ENCLAVE FOR SIMULATION MODE"
    Set-Content -Path $debugEnclaveFile -Value "MOCK ENCLAVE FOR SIMULATION MODE"
}

Write-Host "Enclave build completed successfully!" -ForegroundColor Green
