# Testing with Real SGX Hardware

This document provides instructions for testing the Neo Confidential Serverless Layer with real SGX hardware.

## Prerequisites

- A machine with Intel SGX hardware support
- Intel SGX driver installed
- Open Enclave SDK installed
- Occlum installed (for Occlum tests)
- .NET 9.0 SDK installed

## Verifying SGX Hardware Support

To verify that your machine has SGX hardware support and that it's properly configured:

```bash
# Check if SGX is supported by the CPU
grep -q 'sgx' /proc/cpuinfo && echo "SGX supported" || echo "SGX not supported"

# Check if SGX is enabled in BIOS
dmesg | grep -i sgx

# Check if SGX driver is loaded
lsmod | grep sgx
```

## Installing Required Software

### Intel SGX Driver and SDK

```bash
# Add Intel SGX repository
echo "deb [arch=amd64] https://download.01.org/intel-sgx/sgx_repo/ubuntu $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/intel-sgx.list
wget -qO - https://download.01.org/intel-sgx/sgx_repo/ubuntu/intel-sgx-deb.key | sudo apt-key add -

# Update and install SGX packages
sudo apt-get update
sudo apt-get install -y libsgx-enclave-common libsgx-enclave-common-dev libsgx-urts libsgx-uae-service libsgx-dcap-ql
```

### Open Enclave SDK

```bash
# Add Open Enclave repository
echo "deb [arch=amd64] https://packages.microsoft.com/ubuntu/$(lsb_release -rs)/prod $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/microsoft.list
wget -qO - https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -

# Update and install Open Enclave SDK
sudo apt-get update
sudo apt-get install -y open-enclave
```

### Occlum

Follow the [Occlum installation guide](https://github.com/occlum/occlum/blob/master/docs/quickstart.md) to install Occlum.

## Running Tests on Real SGX Hardware

To run tests on real SGX hardware, you need to set the `OE_SIMULATION` environment variable to `0` (or unset it):

```bash
# Unset OE_SIMULATION to use real SGX hardware
unset OE_SIMULATION

# Or explicitly set it to 0
export OE_SIMULATION=0
```

### Running All Tests

Use the provided script to run all tests:

```bash
# Make the script executable
chmod +x tests/run-enclave-tests.sh

# Run the tests
cd tests
./run-enclave-tests.sh
```

### Running Specific Test Categories

You can run specific test categories using the `--filter` option:

```bash
# Run only attestation tests
dotnet test ./NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj --filter "Category=Attestation" --logger "console;verbosity=detailed"

# Run only security tests
dotnet test ./NeoServiceLayer.Tee.Enclave.Tests/NeoServiceLayer.Tee.Enclave.Tests.csproj --filter "Category=Security" --logger "console;verbosity=detailed"
```

## Troubleshooting

### Common Issues

1. **SGX Not Enabled in BIOS**
   
   Make sure SGX is enabled in your BIOS settings. The exact location varies by motherboard manufacturer.

2. **SGX Driver Not Loaded**
   
   If the SGX driver is not loaded, try:
   
   ```bash
   sudo modprobe sgx
   ```

3. **Enclave Creation Fails**
   
   If enclave creation fails with "SGX device not found" or similar errors:
   
   ```bash
   # Check if the SGX device exists
   ls -la /dev/sgx*
   
   # Check permissions
   sudo chmod 666 /dev/sgx*
   ```

4. **Attestation Fails**
   
   For attestation to work properly, you need to have internet access to connect to the Intel Attestation Service (IAS).

### Logs and Diagnostics

To get more detailed logs, set the log level to Debug:

```bash
export OCCLUM_LOG_LEVEL=debug
export OE_LOG_LEVEL=INFO
```

## Continuous Integration with Real SGX Hardware

For continuous integration with real SGX hardware, you need a CI runner with SGX support. 

1. Set up a self-hosted GitHub Actions runner on a machine with SGX hardware
2. Label the runner with `sgx-hardware`
3. Use the label in your workflow:

```yaml
jobs:
  test-real-sgx:
    name: Test with Real SGX Hardware
    runs-on: [self-hosted, sgx-hardware]
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Build
      run: dotnet build --configuration Release
    
    - name: Run Tests with Real SGX
      run: |
        unset OE_SIMULATION
        cd tests
        ./run-enclave-tests.sh
```

## Security Considerations

When testing with real SGX hardware, be aware of the following security considerations:

1. **Production Secrets**: Never use real production secrets in test environments
2. **Remote Attestation**: For production, always use remote attestation to verify the enclave identity
3. **Physical Security**: SGX provides protection against software attacks, but physical attacks are still possible
4. **Side-Channel Attacks**: Be aware of potential side-channel attacks against SGX enclaves
