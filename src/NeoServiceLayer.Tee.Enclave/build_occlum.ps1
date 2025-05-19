#!/usr/bin/env pwsh
# Script to build the Occlum enclave for Neo Service Layer

# Set error action preference to stop on any error
$ErrorActionPreference = "Stop"

# Configuration variables
$OCCLUM_VERSION = "0.29.5"
$NODEJS_VERSION = "16.15.0"
$OCCLUM_INSTANCE = "/occlum_instance"
$OCCLUM_IMAGE_DIR = "$OCCLUM_INSTANCE/image"

# Create log directory
$LOG_DIR = "build_logs"
if (-not (Test-Path -Path $LOG_DIR)) {
    New-Item -ItemType Directory -Path $LOG_DIR | Out-Null
}

# Log file
$LOG_FILE = "$LOG_DIR/build_occlum_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

# Helper function to log messages
function LogMessage {
    param (
        [Parameter(Mandatory=$true)]
        [string]$Message,
        
        [Parameter(Mandatory=$false)]
        [string]$Color = "White"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $formattedMessage = "[$timestamp] $Message"
    
    # Output to console with color
    Write-Host $formattedMessage -ForegroundColor $Color
    
    # Output to log file
    Add-Content -Path $LOG_FILE -Value $formattedMessage
}

# Helper function to run commands and log output
function RunCommand {
    param (
        [Parameter(Mandatory=$true)]
        [string]$Command,
        
        [Parameter(Mandatory=$false)]
        [string]$Description = "",
        
        [Parameter(Mandatory=$false)]
        [switch]$ContinueOnError
    )
    
    if ($Description) {
        LogMessage "RUNNING: $Description" "Cyan"
    }
    
    LogMessage "COMMAND: $Command" "Gray"
    
    try {
        $output = Invoke-Expression -Command $Command 2>&1
        $exitCode = $LASTEXITCODE
        
        # Log the output
        if ($output) {
            Add-Content -Path $LOG_FILE -Value $output
        }
        
        if ($exitCode -ne 0) {
            LogMessage "ERROR: Command failed with exit code $exitCode" "Red"
            LogMessage $output "Yellow"
            
            if (-not $ContinueOnError) {
                throw "Command execution failed: $Command"
            } else {
                LogMessage "Continuing execution despite error (ContinueOnError flag set)" "Yellow"
            }
        }
        
        return $output
    }
    catch {
        LogMessage "EXCEPTION: $_" "Red"
        
        if (-not $ContinueOnError) {
            throw $_
        } else {
            LogMessage "Continuing execution despite error (ContinueOnError flag set)" "Yellow"
            return $null
        }
    }
}

# Verify dependencies
function VerifyDependencies {
    LogMessage "Verifying dependencies..." "Green"
    
    # Create a flag to track overall success
    $success = $true
    
    # Check for Occlum
    try {
        $occlumVersion = RunCommand "occlum --version" "Checking Occlum version" -ContinueOnError
        if ($occlumVersion) {
            LogMessage "Occlum version: $occlumVersion" "Green"
        } else {
            LogMessage "Occlum not found or not working properly" "Red"
            $success = $false
        }
    } catch {
        LogMessage "Failed to check Occlum: $_" "Red"
        $success = $false
    }
    
    # Check for Node.js
    try {
        $nodeVersion = RunCommand "node --version" "Checking Node.js version" -ContinueOnError
        if ($nodeVersion) {
            LogMessage "Node.js version: $nodeVersion" "Green"
            
            # Verify Node.js version meets minimum requirements
            $versionString = $nodeVersion.ToString().TrimStart('v')
            $versionParts = $versionString.Split('.')
            $major = [int]$versionParts[0]
            
            if ($major -lt 16) {
                LogMessage "WARNING: Node.js version is older than recommended (16.x). Current: $versionString" "Yellow"
            }
        } else {
            LogMessage "Node.js not found or not working properly" "Red"
            $success = $false
        }
    } catch {
        LogMessage "Failed to check Node.js: $_" "Red"
        $success = $false
    }
    
    # Check for .NET SDK
    try {
        $dotnetVersion = RunCommand "dotnet --version" "Checking .NET SDK version" -ContinueOnError
        if ($dotnetVersion) {
            LogMessage ".NET SDK version: $dotnetVersion" "Green"
        } else {
            LogMessage ".NET SDK not found or not working properly" "Red"
            $success = $false
        }
    } catch {
        LogMessage "Failed to check .NET SDK: $_" "Red"
        $success = $false
    }
    
    # Check for CMake
    try {
        $cmakeVersion = RunCommand "cmake --version" "Checking CMake version" -ContinueOnError
        if ($cmakeVersion) {
            $versionLine = $cmakeVersion -split "`n" | Select-Object -First 1
            LogMessage "CMake version: $versionLine" "Green"
        } else {
            LogMessage "CMake not found or not working properly" "Red"
            $success = $false
        }
    } catch {
        LogMessage "Failed to check CMake: $_" "Red"
        $success = $false
    }
    
    # Check if required directories exist
    try {
        $srcDir = "./Enclave"
        if (-not (Test-Path -Path $srcDir)) {
            LogMessage "Enclave source directory not found: $srcDir" "Red"
            $success = $false
        } else {
            LogMessage "Enclave source directory found: $srcDir" "Green"
        }
    } catch {
        LogMessage "Failed to check directories: $_" "Red"
        $success = $false
    }
    
    return $success
}

# Verify build artifacts
function VerifyBuildArtifacts {
    LogMessage "Verifying build artifacts..." "Green"
    
    # Create a flag to track overall success
    $success = $true
    
    # Check if .NET assembly exists
    $dotnetAssembly = "./bin/Release/net6.0/NeoServiceLayer.Tee.Enclave.dll"
    if (-not (Test-Path -Path $dotnetAssembly)) {
        LogMessage ".NET assembly not found: $dotnetAssembly" "Red"
        $success = $false
    } else {
        $assemblyInfo = Get-Item $dotnetAssembly
        LogMessage ".NET assembly found: $dotnetAssembly (Size: $($assemblyInfo.Length) bytes)" "Green"
    }
    
    # Check if native library exists
    $nativeLib = "./build/libNeoServiceLayerEnclave.so"
    if (-not (Test-Path -Path $nativeLib)) {
        LogMessage "Native library not found: $nativeLib" "Red"
        $success = $false
    } else {
        $libInfo = Get-Item $nativeLib
        LogMessage "Native library found: $nativeLib (Size: $($libInfo.Length) bytes)" "Green"
    }
    
    return $success
}

# Main build logic
try {
    LogMessage "Starting Neo Service Layer Occlum build process..." "Green"
    
    # Verify dependencies
    $dependenciesOk = VerifyDependencies
    if (-not $dependenciesOk) {
        LogMessage "WARNING: Some dependencies are missing or not configured correctly" "Yellow"
        $confirmation = Read-Host "Do you want to continue anyway? (y/n)"
        if ($confirmation -ne "y") {
            throw "Build aborted due to missing dependencies"
        }
    }
    
    # Build the .NET project
    LogMessage "Building .NET project..." "Cyan"
    RunCommand "dotnet build -c Release"
    
    # Build the native enclave code
    LogMessage "Building native enclave code..." "Cyan"
    
    # Create build directory if it doesn't exist
    if (-not (Test-Path -Path "build")) {
        New-Item -ItemType Directory -Path "build" | Out-Null
    }
    
    RunCommand "cmake -B build -S Enclave"
    RunCommand "cmake --build build --config Release"
    
    # Verify build artifacts
    $artifactsOk = VerifyBuildArtifacts
    if (-not $artifactsOk) {
        throw "Build artifacts verification failed"
    }
    
    # Check if Occlum instance exists and destroy it if it does
    if (Test-Path -Path $OCCLUM_INSTANCE) {
        LogMessage "Destroying existing Occlum instance..." "Yellow"
        RunCommand "occlum destroy" "Destroying existing Occlum instance"
    }
    
    # Create Occlum instance
    LogMessage "Creating new Occlum instance..." "Green"
    RunCommand "mkdir -p $OCCLUM_INSTANCE" "Creating Occlum instance directory"
    Set-Location $OCCLUM_INSTANCE
    RunCommand "occlum init" "Initializing Occlum instance"
    
    # Create image directories
    $directories = @(
        "$OCCLUM_IMAGE_DIR/bin",
        "$OCCLUM_IMAGE_DIR/lib",
        "$OCCLUM_IMAGE_DIR/lib64",
        "$OCCLUM_IMAGE_DIR/etc",
        "$OCCLUM_IMAGE_DIR/node_modules",
        "$OCCLUM_IMAGE_DIR/tmp",
        "$OCCLUM_IMAGE_DIR/app",
        "$OCCLUM_IMAGE_DIR/data"
    )
    
    foreach ($dir in $directories) {
        if (-not (Test-Path -Path $dir)) {
            RunCommand "mkdir -p $dir" "Creating directory: $dir"
        }
    }
    
    # Copy necessary files to the image
    LogMessage "Copying files to Occlum image..." "Cyan"
    
    # Check if required source files exist before copying
    $sourceFiles = @{
        "Node.js executable" = $(which node)
        "Node.js libraries" = "/usr/lib/x86_64-linux-gnu/libnode*"
        "Native library" = "../build/libNeoServiceLayerEnclave.so"
        ".NET assemblies" = "../bin/Release/net6.0"
        "Hosts file" = "/etc/hosts"
        "DNS config" = "/etc/resolv.conf"
    }
    
    foreach ($item in $sourceFiles.GetEnumerator()) {
        $name = $item.Key
        $path = $item.Value
        
        $exists = RunCommand "[ -e '$path' ] && echo 'exists' || echo 'not exists'" "Checking if $name exists"
        
        if ($exists -eq "exists") {
            LogMessage "$name exists at: $path" "Green"
        } else {
            LogMessage "WARNING: $name not found at: $path" "Yellow"
            
            # For critical components, fail the build
            if ($name -eq "Native library" -or $name -eq ".NET assemblies") {
                throw "$name not found at: $path"
            }
        }
    }
    
    # Copy Node.js and its dependencies
    RunCommand "cp $(which node) $OCCLUM_IMAGE_DIR/bin/" "Copying Node.js executable"
    RunCommand "cp /usr/lib/x86_64-linux-gnu/libnode* $OCCLUM_IMAGE_DIR/lib/" "Copying Node.js libraries"
    
    # Copy native library
    $nativeLibPath = "../build/libNeoServiceLayerEnclave.so"
    RunCommand "cp $nativeLibPath $OCCLUM_IMAGE_DIR/lib/" "Copying native enclave library"
    
    # Copy .NET assemblies
    $dotnetAssemblies = "../bin/Release/net6.0"
    RunCommand "cp -r $dotnetAssemblies/* $OCCLUM_IMAGE_DIR/app/" "Copying .NET assemblies"
    
    # Copy system files
    RunCommand "cp /etc/hosts $OCCLUM_IMAGE_DIR/etc/" "Copying hosts file"
    RunCommand "cp /etc/resolv.conf $OCCLUM_IMAGE_DIR/etc/" "Copying DNS resolver config"
    
    # Create a default Occlum configuration
    $occlumConfig = @"
{
  "resource_limits": {
    "user_space_size": "1GB",
    "kernel_space_heap_size": "64MB",
    "kernel_space_stack_size": "1MB",
    "max_num_of_threads": 64
  },
  "process": {
    "default_stack_size": "4MB",
    "default_heap_size": "32MB",
    "default_mmap_size": "500MB"
  },
  "entry_points": [
    "/bin/node",
    "/bin/run_enclave.sh",
    "/usr/bin/occlum_exec_client"
  ],
  "env": {
    "default": [
      "OCCLUM=yes",
      "PATH=/bin:/usr/bin",
      "LD_LIBRARY_PATH=/lib:/usr/lib:/lib64:/usr/lib64",
      "TERM=xterm"
    ],
    "untrusted": [
      "EXAMPLE_UNTRUSTED_ENV=untrusted_value"
    ]
  },
  "mount": [
    {
      "target": "/",
      "type": "sefs",
      "source": "./build/mount/__ROOT",
      "options": {
        "integrity_only": true
      }
    },
    {
      "target": "/data",
      "type": "sefs",
      "source": "./build/mount/data",
      "options": {
        "integrity_only": false
      }
    },
    {
      "target": "/tmp",
      "type": "sefs",
      "source": "./build/mount/tmp",
      "options": {
        "integrity_only": false
      }
    }
  ]
}
"@
    
    Set-Content -Path "$OCCLUM_INSTANCE/Occlum.json" -Value $occlumConfig
    LogMessage "Created Occlum configuration file" "Green"
    
    # Generate JSON file with test execution parameters
    $testParams = @"
{
  "function_id": "test_function",
  "user_id": "test_user",
  "code": "function main(input) { return { result: input.value * 2 }; }",
  "input": "{ \"value\": 42 }",
  "secrets": "{ \"API_KEY\": \"test_key\" }"
}
"@
    
    Set-Content -Path "$OCCLUM_IMAGE_DIR/app/test_params.json" -Value $testParams
    LogMessage "Created test parameters file" "Green"
    
    # Create a startup script for the enclave
    $startupScript = @"
#!/bin/bash
cd /app
node /app/NeoServiceLayer.Tee.Enclave.js "\$@"
"@
    
    Set-Content -Path "$OCCLUM_IMAGE_DIR/bin/run_enclave.sh" -Value $startupScript -NoNewline
    RunCommand "chmod +x $OCCLUM_IMAGE_DIR/bin/run_enclave.sh" "Making startup script executable"
    
    # Build the Occlum instance
    LogMessage "Building Occlum instance..." "Cyan"
    RunCommand "occlum build" "Building Occlum instance"
    
    # Create a convenience script for running the enclave
    $runScript = @"
#!/bin/bash
cd $OCCLUM_INSTANCE
occlum run /bin/run_enclave.sh "\$@"
"@
    
    Set-Content -Path "../run_enclave.sh" -Value $runScript -NoNewline
    RunCommand "chmod +x ../run_enclave.sh" "Making enclave run script executable"
    
    # Return to the original directory
    Set-Location ".."
    
    # Create a simulation mode script
    $simScript = @"
#!/bin/bash
export OCCLUM_SIMULATION=1
./run_enclave.sh "\$@"
"@
    
    Set-Content -Path "run_enclave_simulation.sh" -Value $simScript -NoNewline
    RunCommand "chmod +x run_enclave_simulation.sh" "Making simulation mode script executable"
    
    # Provide a simple test script
    $testScript = @"
#!/bin/bash
echo "Running Neo Service Layer Occlum enclave test..."
./run_enclave.sh --test --input=./test_params.json
"@
    
    Set-Content -Path "test_enclave.sh" -Value $testScript -NoNewline
    RunCommand "chmod +x test_enclave.sh" "Making test script executable"
    
    # Verify the final build
    LogMessage "Verifying final build..." "Cyan"
    
    $occlumInstanceExists = Test-Path -Path $OCCLUM_INSTANCE
    $runScriptExists = Test-Path -Path "./run_enclave.sh"
    $simScriptExists = Test-Path -Path "./run_enclave_simulation.sh"
    
    if ($occlumInstanceExists -and $runScriptExists -and $simScriptExists) {
        LogMessage "Neo Service Layer Occlum build completed successfully!" "Green"
        LogMessage "To run the enclave, execute: ./run_enclave.sh" "Cyan"
        LogMessage "To run in simulation mode, execute: ./run_enclave_simulation.sh" "Cyan"
        LogMessage "To run a quick test, execute: ./test_enclave.sh" "Cyan"
    } else {
        throw "Build verification failed. Some required files are missing."
    }
}
catch {
    LogMessage "ERROR: Build failed: $_" "Red"
    
    # Try to return to the original directory
    try {
        Set-Location ".."
    } catch {
        # Ignore errors when trying to change directory
    }
    
    exit 1
}
