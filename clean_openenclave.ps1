# PowerShell script to remove all OpenEnclave-related files from the project
# This script should be run after the migration to Occlum LibOS is complete

# Stop on first error
$ErrorActionPreference = "Stop"

Write-Host "Removing OpenEnclave-related files from the project..." -ForegroundColor Red

# Define the files to remove
$filesToRemove = @(
    # OpenEnclave-specific files
    "OpenEnclaveUtils.h",
    "OpenEnclaveUtils.cpp",
    "OpenEnclaveEnclave.h",
    "OpenEnclaveEnclave.cpp",
    "Makefile.oe",
    "NeoServiceLayerEnclave.edl",
    "NeoServiceLayerEnclave.oe.edl",
    "NeoServiceLayerEnclave.conf",
    
    # OpenEnclave-specific implementation files
    "Enclave\NeoServiceLayerEnclave.oe.cpp",
    "Enclave\EnclaveUtils.oe.h",
    "Enclave\EnclaveUtils.oe.cpp",
    "Enclave\GasAccounting.oe.h",
    "Enclave\GasAccounting.oe.cpp",
    "Enclave\SecretManager.oe.h",
    "Enclave\SecretManager.oe.cpp",
    "Enclave\JavaScriptEngine.oe.cpp",
    "Enclave\KeyManager.oe.cpp",
    "Enclave\StorageManager.oe.cpp",
    "Enclave\EventTrigger.oe.cpp",
    "Enclave\BackupManager.oe.cpp",
    
    # OpenEnclave-specific enclave files
    "Enclave\OpenEnclaveEnclave.h",
    "Enclave\OpenEnclaveEnclave.Core.cpp",
    "Enclave\OpenEnclaveEnclave.JavaScript.cpp",
    "Enclave\OpenEnclaveEnclave.Storage.cpp",
    "Enclave\OpenEnclaveEnclave.Secrets.cpp",
    "Enclave\OpenEnclaveEnclave.Attestation.cpp",
    "Enclave\OpenEnclaveEnclave.Events.cpp",
    "Enclave\OpenEnclaveEnclave.Randomness.cpp",
    "Enclave\OpenEnclaveEnclave.Compliance.cpp",
    "Enclave\OpenEnclaveEnclave.MessageProcessor.cpp",
    
    # OpenEnclave-specific build files
    "build_enclave.oe.ps1",
    "build_enclave.oe.sh"
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

# Rename Occlum files to their final names
$filesToRename = @(
    @{Source = "Enclave\NeoServiceLayerEnclave.Occlum.cpp"; Target = "Enclave\NeoServiceLayerEnclave.cpp"},
    @{Source = "Enclave\NeoServiceLayerEnclave.Occlum2.cpp"; Target = "Enclave\NeoServiceLayerEnclave2.cpp"}
)

foreach ($fileRename in $filesToRename) {
    $sourcePath = Join-Path -Path $PWD -ChildPath $fileRename.Source
    $targetPath = Join-Path -Path $PWD -ChildPath $fileRename.Target
    
    if (Test-Path $sourcePath) {
        Write-Host "Renaming file: $($fileRename.Source) to $($fileRename.Target)" -ForegroundColor Green
        
        # Remove target file if it exists
        if (Test-Path $targetPath) {
            Remove-Item -Path $targetPath -Force
        }
        
        # Rename the file
        Rename-Item -Path $sourcePath -NewName (Split-Path $targetPath -Leaf)
    } else {
        Write-Host "Source file not found: $($fileRename.Source)" -ForegroundColor Gray
    }
}

# Replace build_enclave.ps1 with build_occlum.ps1
$buildOcclumPath = Join-Path -Path $PWD -ChildPath "build_occlum.ps1"
$buildEnclavePath = Join-Path -Path $PWD -ChildPath "build_enclave.ps1"

if (Test-Path $buildOcclumPath) {
    Write-Host "Replacing build_enclave.ps1 with build_occlum.ps1" -ForegroundColor Green
    
    # Backup the original build_enclave.ps1
    if (Test-Path $buildEnclavePath) {
        $backupPath = Join-Path -Path $PWD -ChildPath "build_enclave.oe.ps1"
        Copy-Item -Path $buildEnclavePath -Destination $backupPath -Force
        Remove-Item -Path $buildEnclavePath -Force
    }
    
    # Copy build_occlum.ps1 to build_enclave.ps1
    Copy-Item -Path $buildOcclumPath -Destination $buildEnclavePath -Force
} else {
    Write-Host "build_occlum.ps1 not found" -ForegroundColor Red
}

Write-Host "OpenEnclave-related files have been removed from the project." -ForegroundColor Green
Write-Host "Please make sure to update any remaining references to OpenEnclave in the codebase." -ForegroundColor Cyan
