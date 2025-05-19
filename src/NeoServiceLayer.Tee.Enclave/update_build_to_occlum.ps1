# PowerShell script to update the build process to use Occlum
# This script should be run after the migration to Occlum LibOS is complete

# Stop on first error
$ErrorActionPreference = "Stop"

Write-Host "Updating build process to use Occlum..." -ForegroundColor Green

# Define the project root directory
$projectRoot = $PSScriptRoot

# Create a backup of the build_enclave.ps1 script
$buildEnclaveScript = Join-Path -Path $projectRoot -ChildPath "build_enclave.ps1"
if (Test-Path $buildEnclaveScript) {
    $backupScript = Join-Path -Path $projectRoot -ChildPath "build_enclave.oe.ps1"
    Write-Host "Creating backup of build_enclave.ps1 as build_enclave.oe.ps1" -ForegroundColor Yellow
    Copy-Item -Path $buildEnclaveScript -Destination $backupScript -Force
}

# Replace build_enclave.ps1 with build_occlum.ps1
$buildOcclumScript = Join-Path -Path $projectRoot -ChildPath "build_occlum.ps1"
if (Test-Path $buildOcclumScript) {
    Write-Host "Replacing build_enclave.ps1 with build_occlum.ps1" -ForegroundColor Green
    Copy-Item -Path $buildOcclumScript -Destination $buildEnclaveScript -Force
}

# Create a CMakeLists.txt file for the project
$cmakeListsPath = Join-Path -Path $projectRoot -ChildPath "CMakeLists.txt"
Write-Host "Creating CMakeLists.txt file for the project" -ForegroundColor Green
$cmakeListsContent = @"
# CMakeLists.txt for NeoServiceLayer.Tee.Enclave
cmake_minimum_required(VERSION 3.10)
project(NeoServiceLayerEnclave)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Find required packages
find_package(nlohmann_json REQUIRED)
find_package(mbedtls REQUIRED)

# Set include directories
include_directories(
    ${CMAKE_CURRENT_SOURCE_DIR}/Enclave
    ${CMAKE_CURRENT_SOURCE_DIR}/Enclave/QuickJs
)

# Set source files
set(ENCLAVE_SOURCES
    # Core files
    Enclave/NeoServiceLayerEnclave.cpp
    Enclave/NeoServiceLayerEnclave2.cpp
    # Integration files
    Enclave/OcclumIntegration.Core.cpp
    Enclave/OcclumIntegration.Crypto.cpp
    Enclave/OcclumIntegration.JavaScriptEngine.cpp
    Enclave/OcclumIntegration.Sealing.cpp
    Enclave/OcclumIntegration.Utils.cpp
    Enclave/OcclumIntegration.Attestation.cpp
    # Manager files
    Enclave/StorageManager.cpp
    Enclave/KeyManager.Core.cpp
    Enclave/KeyManager.Crypto.cpp
    Enclave/KeyManager.Storage.cpp
    Enclave/SecretManager.Core.cpp
    Enclave/SecretManager.Storage.cpp
    Enclave/SecretManager.Crypto.cpp
    Enclave/SecretManager.Access.cpp
    Enclave/SecretManager.Access2.cpp
    Enclave/GasAccounting.cpp
    Enclave/GasAccountingManager.cpp
    # JavaScript engine
    Enclave/JavaScriptEngine.cpp
    Enclave/JavaScriptManager.cpp
    Enclave/QuickJs/QuickJsEngineAdapter.cpp
    # Event trigger
    Enclave/EventTrigger.Core.cpp
    Enclave/EventTrigger.Registration.cpp
    Enclave/EventTrigger.Processing.cpp
    Enclave/EventTrigger.Processing2.cpp
    Enclave/EventTrigger.Storage.cpp
    # Backup manager
    Enclave/BackupManager.Core.cpp
    Enclave/BackupManager.Backup.cpp
    Enclave/BackupManager.Restore.cpp
    Enclave/BackupManager.Scheduling.cpp
    Enclave/BackupManager.Utils.cpp
    # Utility files
    Enclave/EnclaveUtils.cpp
)

# Set compile definitions
add_compile_definitions(OCCLUM)
if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    add_compile_definitions(_DEBUG)
endif()

# Create shared library
add_library(enclave SHARED ${ENCLAVE_SOURCES})

# Link libraries
target_link_libraries(enclave
    mbedtls
    mbedcrypto
    mbedx509
)

# Set output directory
set_target_properties(enclave PROPERTIES
    LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib
)

# Install targets
install(TARGETS enclave
    LIBRARY DESTINATION lib
)
"@
Set-Content -Path $cmakeListsPath -Value $cmakeListsContent -Force

# Create a Dockerfile for the project
$dockerfilePath = Join-Path -Path $projectRoot -ChildPath "Dockerfile"
Write-Host "Creating Dockerfile for the project" -ForegroundColor Green
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

# Create Occlum instance
RUN cd build && \
    occlum new occlum_instance && \
    cd occlum_instance && \
    cp ../lib/libenclave.so image/lib/ && \
    cp /usr/bin/node image/bin/ && \
    mkdir -p image/app && \
    echo 'const { NeoServiceEnclave } = require("./libenclave"); NeoServiceEnclave.initialize(); NeoServiceEnclave.startServer();' > image/app/enclave_main.js && \
    occlum build

# Set the entrypoint
ENTRYPOINT ["occlum", "run", "/bin/node", "/app/enclave_main.js"]
"@
Set-Content -Path $dockerfilePath -Value $dockerfileContent -Force

# Create a run_enclave.ps1 script
$runEnclaveScript = Join-Path -Path $projectRoot -ChildPath "run_enclave.ps1"
Write-Host "Creating run_enclave.ps1 script" -ForegroundColor Green
$runEnclaveContent = @"
# PowerShell script to run the enclave
# This script should be run after building the enclave with build_enclave.ps1

# Stop on first error
$ErrorActionPreference = "Stop"

Write-Host "Running the enclave..." -ForegroundColor Green

# Define the project root directory
$projectRoot = $PSScriptRoot

# Check if the enclave has been built
$occlumInstanceDir = Join-Path -Path $projectRoot -ChildPath "build\occlum_instance"
if (-not (Test-Path $occlumInstanceDir)) {
    Write-Host "Enclave has not been built. Please run build_enclave.ps1 first." -ForegroundColor Red
    exit 1
}

# Check if we're in simulation mode
$simulationMode = $env:OCCLUM_SIMULATION -eq "1"
if ($simulationMode) {
    Write-Host "Running in simulation mode." -ForegroundColor Yellow
    $env:OCCLUM_SIMULATION_MODE = "1"
}

# Run the enclave
Write-Host "Starting the enclave..." -ForegroundColor Cyan
Push-Location $occlumInstanceDir
& occlum run /bin/node /app/enclave_main.js
$exitCode = $LASTEXITCODE
Pop-Location

if ($exitCode -ne 0) {
    Write-Host "Enclave exited with error code: $exitCode" -ForegroundColor Red
    exit $exitCode
}

Write-Host "Enclave exited successfully." -ForegroundColor Green
"@
Set-Content -Path $runEnclaveScript -Value $runEnclaveContent -Force

Write-Host "Build process has been updated to use Occlum." -ForegroundColor Green
Write-Host "To build the enclave, run: .\build_enclave.ps1" -ForegroundColor Cyan
Write-Host "To run the enclave, run: .\run_enclave.ps1" -ForegroundColor Cyan
