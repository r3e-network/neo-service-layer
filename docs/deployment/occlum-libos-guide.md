# Occlum LibOS Deployment Guide

## Overview

Occlum is a memory-safe, multi-process Library OS (LibOS) for Intel SGX that enables running unmodified applications inside SGX enclaves. This guide covers deploying Neo Service Layer with Occlum LibOS integration for enhanced security and Linux application compatibility.

## What is Occlum LibOS?

Occlum LibOS provides:
- **Memory Safety**: Rust-based implementation with memory protection
- **Multi-Process Support**: Run multiple processes within a single enclave
- **POSIX Compatibility**: Standard Linux API support for existing applications
- **File System**: Encrypted in-memory and persistent file systems
- **Networking**: Secure networking capabilities within enclaves
- **Container Support**: Docker and Kubernetes integration

## Prerequisites

### System Requirements

#### Minimum Requirements
- Intel SGX-capable processor (6th generation Core or newer)
- 8GB RAM (16GB recommended for production)
- 50GB available disk space
- Ubuntu 20.04+ or CentOS 8+ (other distributions may work)

#### SGX Requirements
- Intel SGX SDK 2.23.100.2 or later
- Intel SGX PSW (Platform Software)
- SGX driver (kernel module or DCAP driver)

### Development Dependencies

```bash
# Essential tools
sudo apt update
sudo apt install -y \
    build-essential \
    wget \
    curl \
    git \
    cmake \
    ninja-build \
    python3 \
    python3-pip \
    pkg-config \
    libssl-dev \
    libprotobuf-dev \
    protobuf-compiler

# Rust toolchain (required for Occlum)
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
source ~/.cargo/env

# Install nightly Rust (required for some Occlum features)
rustup install nightly
rustup component add rust-src --toolchain nightly
```

## Installation

### Intel SGX Setup

#### Install SGX SDK and PSW

```bash
# Add Intel SGX repository
echo 'deb [arch=amd64] https://download.01.org/intel-sgx/sgx_repo/ubuntu focal main' | sudo tee /etc/apt/sources.list.d/intel-sgx.list
wget -qO - https://download.01.org/intel-sgx/sgx_repo/ubuntu/intel-sgx-deb.key | sudo apt-key add -

sudo apt update

# Install SGX SDK and runtime
sudo apt install -y \
    libsgx-urts \
    libsgx-uae-service \
    libsgx-ae-service \
    libsgx-aesm-service \
    libsgx-headers \
    libsgx-enclave-common \
    libsgx-enclave-common-dev \
    libsgx-dcap-ql \
    libsgx-dcap-default-qpl

# Start AESM service
sudo systemctl enable aesmd
sudo systemctl start aesmd
```

### Occlum Installation

#### Option 1: Install from Pre-built Packages

```bash
# Download Occlum release
OCCLUM_VERSION="0.29.6"
wget https://github.com/occlum/occlum/releases/download/${OCCLUM_VERSION}/occlum_${OCCLUM_VERSION}-1_amd64.deb

# Install Occlum
sudo dpkg -i occlum_${OCCLUM_VERSION}-1_amd64.deb
sudo apt-get install -f  # Fix any dependency issues

# Verify installation
occlum version
```

#### Option 2: Build from Source

```bash
# Clone Occlum repository
git clone https://github.com/occlum/occlum.git --branch v0.29.6
cd occlum

# Install dependencies
sudo apt install -y \
    alien \
    autoconf \
    automake \
    bison \
    debhelper \
    expect \
    flex \
    gdb \
    git-lfs \
    golang-go \
    jq \
    libbinutils \
    libcurl4-openssl-dev \
    libfuse-dev \
    libjsoncpp-dev \
    libnuma1 \
    libnuma-dev \
    libprotobuf17 \
    libtool \
    musl-tools \
    python \
    python3-pip \
    uuid-dev \
    vim

# Build and install
make submodule
OCCLUM_RELEASE_BUILD=1 make
sudo make install

# Add to PATH
echo 'export PATH=/opt/occlum/build/bin:$PATH' >> ~/.bashrc
source ~/.bashrc
```

### Docker Integration

#### Install Docker with Occlum Support

```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Add user to docker group
sudo usermod -aG docker $USER
newgrp docker

# Pull Occlum development image
docker pull occlum/occlum:0.29.6-ubuntu20.04
```

## Configuration

### Occlum Configuration File

Create `Occlum.json` in your project root:

```json
{
    "resource_limits": {
        "user_space_size": "1GB",
        "user_space_max_size": "4GB",
        "kernel_space_heap_size": "64MB",
        "kernel_space_heap_max_size": "256MB",
        "kernel_space_stack_size": "8MB",
        "max_num_of_threads": 64
    },
    "process": {
        "default_stack_size": "8MB",
        "default_heap_size": "64MB",
        "default_mmap_size": "256MB"
    },
    "entry_points": [
        "/bin/dotnet"
    ],
    "env": {
        "default": [
            "DOTNET_ENVIRONMENT=Production",
            "ASPNETCORE_URLS=http://localhost:5000",
            "SGX_MODE=HW",
            "OCCLUM_LIBOS=1"
        ]
    },
    "metadata": {
        "product_id": 1,
        "version_number": 1,
        "debuggable": false
    },
    "mount": [
        {
            "target": "/",
            "type": "unionfs",
            "options": {
                "layers": [
                    {
                        "target": "/",
                        "type": "sefs",
                        "source": "./image",
                        "options": {
                            "integrity_only": false
                        }
                    }
                ]
            }
        },
        {
            "target": "/host",
            "type": "hostfs",
            "source": "./host"
        },
        {
            "target": "/tmp",
            "type": "ramfs"
        }
    ]
}
```

### Rust Enclave Configuration

#### Cargo.toml for Occlum Integration

```toml
[package]
name = "neo-service-enclave"
version = "0.1.0"
edition = "2021"

[lib]
name = "neo_service_enclave"
crate-type = ["cdylib", "staticlib"]

[dependencies]
# Core Occlum dependencies
occlum-pal = "0.29.6"
occlum-dcap = "0.29.6"

# Cryptography
ring = "0.17"
secp256k1 = { version = "0.28", features = ["recovery", "rand"] }
aes-gcm = "0.10"
sha2 = "0.10"
hmac = "0.12"

# Networking and HTTP
reqwest = { version = "0.11", features = ["json", "rustls-tls"], default-features = false }
tokio = { version = "1.0", features = ["full"] }
rustls = "0.22"
webpki = "0.22"

# Serialization
serde = { version = "1.0", features = ["derive"] }
serde_json = "1.0"
bincode = "1.3"

# File system and storage
sefs = "0.29.6"
compression = "0.1"

# JavaScript execution
rusty_v8 = "0.78"

# Logging and error handling
log = "0.4"
env_logger = "0.10"
anyhow = "1.0"
thiserror = "1.0"

# Memory management
libc = "0.2"
once_cell = "1.19"

[features]
default = ["simulation"]
simulation = []
hardware = []
debug = []

[profile.release]
lto = true
codegen-units = 1
panic = "abort"
opt-level = 3

[profile.dev]
panic = "abort"
```

### Environment Setup

#### Development Environment

```bash
# Create development environment script
cat > setup-occlum-dev.sh << 'EOF'
#!/bin/bash
set -e

# Set Occlum environment variables
export OCCLUM_VERSION="0.29.6"
export OCCLUM_LOG_LEVEL="info"
export SGX_MODE="SIM"  # Use "HW" for hardware mode
export SGX_DEBUG="1"

# Set PATH for Occlum tools
export PATH=/opt/occlum/build/bin:$PATH

# Create Occlum workspace
if [ ! -d "occlum_workspace" ]; then
    mkdir occlum_workspace
    cd occlum_workspace
    occlum init
    cd ..
fi

echo "Occlum development environment ready!"
echo "SGX Mode: $SGX_MODE"
echo "Occlum Version: $OCCLUM_VERSION"
EOF

chmod +x setup-occlum-dev.sh
./setup-occlum-dev.sh
```

## Building Applications with Occlum

### Rust Enclave Implementation

#### src/lib.rs - Main Library

```rust
//! Neo Service Layer Occlum LibOS Integration
//! Provides secure enclave operations using Occlum LibOS

use std::ffi::{CStr, CString};
use std::os::raw::{c_char, c_int, c_void};
use std::slice;
use std::ptr;

use occlum_pal::*;
use serde::{Deserialize, Serialize};
use ring::aead;
use secp256k1::{Secp256k1, SecretKey, PublicKey};

#[derive(Serialize, Deserialize)]
pub struct EnclaveRequest {
    pub operation: String,
    pub data: Vec<u8>,
    pub key_id: String,
}

#[derive(Serialize, Deserialize)]
pub struct EnclaveResponse {
    pub success: bool,
    pub data: Vec<u8>,
    pub error_message: Option<String>,
}

/// Initialize the Occlum LibOS enclave
#[no_mangle]
pub extern "C" fn enclave_init() -> c_int {
    env_logger::init();
    log::info!("Initializing Neo Service Enclave with Occlum LibOS");
    
    // Initialize Occlum PAL
    match occlum_pal::init() {
        Ok(_) => {
            log::info!("Occlum PAL initialized successfully");
            0
        }
        Err(e) => {
            log::error!("Failed to initialize Occlum PAL: {:?}", e);
            -1
        }
    }
}

/// Seal data using SGX sealing capabilities
#[no_mangle]
pub extern "C" fn enclave_seal_data(
    data: *const u8,
    data_len: usize,
    key_id: *const c_char,
    sealed_data: *mut *mut u8,
    sealed_len: *mut usize,
) -> c_int {
    if data.is_null() || key_id.is_null() || sealed_data.is_null() || sealed_len.is_null() {
        return -1;
    }

    let data_slice = unsafe { slice::from_raw_parts(data, data_len) };
    let key_id_str = unsafe {
        match CStr::from_ptr(key_id).to_str() {
            Ok(s) => s,
            Err(_) => return -1,
        }
    };

    match seal_data_internal(data_slice, key_id_str) {
        Ok(sealed) => {
            let sealed_ptr = sealed.as_ptr() as *mut u8;
            let sealed_size = sealed.len();
            
            // Allocate memory for sealed data
            let allocated = unsafe { libc::malloc(sealed_size) as *mut u8 };
            if allocated.is_null() {
                return -1;
            }
            
            unsafe {
                ptr::copy_nonoverlapping(sealed_ptr, allocated, sealed_size);
                *sealed_data = allocated;
                *sealed_len = sealed_size;
            }
            
            0
        }
        Err(e) => {
            log::error!("Failed to seal data: {:?}", e);
            -1
        }
    }
}

/// Unseal data using SGX unsealing capabilities
#[no_mangle]
pub extern "C" fn enclave_unseal_data(
    sealed_data: *const u8,
    sealed_len: usize,
    key_id: *const c_char,
    data: *mut *mut u8,
    data_len: *mut usize,
) -> c_int {
    if sealed_data.is_null() || key_id.is_null() || data.is_null() || data_len.is_null() {
        return -1;
    }

    let sealed_slice = unsafe { slice::from_raw_parts(sealed_data, sealed_len) };
    let key_id_str = unsafe {
        match CStr::from_ptr(key_id).to_str() {
            Ok(s) => s,
            Err(_) => return -1,
        }
    };

    match unseal_data_internal(sealed_slice, key_id_str) {
        Ok(unsealed) => {
            let unsealed_ptr = unsealed.as_ptr() as *mut u8;
            let unsealed_size = unsealed.len();
            
            // Allocate memory for unsealed data
            let allocated = unsafe { libc::malloc(unsealed_size) as *mut u8 };
            if allocated.is_null() {
                return -1;
            }
            
            unsafe {
                ptr::copy_nonoverlapping(unsealed_ptr, allocated, unsealed_size);
                *data = allocated;
                *data_len = unsealed_size;
            }
            
            0
        }
        Err(e) => {
            log::error!("Failed to unseal data: {:?}", e);
            -1
        }
    }
}

/// Execute secure HTTP request
#[no_mangle]
pub extern "C" fn enclave_secure_http_request(
    url: *const c_char,
    method: *const c_char,
    headers: *const c_char,
    body: *const u8,
    body_len: usize,
    response: *mut *mut u8,
    response_len: *mut usize,
) -> c_int {
    if url.is_null() || method.is_null() || response.is_null() || response_len.is_null() {
        return -1;
    }

    let url_str = unsafe {
        match CStr::from_ptr(url).to_str() {
            Ok(s) => s,
            Err(_) => return -1,
        }
    };

    let method_str = unsafe {
        match CStr::from_ptr(method).to_str() {
            Ok(s) => s,
            Err(_) => return -1,
        }
    };

    let headers_str = if headers.is_null() {
        ""
    } else {
        unsafe {
            match CStr::from_ptr(headers).to_str() {
                Ok(s) => s,
                Err(_) => return -1,
            }
        }
    };

    let body_data = if body.is_null() || body_len == 0 {
        Vec::new()
    } else {
        unsafe { slice::from_raw_parts(body, body_len).to_vec() }
    };

    match execute_http_request(url_str, method_str, headers_str, &body_data) {
        Ok(response_data) => {
            let response_ptr = response_data.as_ptr() as *mut u8;
            let response_size = response_data.len();
            
            // Allocate memory for response
            let allocated = unsafe { libc::malloc(response_size) as *mut u8 };
            if allocated.is_null() {
                return -1;
            }
            
            unsafe {
                ptr::copy_nonoverlapping(response_ptr, allocated, response_size);
                *response = allocated;
                *response_len = response_size;
            }
            
            0
        }
        Err(e) => {
            log::error!("HTTP request failed: {:?}", e);
            -1
        }
    }
}

// Internal implementation functions

fn seal_data_internal(data: &[u8], key_id: &str) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
    // Use AES-256-GCM for encryption
    let key = derive_key(key_id)?;
    let nonce = aead::Nonce::assume_unique_for_key([0u8; 12]); // In production, use random nonce
    
    let sealing_key = aead::LessSafeKey::new(
        aead::UnboundKey::new(&aead::AES_256_GCM, &key)?
    );
    
    let mut in_out = data.to_vec();
    let tag = sealing_key.seal_in_place_separate_tag(nonce, aead::Aad::empty(), &mut in_out)?;
    
    // Combine nonce, tag, and encrypted data
    let mut sealed = Vec::new();
    sealed.extend_from_slice(nonce.as_ref());
    sealed.extend_from_slice(tag.as_ref());
    sealed.extend_from_slice(&in_out);
    
    Ok(sealed)
}

fn unseal_data_internal(sealed_data: &[u8], key_id: &str) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
    if sealed_data.len() < 12 + 16 { // nonce + tag minimum
        return Err("Invalid sealed data length".into());
    }
    
    let key = derive_key(key_id)?;
    let nonce = aead::Nonce::try_assume_unique_for_key(&sealed_data[0..12])?;
    let tag = &sealed_data[12..28];
    let encrypted_data = &sealed_data[28..];
    
    let opening_key = aead::LessSafeKey::new(
        aead::UnboundKey::new(&aead::AES_256_GCM, &key)?
    );
    
    let mut in_out = encrypted_data.to_vec();
    let tag = aead::Tag::try_from(tag)?;
    
    opening_key.open_in_place(nonce, aead::Aad::empty(), &mut in_out, tag)?;
    
    Ok(in_out)
}

fn derive_key(key_id: &str) -> Result<[u8; 32], Box<dyn std::error::Error>> {
    // In production, use SGX key derivation functions
    use ring::digest;
    
    let digest = digest::digest(&digest::SHA256, key_id.as_bytes());
    let mut key = [0u8; 32];
    key.copy_from_slice(digest.as_ref());
    Ok(key)
}

async fn execute_http_request(
    url: &str,
    method: &str,
    headers: &str,
    body: &[u8],
) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
    let client = reqwest::Client::builder()
        .use_rustls_tls()
        .build()?;
    
    let mut request = match method.to_uppercase().as_str() {
        "GET" => client.get(url),
        "POST" => client.post(url),
        "PUT" => client.put(url),
        "DELETE" => client.delete(url),
        _ => return Err("Unsupported HTTP method".into()),
    };
    
    // Parse headers
    if !headers.is_empty() {
        for header_line in headers.lines() {
            if let Some((key, value)) = header_line.split_once(':') {
                request = request.header(key.trim(), value.trim());
            }
        }
    }
    
    // Add body if provided
    if !body.is_empty() {
        request = request.body(body.to_vec());
    }
    
    let response = request.send().await?;
    let response_bytes = response.bytes().await?;
    
    Ok(response_bytes.to_vec())
}

/// Free allocated memory
#[no_mangle]
pub extern "C" fn enclave_free(ptr: *mut c_void) {
    if !ptr.is_null() {
        unsafe {
            libc::free(ptr);
        }
    }
}
```

### Building the Enclave

#### Build Script

```bash
#!/bin/bash
# build-occlum-enclave.sh
set -e

echo "Building Neo Service Enclave with Occlum LibOS..."

# Set environment variables
export SGX_MODE=${SGX_MODE:-SIM}
export SGX_DEBUG=${SGX_DEBUG:-1}
export OCCLUM_VERSION=${OCCLUM_VERSION:-0.29.6}

# Create Occlum workspace
WORKSPACE_DIR="occlum_workspace"
if [ ! -d "$WORKSPACE_DIR" ]; then
    echo "Creating Occlum workspace..."
    mkdir -p "$WORKSPACE_DIR"
    cd "$WORKSPACE_DIR"
    occlum init
    cd ..
fi

# Build Rust enclave library
echo "Building Rust enclave library..."
if [ "$SGX_MODE" == "HW" ]; then
    cargo build --release --features hardware
else
    cargo build --release --features simulation
fi

# Copy built library to Occlum workspace
cp target/release/libneo_service_enclave.so "$WORKSPACE_DIR/image/lib/"

# Copy .NET application (if building .NET components)
if [ -d "../../../src/Api/NeoServiceLayer.Api/bin/Release/net9.0/publish" ]; then
    echo "Copying .NET application..."
    cp -r ../../../src/Api/NeoServiceLayer.Api/bin/Release/net9.0/publish/* "$WORKSPACE_DIR/image/"
fi

# Build Occlum image
cd "$WORKSPACE_DIR"
echo "Building Occlum LibOS image..."
occlum build

echo "Occlum enclave build completed successfully!"
echo "SGX Mode: $SGX_MODE"
echo "Enclave location: $(pwd)/build/lib/libocclum-libos.signed.so"
```

### Docker Integration

#### Dockerfile.occlum

```dockerfile
# Multi-stage build for Occlum LibOS applications
FROM occlum/occlum:0.29.6-ubuntu20.04 AS builder

# Install additional dependencies
RUN apt-get update && apt-get install -y \
    wget \
    curl \
    git \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

# Install .NET SDK
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y dotnet-sdk-9.0

# Install Rust
RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y
ENV PATH="/root/.cargo/bin:${PATH}"

# Set working directory
WORKDIR /workspace

# Copy source code
COPY . .

# Build Rust enclave
RUN cd src && cargo build --release

# Build .NET application
RUN dotnet publish src/Api/NeoServiceLayer.Api -c Release -o /app/publish

# Create Occlum workspace and build
RUN mkdir occlum_workspace && cd occlum_workspace && occlum init
RUN cp target/release/libneo_service_enclave.so occlum_workspace/image/lib/
RUN cp -r /app/publish/* occlum_workspace/image/
RUN cd occlum_workspace && occlum build

# Runtime stage
FROM occlum/occlum:0.29.6-ubuntu20.04

# Copy built Occlum application
COPY --from=builder /workspace/occlum_workspace /app

# Set working directory
WORKDIR /app

# Expose ports
EXPOSE 5000 5001

# Set environment variables
ENV SGX_MODE=SIM
ENV OCCLUM_LOG_LEVEL=info

# Run Occlum application
CMD ["occlum", "run", "/bin/dotnet", "NeoServiceLayer.Api.dll"]
```

## Deployment

### Local Development Deployment

```bash
# Build and run locally
./build-occlum-enclave.sh

# Run in Occlum LibOS
cd occlum_workspace
occlum run /bin/dotnet NeoServiceLayer.Api.dll
```

### Docker Deployment

```bash
# Build Docker image
docker build -f Dockerfile.occlum -t neo-service-occlum:latest .

# Run with SGX device access (hardware mode)
docker run -d \
  --name neo-service-occlum \
  --device /dev/sgx_enclave \
  --device /dev/sgx_provision \
  -p 5000:5000 \
  -p 5001:5001 \
  -e SGX_MODE=HW \
  -e OCCLUM_LOG_LEVEL=info \
  --restart unless-stopped \
  neo-service-occlum:latest

# Run in simulation mode (no SGX devices needed)
docker run -d \
  --name neo-service-occlum-sim \
  -p 5000:5000 \
  -p 5001:5001 \
  -e SGX_MODE=SIM \
  -e OCCLUM_LOG_LEVEL=debug \
  --restart unless-stopped \
  neo-service-occlum:latest
```

### Kubernetes Deployment

#### Occlum LibOS Pod Specification

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: neo-service-occlum
  labels:
    app: neo-service-layer
    runtime: occlum
spec:
  nodeSelector:
    sgx.intel.com/epc: "true"
  containers:
  - name: neo-service-occlum
    image: neo-service-occlum:latest
    ports:
    - containerPort: 5000
      name: http
    - containerPort: 5001
      name: https
    env:
    - name: SGX_MODE
      value: "HW"
    - name: OCCLUM_LOG_LEVEL
      value: "info"
    - name: DOTNET_ENVIRONMENT
      value: "Production"
    resources:
      requests:
        sgx.intel.com/epc: 128Mi
        memory: 4Gi
        cpu: 1000m
      limits:
        sgx.intel.com/epc: 256Mi
        memory: 8Gi
        cpu: 2000m
    volumeMounts:
    - name: sgx-devices
      mountPath: /dev/sgx_enclave
    - name: sgx-provision
      mountPath: /dev/sgx_provision
    - name: occlum-data
      mountPath: /app/data
    livenessProbe:
      httpGet:
        path: /health
        port: 5000
      initialDelaySeconds: 120
      periodSeconds: 30
      timeoutSeconds: 10
    readinessProbe:
      httpGet:
        path: /health/ready
        port: 5000
      initialDelaySeconds: 60
      periodSeconds: 10
      timeoutSeconds: 5
  volumes:
  - name: sgx-devices
    hostPath:
      path: /dev/sgx_enclave
  - name: sgx-provision
    hostPath:
      path: /dev/sgx_provision
  - name: occlum-data
    persistentVolumeClaim:
      claimName: occlum-data-pvc
```

## Monitoring and Debugging

### Logging Configuration

```bash
# Enable detailed Occlum logging
export OCCLUM_LOG_LEVEL=debug
export RUST_LOG=debug

# Run with verbose output
occlum run --verbose /bin/dotnet NeoServiceLayer.Api.dll
```

### Performance Monitoring

```rust
// Add performance metrics to Rust code
use std::time::Instant;

pub fn measure_operation<F, R>(name: &str, operation: F) -> R
where
    F: FnOnce() -> R,
{
    let start = Instant::now();
    let result = operation();
    let duration = start.elapsed();
    
    log::info!("Operation '{}' completed in {:?}", name, duration);
    result
}

// Usage example
let sealed_data = measure_operation("seal_data", || {
    seal_data_internal(data, key_id)
});
```

### Health Checks

```bash
# Check Occlum enclave status
occlum status

# Validate enclave integrity
occlum verify

# Check SGX device availability
ls -la /dev/sgx*

# Monitor system resources
top -p $(pgrep occlum)
```

## Troubleshooting

### Common Issues

#### Occlum Initialization Failed

**Symptoms**: "Failed to initialize Occlum PAL" error

**Solutions**:
```bash
# Check SGX driver status
dmesg | grep sgx
lsmod | grep intel_sgx

# Verify Occlum installation
occlum version
which occlum

# Check permissions
ls -la /dev/sgx*
sudo chmod 666 /dev/sgx_enclave /dev/sgx_provision
```

#### Memory Allocation Errors

**Symptoms**: "Out of memory" or segmentation faults

**Solutions**:
```json
// Increase memory limits in Occlum.json
{
    "resource_limits": {
        "user_space_size": "2GB",
        "user_space_max_size": "8GB",
        "kernel_space_heap_size": "256MB",
        "kernel_space_heap_max_size": "1GB"
    }
}
```

#### File System Errors

**Symptoms**: "No such file or directory" for files that exist

**Solutions**:
```bash
# Check file system mounting
occlum mount

# Verify file permissions in image
ls -la occlum_workspace/image/

# Rebuild Occlum image
cd occlum_workspace
occlum build --force
```

### Debugging Tools

#### GDB Integration

```bash
# Enable debug mode
export SGX_DEBUG=1

# Run with GDB
gdb --args occlum run /bin/dotnet NeoServiceLayer.Api.dll
```

#### Strace Analysis

```bash
# Trace system calls
strace -f occlum run /bin/dotnet NeoServiceLayer.Api.dll
```

## Security Considerations

### Secure Configuration

```json
{
    "metadata": {
        "debuggable": false,
        "product_id": 1,
        "version_number": 1
    },
    "env": {
        "default": [
            "DOTNET_ENVIRONMENT=Production",
            "ASPNETCORE_URLS=https://localhost:5001",
            "SGX_MODE=HW"
        ]
    }
}
```

### Production Hardening

```bash
# Disable debug mode
export SGX_DEBUG=0

# Use hardware mode
export SGX_MODE=HW

# Enable integrity protection
# Configure in Occlum.json
```

## Performance Optimization

### Configuration Tuning

```json
{
    "resource_limits": {
        "user_space_size": "4GB",
        "max_num_of_threads": 128
    },
    "process": {
        "default_heap_size": "256MB",
        "default_mmap_size": "1GB"
    }
}
```

### Memory Management

```rust
// Use memory pools for frequent allocations
use std::sync::Mutex;
use std::collections::VecDeque;

lazy_static::lazy_static! {
    static ref BUFFER_POOL: Mutex<VecDeque<Vec<u8>>> = Mutex::new(VecDeque::new());
}

pub fn get_buffer(size: usize) -> Vec<u8> {
    let mut pool = BUFFER_POOL.lock().unwrap();
    pool.pop_front().unwrap_or_else(|| Vec::with_capacity(size))
}

pub fn return_buffer(mut buffer: Vec<u8>) {
    buffer.clear();
    let mut pool = BUFFER_POOL.lock().unwrap();
    if pool.len() < 10 { // Limit pool size
        pool.push_back(buffer);
    }
}
```

## Related Documentation

- [SGX Deployment Guide](sgx-deployment-guide.md)
- [TEE Enclave Service](../services/tee-enclave-service.md)
- [TEE Troubleshooting Guide](../troubleshooting/tee-troubleshooting.md)
- [Security Architecture](../security/tee-security-architecture.md) 