# PowerShell script to remove all OpenEnclave-related files from the project
# This script should be run after the migration to Occlum LibOS is complete

# Stop on first error
$ErrorActionPreference = "Stop"

Write-Host "Removing OpenEnclave-related files from the project..." -ForegroundColor Red

# Define the files to remove
$filesToRemove = @(
    # OpenEnclave-specific files in NeoServiceLayer.Tee.Enclave
    "src\NeoServiceLayer.Tee.Enclave\OpenEnclaveUtils.h",
    "src\NeoServiceLayer.Tee.Enclave\OpenEnclaveUtils.cpp",
    "src\NeoServiceLayer.Tee.Enclave\OpenEnclaveEnclave.h",
    "src\NeoServiceLayer.Tee.Enclave\OpenEnclaveEnclave.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Makefile.oe",
    "src\NeoServiceLayer.Tee.Enclave\NeoServiceLayerEnclave.edl",
    "src\NeoServiceLayer.Tee.Enclave\NeoServiceLayerEnclave.oe.edl",
    "src\NeoServiceLayer.Tee.Enclave\NeoServiceLayerEnclave.conf",
    "src\NeoServiceLayer.Tee.Enclave\build_enclave.oe.ps1",
    "src\NeoServiceLayer.Tee.Enclave\build_enclave.oe.sh",
    
    # OpenEnclave-specific implementation files in NeoServiceLayer.Tee.Enclave\Enclave
    "src\NeoServiceLayer.Tee.Enclave\Enclave\NeoServiceLayerEnclave.oe.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\EnclaveUtils.oe.h",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\EnclaveUtils.oe.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\GasAccounting.oe.h",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\GasAccounting.oe.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\SecretManager.oe.h",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\SecretManager.oe.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\JavaScriptEngine.oe.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\KeyManager.oe.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\StorageManager.oe.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\EventTrigger.oe.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\BackupManager.oe.cpp",
    
    # OpenEnclave-specific enclave files in NeoServiceLayer.Tee.Enclave\Enclave
    "src\NeoServiceLayer.Tee.Enclave\Enclave\OpenEnclaveEnclave.h",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\OpenEnclaveEnclave.Core.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\OpenEnclaveEnclave.JavaScript.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\OpenEnclaveEnclave.Storage.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\OpenEnclaveEnclave.Secrets.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\OpenEnclaveEnclave.Attestation.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\OpenEnclaveEnclave.Events.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\OpenEnclaveEnclave.Randomness.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\OpenEnclaveEnclave.Compliance.cpp",
    "src\NeoServiceLayer.Tee.Enclave\Enclave\OpenEnclaveEnclave.MessageProcessor.cpp",
    
    # OpenEnclave-specific files in NeoServiceLayer.Tee.Host
    "src\NeoServiceLayer.Tee.Host\OpenEnclaveInterface.cs",
    "src\NeoServiceLayer.Tee.Host\OpenEnclaveDelegates.cs",
    "src\NeoServiceLayer.Tee.Host\OpenEnclaveHostCallbacks.cs",
    "src\NeoServiceLayer.Tee.Host\OpenEnclaveNativeMethods.cs",
    "src\NeoServiceLayer.Tee.Host\OpenEnclaveSdkLoader.cs",
    "src\NeoServiceLayer.Tee.Host\OpenEnclaveSdkWrappers.cs",
    "src\NeoServiceLayer.Tee.Host\OpenEnclaveTeeInterface.cs",
    "src\NeoServiceLayer.Tee.Host\OpenEnclaveTeeOptions.cs",
    "src\NeoServiceLayer.Tee.Host\OpenEnclaveAvailabilityChecker.cs",
    "src\NeoServiceLayer.Tee.Host\IOpenEnclaveInterface.cs",
    "src\NeoServiceLayer.Tee.Host\Native\OpenEnclaveNative.cs",
    
    # OpenEnclave-specific files in NeoServiceLayer.Tee.Host\JavaScriptExecution
    "src\NeoServiceLayer.Tee.Host\JavaScriptExecution\OpenEnclaveJavaScriptExecution.cs",
    
    # OpenEnclave-specific files in NeoServiceLayer.Tee.Host\RemoteAttestation
    "src\NeoServiceLayer.Tee.Host\RemoteAttestation\OpenEnclaveAttestation.cs",
    "src\NeoServiceLayer.Tee.Host\RemoteAttestation\OpenEnclaveAttestationProvider.cs",
    
    # OpenEnclave-specific files in NeoServiceLayer.Tee.Host\Services
    "src\NeoServiceLayer.Tee.Host\Services\OpenEnclaveTeeClient.cs",
    
    # OpenEnclave-specific test files
    "tests\NeoServiceLayer.Tee.Enclave.Tests\OpenEnclaveTests.cs",
    "tests\NeoServiceLayer.Tee.Host.Tests\OpenEnclaveInterfaceTests.cs",
    "tests\NeoServiceLayer.Tee.Host.Tests\OpenEnclaveJavaScriptExecutionTests.cs",
    "tests\NeoServiceLayer.Tee.Host.Tests\OpenEnclaveAttestationTests.cs"
)

# Remove the files
foreach ($file in $filesToRemove) {
    $filePath = Join-Path -Path $PWD -ChildPath $file
    if (Test-Path $filePath) {
        Write-Host "Removing file: $file" -ForegroundColor Yellow
        Remove-Item -Path $filePath -Force
    } else {
        Write-Host "File not found: $file" -ForegroundColor Gray
    }
}

# Define the patterns to search for in code files
$patterns = @(
    "using NeoServiceLayer.Tee.Host.OpenEnclave",
    "IOpenEnclaveInterface",
    "OpenEnclaveInterface",
    "OpenEnclaveTeeInterface",
    "OpenEnclaveSdkLoader",
    "OpenEnclaveSdkWrappers",
    "OpenEnclaveHostCallbacks",
    "OpenEnclaveNativeMethods",
    "OpenEnclaveAvailabilityChecker",
    "OpenEnclaveDelegates",
    "OpenEnclaveTeeOptions",
    "OpenEnclaveAttestation",
    "OpenEnclaveAttestationProvider",
    "OpenEnclaveJavaScriptExecution",
    "OpenEnclaveTeeClient",
    "#include ""openenclave/",
    "openenclave::",
    "oe_",
    "OE_"
)

# Search for OpenEnclave references in the project
Write-Host "Searching for references to OpenEnclave in the codebase..." -ForegroundColor Cyan

# Get all code files
$codeFiles = Get-ChildItem -Path $PWD -Recurse -Include *.cs, *.cpp, *.h, *.csproj, *.edl, *.md -File

# Search for patterns in each file
foreach ($file in $codeFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    $foundPatterns = @()
    
    foreach ($pattern in $patterns) {
        if ($content -match [regex]::Escape($pattern)) {
            $foundPatterns += $pattern
        }
    }
    
    if ($foundPatterns.Count -gt 0) {
        Write-Host "Found OpenEnclave references in file: $($file.FullName)" -ForegroundColor Yellow
        Write-Host "  Patterns found: $($foundPatterns -join ', ')" -ForegroundColor Yellow
    }
}

Write-Host "OpenEnclave-related files have been removed from the project." -ForegroundColor Green
Write-Host "Please manually update any remaining references to OpenEnclave in the codebase." -ForegroundColor Cyan
