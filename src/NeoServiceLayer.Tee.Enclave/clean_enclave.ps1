# Clean script for Occlum Enclave
# This script removes all build artifacts

Write-Host "Cleaning Occlum Enclave..." -ForegroundColor Green

# Set paths
$buildDir = Join-Path $PSScriptRoot "build"

# Remove build directory if it exists
if (Test-Path $buildDir) {
    Write-Host "Removing build directory..." -ForegroundColor Cyan
    Remove-Item -Path $buildDir -Recurse -Force
}

# Remove Occlum instance if it exists
$occlumInstanceDir = Join-Path $PSScriptRoot "occlum_instance"
if (Test-Path $occlumInstanceDir) {
    Write-Host "Removing Occlum instance directory..." -ForegroundColor Cyan
    Remove-Item -Path $occlumInstanceDir -Recurse -Force
}

Write-Host "Occlum enclave clean completed successfully!" -ForegroundColor Green
