# SGX Simulation Mode Testing Guide

## Overview

This document explains how to test SGX (Software Guard Extensions) enclave functionality in **simulation mode** versus **mocked/simulated behavior**. These are two very different approaches to testing SGX applications.

## SGX Simulation Mode vs. Simulated SGX Behavior

### SGX Simulation Mode (Real SGX Testing)
- **What it is**: Running actual SGX enclave code using Intel SGX SDK in simulation mode
- **Environment**: Uses real SGX SDK with `SGX_MODE=SIM`
- **Purpose**: Test real enclave logic without requiring SGX hardware
- **Implementation**: Actual SGX ECALLs, OCALLs, sealing, attestation APIs
- **Benefits**: Tests real enclave behavior, cryptographic operations, memory protection simulation

### Simulated SGX Behavior (Mock Testing)  
- **What it is**: Mock/fake implementation that mimics SGX behavior
- **Environment**: Pure software simulation without SGX SDK
- **Purpose**: Unit testing and development when SGX SDK is not available
- **Implementation**: Custom classes that simulate expected outputs
- **Benefits**: Fast, no dependencies, good for CI/CD pipelines

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Testing Approaches                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────┐    ┌─────────────────────────────┐ │
│  │   SGX Simulation    │    │   Simulated SGX Behavior   │ │
│  │      Mode          │    │        (Mock)               │ │
│  │                     │    │                             │ │
│  │ ┌─────────────────┐ │    │ ┌─────────────────────────┐ │ │
│  │ │ SGX SDK (SIM)   │ │    │ │ SGXSimulationEnclave   │ │ │
│  │ │ - sgx_create_   │ │    │ │ Wrapper (Mock)         │ │ │
│  │ │   enclave()     │ │    │ │ - Pure C# simulation   │ │ │
│  │ │ - sgx_ecall_*() │ │    │ │ - No SGX dependencies  │ │ │
│  │ │ - sgx_seal_data │ │    │ │ - Fast execution       │ │ │
│  │ │ - Real crypto   │ │    │ │ - Predictable results  │ │ │
│  │ └─────────────────┘ │    │ └─────────────────────────┘ │ │
│  │                     │    │                             │ │
│  │ Benefits:           │    │ Benefits:                   │ │
│  │ ✅ Real SGX APIs    │    │ ✅ No SGX SDK required     │ │
│  │ ✅ Actual crypto    │    │ ✅ Fast CI/CD integration  │ │
│  │ ✅ True attestation │    │ ✅ Cross-platform          │ │
│  │ ✅ Memory isolation │    │ ✅ Deterministic testing   │ │
│  │                     │    │                             │ │
│  │ Requirements:       │    │ Limitations:                │ │
│  │ ❗ SGX SDK needed   │    │ ❗ Not real SGX behavior    │ │
│  │ ❗ Linux preferred  │    │ ❗ Limited crypto validation│ │
│  │ ❗ Build complexity │    │ ❗ No actual memory protect │ │
│  └─────────────────────┘    │ ❗ Mock attestation only    │ │
│                              └─────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Test Classes Available

### 1. SGXEnclaveSimModeTests (Real SGX Simulation)
**File**: `tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/SGXEnclaveSimModeTests.cs`

Tests that run **actual SGX enclave code** in simulation mode:

```csharp
[Fact]
public void SGXEnclave_Initialize_ShouldSucceedInSimulationMode()
{
    // Uses real SGX SDK calls: sgx_create_enclave()
    var result = _enclave.Initialize();
    result.Should().BeTrue();
}

[Fact] 
public void SGXEnclave_CryptographicOperations_ShouldWorkInSimulationMode()
{
    // Uses real SGX crypto: sgx_aes_ctr_encrypt(), sgx_sha256_msg()
    var encrypted = _enclave.Encrypt(testData, key);
    var decrypted = _enclave.Decrypt(encrypted, key);
    // Tests actual cryptographic behavior
}
```

**Requirements**:
- Intel SGX SDK installed
- Linux environment (preferred)
- `SGX_MODE=SIM` environment variable
- Proper enclave build process

**What it tests**:
- Real SGX ECALL/OCALL interfaces
- Actual SGX cryptographic functions
- Real enclave sealing/unsealing
- True SGX attestation (simulated mode)
- Memory protection simulation

### 2. SGXAttestationAndLifecycleTests (Mock Implementation)
**File**: `tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/SGXAttestationAndLifecycleTests.cs`

Uses `SGXSimulationEnclaveWrapper` - a **mock implementation**:

```csharp
[Fact]
public void SGXAttestation_KeyGeneration_ShouldIncludeAttestationData()
{
    // Uses mock implementation - no real SGX calls
    var keyResult = _enclave.GenerateKey(keyId, keyType, "Sign", true, "Test");
    // Tests expected JSON structure and behavior
}
```

**Benefits**:
- No SGX SDK required
- Fast execution 
- Cross-platform compatibility
- Predictable results for unit testing

**What it tests**:
- API interface contracts
- Expected JSON response formats
- Error handling paths
- Integration logic

## SGX Enclave Implementation

### Real SGX Implementation Structure

```
src/Tee/NeoServiceLayer.Tee.Enclave/Enclave/
├── NeoServiceEnclave.edl          # SGX Interface Definition
├── src/enclave_main.cpp           # Enclave Implementation (Trusted)
├── include/                       # Enclave Headers
├── Makefile.sgx                   # SGX Build Configuration
└── enclave.config.xml            # Enclave Configuration
```

### EDL (Enclave Definition Language) Interface

```c
// NeoServiceEnclave.edl
enclave {
    trusted {
        // ECALLs - Called from untrusted to trusted
        public sgx_status_t ecall_enclave_init(void);
        public sgx_status_t ecall_generate_random_bytes(
            [out, size=length] unsigned char* buffer,
            size_t length
        );
        public sgx_status_t ecall_encrypt_data(
            [in, size=data_size] const unsigned char* data,
            // ... more parameters
        );
    };
    
    untrusted {
        // OCALLs - Called from trusted to untrusted  
        void ocall_print([in, string] const char* message);
        uint64_t ocall_get_system_time(void);
    };
};
```

### Enclave Implementation (C++)

```cpp
// enclave_main.cpp
#include "NeoServiceEnclave_t.h"  // Generated from EDL
#include "sgx_trts.h"
#include "sgx_tcrypto.h"

sgx_status_t ecall_generate_random_bytes(unsigned char* buffer, size_t length) {
    if (!g_enclave_initialized || buffer == nullptr || length == 0) {
        return SGX_ERROR_INVALID_PARAMETER;
    }
    
    // Use real SGX random number generation
    return sgx_read_rand(buffer, length);
}

sgx_status_t ecall_encrypt_data(const unsigned char* data, size_t data_size,
                               const unsigned char* key, size_t key_size,
                               unsigned char* encrypted_data, size_t encrypted_size,
                               size_t* actual_encrypted_size) {
    // Use real SGX AES encryption
    sgx_aes_ctr_128bit_key_t aes_key;
    // ... key derivation
    return sgx_aes_ctr_encrypt(&aes_key, data, data_size, ctr, 32, encrypted_data);
}
```

## Running Tests

### Option 1: SGX Simulation Mode Tests (Recommended)

```bash
# Set up SGX simulation environment
export SGX_MODE=SIM
export SGX_DEBUG=1
export SGX_SDK=/opt/intel/sgxsdk

# Run the PowerShell test script
cd tests/Tee/NeoServiceLayer.Tee.Enclave.Tests
./run-sgx-sim-tests.ps1

# Or run specific test class
./run-sgx-sim-tests.ps1 -TestClass "SGXEnclaveSimModeTests"

# Or run with verbose output
./run-sgx-sim-tests.ps1 -Verbose -Coverage
```

### Option 2: Mock Implementation Tests (Fallback)

```bash
# Run mock implementation tests (no SGX SDK required)
dotnet test --filter "FullyQualifiedName~SGXAttestationAndLifecycleTests"

# Or run comprehensive tests with mock
dotnet test --filter "FullyQualifiedName~ComprehensiveEnclaveTests"
```

### Option 3: Building SGX Enclave Manually

```bash
cd src/Tee/NeoServiceLayer.Tee.Enclave/Enclave

# Build in simulation mode
make -f Makefile.sgx SGX_MODE=SIM test-sim

# Build for hardware mode (requires SGX hardware)
make -f Makefile.sgx SGX_MODE=HW all

# Clean build artifacts
make -f Makefile.sgx clean
```

## Environment Setup

### SGX SDK Installation (Linux)

```bash
# Download SGX SDK
wget https://download.01.org/intel-sgx/sgx-linux/2.18/distro/ubuntu20.04-server/sgx_linux_x64_sdk_2.18.100.3.bin

# Install SDK
chmod +x sgx_linux_x64_sdk_2.18.100.3.bin
sudo ./sgx_linux_x64_sdk_2.18.100.3.bin --prefix=/opt/intel

# Source the environment
source /opt/intel/sgxsdk/environment
```

### Environment Variables

```bash
# For SGX simulation mode
export SGX_MODE=SIM          # Enable simulation mode
export SGX_DEBUG=1           # Enable debug mode  
export SGX_SDK=/opt/intel/sgxsdk

# Optional: Specific settings
export SGX_ARCH=x64          # Architecture
export LD_LIBRARY_PATH=$SGX_SDK/sdk_libs:$LD_LIBRARY_PATH
```

## Testing Strategy

### Development Phase
1. **Start with Mock Tests**: Use `SGXSimulationEnclaveWrapper` for rapid development
2. **API Contract Testing**: Ensure interfaces work correctly
3. **Unit Test Logic**: Test business logic without SGX complexity

### Integration Phase  
1. **SGX Simulation Mode**: Test with real SGX APIs in simulation
2. **Crypto Validation**: Verify actual cryptographic operations
3. **Attestation Testing**: Test real attestation report generation

### Production Preparation
1. **Hardware Testing**: Test on actual SGX-enabled hardware
2. **Performance Testing**: Measure real-world performance
3. **Security Validation**: Validate isolation and protection

## Troubleshooting

### Common Issues

#### "SGX SDK not found"
```bash
# Verify SGX SDK installation
ls -la /opt/intel/sgxsdk/
source /opt/intel/sgxsdk/environment
```

#### "Enclave creation failed"
```bash
# Check SGX simulation mode
echo $SGX_MODE  # Should be "SIM"

# Verify enclave file exists
ls -la *.signed.so
```

#### "ECALL failed"
```bash
# Enable detailed logging
export SGX_DEBUG=1

# Check enclave logs
dmesg | grep -i sgx
```

### Debug Commands

```bash
# Check SGX support
ls /dev/sgx* 2>/dev/null || echo "No SGX devices found"

# Verify simulation mode
echo "SGX_MODE: $SGX_MODE"
echo "SGX_DEBUG: $SGX_DEBUG"

# Test enclave loading
cd src/Tee/NeoServiceLayer.Tee.Enclave/Enclave
make -f Makefile.sgx test-sim
```

## Best Practices

### 1. Layered Testing Approach
```
┌─────────────────────────────────────┐
│            Unit Tests               │  ← Mock implementation
│         (Fast, No SGX)              │
├─────────────────────────────────────┤
│        Integration Tests            │  ← SGX simulation mode  
│       (SGX APIs, SIM mode)          │
├─────────────────────────────────────┤
│         E2E Tests                   │  ← Real SGX hardware
│      (Production-like)              │
└─────────────────────────────────────┘
```

### 2. Test Organization
- **Unit Tests**: Mock implementations for business logic
- **Integration Tests**: SGX simulation mode for API validation
- **System Tests**: Real hardware for production validation

### 3. CI/CD Strategy
- **PR Builds**: Run mock tests (fast feedback)
- **Main Builds**: Run SGX simulation tests
- **Release Builds**: Run hardware tests (if available)

### 4. Documentation Requirements
- Document which tests use real SGX vs. mock
- Provide clear setup instructions for each approach
- Include troubleshooting guides for common issues

## Conclusion

This testing approach provides comprehensive coverage:

1. **Mock Tests** (`SGXSimulationEnclaveWrapper`): Fast development and unit testing
2. **SGX Simulation Tests** (`SGXEnclaveSimModeTests`): Real SGX API validation without hardware
3. **Hardware Tests**: Production validation on SGX-enabled systems

The key difference is that **SGX simulation mode runs real enclave code** using the Intel SGX SDK, while **simulated SGX behavior** uses mock implementations for testing purposes. Both approaches are valuable in different phases of development and testing.

Use mock implementations for rapid development and unit testing, then validate with SGX simulation mode before deploying to SGX hardware. 