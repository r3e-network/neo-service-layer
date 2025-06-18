# Running SGX SDK Tests in Simulation Mode

This guide explains how to run the Neo Service Layer tests using the real Intel SGX SDK in simulation mode.

## Overview

The tests in `RealSGXSimulationTests.cs` use the actual Intel SGX SDK through the `ProductionSGXEnclaveWrapper` and `OcclumEnclaveWrapper` classes. This provides more realistic testing compared to the pure simulation wrapper.

## Prerequisites

### Linux/Ubuntu

1. **Intel SGX SDK** (required)
   ```bash
   wget https://download.01.org/intel-sgx/latest/linux-latest/distro/ubuntu20.04-server/sgx_linux_x64_sdk_2.22.100.3.bin
   chmod +x sgx_linux_x64_sdk_2.22.100.3.bin
   ./sgx_linux_x64_sdk_2.22.100.3.bin --prefix=/opt/intel
   ```

2. **Occlum LibOS** (required for production wrapper)
   ```bash
   wget https://github.com/occlum/occlum/releases/download/0.29.7/occlum_0.29.7_amd64.deb
   sudo apt install ./occlum_0.29.7_amd64.deb
   ```

3. **.NET 9.0 SDK**
   ```bash
   wget https://dot.net/v1/dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --channel 9.0
   ```

### Windows

1. **Intel SGX SDK for Windows**
   - Download from [Intel SGX Software](https://www.intel.com/content/www/us/en/developer/tools/software-guard-extensions/sdk.html)
   - Install to default location (C:\Program Files\Intel\SGXWindows\)

2. **Visual Studio 2019/2022** (for native compilation)
   - Include C++ development tools

3. **.NET 9.0 SDK**
   - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0)

## Setup Instructions

### Option 1: Automated Setup (Recommended)

#### Linux/Ubuntu
```bash
cd tests/Tee/NeoServiceLayer.Tee.Enclave.Tests
chmod +x setup-sgx-sim-tests.sh
./setup-sgx-sim-tests.sh
```

#### Windows (PowerShell)
```powershell
cd tests\Tee\NeoServiceLayer.Tee.Enclave.Tests
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process
.\setup-sgx-sim-tests.ps1
```

### Option 2: Manual Setup

#### Linux/Ubuntu
```bash
# Set environment variables
export SGX_MODE=SIM
export SGX_DEBUG=1
source /opt/intel/sgxsdk/environment
export LD_LIBRARY_PATH=/opt/occlum/lib:$LD_LIBRARY_PATH

# Run tests
cd tests/Tee/NeoServiceLayer.Tee.Enclave.Tests
dotnet test --filter "Category=SGXIntegration"
```

#### Windows
```powershell
# Set environment variables
$env:SGX_MODE = "SIM"
$env:SGX_DEBUG = "1"
$env:SGX_SDK = "C:\Program Files\Intel\SGXWindows\"
$env:PATH = "$env:SGX_SDK\bin\x64\Release;$env:PATH"

# Run tests
cd tests\Tee\NeoServiceLayer.Tee.Enclave.Tests
dotnet test --filter "Category=SGXIntegration"
```

### Option 3: Docker (Cross-platform)

```bash
cd tests/Tee/NeoServiceLayer.Tee.Enclave.Tests
docker-compose -f docker-compose.sgx-test.yml up --build
```

## Running Specific Tests

### Run all SGX SDK tests
```bash
dotnet test --filter "Category=SGXIntegration"
```

### Run a specific test
```bash
dotnet test --filter "FullyQualifiedName~RealSGX_Initialize_ShouldSucceed"
```

### Run with detailed output
```bash
dotnet test --filter "Category=SGXIntegration" --logger "console;verbosity=detailed"
```

### Run with code coverage
```bash
dotnet test --filter "Category=SGXIntegration" --collect:"XPlat Code Coverage"
```

## Test Categories

The `RealSGXSimulationTests` class includes tests for:

1. **Initialization** - Verifies SGX SDK initialization
2. **Cryptographic Operations** - Tests encryption, decryption, signing, verification
3. **Random Generation** - Uses SGX hardware RNG (simulated)
4. **Secure Storage** - Tests enclave-protected storage
5. **JavaScript Execution** - Runs code within enclave
6. **Key Generation** - Creates keys using enclave key derivation
7. **Attestation Reports** - Generates SGX attestation (simulation mode)
8. **Data Sealing** - Uses platform-specific sealing keys
9. **Oracle Data** - Fetches external data with enclave protection
10. **Abstract Accounts** - Manages blockchain accounts in enclave

## Troubleshooting

### SGX SDK Not Found

**Linux:**
```bash
# Check if SGX SDK is installed
ls /opt/intel/sgxsdk/

# If not found, verify installation path
find / -name "sgx_edger8r" 2>/dev/null
```

**Windows:**
```powershell
# Check if SGX SDK is installed
Test-Path "C:\Program Files\Intel\SGXWindows\"

# Check environment variables
echo $env:SGX_SDK
```

### Occlum Not Found (Linux only)

```bash
# Check if Occlum is installed
which occlum

# Verify Occlum libraries
ls /opt/occlum/lib/
```

### Tests Skipped

If tests are being skipped, check:

1. Environment variables are set correctly
2. SGX SDK is properly installed
3. Library paths are configured

```bash
# Verify environment
echo "SGX_MODE=$SGX_MODE"
echo "LD_LIBRARY_PATH=$LD_LIBRARY_PATH"
```

### Native Library Loading Errors

**Linux:**
```bash
# Check library dependencies
ldd /opt/occlum/lib/libocclum-pal.so.0

# Install missing dependencies
sudo apt-get install libssl-dev libcurl4-openssl-dev libprotobuf-dev
```

**Windows:**
```powershell
# Check Visual C++ Redistributables
# Install from: https://aka.ms/vs/17/release/vc_redist.x64.exe
```

## Expected Output

When running successfully, you should see:

```
Starting test execution, please wait...
A total of 10 test files matched the specified pattern.

[Information] ProductionSGXEnclaveWrapper: Initializing production SGX enclave...
[Information] OcclumEnclaveWrapper: Initializing Occlum LibOS enclave
[Information] OcclumEnclaveWrapper: Occlum LibOS enclave initialized successfully

✅ Real SGX SDK initialization successful (simulation mode)
✅ Real SGX cryptographic operations successful
✅ Real SGX random generation successful
✅ Real SGX secure storage successful
✅ Real SGX JavaScript execution successful
✅ Real SGX key generation successful
✅ Real SGX attestation report generated
✅ Real SGX data sealing successful
✅ Real SGX oracle data fetch successful
✅ Real SGX abstract account management successful

Test Run Successful.
Total tests: 10
     Passed: 10
 Total time: X.XXX Seconds
```

## CI/CD Integration

### GitHub Actions

```yaml
- name: Setup SGX SDK
  run: |
    wget https://download.01.org/intel-sgx/latest/linux-latest/distro/ubuntu20.04-server/sgx_linux_x64_sdk.bin
    chmod +x sgx_linux_x64_sdk.bin
    echo 'yes' | ./sgx_linux_x64_sdk.bin --prefix=$HOME/sgxsdk
    
- name: Run SGX Tests
  env:
    SGX_MODE: SIM
  run: |
    source $HOME/sgxsdk/environment
    dotnet test --filter "Category=SGXIntegration"
```

### Azure DevOps

```yaml
- script: |
    chmod +x tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/setup-sgx-sim-tests.sh
    ./tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/setup-sgx-sim-tests.sh
  displayName: 'Setup SGX Environment'

- task: DotNetCoreCLI@2
  displayName: 'Run SGX SDK Tests'
  inputs:
    command: 'test'
    projects: '**/NeoServiceLayer.Tee.Enclave.Tests.csproj'
    arguments: '--filter Category=SGXIntegration --logger trx'
```

## Performance Considerations

When running with real SGX SDK (even in simulation mode):

- **Initialization**: ~100-500ms (vs <10ms for pure simulation)
- **Cryptographic Operations**: ~50-200ms (vs <5ms for pure simulation)
- **Attestation Generation**: ~200-1000ms (vs <10ms for pure simulation)

These are expected due to the additional overhead of the SGX SDK simulation layer.

## Security Notes

1. **Simulation Mode**: Tests run in SGX simulation mode, which doesn't provide hardware security guarantees
2. **Production Deployment**: For production, use SGX_MODE=HW with real SGX hardware
3. **Key Material**: Test keys are ephemeral and should not be used in production
4. **Attestation**: Simulation mode attestation is for testing only and cannot be verified remotely

## Additional Resources

- [Intel SGX SDK Documentation](https://software.intel.com/content/www/us/en/develop/topics/software-guard-extensions/sdk.html)
- [Occlum Documentation](https://occlum.readthedocs.io/)
- [SGX Developer Guide](https://download.01.org/intel-sgx/latest/linux-latest/docs/)
- [Neo Service Layer Documentation](../../../README.md)