#!/usr/bin/env pwsh
# Neo Service Layer - Comprehensive Build Script
# Supports SGX SDK and Occlum LibOS integration with cross-platform capabilities

param(
    [string]$Configuration = "Debug",
    [string]$SGX_MODE = "SIM",
    [string]$SGX_DEBUG = "1",
    [switch]$SkipTests = $false,
    [switch]$SkipEnclave = $false,
    [switch]$Docker = $false,
    [switch]$Clean = $false,
    [switch]$Verbose = $false,
    [string]$OutputPath = "./artifacts",
    [string]$SGXSdkPath = ""
)

# Script configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Colors for output - Fixed color constants
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Cyan = "Cyan"
$Blue = "Blue"
$Magenta = "Magenta"
$White = "White"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Write-Header {
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput ("=" * 80) $Cyan
    Write-ColorOutput "  $Title" $Cyan
    Write-ColorOutput ("=" * 80) $Cyan
    Write-Host ""
}

function Write-Step {
    param([string]$Message)
    Write-ColorOutput "🔧 $Message" $Blue
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✅ $Message" $Green
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠️  $Message" $Yellow
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "❌ $Message" $Red
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput "ℹ️  $Message" $Magenta
}

# Detect platform and set default SGX SDK path
function Initialize-Environment {
    Write-Step "Initializing build environment..."
    
    # Set SGX SDK path if not provided
    if ([string]::IsNullOrEmpty($SGXSdkPath)) {
        if ($IsLinux) {
            $script:SGXSdkPath = "/opt/intel/sgxsdk"
        } elseif ($IsWindows) {
            $script:SGXSdkPath = "C:\Program Files (x86)\Intel\sgxsdk"
        }
    }
    
    # Set environment variables
    $env:SGX_MODE = $SGX_MODE
    $env:SGX_DEBUG = $SGX_DEBUG
    $env:SGX_SDK = $script:SGXSdkPath
    $env:DOTNET_ENVIRONMENT = "Development"
    $env:EnableTEESupport = "true"
    $env:EnableSGXSupport = "true"
    $env:EnableOcclumSupport = "true"
    
    Write-Success "Build environment initialized"
    Write-Host "  Platform: $([System.Runtime.InteropServices.RuntimeInformation]::OSDescription)"
    Write-Host "  Architecture: $([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)"
    Write-Host "  Configuration: $Configuration"
    Write-Host "  SGX Mode: $SGX_MODE"
    Write-Host "  SGX Debug: $SGX_DEBUG"
    Write-Host "  SGX SDK: $script:SGXSdkPath"
    Write-Host "  Output Path: $OutputPath"
}

# Validate dependencies
function Test-Dependencies {
    Write-Step "Validating build dependencies..."
    
    # Check .NET SDK
    try {
        $dotnetVersion = & dotnet --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success ".NET SDK version: $dotnetVersion"
        } else {
            throw "dotnet command failed"
        }
    } catch {
        Write-Error ".NET SDK not found. Please install .NET 9.0 SDK or later"
        return $false
    }
    
    # Check PowerShell version
    $psVersion = $PSVersionTable.PSVersion
    if ($psVersion.Major -lt 7) {
        Write-Warning "PowerShell 7+ recommended for best cross-platform compatibility (current: $psVersion)"
    } else {
        Write-Success "PowerShell version: $psVersion"
    }
    
    # Check Rust if not skipping enclave
    if (!$SkipEnclave) {
        try {
            $rustVersion = & rustc --version 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Rust version: $rustVersion"
            } else {
                Write-Warning "Rust not found. Enclave build may fail. Install from https://rustup.rs/"
            }
        } catch {
            Write-Warning "Rust not found. Enclave build may fail. Install from https://rustup.rs/"
        }
    }
    
    # Check Docker if building Docker images
    if ($Docker) {
        try {
            $dockerVersion = & docker --version 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Docker version: $dockerVersion"
            } else {
                Write-Error "Docker not found but required for Docker build"
                return $false
            }
        } catch {
            Write-Error "Docker not found but required for Docker build"
            return $false
        }
    }
    
    # Check SGX SDK
    if (Test-Path $script:SGXSdkPath) {
        Write-Success "SGX SDK found at: $script:SGXSdkPath"
    } else {
        Write-Warning "SGX SDK not found at $script:SGXSdkPath"
        Write-Info "Install Intel SGX SDK for enhanced SGX support, or continue with simulation mode"
    }
    
    return $true
}

# Clean build artifacts
function Invoke-CleanBuild {
    Write-Step "Cleaning build artifacts..."
    
    # Clean .NET artifacts
    & dotnet clean --configuration $Configuration --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "dotnet clean failed, continuing..."
    }
    
    # Clean Rust artifacts in enclave projects
    if (!$SkipEnclave) {
        $enclaveProjects = Get-ChildItem -Path "src/Tee" -Filter "Cargo.toml" -Recurse
        foreach ($project in $enclaveProjects) {
            Write-Step "Cleaning Rust artifacts in $($project.Directory.Name)..."
            Push-Location $project.Directory
            try {
                & cargo clean 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "Cleaned Rust artifacts"
                }
            } catch {
                Write-Warning "Failed to clean Rust artifacts"
            } finally {
                Pop-Location
            }
        }
    }
    
    # Clean output directory
    if (Test-Path $OutputPath) {
        Remove-Item -Path $OutputPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "Cleaned output directory: $OutputPath"
    }
    
    Write-Success "Build artifacts cleaned"
}

# Build Rust enclave components
function Build-EnclaveComponents {
    if ($SkipEnclave) {
        Write-Info "Skipping enclave build as requested"
        return $true
    }
    
    Write-Step "Building Rust enclave components..."
    
    $enclaveProjects = Get-ChildItem -Path "src/Tee" -Filter "Cargo.toml" -Recurse
    if ($enclaveProjects.Count -eq 0) {
        Write-Warning "No Rust enclave projects found"
        return $true
    }
    
    foreach ($project in $enclaveProjects) {
        Write-Step "Building enclave project: $($project.Directory.Name)"
        Push-Location $project.Directory
        
        try {
            # Set Rust build mode
            $cargoFlags = if ($Configuration -eq "Release") { "--release" } else { "" }
            
            # Build with environment variables
            $env:RUST_LOG = if ($Verbose) { "debug" } else { "info" }
            
            & cargo build $cargoFlags
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Enclave project built successfully"
            } else {
                Write-Error "Failed to build enclave project: $($project.Directory.Name)"
                return $false
            }
        } finally {
            Pop-Location
        }
    }
    
    Write-Success "All enclave components built successfully"
    return $true
}

# Build .NET projects
function Build-DotNetProjects {
    Write-Step "Building .NET projects..."
    
    # Restore packages
    Write-Step "Restoring NuGet packages..."
    & dotnet restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to restore NuGet packages"
        return $false
    }
    Write-Success "NuGet packages restored"
    
    # Build projects
    Write-Step "Building .NET projects..."
    $buildArgs = @(
        "build"
        "--configuration", $Configuration
        "--no-restore"
    )
    
    if ($Verbose) {
        $buildArgs += "--verbosity", "normal"
    } else {
        $buildArgs += "--verbosity", "quiet"
    }
    
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build .NET projects"
        return $false
    }
    
    Write-Success ".NET projects built successfully"
    return $true
}

# Run tests
function Invoke-Tests {
    if ($SkipTests) {
        Write-Info "Skipping tests as requested"
        return $true
    }
    
    Write-Step "Running tests..."
    
    # Set test environment variables
    $env:ASPNETCORE_ENVIRONMENT = "Test"
    $env:NEO_SERVICE_TEST_MODE = "SGX_SIMULATION"
    
    $testArgs = @(
        "test"
        "--configuration", $Configuration
        "--no-build"
        "--logger", "console;verbosity=normal"
        "--logger", "trx;LogFileName=test-results.trx"
        "--results-directory", "$OutputPath/TestResults"
    )
    
    if ($Verbose) {
        $testArgs += "--verbosity", "normal"
    } else {
        $testArgs += "--verbosity", "quiet"
    }
    
    # Create test results directory
    New-Item -Path "$OutputPath/TestResults" -ItemType Directory -Force | Out-Null
    
    & dotnet @testArgs
    if ($LASTEXITCODE -eq 0) {
        Write-Success "All tests passed"
        return $true
    } else {
        Write-Warning "Some tests failed (Exit code: $LASTEXITCODE)"
        return $false
    }
}

# Build Docker images
function Build-DockerImages {
    if (!$Docker) {
        return $true
    }
    
    Write-Step "Building Docker images..."
    
    # Build main application image
    Write-Step "Building main application Docker image..."
    & docker build -t neo-service-layer:$Configuration-$SGX_MODE --build-arg SGX_SDK_VERSION=2.23.100.2 .
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Main Docker image built successfully"
    } else {
        Write-Error "Failed to build main Docker image"
        return $false
    }
    
    # Build enclave-specific images if available
    $enclaveDockerfiles = Get-ChildItem -Path "src/Tee" -Filter "Dockerfile.occlum" -Recurse
    foreach ($dockerfile in $enclaveDockerfiles) {
        $imageName = "neo-service-enclave-$($dockerfile.Directory.Name.ToLower())"
        Write-Step "Building enclave Docker image: $imageName"
        
        Push-Location $dockerfile.Directory
        try {
            & docker build -f "Dockerfile.occlum" -t "$imageName`:$Configuration-$SGX_MODE" .
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Enclave Docker image built: $imageName"
            } else {
                Write-Warning "Failed to build enclave Docker image: $imageName"
            }
        } finally {
            Pop-Location
        }
    }
    
    Write-Success "Docker images built successfully"
    return $true
}

# Package artifacts
function New-BuildArtifacts {
    Write-Step "Creating build artifacts..."
    
    # Create output directory
    New-Item -Path $OutputPath -ItemType Directory -Force | Out-Null
    
    # Publish main API project
    Write-Step "Publishing API project..."
    $publishPath = "$OutputPath/publish"
    & dotnet publish "src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj" `
        --configuration $Configuration `
        --output $publishPath `
        --no-build `
        --verbosity quiet
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "API project published to: $publishPath"
    } else {
        Write-Error "Failed to publish API project"
        return $false
    }
    
    # Copy enclave artifacts
    if (!$SkipEnclave) {
        Write-Step "Copying enclave artifacts..."
        $enclavePath = "$OutputPath/enclave"
        New-Item -Path $enclavePath -ItemType Directory -Force | Out-Null
        
        # Copy Rust build artifacts
        $rustTargets = Get-ChildItem -Path "src/Tee" -Filter "target" -Directory -Recurse
        foreach ($target in $rustTargets) {
            $libFiles = Get-ChildItem -Path $target.FullName -Filter "*.so" -Recurse
            foreach ($lib in $libFiles) {
                Copy-Item -Path $lib.FullName -Destination $enclavePath -Force
                Write-Success "Copied enclave library: $($lib.Name)"
            }
        }
    }
    
    # Copy configuration files
    Write-Step "Copying configuration files..."
    $configFiles = @(
        "appsettings.json",
        "appsettings.Production.json",
        "env.production.template"
    )
    
    foreach ($configFile in $configFiles) {
        if (Test-Path $configFile) {
            Copy-Item -Path $configFile -Destination "$OutputPath/" -Force
            Write-Success "Copied configuration: $configFile"
        }
    }
    
    # Generate build metadata
    Write-Step "Generating build metadata..."
    $buildMetadata = @{
        build_info = @{
            timestamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
            configuration = $Configuration
            sgx_mode = $SGX_MODE
            sgx_debug = $SGX_DEBUG
            platform = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
            architecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
            dotnet_version = (& dotnet --version)
            powershell_version = $PSVersionTable.PSVersion.ToString()
            enclave_support = -not $SkipEnclave
        }
    }
    
    $buildMetadata | ConvertTo-Json -Depth 3 | Out-File -FilePath "$OutputPath/build-metadata.json" -Encoding UTF8
    Write-Success "Build metadata generated"
    
    Write-Success "Build artifacts created in: $OutputPath"
    return $true
}

# Main execution
try {
    Write-Header "Neo Service Layer - Comprehensive Build Script"
    
    $buildStartTime = Get-Date
    
    # Initialize environment
    Initialize-Environment
    
    # Validate dependencies
    if (!(Test-Dependencies)) {
        Write-Error "Dependency validation failed"
        exit 1
    }
    
    # Clean if requested
    if ($Clean) {
        Invoke-CleanBuild
    }
    
    # Build enclave components
    if (!(Build-EnclaveComponents)) {
        Write-Error "Enclave build failed"
        exit 1
    }
    
    # Build .NET projects
    if (!(Build-DotNetProjects)) {
        Write-Error ".NET build failed"
        exit 1
    }
    
    # Run tests
    $testsPassed = Invoke-Tests
    
    # Build Docker images
    if (!(Build-DockerImages)) {
        Write-Error "Docker build failed"
        exit 1
    }
    
    # Package artifacts
    if (!(New-BuildArtifacts)) {
        Write-Error "Artifact packaging failed"
        exit 1
    }
    
    $buildEndTime = Get-Date
    $buildDuration = $buildEndTime - $buildStartTime
    
    # Summary
    Write-Header "Build Completed Successfully"
    Write-Success "Total build time: $($buildDuration.TotalSeconds.ToString('F2')) seconds"
    Write-Success "Configuration: $Configuration"
    Write-Success "SGX Mode: $SGX_MODE"
    Write-Success "Output Path: $OutputPath"
    
    if (!$testsPassed) {
        Write-Warning "Build completed but some tests failed"
        exit 2
    }
    
    Write-Success "🎉 Build completed successfully with all tests passing!"
    
} catch {
    Write-Error "Build failed with error: $($_.Exception.Message)"
    Write-Error "Stack trace: $($_.ScriptStackTrace)"
    exit 1
}

# Help information
if ($args -contains "--help" -or $args -contains "-h") {
    Write-Host @"
Neo Service Layer - Comprehensive Build Script

DESCRIPTION:
    Builds the Neo Service Layer with full SGX and Occlum LibOS support.
    Supports cross-platform builds with extensive validation and testing.

USAGE:
    build-neo-service-layer.ps1 [OPTIONS]

OPTIONS:
    -Configuration <string>  Build configuration (Debug/Release, default: Debug)
    -SGX_MODE <string>       SGX mode (SIM/HW, default: SIM)
    -SGX_DEBUG <string>      SGX debug mode (0/1, default: 1)
    -SkipTests              Skip running tests
    -SkipEnclave            Skip building enclave components
    -Docker                 Build Docker images
    -Clean                  Clean build artifacts before building
    -Verbose                Enable verbose output
    -OutputPath <string>    Output directory (default: ./artifacts)
    -SGXSdkPath <string>    Custom SGX SDK path

EXAMPLES:
    # Basic debug build
    ./build-neo-service-layer.ps1

    # Release build with Docker images
    ./build-neo-service-layer.ps1 -Configuration Release -Docker

    # Clean release build without tests
    ./build-neo-service-layer.ps1 -Configuration Release -Clean -SkipTests

    # Build with custom SGX SDK path
    ./build-neo-service-layer.ps1 -SGXSdkPath "/custom/sgx/path"

    # Verbose build with all features
    ./build-neo-service-layer.ps1 -Configuration Release -Docker -Verbose

ENVIRONMENT:
    The script automatically configures environment variables:
    - SGX_MODE (simulation or hardware mode)
    - SGX_DEBUG (debug symbols)
    - SGX_SDK (SDK path)
    - EnableTEESupport, EnableSGXSupport, EnableOcclumSupport

OUTPUTS:
    - Published applications in {OutputPath}/publish/
    - Enclave libraries in {OutputPath}/enclave/
    - Test results in {OutputPath}/TestResults/
    - Build metadata in {OutputPath}/build-metadata.json
    - Docker images (if -Docker specified)

REQUIREMENTS:
    - .NET 9.0 SDK or later
    - PowerShell 7+ (recommended)
    - Rust toolchain (for enclave components)
    - Intel SGX SDK (optional, enhances functionality)
    - Docker (if building container images)

"@
    exit 0
} 