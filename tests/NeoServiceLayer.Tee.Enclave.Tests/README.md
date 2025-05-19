# NeoServiceLayer.Tee.Enclave.Tests

This project contains tests for the NeoServiceLayer.Tee.Enclave component, which is responsible for running JavaScript functions securely in an SGX enclave.

## Simulation Mode

The tests in this project can run in two modes:

1. **Real SGX Mode**: This mode requires an actual SGX enclave and the Occlum LibOS to be installed. It runs the tests against the real SGX enclave.

2. **Simulation Mode**: This mode simulates the SGX enclave and allows the tests to run without requiring an actual SGX enclave or the Occlum LibOS. This is useful for development and CI/CD pipelines where SGX hardware is not available.

### Running Tests in Simulation Mode

To run the tests in simulation mode, set the `OCCLUM_SIMULATION` environment variable to `1`:

```bash
# Windows
set OCCLUM_SIMULATION=1
dotnet test

# Linux/macOS
export OCCLUM_SIMULATION=1
dotnet test
```

### Simulation Mode Infrastructure

The simulation mode infrastructure consists of the following components:

1. **SimulationModeFixture**: This class sets up the simulation environment and provides the necessary mocks for the tests.

2. **MockEnclaveFixture**: This class provides a mock enclave for testing.

3. **MockSgxEnclaveInterface**: This class simulates the SGX enclave interface.

4. **MockJavaScriptEngine**: This class simulates the JavaScript engine.

### How Simulation Mode Works

When the `OCCLUM_SIMULATION` environment variable is set to `1`, the `JavaScriptEngine` class detects this and uses the mock implementations instead of the real SGX enclave. This allows the tests to run without requiring an actual SGX enclave.

The mock implementations simulate the behavior of the real SGX enclave and JavaScript engine, allowing the tests to verify the functionality of the code without requiring the actual hardware.

### Test Categories

The tests in this project are divided into several categories:

1. **GasAccountingTests**: These tests verify the gas accounting functionality, which tracks the computational resources used by JavaScript functions.

2. **JavaScriptEngineTests**: These tests verify the JavaScript engine functionality, which executes JavaScript functions securely.

3. **UserSecretsTests**: These tests verify the user secrets functionality, which allows JavaScript functions to access user-specific secrets.

4. **OcclumTests**: These tests verify the Occlum functionality, which provides the secure enclave environment. These tests are skipped in simulation mode.

5. **AttestationTests**: These tests verify the attestation functionality, which provides proof that the code is running in a secure enclave. These tests are skipped in simulation mode.

6. **SecurityTests**: These tests verify the security functionality, which ensures that the enclave is secure. These tests are skipped in simulation mode.

7. **ErrorHandlingTests**: These tests verify the error handling functionality, which ensures that errors are handled gracefully. These tests are skipped in simulation mode.

8. **PerformanceTests**: These tests verify the performance of the enclave. These tests are skipped in simulation mode.

### Adding New Tests

When adding new tests, consider whether they need to run in simulation mode or if they require the actual SGX enclave. If they require the actual SGX enclave, use the `Skip.If` method to skip the test in simulation mode:

```csharp
[Fact]
public void MyTest()
{
    Skip.If(Environment.GetEnvironmentVariable("OCCLUM_SIMULATION") == "1", "Skipping test because it requires the actual SGX enclave");

    // Test code here
}
```

If the test should run in simulation mode, make sure it uses the mock implementations provided by the simulation mode infrastructure.
