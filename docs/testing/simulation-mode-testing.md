# Simulation Mode Testing

This document provides instructions for testing the Neo Confidential Serverless Layer in simulation mode.

## Overview

Simulation mode allows you to test the enclave code without requiring actual SGX hardware. This is useful for development and CI/CD environments where SGX hardware may not be available.

## Prerequisites

- .NET 9.0 SDK
- Docker (optional, for containerized testing)
- Open Enclave SDK (for local testing)

## Setting Up the Environment

### Local Testing

1. Install the Open Enclave SDK:

   ```bash
   # Ubuntu
   echo "deb [arch=amd64] https://download.01.org/intel-sgx/sgx_repo/ubuntu $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/intel-sgx.list
   wget -qO - https://download.01.org/intel-sgx/sgx_repo/ubuntu/intel-sgx-deb.key | sudo apt-key add -
   echo "deb [arch=amd64] https://packages.microsoft.com/ubuntu/$(lsb_release -rs)/prod $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/msprod.list
   wget -qO - https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
   sudo apt-get update
   sudo apt-get install -y libsgx-enclave-common libsgx-enclave-common-dev libsgx-urts libsgx-uae-service libsgx-dcap-ql open-enclave
   ```

   ```powershell
   # Windows (PowerShell)
   # Follow the instructions at https://github.com/openenclave/openenclave/blob/master/docs/GettingStartedDocs/install_oe_sdk-Windows.md
   ```

2. Set environment variables:

   ```bash
   # Linux
   export OE_SIMULATION=1
   export OE_ENCLAVE_PATH="$(pwd)/src/NeoServiceLayer.Tee.Enclave/bin/Debug/net9.0/liboe_enclave.signed.so"
   export OCCLUM_INSTANCE_DIR="$(pwd)/occlum_instance"
   export DOTNET_ENVIRONMENT=Testing
   export ASPNETCORE_ENVIRONMENT=Testing
   export TEST_CONFIG_PATH="$(pwd)/tests/simulation-test-config.json"
   ```

   ```powershell
   # Windows
   $env:OE_SIMULATION = "1"
   $env:OE_ENCLAVE_PATH = "$PWD\src\NeoServiceLayer.Tee.Enclave\bin\Debug\net9.0\liboe_enclave.signed.so"
   $env:OCCLUM_INSTANCE_DIR = "$PWD\occlum_instance"
   $env:DOTNET_ENVIRONMENT = "Testing"
   $env:ASPNETCORE_ENVIRONMENT = "Testing"
   $env:TEST_CONFIG_PATH = "$PWD\tests\simulation-test-config.json"
   ```

### Docker Testing

1. Make sure Docker and Docker Compose are installed.

2. Run the tests using Docker Compose:

   ```bash
   docker-compose -f docker-compose.test.yml up --build simulation-tests
   ```

## Running Tests

### Using the Test Runner Script

We provide a test runner script that sets up the environment and runs all the tests:

```bash
# Linux
cd tests
./run-simulation-tests.sh
```

```powershell
# Windows
cd tests
.\run-simulation-tests.ps1
```

### Running Specific Test Categories

You can run specific test categories using the `--filter` option:

```bash
# Run only security tests
dotnet test ./NeoServiceLayer.MockTests/NeoServiceLayer.MockTests.csproj --filter "Category=Security" --logger "console;verbosity=detailed"

# Run only performance tests
dotnet test ./NeoServiceLayer.MockTests/NeoServiceLayer.MockTests.csproj --filter "Category=Performance" --logger "console;verbosity=detailed"

# Run only JavaScript engine tests
dotnet test ./NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj --filter "Category=JavaScriptEngine" --logger "console;verbosity=detailed"

# Run only gas accounting tests
dotnet test ./NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj --filter "Category=GasAccounting" --logger "console;verbosity=detailed"

# Run only user secrets tests
dotnet test ./NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj --filter "Category=UserSecrets" --logger "console;verbosity=detailed"
```

### Running API Integration Tests

The API integration tests require the API to be running. You can run the API using the following command:

```bash
# Start the API
dotnet run --project ./src/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj
```

Then, in a separate terminal, run the API integration tests:

```bash
# Run API integration tests
dotnet test ./tests/NeoServiceLayer.IntegrationTests/NeoServiceLayer.IntegrationTests.csproj --filter "Category=ApiIntegration" --logger "console;verbosity=detailed"
```

Alternatively, you can use the test runner script, which will automatically detect if the API is running and run the API integration tests if it is:

```bash
# Linux
cd tests
./run-simulation-tests.sh

# Windows
cd tests
.\run-simulation-tests.ps1
```

## Test Categories

The tests are organized into the following categories:

1. **Basic Tests**: Tests for basic functionality without requiring the enclave.
2. **Mock Tests**: Tests that use a mock implementation of the enclave interface.
3. **Security Tests**: Tests that verify the security properties of the enclave.
4. **Performance Tests**: Tests that measure the performance of enclave operations.
5. **Error Handling Tests**: Tests that verify proper error handling.
6. **OpenEnclave Tests**: Tests that verify the OpenEnclave integration.
7. **Attestation Tests**: Tests that verify the attestation functionality.
8. **Occlum Tests**: Tests that verify the Occlum integration.
9. **Integration Tests**: Tests that verify the integration of all components.
10. **JavaScript Engine Tests**: Tests that verify the JavaScript engine functionality.
11. **Gas Accounting Tests**: Tests that verify the gas accounting functionality.
12. **User Secrets Tests**: Tests that verify the user secrets functionality.
13. **API Integration Tests**: Tests that verify the API integration with the enclave.

## Test Results

Test results are saved to the `TestResults` directory. Each test run creates a new subdirectory with the timestamp of the run.

The test results include:
- TRX files for each test category
- Code coverage reports
- Logs

## Continuous Integration

We use GitHub Actions to run the tests in CI. The workflow is defined in `.github/workflows/simulation-tests.yml`.

The workflow runs on every push to the `main` and `develop` branches, and on every pull request to these branches.

## Troubleshooting

### Common Issues

1. **Enclave Binary Not Found**

   Make sure the enclave binary is built and located at the expected path:

   ```bash
   dotnet build ./src/NeoServiceLayer.Tee.Enclave/NeoServiceLayer.Tee.Enclave.csproj -c Debug
   ```

2. **Open Enclave SDK Not Found**

   Make sure the Open Enclave SDK is installed and the environment variables are set correctly:

   ```bash
   # Linux
   source /opt/openenclave/share/openenclave/openenclaverc
   ```

3. **Test Failures**

   Check the test logs for details on the failures. The logs are saved to the `TestResults` directory.

### Getting Help

If you encounter issues that you can't resolve, please open an issue on the GitHub repository or contact the development team.
