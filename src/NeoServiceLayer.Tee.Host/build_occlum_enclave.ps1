# Build script for Occlum Enclave
# This script compiles the C++ enclave code and packages it for Occlum

# Stop on first error
$ErrorActionPreference = "Stop"

Write-Host "Building Occlum Enclave..." -ForegroundColor Green

# Check if Occlum is installed
$occlumPath = $env:OCCLUM_PATH
if (-not $occlumPath) {
    $occlumPath = "/opt/occlum"
    if (-not (Test-Path $occlumPath)) {
        Write-Host "Occlum not found. Please install it or set OCCLUM_PATH environment variable." -ForegroundColor Red
        exit 1
    }
}

Write-Host "Using Occlum at: $occlumPath" -ForegroundColor Cyan

# Check if we're in simulation mode
$simulationMode = $env:OCCLUM_SIMULATION -eq "1"
if ($simulationMode) {
    Write-Host "Running in simulation mode." -ForegroundColor Yellow
    $env:OCCLUM_SIMULATION_MODE = "1"
}

# Create build directory if it doesn't exist
$buildDir = Join-Path $PWD "build"
if (-not (Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir | Out-Null
}

# Set paths
$enclaveDir = Join-Path $PWD "Enclave"
$occlumInstanceDir = Join-Path $buildDir "occlum_instance"

# Set compiler flags
$cflags = "-Wall -Wextra -std=c++17 -O2"
$includes = "-I$occlumPath/include -I$enclaveDir"
$defines = "-DOCCLUM -D_DEBUG"

# Set linker flags
$lflags = "-L$occlumPath/lib"
$libs = "-locclum -lmbedtls -lmbedcrypto -lmbedx509"

# Compile enclave source files
Write-Host "Compiling enclave source files..." -ForegroundColor Cyan
$sourceFiles = @(
    # Core files
    "$enclaveDir\NeoServiceLayerEnclave.cpp",
    "$enclaveDir\NeoServiceLayerEnclave2.cpp",
    # Integration files
    "$enclaveDir\OcclumIntegration.Core.cpp",
    "$enclaveDir\OcclumIntegration.Crypto.cpp",
    "$enclaveDir\OcclumIntegration.JavaScriptEngine.cpp",
    "$enclaveDir\OcclumIntegration.Sealing.cpp",
    "$enclaveDir\OcclumIntegration.Utils.cpp",
    "$enclaveDir\OcclumIntegration.Attestation.cpp",
    # Manager files
    "$enclaveDir\StorageManager.cpp",
    "$enclaveDir\KeyManager.Core.cpp",
    "$enclaveDir\KeyManager.Crypto.cpp",
    "$enclaveDir\KeyManager.Storage.cpp",
    "$enclaveDir\SecretManager.Core.cpp",
    "$enclaveDir\SecretManager.Storage.cpp",
    "$enclaveDir\SecretManager.Crypto.cpp",
    "$enclaveDir\SecretManager.Access.cpp",
    "$enclaveDir\SecretManager.Access2.cpp",
    "$enclaveDir\GasAccounting.cpp",
    "$enclaveDir\GasAccountingManager.cpp",
    # JavaScript engine
    "$enclaveDir\JavaScriptEngine.cpp",
    "$enclaveDir\JavaScriptManager.cpp",
    "$enclaveDir\QuickJs\QuickJsEngineAdapter.cpp",
    # Event trigger
    "$enclaveDir\EventTrigger.Core.cpp",
    "$enclaveDir\EventTrigger.Registration.cpp",
    "$enclaveDir\EventTrigger.Processing.cpp",
    "$enclaveDir\EventTrigger.Processing2.cpp",
    "$enclaveDir\EventTrigger.Storage.cpp",
    # Backup manager
    "$enclaveDir\BackupManager.Core.cpp",
    "$enclaveDir\BackupManager.Backup.cpp",
    "$enclaveDir\BackupManager.Restore.cpp",
    "$enclaveDir\BackupManager.Scheduling.cpp",
    "$enclaveDir\BackupManager.Utils.cpp",
    # Utility files
    "$enclaveDir\EnclaveUtils.cpp"
)

$objectFiles = @()
foreach ($sourceFile in $sourceFiles) {
    $objectFile = [System.IO.Path]::ChangeExtension([System.IO.Path]::GetFileName($sourceFile), ".o")
    $objectFile = Join-Path $buildDir $objectFile

    # Skip if source file doesn't exist
    if (-not (Test-Path $sourceFile)) {
        Write-Host "Source file not found, skipping: $sourceFile" -ForegroundColor Yellow
        continue
    }

    $objectFiles += $objectFile

    # Compile C++ files
    & g++ -c $cflags $includes $defines $sourceFile -o $objectFile

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to compile $sourceFile" -ForegroundColor Red
        exit 1
    }
}

# Link object files to create libenclave.so
Write-Host "Linking enclave..." -ForegroundColor Cyan
$enclaveFile = Join-Path $buildDir "libenclave.so"
& g++ -shared -o $enclaveFile $objectFiles $lflags $libs
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to link enclave" -ForegroundColor Red
    exit 1
}

# Create Occlum instance
Write-Host "Creating Occlum instance..." -ForegroundColor Cyan
if (Test-Path $occlumInstanceDir) {
    # Clean up existing instance
    Push-Location $occlumInstanceDir
    & occlum destroy -f
    Pop-Location
    Remove-Item -Path $occlumInstanceDir -Recurse -Force
}

# Initialize new instance
Push-Location $buildDir
& occlum new occlum_instance
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to create Occlum instance" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

# Copy enclave library to Occlum instance
Write-Host "Copying enclave library to Occlum instance..." -ForegroundColor Cyan
$occlumLibDir = Join-Path $occlumInstanceDir "image/lib"
Copy-Item $enclaveFile $occlumLibDir

# Copy Node.js to Occlum instance
Write-Host "Copying Node.js to Occlum instance..." -ForegroundColor Cyan
$nodePath = "/usr/bin/node"
if (Test-Path $nodePath) {
    $occlumBinDir = Join-Path $occlumInstanceDir "image/bin"
    Copy-Item $nodePath $occlumBinDir
} else {
    Write-Host "Node.js not found at $nodePath. Please install it." -ForegroundColor Yellow
}

# Create JavaScript entry point
Write-Host "Creating JavaScript entry point..." -ForegroundColor Cyan
$jsEntryPoint = @"
// Neo Service Layer Enclave JavaScript entry point
const { NeoServiceEnclave } = require('./libenclave');

// Initialize the enclave
NeoServiceEnclave.initialize();

// Start the enclave server
NeoServiceEnclave.startServer();
"@
$jsEntryPointFile = Join-Path $occlumInstanceDir "image/app/enclave_main.js"
New-Item -Path (Split-Path $jsEntryPointFile -Parent) -ItemType Directory -Force | Out-Null
Set-Content -Path $jsEntryPointFile -Value $jsEntryPoint

# Build Occlum instance
Write-Host "Building Occlum instance..." -ForegroundColor Cyan
Push-Location $occlumInstanceDir
& occlum build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build Occlum instance" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

Write-Host "Occlum enclave build completed successfully!" -ForegroundColor Green
Write-Host "To run the enclave, use: cd $occlumInstanceDir && occlum run /bin/node /app/enclave_main.js" -ForegroundColor Cyan
