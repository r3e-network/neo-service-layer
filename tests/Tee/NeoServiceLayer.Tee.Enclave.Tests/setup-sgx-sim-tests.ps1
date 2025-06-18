# Setup script for running SGX SDK tests in simulation mode on Windows
# This script configures the environment to use Intel SGX SDK

Write-Host "=== SGX SDK Simulation Mode Test Setup (Windows) ===" -ForegroundColor Cyan
Write-Host

# Set SGX mode to simulation
$env:SGX_MODE = "SIM"
$env:SGX_DEBUG = "1"
$env:SGX_PRERELEASE = "1"

# Common SGX SDK installation paths on Windows
$sgxPaths = @(
    "C:\Program Files\Intel\SGXWindows\",
    "C:\Intel\SGXWindows\",
    "$env:ProgramFiles\Intel\SGXWindows\",
    "$env:LOCALAPPDATA\Intel\SGXWindows\"
)

$sgxFound = $false
$sgxPath = ""

foreach ($path in $sgxPaths) {
    if (Test-Path $path) {
        $sgxFound = $true
        $sgxPath = $path
        break
    }
}

if ($sgxFound) {
    Write-Host "✓ Intel SGX SDK found at $sgxPath" -ForegroundColor Green
    
    # Set SGX SDK environment variables
    $env:SGX_SDK = $sgxPath
    $env:PATH = "$sgxPath\bin\x64\Release;$sgxPath\bin\x64\Debug;$env:PATH"
    
    # Add SGX libraries to PATH
    $env:PATH = "$sgxPath\sdk\lib\x64;$env:PATH"
} else {
    Write-Host "✗ Intel SGX SDK not found!" -ForegroundColor Red
    Write-Host "Please install Intel SGX SDK for Windows first:" -ForegroundColor Yellow
    Write-Host "  Download from: https://software.intel.com/content/www/us/en/develop/topics/software-guard-extensions/sdk.html" -ForegroundColor Yellow
    exit 1
}

# Create native library directory if it doesn't exist
$nativeLibDir = ".\native"
if (!(Test-Path $nativeLibDir)) {
    New-Item -ItemType Directory -Path $nativeLibDir | Out-Null
    Write-Host "Created native library directory: $nativeLibDir" -ForegroundColor Gray
}

# Check for Visual Studio to compile native components
$vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vsWhere) {
    $vsPath = & $vsWhere -latest -property installationPath
    if ($vsPath) {
        Write-Host "✓ Visual Studio found at $vsPath" -ForegroundColor Green
        
        # Set up Visual Studio environment
        $vcvarsPath = "$vsPath\VC\Auxiliary\Build\vcvars64.bat"
        if (Test-Path $vcvarsPath) {
            Write-Host "Setting up Visual Studio environment..." -ForegroundColor Gray
            cmd /c "`"$vcvarsPath`" && set" | foreach {
                if ($_ -match "^(.*?)=(.*)$") {
                    Set-Item -Path "env:$($matches[1])" -Value $matches[2]
                }
            }
        }
    }
} else {
    Write-Host "⚠ Visual Studio not found. Native compilation may not work." -ForegroundColor Yellow
}

# Create a test runner script
$testRunner = @'
# SGX SDK Test Runner for Windows
param(
    [string]$Filter = "Category=SGXIntegration",
    [switch]$Verbose,
    [switch]$Coverage
)

Write-Host "Running SGX SDK tests in simulation mode..." -ForegroundColor Cyan
Write-Host "SGX_MODE=$env:SGX_MODE" -ForegroundColor Gray
Write-Host "SGX_SDK=$env:SGX_SDK" -ForegroundColor Gray
Write-Host

$testArgs = @(
    "test",
    "--filter", $Filter,
    "--logger", "console;verbosity=$(if($Verbose) {'detailed'} else {'normal'})"
)

if ($Coverage) {
    $testArgs += @(
        "--collect", "Code Coverage",
        "--results-directory", ".\TestResults"
    )
}

& dotnet $testArgs
'@

Set-Content -Path ".\run-sgx-tests.ps1" -Value $testRunner

# Create batch file for command prompt users
$batchRunner = @"
@echo off
echo Running SGX SDK tests in simulation mode...
echo SGX_MODE=%SGX_MODE%
echo SGX_SDK=%SGX_SDK%
echo.

dotnet test --filter "Category=SGXIntegration" --logger "console;verbosity=normal"
"@

Set-Content -Path ".\run-sgx-tests.bat" -Value $batchRunner

Write-Host
Write-Host "=== Setup Complete ===" -ForegroundColor Green
Write-Host
Write-Host "Environment variables set:" -ForegroundColor Cyan
Write-Host "  SGX_MODE=$env:SGX_MODE" -ForegroundColor Gray
Write-Host "  SGX_SDK=$env:SGX_SDK" -ForegroundColor Gray
Write-Host
Write-Host "To run SGX SDK tests in simulation mode:" -ForegroundColor Cyan
Write-Host "  PowerShell: .\run-sgx-tests.ps1" -ForegroundColor Yellow
Write-Host "  CMD: run-sgx-tests.bat" -ForegroundColor Yellow
Write-Host
Write-Host "Or run directly with:" -ForegroundColor Cyan
Write-Host '  dotnet test --filter "Category=SGXIntegration"' -ForegroundColor Yellow