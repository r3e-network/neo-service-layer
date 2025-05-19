# Script to remove OpenEnclave-specific files and references
# This script helps clean up OpenEnclave dependencies when using Occlum exclusively

# Set error action preference to stop on any error
$ErrorActionPreference = "Stop"

Write-Host "Starting OpenEnclave cleanup..." -ForegroundColor Green

# Define file patterns to remove (OpenEnclave specific)
$filesToRemove = @(
    "ISgxEnclaveInterface.cs",
    "SgxEnclaveInterface.cs",
    "SgxJavaScriptUtilities.cs",
    "OpenEnclaveInterface.cs",
    "*OpenEnclave*.cs",
    "*Sgx*.cs"
)

# Define exclusions (files containing Occlum references that should be kept)
$exclusions = @(
    "OcclumInterface.cs",
    "OcclumJavaScriptUtilities.cs",
    "OcclumFileStorageProvider.cs"
)

# Define directories to clean
$directoriesToClean = @(
    ".",
    "./OpenEnclave",
    "./Enclave/OpenEnclave"
)

# Remove specific OpenEnclave files
foreach ($dir in $directoriesToClean) {
    if (Test-Path $dir) {
        Write-Host "Checking directory: $dir" -ForegroundColor Yellow
        foreach ($pattern in $filesToRemove) {
            $matchingFiles = Get-ChildItem -Path $dir -Filter $pattern -File -ErrorAction SilentlyContinue
            foreach ($file in $matchingFiles) {
                # Check if file should be excluded
                $shouldExclude = $false
                foreach ($exclusion in $exclusions) {
                    if ($file.Name -like $exclusion) {
                        $shouldExclude = $true
                        break
                    }
                }
                
                if (-not $shouldExclude) {
                    Write-Host "Removing file: $($file.FullName)" -ForegroundColor Cyan
                    Remove-Item -Path $file.FullName -Force
                } else {
                    Write-Host "Skipping exclusion: $($file.Name)" -ForegroundColor DarkYellow
                }
            }
        }
    } else {
        Write-Host "Directory not found: $dir" -ForegroundColor DarkGray
    }
}

# Remove OpenEnclave directory if it exists
$oeDir = "./OpenEnclave"
if (Test-Path $oeDir) {
    Write-Host "Removing OpenEnclave directory: $oeDir" -ForegroundColor Red
    Remove-Item -Path $oeDir -Recurse -Force
}

# Remove OpenEnclave directory in Enclave if it exists
$oeEnclaveDir = "./Enclave/OpenEnclave"
if (Test-Path $oeEnclaveDir) {
    Write-Host "Removing OpenEnclave directory in Enclave: $oeEnclaveDir" -ForegroundColor Red
    Remove-Item -Path $oeEnclaveDir -Recurse -Force
}

# Update references in project file
$projectFile = "./NeoServiceLayer.Tee.Enclave.csproj"
if (Test-Path $projectFile) {
    Write-Host "Updating project file to remove OpenEnclave references..." -ForegroundColor Yellow
    
    $content = Get-Content -Path $projectFile -Raw
    
    # Replace OpenEnclave references with Occlum
    $content = $content -replace '<ProjectReference Include=".*OpenEnclave.*" />', ''
    $content = $content -replace '<PackageReference Include="OpenEnclave.*" />', ''
    
    # Save the updated content
    Set-Content -Path $projectFile -Value $content
    
    Write-Host "Project file updated" -ForegroundColor Green
} else {
    Write-Host "Project file not found: $projectFile" -ForegroundColor Red
}

# Update CMakeLists.txt to remove OpenEnclave references
$cmakeFile = "./CMakeLists.txt"
if (Test-Path $cmakeFile) {
    Write-Host "Updating CMakeLists.txt to remove OpenEnclave references..." -ForegroundColor Yellow
    
    $content = Get-Content -Path $cmakeFile -Raw
    
    # Replace OpenEnclave references with Occlum
    $content = $content -replace 'find_package\(OpenEnclave.*\)', '# OpenEnclave references removed'
    $content = $content -replace 'target_link_libraries\(.*openenclave.*\)', '# OpenEnclave link libraries removed'
    
    # Save the updated content
    Set-Content -Path $cmakeFile -Value $content
    
    Write-Host "CMakeLists.txt updated" -ForegroundColor Green
} else {
    Write-Host "CMakeLists.txt not found: $cmakeFile" -ForegroundColor Red
}

# Check for other files that might contain OpenEnclave references
Write-Host "Checking for other files with OpenEnclave references..." -ForegroundColor Yellow
$filesToCheck = Get-ChildItem -Path "." -Filter "*.cs" -Recurse -File
$foundReferences = $false

foreach ($file in $filesToCheck) {
    $content = Get-Content -Path $file.FullName -Raw
    if ($content -match "OpenEnclave" -or $content -match "SGX" -or $content -match "Sgx") {
        Write-Host "OpenEnclave references found in: $($file.FullName)" -ForegroundColor Magenta
        $foundReferences = $true
    }
}

if (-not $foundReferences) {
    Write-Host "No other OpenEnclave references found in code files" -ForegroundColor Green
}

Write-Host "OpenEnclave cleanup completed" -ForegroundColor Green
