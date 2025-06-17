# SGX Deployment Guide

## Overview

This guide provides comprehensive instructions for deploying Neo Service Layer with Intel SGX (Software Guard Extensions) support. It covers both simulation mode for development/testing and hardware mode for production environments.

## Prerequisites

### Hardware Requirements

#### For Simulation Mode
- Any x64 processor
- Minimum 4GB RAM
- Windows 10/11 or Linux (Ubuntu 20.04+, CentOS 8+)

#### For Hardware Mode
- Intel processor with SGX support (6th generation Core or newer)
- SGX enabled in BIOS/UEFI
- Minimum 8GB RAM (16GB recommended)
- Linux with SGX driver support or Windows with SGX platform software

### Software Requirements

#### Required Components
- **.NET 9.0 SDK** or later
- **Intel SGX SDK** (version 2.23.100.2 or later)
- **Rust toolchain** (latest stable)
- **Docker** (for containerized deployments)
- **PowerShell 7+** (for build scripts)

#### Optional Components
- **Intel SGX PSW** (Platform Software) for hardware mode
- **Intel SGX DCAP** (Data Center Attestation Primitives) for cloud deployments
- **Occlum LibOS** (integrated via Rust dependencies)

## Installation

### Intel SGX SDK Installation

#### Linux (Ubuntu/Debian)

```bash
# Add Intel SGX repository
echo 'deb [arch=amd64] https://download.01.org/intel-sgx/sgx_repo/ubuntu focal main' | sudo tee /etc/apt/sources.list.d/intel-sgx.list
wget -qO - https://download.01.org/intel-sgx/sgx_repo/ubuntu/intel-sgx-deb.key | sudo apt-key add -

# Update package list
sudo apt update

# Install SGX SDK and runtime (simulation mode)
sudo apt install -y libsgx-urts libsgx-uae-service libsgx-urts-dbgsym libsgx-uae-service-dbgsym

# Install SGX SDK development package
sudo apt install -y libsgx-headers libsgx-ae-service libsgx-aesm-service libsgx-ra-network libsgx-ra-uefi

# For hardware mode, also install:
sudo apt install -y libsgx-enclave-common libsgx-enclave-common-dev libsgx-dcap-ql libsgx-dcap-default-qpl
```

#### Linux (CentOS/RHEL)

```bash
# Add Intel SGX repository
sudo tee /etc/yum.repos.d/intel-sgx.repo > /dev/null <<EOF
[intel-sgx]
name=Intel(R) SGX Repository
baseurl=https://download.01.org/intel-sgx/sgx_repo/rhel8
enabled=1
gpgcheck=1
repo_gpgcheck=1
gpgkey=https://download.01.org/intel-sgx/sgx_repo/rhel8/intel-sgx-deb.key
EOF

# Install SGX packages
sudo yum install -y libsgx-urts libsgx-uae-service
```

#### Windows

```powershell
# Download and install Intel SGX SDK from:
# https://download.01.org/intel-sgx/sgx-windows/
# Follow the installer wizard

# Verify installation
Test-Path "C:\Program Files (x86)\Intel\sgxsdk"

# Set environment variable (if not set by installer)
[Environment]::SetEnvironmentVariable("SGX_SDK", "C:\Program Files (x86)\Intel\sgxsdk", "Machine")
```

### Environment Setup

#### Environment Variables

Create a `.env` file in your project root:

```bash
# SGX Configuration
SGX_MODE=SIM                          # SIM for simulation, HW for hardware
SGX_DEBUG=1                           # 1 for debug, 0 for release
SGX_SDK=/opt/intel/sgxsdk            # Path to SGX SDK (Linux)
# SGX_SDK=C:\Program Files (x86)\Intel\sgxsdk  # Path for Windows

# Occlum Configuration
OCCLUM_VERSION=0.29.6
OCCLUM_LOG_LEVEL=info

# Runtime Configuration
RUST_LOG=info
DOTNET_ENVIRONMENT=Production
NEO_SERVICE_TEE_MODE=enabled

# Performance Tuning
DOTNET_gcServer=1
DOTNET_ThreadPool_UnfairSemaphoreSpinLimit=6
```

#### PowerShell Profile (Windows)

```powershell
# Add to $PROFILE
$env:SGX_SDK = "C:\Program Files (x86)\Intel\sgxsdk"
$env:SGX_MODE = "SIM"
$env:SGX_DEBUG = "1"
$env:PATH += ";$env:SGX_SDK\bin\x64\Release"
```

#### Bash Profile (Linux)

```bash
# Add to ~/.bashrc or ~/.profile
export SGX_SDK=/opt/intel/sgxsdk
export SGX_MODE=SIM
export SGX_DEBUG=1
export PATH=$PATH:$SGX_SDK/bin/x64
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$SGX_SDK/lib64
```

## Configuration

### Application Configuration

#### appsettings.Production.json

```json
{
  "Enclave": {
    "SGXMode": "SIM",
    "EnableDebug": false,
    "OcclumVersion": "0.29.6",
    "Cryptography": {
      "EncryptionAlgorithm": "AES-256-GCM",
      "SigningAlgorithm": "secp256k1",
      "KeySize": 256,
      "EnableHardwareRNG": true,
      "KeyRotationInterval": "30.00:00:00"
    },
    "Storage": {
      "EnableCompression": true,
      "EnableIntegrityCheck": true,
      "MaxFileSize": 104857600,
      "EncryptionEnabled": true
    },
    "Network": {
      "MaxConnections": 100,
      "RequestTimeout": "00:00:30",
      "DomainValidation": {
        "AllowedDomains": ["api.neo.org", "rpc.neo.org"],
        "RequireHttps": true,
        "ValidateCertificate": true
      }
    },
    "JavaScript": {
      "MaxExecutionTime": "00:00:05",
      "MaxMemoryUsage": 67108864,
      "SecurityConstraints": {
        "DisallowNetworking": true,
        "DisallowFileSystem": true,
        "DisallowProcessExecution": true
      }
    },
    "Performance": {
      "EnableMetrics": true,
      "MetricsInterval": "00:01:00",
      "EnableBenchmarking": false,
      "ExpectedBaselineMs": 100
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "NeoServiceLayer.Tee": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### SGX-Specific Configuration

#### Enclave.config.xml

```xml
<EnclaveConfiguration>
  <ProdID>1</ProdID>
  <ISVSVN>1</ISVSVN>
  <StackMaxSize>0x40000</StackMaxSize>
  <HeapMaxSize>0x1000000</HeapMaxSize>
  <TCSNum>10</TCSNum>
  <TCSPolicy>1</TCSPolicy>
  <DisableDebug>0</DisableDebug>
  <MiscSelect>0</MiscSelect>
  <MiscMask>0xFFFFFFFF</MiscMask>
</EnclaveConfiguration>
```

## Deployment Scenarios

### Development Environment

#### Quick Setup Script

```powershell
# run-dev-setup.ps1
param(
    [string]$SGXMode = "SIM"
)

Write-Host "Setting up Neo Service Layer development environment..." -ForegroundColor Green

# Set environment variables
$env:SGX_MODE = $SGXMode
$env:SGX_DEBUG = "1"
$env:DOTNET_ENVIRONMENT = "Development"

# Build and run
./build-neo-service-layer.ps1 -Configuration Debug -SGX_MODE $SGXMode -Verbose

# Start development server
dotnet run --project src/Api/NeoServiceLayer.Api --configuration Debug
```

### Testing Environment

#### Continuous Integration Setup

```yaml
# .github/workflows/sgx-tests.yml
name: SGX Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  sgx-simulation-tests:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
    
    - name: Setup Rust
      uses: actions-rs/toolchain@v1
      with:
        toolchain: stable
        override: true
    
    - name: Install SGX SDK
      run: |
        echo 'deb [arch=amd64] https://download.01.org/intel-sgx/sgx_repo/ubuntu focal main' | sudo tee /etc/apt/sources.list.d/intel-sgx.list
        wget -qO - https://download.01.org/intel-sgx/sgx_repo/ubuntu/intel-sgx-deb.key | sudo apt-key add -
        sudo apt update
        sudo apt install -y libsgx-urts libsgx-uae-service
    
    - name: Build and Test
      env:
        SGX_MODE: SIM
        SGX_DEBUG: 1
      run: |
        ./build-neo-service-layer.ps1 -Configuration Release -SGX_MODE SIM
```

### Production Environment

#### Systemd Service Configuration

```ini
# /etc/systemd/system/neo-service-layer.service
[Unit]
Description=Neo Service Layer with SGX Support
After=network.target
Wants=network.target

[Service]
Type=notify
User=neoservice
Group=neoservice
WorkingDirectory=/opt/neo-service-layer
ExecStart=/usr/bin/dotnet /opt/neo-service-layer/NeoServiceLayer.Api.dll
Restart=always
RestartSec=10

# SGX Environment Variables
Environment=SGX_MODE=HW
Environment=SGX_DEBUG=0
Environment=SGX_SDK=/opt/intel/sgxsdk
Environment=DOTNET_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001

# Security Settings
NoNewPrivileges=yes
ProtectSystem=strict
ProtectHome=yes
ReadWritePaths=/opt/neo-service-layer/data
ReadWritePaths=/var/log/neo-service-layer

# Resource Limits
LimitNOFILE=65536
LimitNPROC=4096

[Install]
WantedBy=multi-user.target
```

#### Docker Production Deployment

```bash
# Build production image
docker build -t neo-service-layer:production --build-arg SGX_SDK_VERSION=2.23.100.2 .

# Run with SGX device access (hardware mode)
docker run -d \
  --name neo-service-layer \
  --device /dev/sgx_enclave \
  --device /dev/sgx_provision \
  -p 5000:5000 \
  -p 5001:5001 \
  -e SGX_MODE=HW \
  -e SGX_DEBUG=0 \
  -e DOTNET_ENVIRONMENT=Production \
  -v /opt/neo-service-layer/data:/app/data \
  -v /var/log/neo-service-layer:/var/log/neo-service-layer \
  --restart unless-stopped \
  neo-service-layer:production

# For simulation mode (no SGX devices needed)
docker run -d \
  --name neo-service-layer-sim \
  -p 5000:5000 \
  -p 5001:5001 \
  -e SGX_MODE=SIM \
  -e SGX_DEBUG=0 \
  -e DOTNET_ENVIRONMENT=Production \
  --restart unless-stopped \
  neo-service-layer:production
```

### Kubernetes Deployment

#### SGX Node Pool Configuration

```yaml
# sgx-nodepool.yaml
apiVersion: v1
kind: Node
metadata:
  name: sgx-node-1
  labels:
    sgx.intel.com/epc: "true"
    hardware.features/sgx: "true"
spec:
  capacity:
    sgx.intel.com/epc: 128Mi
  allocatable:
    sgx.intel.com/epc: 128Mi
```

#### SGX Deployment Manifest

```yaml
# sgx-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: neo-service-layer-sgx
  namespace: neo-system
spec:
  replicas: 3
  selector:
    matchLabels:
      app: neo-service-layer
      mode: sgx
  template:
    metadata:
      labels:
        app: neo-service-layer
        mode: sgx
    spec:
      nodeSelector:
        sgx.intel.com/epc: "true"
      containers:
      - name: neo-service-layer
        image: neo-service-layer:production
        ports:
        - containerPort: 5000
        - containerPort: 5001
        env:
        - name: SGX_MODE
          value: "HW"
        - name: SGX_DEBUG
          value: "0"
        - name: DOTNET_ENVIRONMENT
          value: "Production"
        resources:
          requests:
            sgx.intel.com/epc: 64Mi
            memory: 2Gi
            cpu: 500m
          limits:
            sgx.intel.com/epc: 128Mi
            memory: 4Gi
            cpu: 2000m
        volumeMounts:
        - name: sgx-devices
          mountPath: /dev/sgx_enclave
        - name: sgx-provision
          mountPath: /dev/sgx_provision
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 60
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
      volumes:
      - name: sgx-devices
        hostPath:
          path: /dev/sgx_enclave
      - name: sgx-provision
        hostPath:
          path: /dev/sgx_provision
```

## Security Considerations

### Hardware Mode Security

#### BIOS/UEFI Configuration

1. **Enable SGX Support**
   - Navigate to Security settings in BIOS/UEFI
   - Enable "Intel SGX" or "Software Guard Extensions"
   - Set SGX memory size (minimum 128MB recommended)

2. **Secure Boot Configuration**
   - Enable Secure Boot for additional security
   - Verify boot chain integrity

#### Platform Attestation

```csharp
// Verify platform attestation before sensitive operations
public async Task<bool> VerifyPlatformSecurityAsync()
{
    try
    {
        var attestation = await _enclave.GetAttestationAsync();
        
        // Verify attestation signature
        var isValidSignature = await VerifyAttestationSignatureAsync(attestation);
        
        // Check platform security version
        var isValidPSV = attestation.PlatformSecurityVersion >= MinimumPSV;
        
        // Verify enclave measurement
        var isValidMeasurement = attestation.EnclaveHash.SequenceEqual(ExpectedEnclaveHash);
        
        return isValidSignature && isValidPSV && isValidMeasurement;
    }
    catch (AttestationException ex)
    {
        _logger.LogError(ex, "Platform attestation failed");
        return false;
    }
}
```

### Network Security

#### Firewall Configuration

```bash
# UFW (Ubuntu)
sudo ufw allow 5000/tcp   # HTTP
sudo ufw allow 5001/tcp   # HTTPS
sudo ufw allow 9090/tcp   # Metrics (restrict to monitoring subnet)

# iptables
iptables -A INPUT -p tcp --dport 5000 -j ACCEPT
iptables -A INPUT -p tcp --dport 5001 -j ACCEPT
iptables -A INPUT -p tcp --dport 9090 -s 10.0.0.0/8 -j ACCEPT
```

#### TLS Configuration

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "Https": {
        "Url": "https://localhost:5001",
        "Certificate": {
          "Path": "/etc/ssl/certs/neo-service-layer.pfx",
          "Password": "${CERT_PASSWORD}"
        }
      }
    }
  }
}
```

## Monitoring and Observability

### Health Checks

#### Custom SGX Health Check

```csharp
public class SGXHealthCheck : IHealthCheck
{
    private readonly IEnclaveWrapper _enclave;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test basic enclave functionality
            var testData = new byte[] { 1, 2, 3, 4 };
            var sealed = await _enclave.SealDataAsync(testData, "health-check");
            var unsealed = await _enclave.UnsealDataAsync(sealed, "health-check");
            
            if (!testData.SequenceEqual(unsealed))
            {
                return HealthCheckResult.Unhealthy("Enclave seal/unseal test failed");
            }

            // Verify attestation (hardware mode only)
            if (Environment.GetEnvironmentVariable("SGX_MODE") == "HW")
            {
                var attestation = await _enclave.GetAttestationAsync();
                if (attestation == null || attestation.Quote.Length == 0)
                {
                    return HealthCheckResult.Degraded("Attestation not available");
                }
            }

            return HealthCheckResult.Healthy("SGX enclave is functioning correctly");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SGX enclave health check failed", ex);
        }
    }
}
```

### Metrics Collection

#### Prometheus Metrics

```csharp
// Custom SGX metrics
public class SGXMetrics
{
    private static readonly Counter SealOperations = Metrics
        .CreateCounter("sgx_seal_operations_total", "Total number of seal operations");
    
    private static readonly Histogram SealDuration = Metrics
        .CreateHistogram("sgx_seal_duration_seconds", "Duration of seal operations");
    
    private static readonly Gauge EnclaveMemoryUsage = Metrics
        .CreateGauge("sgx_enclave_memory_bytes", "Enclave memory usage");

    public static void RecordSealOperation(double durationSeconds)
    {
        SealOperations.Inc();
        SealDuration.Observe(durationSeconds);
    }
}
```

## Troubleshooting

### Common Issues

#### SGX SDK Not Found

**Symptoms**: Build fails with "SGX SDK not found" error

**Solution**:
```bash
# Verify SGX SDK installation
ls -la /opt/intel/sgxsdk  # Linux
dir "C:\Program Files (x86)\Intel\sgxsdk"  # Windows

# Set environment variable
export SGX_SDK=/opt/intel/sgxsdk  # Linux
$env:SGX_SDK="C:\Program Files (x86)\Intel\sgxsdk"  # Windows
```

#### Enclave Initialization Failed

**Symptoms**: Runtime error "Failed to initialize enclave"

**Solution**:
```bash
# Check SGX driver status (Linux)
ls -la /dev/sgx*
dmesg | grep sgx

# Verify AESM service status
systemctl status aesmd
sudo systemctl start aesmd

# For simulation mode, ensure correct environment
export SGX_MODE=SIM
```

#### Performance Issues

**Symptoms**: Slow enclave operations

**Solution**:
```csharp
// Use batch operations
var sealTasks = data.Select(d => _enclave.SealDataAsync(d, keyId)).ToArray();
var results = await Task.WhenAll(sealTasks);

// Enable parallel processing
services.Configure<EnclaveConfig>(config => 
{
    config.Performance.MaxConcurrentOperations = Environment.ProcessorCount;
});
```

### Diagnostic Commands

```bash
# Check SGX capabilities
cpuid | grep -i sgx

# Verify SGX driver
modinfo intel_sgx

# Test SGX functionality
sgx-detect  # If available

# Check enclave status
ps aux | grep aesm
sudo journalctl -u aesmd -f
```

## Performance Optimization

### Configuration Tuning

#### Production Optimizations

```json
{
  "Enclave": {
    "Performance": {
      "EnableMetrics": true,
      "MaxConcurrentOperations": 8,
      "BufferPoolSize": 1048576,
      "EnableCaching": true,
      "CacheSize": 67108864
    },
    "Memory": {
      "StackSize": 262144,
      "HeapSize": 16777216,
      "EnableGarbageCollection": true
    }
  }
}
```

#### Thread Pool Configuration

```csharp
// Configure thread pool for SGX workloads
ThreadPool.SetMinThreads(
    workerThreads: Environment.ProcessorCount * 2,
    completionPortThreads: Environment.ProcessorCount
);

ThreadPool.SetMaxThreads(
    workerThreads: Environment.ProcessorCount * 4,
    completionPortThreads: Environment.ProcessorCount * 2
);
```

### Load Testing

```bash
# Use Apache Bench for basic load testing
ab -n 1000 -c 10 http://localhost:5000/api/enclave/health

# Use custom load test for enclave operations
dotnet test tests/Load/ --configuration Release --logger "console;verbosity=normal"
```

## Backup and Recovery

### Key Management

```bash
# Backup sealed keys (production)
sudo cp -r /opt/neo-service-layer/data/sealed-keys /backup/sealed-keys-$(date +%Y%m%d)

# Verify backup integrity
sha256sum /backup/sealed-keys-*/key_* > /backup/checksums.txt
```

### Disaster Recovery

```csharp
public async Task<bool> RestoreFromBackupAsync(string backupPath)
{
    try
    {
        // Validate backup integrity
        var isValid = await ValidateBackupIntegrityAsync(backupPath);
        if (!isValid) return false;

        // Re-initialize enclave
        await _enclave.InitializeAsync(_config);

        // Restore sealed data
        await RestoreSealedDataAsync(backupPath);

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Backup restoration failed");
        return false;
    }
}
```

## Related Documentation

- [Occlum LibOS Guide](occlum-libos-guide.md)
- [TEE Troubleshooting Guide](../troubleshooting/tee-troubleshooting.md)
- [Security Architecture](../security/tee-security-architecture.md)
- [API Reference](../api/tee-api-reference.md) 