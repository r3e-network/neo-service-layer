# PowerShell script to update the project to use only Occlum

Write-Host "Updating the project to use only Occlum..."

# Define the project root directory
$projectRoot = "c:\Users\liaoj\git\neo-service-layer"

# Create a backup of the project
$backupDir = Join-Path -Path $projectRoot -ChildPath "backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Write-Host "Creating backup of the project in: $backupDir"
New-Item -Path $backupDir -ItemType Directory -Force | Out-Null
Copy-Item -Path "$projectRoot\*" -Destination $backupDir -Recurse -Force

# Run the script to remove OpenEnclave dependencies
Write-Host "Running script to remove OpenEnclave dependencies..."
$removeOpenEnclaveScript = Join-Path -Path $projectRoot -ChildPath "src\NeoServiceLayer.Tee.Enclave\Enclave\remove_openenclave.ps1"
& $removeOpenEnclaveScript

# Update the project to use Occlum
Write-Host "Updating the project to use Occlum..."

# Create a new directory for the Occlum integration
$occlumDir = Join-Path -Path $projectRoot -ChildPath "src\NeoServiceLayer.Tee.Enclave\Occlum"
Write-Host "Creating directory for Occlum integration: $occlumDir"
New-Item -Path $occlumDir -ItemType Directory -Force | Out-Null

# Copy the Occlum integration files to the new directory
Write-Host "Copying Occlum integration files..."
$occlumFiles = @(
    "OcclumIntegration.h",
    "OcclumIntegration.cpp",
    "OcclumIntegration_Utils.cpp",
    "OcclumIntegration_Sealing.cpp",
    "OcclumEnclave.h",
    "OcclumEnclave.cpp",
    "OcclumEnclave_JS.cpp",
    "OcclumEnclave_Utils.cpp",
    "OcclumEnclave_Random.cpp",
    "EnclaveMessageTypes.h",
    "CMakeLists.txt",
    "Dockerfile",
    "enclave_main.js",
    "libneoserviceenclave.js",
    "README.md"
)

foreach ($file in $occlumFiles) {
    $sourcePath = Join-Path -Path $projectRoot -ChildPath "src\NeoServiceLayer.Tee.Enclave\Enclave\$file"
    $destinationPath = Join-Path -Path $occlumDir -ChildPath $file
    if (Test-Path $sourcePath) {
        Write-Host "Copying file: $file"
        Copy-Item -Path $sourcePath -Destination $destinationPath -Force
    } else {
        Write-Host "File not found: $file"
    }
}

# Create a new CMakeLists.txt file for the project
$cmakeListsPath = Join-Path -Path $projectRoot -ChildPath "CMakeLists.txt"
Write-Host "Creating new CMakeLists.txt file: $cmakeListsPath"
$cmakeListsContent = @"
cmake_minimum_required(VERSION 3.10)
project(NeoServiceLayer)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Add subdirectories
add_subdirectory(src/NeoServiceLayer.Tee.Enclave/Occlum)
"@
Set-Content -Path $cmakeListsPath -Value $cmakeListsContent -Force

# Create a new Dockerfile for the project
$dockerfilePath = Join-Path -Path $projectRoot -ChildPath "Dockerfile"
Write-Host "Creating new Dockerfile: $dockerfilePath"
$dockerfileContent = @"
# Use the Occlum base image
FROM occlum/occlum:latest

# Install dependencies
RUN apt-get update && apt-get install -y \
    build-essential \
    cmake \
    libmbedtls-dev \
    nlohmann-json3-dev \
    nodejs \
    npm \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy the source code
COPY . /app

# Build the project
RUN mkdir -p build && \
    cd build && \
    cmake .. && \
    make

# Set the entrypoint
ENTRYPOINT ["/app/build/bin/run_enclave.sh"]
"@
Set-Content -Path $dockerfilePath -Value $dockerfileContent -Force

# Create a new .gitignore file for the project
$gitignorePath = Join-Path -Path $projectRoot -ChildPath ".gitignore"
Write-Host "Creating new .gitignore file: $gitignorePath"
$gitignoreContent = @"
# Build directories
build/
bin/
obj/
out/
Debug/
Release/
x64/
x86/

# Backup directories
backup_*/

# IDE files
.vs/
.vscode/
*.user
*.suo
*.userprefs
*.sln.docstates

# Dependency directories
packages/
node_modules/

# Occlum files
occlum_instance/

# Compiled files
*.o
*.obj
*.so
*.dll
*.exe
*.pdb
*.ilk
*.exp
*.lib
*.a
*.dylib

# Logs
*.log
logs/

# Temporary files
*.tmp
*.temp
*.swp
*~

# OS files
.DS_Store
Thumbs.db
"@
Set-Content -Path $gitignorePath -Value $gitignoreContent -Force

# Create a new README.md file for the project
$readmePath = Join-Path -Path $projectRoot -ChildPath "README.md"
Write-Host "Creating new README.md file: $readmePath"
$readmeContent = @"
# Neo Service Layer

This project implements the Neo Service Layer using Occlum LibOS for secure execution of JavaScript code in SGX enclaves.

## Overview

The Neo Service Layer provides a secure environment for executing JavaScript code within an SGX enclave using Occlum LibOS. It includes the following features:

- Secure JavaScript execution using Node.js
- User secret management
- Secure random number generation
- Remote attestation
- Persistent storage
- Gas accounting
- Compliance verification

## Building

To build the project, you need to have Occlum installed. You can build the project using CMake:

```bash
mkdir build
cd build
cmake ..
make
```

## Running

To run the project, you can use the following command:

```bash
cd build
make run_enclave
```

## Docker

You can also build and run the project in a Docker container:

```bash
docker build -t neoserviceenclave:latest .
docker run --rm -it neoserviceenclave:latest
```

## Documentation

For more information, see the documentation in the `src/NeoServiceLayer.Tee.Enclave/Occlum/README.md` file.
"@
Set-Content -Path $readmePath -Value $readmeContent -Force

Write-Host "Project updated to use only Occlum!"
Write-Host "Backup of the original project is available in: $backupDir"
