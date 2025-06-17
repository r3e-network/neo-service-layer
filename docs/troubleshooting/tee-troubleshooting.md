# TEE Troubleshooting Guide

## Overview

This guide provides comprehensive troubleshooting information for Intel SGX and Occlum LibOS implementations in the Neo Service Layer. It covers common issues, diagnostic procedures, and step-by-step solutions.

## Quick Diagnosis

### System Health Check Script

```bash
#!/bin/bash
# tee-health-check.sh - Comprehensive TEE system health check

echo "=== Neo Service Layer TEE Health Check ==="
echo "Timestamp: $(date)"
echo ""

# Check SGX hardware support
echo "1. SGX Hardware Support:"
if cpuid 2>/dev/null | grep -q SGX; then
    echo "   ✅ SGX hardware support detected"
else
    echo "   ❌ SGX hardware support NOT detected"
fi

# Check SGX BIOS settings
echo ""
echo "2. SGX BIOS Configuration:"
if [ -f /sys/firmware/efi/efivars/SgxEnable-* ]; then
    echo "   ✅ SGX appears to be enabled in BIOS"
else
    echo "   ⚠️  SGX BIOS status unclear - check BIOS settings"
fi

# Check SGX driver
echo ""
echo "3. SGX Driver Status:"
if lsmod | grep -q intel_sgx; then
    echo "   ✅ Intel SGX driver loaded"
    ls -la /dev/sgx* 2>/dev/null || echo "   ❌ SGX device nodes not found"
else
    echo "   ❌ Intel SGX driver not loaded"
fi

# Check AESM service
echo ""
echo "4. AESM Service Status:"
if systemctl is-active aesmd >/dev/null 2>&1; then
    echo "   ✅ AESM service is running"
else
    echo "   ❌ AESM service is not running"
fi

# Check SGX SDK
echo ""
echo "5. SGX SDK Installation:"
if [ -d "${SGX_SDK:-/opt/intel/sgxsdk}" ]; then
    echo "   ✅ SGX SDK found at ${SGX_SDK:-/opt/intel/sgxsdk}"
else
    echo "   ❌ SGX SDK not found"
fi

# Check Occlum installation
echo ""
echo "6. Occlum LibOS Status:"
if command -v occlum >/dev/null 2>&1; then
    echo "   ✅ Occlum installed: $(occlum version 2>/dev/null || echo 'version unknown')"
else
    echo "   ❌ Occlum not installed"
fi

# Check Rust toolchain
echo ""
echo "7. Rust Toolchain:"
if command -v rustc >/dev/null 2>&1; then
    echo "   ✅ Rust installed: $(rustc --version)"
else
    echo "   ❌ Rust not installed"
fi

# Check .NET SDK
echo ""
echo "8. .NET SDK:"
if command -v dotnet >/dev/null 2>&1; then
    echo "   ✅ .NET SDK installed: $(dotnet --version)"
else
    echo "   ❌ .NET SDK not installed"
fi

# Check environment variables
echo ""
echo "9. Environment Variables:"
echo "   SGX_MODE: ${SGX_MODE:-not set}"
echo "   SGX_DEBUG: ${SGX_DEBUG:-not set}"
echo "   SGX_SDK: ${SGX_SDK:-not set}"
echo "   OCCLUM_LOG_LEVEL: ${OCCLUM_LOG_LEVEL:-not set}"

echo ""
echo "=== Health Check Complete ==="
```

## SGX-Specific Issues

### Hardware Mode Issues

#### Issue: SGX Hardware Not Detected

**Symptoms:**
- Error: "SGX is not enabled"
- Health check shows no SGX hardware support
- Build fails with hardware mode

**Diagnostic Steps:**
```bash
# Check CPU SGX support
cpuid | grep -i sgx
cat /proc/cpuinfo | grep sgx

# Check for SGX in dmesg
dmesg | grep -i sgx

# Check SGX MSR (Model-Specific Register)
sudo rdmsr 0x3a  # IA32_FEATURE_CONTROL
```

**Solutions:**

1. **Enable SGX in BIOS/UEFI:**
   ```
   1. Restart and enter BIOS/UEFI setup
   2. Navigate to Security → Intel SGX
   3. Set SGX to "Enabled" or "Software Controlled"
   4. Set SGX memory size (minimum 128MB)
   5. Save and restart
   ```

2. **Verify processor support:**
   ```bash
   # Check if processor supports SGX
   lscpu | grep -i sgx
   
   # For Intel processors 6th gen and newer
   cat /proc/cpuinfo | grep "model name"
   ```

#### Issue: SGX Driver Not Loaded

**Symptoms:**
- `/dev/sgx_enclave` and `/dev/sgx_provision` don't exist
- Error: "No such device"
- Driver not in `lsmod` output

**Diagnostic Steps:**
```bash
# Check loaded modules
lsmod | grep sgx

# Check for driver in kernel
modinfo intel_sgx

# Check kernel version (SGX support in 5.11+)
uname -r
```

**Solutions:**

1. **Install SGX driver (older kernels):**
   ```bash
   # Download Intel SGX driver
   git clone https://github.com/intel/linux-sgx-driver.git
   cd linux-sgx-driver
   make
   sudo mkdir -p "/lib/modules/$(uname -r)/kernel/drivers/intel/sgx"
   sudo cp isgx.ko "/lib/modules/$(uname -r)/kernel/drivers/intel/sgx"
   sudo sh -c "cat /etc/modules | grep -Fxq isgx || echo isgx >> /etc/modules"
   sudo /sbin/depmod
   sudo /sbin/modprobe isgx
   ```

2. **Use in-kernel driver (newer kernels):**
   ```bash
   # Load SGX driver
   sudo modprobe intel_sgx
   
   # Verify devices
   ls -la /dev/sgx*
   ```

3. **Set correct permissions:**
   ```bash
   sudo chmod 666 /dev/sgx_enclave /dev/sgx_provision
   sudo chown $(whoami):$(whoami) /dev/sgx*
   ```

### AESM Service Issues

#### Issue: AESM Service Fails to Start

**Symptoms:**
- Attestation operations fail
- Error: "AESM service unavailable"
- Service status shows failed

**Diagnostic Steps:**
```bash
# Check service status
systemctl status aesmd

# Check logs
journalctl -u aesmd -f

# Check AESM socket
ls -la /var/run/aesmd/
```

**Solutions:**

1. **Reinstall AESM service:**
   ```bash
   # Remove existing installation
   sudo apt remove --purge libsgx-ae-service libsgx-aesm-service
   
   # Reinstall
   sudo apt update
   sudo apt install libsgx-ae-service libsgx-aesm-service
   
   # Start service
   sudo systemctl enable aesmd
   sudo systemctl start aesmd
   ```

2. **Fix permissions:**
   ```bash
   # Create AESM directory
   sudo mkdir -p /var/run/aesmd
   sudo chown aesmd:aesmd /var/run/aesmd
   sudo chmod 755 /var/run/aesmd
   ```

3. **Configure proxy (if behind firewall):**
   ```bash
   # Edit AESM configuration
   sudo nano /etc/aesmd.conf
   
   # Add proxy settings
   proxy type = direct
   # OR for HTTP proxy:
   # proxy type = manual
   # aesm proxy = http://proxy.example.com:8080
   ```

## Occlum LibOS Issues

### Installation and Setup Issues

#### Issue: Occlum Installation Fails

**Symptoms:**
- Package installation errors
- Missing dependencies
- Build failures during installation

**Diagnostic Steps:**
```bash
# Check system requirements
lsb_release -a
uname -m

# Check available disk space
df -h

# Check installed packages
dpkg -l | grep sgx
```

**Solutions:**

1. **Install missing dependencies:**
   ```bash
   sudo apt update
   sudo apt install -y \
       build-essential \
       cmake \
       ninja-build \
       python3 \
       python3-pip \
       pkg-config \
       libssl-dev \
       libprotobuf-dev \
       protobuf-compiler \
       git \
       wget \
       curl
   ```

2. **Use alternative installation method:**
   ```bash
   # Try building from source
   git clone https://github.com/occlum/occlum.git
   cd occlum
   make submodule
   OCCLUM_RELEASE_BUILD=1 make
   sudo make install
   ```

#### Issue: Occlum Workspace Creation Fails

**Symptoms:**
- `occlum init` command fails
- Workspace directory not created
- Permission errors

**Diagnostic Steps:**
```bash
# Check Occlum installation
occlum version

# Check current directory permissions
ls -la .

# Check available space
df -h .
```

**Solutions:**

1. **Fix permissions:**
   ```bash
   # Ensure write permissions
   chmod 755 .
   
   # Create workspace with explicit path
   mkdir -p occlum_workspace
   cd occlum_workspace
   occlum init
   ```

2. **Clean and retry:**
   ```bash
   # Remove partial workspace
   rm -rf occlum_workspace
   
   # Create fresh workspace
   occlum new occlum_workspace
   cd occlum_workspace
   ```

### Runtime Issues

#### Issue: Occlum Build Fails

**Symptoms:**
- `occlum build` command fails
- Missing files in image directory
- Compilation errors

**Diagnostic Steps:**
```bash
# Check workspace structure
ls -la image/

# Check Occlum configuration
cat Occlum.json

# Check build logs
occlum build --verbose
```

**Solutions:**

1. **Verify image contents:**
   ```bash
   # Check required files are in image/
   ls -la image/bin/
   ls -la image/lib/
   
   # Copy missing files
   cp /usr/bin/dotnet image/bin/ 2>/dev/null || echo "dotnet not found"
   ```

2. **Fix configuration:**
   ```json
   {
     "resource_limits": {
       "user_space_size": "2GB",
       "kernel_space_heap_size": "128MB"
     },
     "entry_points": ["/bin/dotnet"]
   }
   ```

3. **Force rebuild:**
   ```bash
   occlum build --force
   ```

#### Issue: Enclave Execution Fails

**Symptoms:**
- Segmentation faults during execution
- "Out of memory" errors
- Unexpected termination

**Diagnostic Steps:**
```bash
# Run with debug output
OCCLUM_LOG_LEVEL=debug occlum run /bin/dotnet app.dll

# Check memory usage
cat Occlum.json | grep -A 10 resource_limits

# Check for core dumps
ls -la core*
```

**Solutions:**

1. **Increase memory limits:**
   ```json
   {
     "resource_limits": {
       "user_space_size": "4GB",
       "user_space_max_size": "8GB",
       "kernel_space_heap_size": "256MB",
       "kernel_space_heap_max_size": "1GB"
     }
   }
   ```

2. **Debug with GDB:**
   ```bash
   # Enable debug mode
   export SGX_DEBUG=1
   
   # Run with GDB
   gdb --args occlum run /bin/dotnet app.dll
   ```

## Build System Issues

### Compilation Errors

#### Issue: P/Invoke Declaration Errors

**Symptoms:**
- "DllNotFoundException" at runtime
- "EntryPointNotFoundException" errors
- Linking failures

**Diagnostic Steps:**
```bash
# Check library availability
find /opt/intel/sgxsdk -name "*.so" 2>/dev/null
find . -name "libneo_service_enclave.so"

# Check P/Invoke declarations
grep -r "DllImport" src/
```

**Solutions:**

1. **Verify library paths:**
   ```csharp
   // Use conditional compilation for library names
   #if SGX_SIMULATION_MODE
   private const string SgxLibrary = "sgx_urts_sim";
   #else
   private const string SgxLibrary = "sgx_urts";
   #endif
   ```

2. **Set library search path:**
   ```bash
   export LD_LIBRARY_PATH=$SGX_SDK/lib64:$LD_LIBRARY_PATH
   ```

#### Issue: Rust Build Failures

**Symptoms:**
- Cargo build errors
- Missing crate dependencies
- Compilation timeouts

**Diagnostic Steps:**
```bash
# Check Rust version
rustc --version

# Check cargo configuration
cat Cargo.toml

# Build with verbose output
cargo build --verbose
```

**Solutions:**

1. **Update Rust toolchain:**
   ```bash
   rustup update stable
   rustup install nightly
   rustup component add rust-src --toolchain nightly
   ```

2. **Fix dependencies:**
   ```toml
   [dependencies]
   # Use specific versions for stability
   ring = "0.17"
   secp256k1 = { version = "0.28", features = ["recovery"] }
   
   # Platform-specific dependencies
   [target.'cfg(not(target_env = "sgx"))'.dependencies]
   std = { version = "1.0", package = "std" }
   ```

3. **Clear cache and rebuild:**
   ```bash
   cargo clean
   rm -rf target/
   cargo build --release
   ```

## Performance Issues

### Slow Enclave Operations

#### Issue: High Latency in SGX Operations

**Symptoms:**
- Seal/unseal operations take too long
- Timeouts in enclave calls
- Poor application performance

**Diagnostic Steps:**
```bash
# Profile enclave operations
time ./run-sgx-tests.ps1

# Check system load
top
htop

# Monitor SGX-specific metrics
cat /proc/interrupts | grep sgx
```

**Solutions:**

1. **Optimize enclave configuration:**
   ```xml
   <!-- Increase thread count -->
   <TCSNum>16</TCSNum>
   
   <!-- Increase heap size -->
   <HeapMaxSize>0x4000000</HeapMaxSize>
   ```

2. **Use batch operations:**
   ```csharp
   // Batch multiple operations together
   var tasks = data.Select(d => enclave.SealDataAsync(d, keyId));
   var results = await Task.WhenAll(tasks);
   ```

3. **Enable caching:**
   ```csharp
   // Cache frequently used sealed data
   private readonly MemoryCache _sealedDataCache = new MemoryCache(
       new MemoryCacheOptions { SizeLimit = 1000 });
   ```

### Memory Issues

#### Issue: Memory Exhaustion in Enclave

**Symptoms:**
- "Out of enclave memory" errors
- Application crashes
- Poor performance with large datasets

**Diagnostic Steps:**
```bash
# Check memory usage
ps aux | grep dotnet
cat /proc/meminfo

# Check SGX memory allocation
dmesg | grep -i sgx | grep memory
```

**Solutions:**

1. **Increase SGX memory:**
   ```bash
   # In BIOS, increase SGX memory allocation to 256MB or higher
   # Check current allocation
   dmesg | grep -i sgx | grep size
   ```

2. **Optimize memory usage:**
   ```csharp
   // Use pooled buffers
   private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
   
   public async Task<byte[]> ProcessDataAsync(byte[] input)
   {
       var buffer = _bufferPool.Rent(input.Length * 2);
       try
       {
           // Process data
           return await ProcessInternalAsync(buffer, input);
       }
       finally
       {
           _bufferPool.Return(buffer);
       }
   }
   ```

## Network and Connectivity Issues

### HTTPS Certificate Issues

#### Issue: TLS Certificate Validation Fails

**Symptoms:**
- HTTPS requests fail within enclave
- Certificate validation errors
- SSL handshake failures

**Diagnostic Steps:**
```bash
# Test external connectivity
curl -v https://api.neo.org

# Check certificate chain
openssl s_client -connect api.neo.org:443 -showcerts

# Check system time
date
timedatectl status
```

**Solutions:**

1. **Update certificates:**
   ```bash
   # Update CA certificates
   sudo apt update
   sudo apt install ca-certificates
   sudo update-ca-certificates
   ```

2. **Configure certificate validation:**
   ```csharp
   var client = new HttpClient(new HttpClientHandler()
   {
       ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
       {
           // In production, implement proper validation
           return ValidateCertificateChain(cert, chain, errors);
       }
   });
   ```

## Monitoring and Logging Issues

### Log Analysis

#### Issue: Missing or Unclear Log Messages

**Symptoms:**
- No debug output from enclave
- Truncated log messages
- Missing performance metrics

**Diagnostic Steps:**
```bash
# Check log levels
echo $OCCLUM_LOG_LEVEL
echo $RUST_LOG

# Check log files
find /var/log -name "*neo*" -o -name "*sgx*" -o -name "*occlum*"

# Check journald logs
journalctl -u aesmd --since "1 hour ago"
```

**Solutions:**

1. **Enable verbose logging:**
   ```bash
   export OCCLUM_LOG_LEVEL=debug
   export RUST_LOG=debug
   export SGX_DEBUG=1
   ```

2. **Configure structured logging:**
   ```csharp
   builder.Services.AddLogging(config =>
   {
       config.AddConsole();
       config.AddFile("logs/neo-service-{Date}.log");
       config.SetMinimumLevel(LogLevel.Debug);
   });
   ```

3. **Add performance logging:**
   ```csharp
   using var activity = ActivitySource.StartActivity("EnclaveSealData");
   activity?.SetTag("data.size", data.Length);
   activity?.SetTag("key.id", keyId);
   
   var stopwatch = Stopwatch.StartNew();
   var result = await SealDataInternalAsync(data, keyId);
   
   _logger.LogInformation("Seal operation completed in {Duration}ms", 
       stopwatch.ElapsedMilliseconds);
   ```

## Development Environment Issues

### IDE and Tooling Issues

#### Issue: Visual Studio/VS Code SGX Support

**Symptoms:**
- IntelliSense errors for SGX code
- Build failures in IDE
- Debugging not working

**Solutions:**

1. **Configure VS Code:**
   ```json
   // .vscode/settings.json
   {
     "rust-analyzer.cargo.features": ["simulation"],
     "C_Cpp.default.includePath": [
       "/opt/intel/sgxsdk/include"
     ],
     "C_Cpp.default.defines": [
       "SGX_SIMULATION"
     ]
   }
   ```

2. **Set up build tasks:**
   ```json
   // .vscode/tasks.json
   {
     "tasks": [
       {
         "label": "Build SGX",
         "type": "shell",
         "command": "./build-neo-service-layer.ps1",
         "args": ["-Configuration", "Debug", "-SGX_MODE", "SIM"],
         "group": "build"
       }
     ]
   }
   ```

## Testing Issues

### Test Environment Setup

#### Issue: Tests Fail in CI/CD Pipeline

**Symptoms:**
- Tests pass locally but fail in CI
- SGX simulation not working in containers
- Permission errors in build agents

**Solutions:**

1. **Configure CI environment:**
   ```yaml
   # GitHub Actions
   - name: Setup SGX
     run: |
       echo 'deb [arch=amd64] https://download.01.org/intel-sgx/sgx_repo/ubuntu focal main' | sudo tee /etc/apt/sources.list.d/intel-sgx.list
       wget -qO - https://download.01.org/intel-sgx/sgx_repo/ubuntu/intel-sgx-deb.key | sudo apt-key add -
       sudo apt update
       sudo apt install -y libsgx-urts libsgx-uae-service
   
   - name: Run Tests
     env:
       SGX_MODE: SIM
       SGX_DEBUG: 1
     run: dotnet test --configuration Release
   ```

2. **Mock SGX in tests:**
   ```csharp
   public class MockEnclaveWrapper : IEnclaveWrapper
   {
       public Task<EncryptedData> SealDataAsync(byte[] data, string keyId)
       {
           // Simulate sealing without actual SGX
           return Task.FromResult(new EncryptedData 
           { 
               EncryptedData = Convert.ToBase64String(data) 
           });
       }
   }
   ```

## Emergency Recovery Procedures

### Service Recovery

#### Complete System Recovery

```bash
#!/bin/bash
# emergency-recovery.sh - Emergency TEE service recovery

echo "=== Emergency TEE Service Recovery ==="

# Stop all services
sudo systemctl stop neo-service-layer
sudo systemctl stop aesmd

# Clean temporary files
sudo rm -rf /tmp/sgx_*
sudo rm -rf /var/run/aesmd/*

# Restart SGX infrastructure
sudo systemctl start aesmd
sleep 5

# Verify SGX functionality
if systemctl is-active aesmd >/dev/null; then
    echo "AESM service restarted successfully"
else
    echo "AESM service failed to start - manual intervention required"
    exit 1
fi

# Restart application
sudo systemctl start neo-service-layer

echo "Recovery completed. Check service status:"
echo "sudo systemctl status neo-service-layer"
echo "sudo systemctl status aesmd"
```

## Getting Help

### Diagnostic Information Collection

```bash
#!/bin/bash
# collect-diagnostics.sh - Collect system information for support

OUTPUT_FILE="neo-service-tee-diagnostics-$(date +%Y%m%d-%H%M%S).txt"

{
    echo "=== Neo Service Layer TEE Diagnostics ==="
    echo "Generated: $(date)"
    echo ""
    
    echo "=== System Information ==="
    uname -a
    lsb_release -a
    
    echo ""
    echo "=== SGX Hardware ==="
    cpuid | grep -i sgx || echo "No SGX support detected"
    
    echo ""
    echo "=== SGX Driver ==="
    lsmod | grep sgx
    ls -la /dev/sgx* 2>/dev/null || echo "No SGX devices found"
    
    echo ""
    echo "=== AESM Service ==="
    systemctl status aesmd
    
    echo ""
    echo "=== Environment Variables ==="
    printenv | grep -E "(SGX|OCCLUM|RUST|DOTNET)" | sort
    
    echo ""
    echo "=== Installed Packages ==="
    dpkg -l | grep -E "(sgx|occlum)"
    
    echo ""
    echo "=== Recent Logs ==="
    journalctl -u aesmd --since "1 hour ago" | tail -50
    
} > "$OUTPUT_FILE"

echo "Diagnostics saved to: $OUTPUT_FILE"
echo "Please include this file when requesting support."
```

### Support Channels

- **GitHub Issues**: https://github.com/neo-project/neo-service-layer/issues
- **Documentation**: Check related documentation for specific error patterns
- **Community Forums**: Neo blockchain community channels

### Related Documentation

- [SGX Deployment Guide](../deployment/sgx-deployment-guide.md)
- [Occlum LibOS Guide](../deployment/occlum-libos-guide.md)
- [TEE Enclave Service](../services/tee-enclave-service.md)
- [Security Architecture](../security/tee-security-architecture.md) 