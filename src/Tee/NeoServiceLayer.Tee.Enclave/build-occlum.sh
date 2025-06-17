#!/bin/bash
# Build script for Neo Service Layer Occlum LibOS Application
# This script builds a production-ready Occlum enclave with the Neo Service Layer

set -e

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
BUILD_DIR="$PROJECT_ROOT/build/occlum"
OCCLUM_APP_DIR="$BUILD_DIR/neo-service-enclave"

# Build configuration
BUILD_TYPE="${BUILD_TYPE:-Release}"
OCCLUM_VERSION="${OCCLUM_VERSION:-0.29.6}"
SGX_MODE="${SGX_MODE:-HW}"
ENCLAVE_SIZE="${ENCLAVE_SIZE:-2GB}"
THREAD_NUM="${THREAD_NUM:-32}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if running on supported platform
    if [[ "$OSTYPE" != "linux-gnu"* ]]; then
        log_error "Occlum LibOS is only supported on Linux"
        exit 1
    fi
    
    # Check for SGX support
    if [ "$SGX_MODE" = "HW" ]; then
        if [ ! -e /dev/sgx_enclave ]; then
            log_warning "SGX device not found. Switching to simulation mode."
            SGX_MODE="SIM"
        fi
    fi
    
    # Check for Occlum installation
    if ! command -v occlum >/dev/null 2>&1; then
        log_error "Occlum is not installed. Please install Occlum LibOS first."
        log_info "Installation guide: https://github.com/occlum/occlum#how-to-install"
        exit 1
    fi
    
    # Check for .NET SDK
    if ! command -v dotnet >/dev/null 2>&1; then
        log_error ".NET SDK is not installed. Please install .NET 9.0 SDK."
        exit 1
    fi
    
    # Check for Rust (optional for native components)
    if ! command -v cargo >/dev/null 2>&1; then
        log_warning "Rust is not installed. Native components will be skipped."
    fi
    
    log_success "Prerequisites check completed"
}

# Clean previous builds
clean_build() {
    log_info "Cleaning previous builds..."
    
    if [ -d "$BUILD_DIR" ]; then
        rm -rf "$BUILD_DIR"
    fi
    
    # Clean .NET artifacts
    cd "$PROJECT_ROOT"
    dotnet clean -c "$BUILD_TYPE" --nologo
    
    log_success "Build directory cleaned"
}

# Build .NET components
build_dotnet_components() {
    log_info "Building .NET components..."
    
    cd "$PROJECT_ROOT"
    
    # Restore dependencies
    log_info "Restoring .NET dependencies..."
    dotnet restore --nologo
    
    # Build the solution
    log_info "Building .NET solution..."
    dotnet build -c "$BUILD_TYPE" --no-restore --nologo
    
    # Publish the enclave project for self-contained deployment
    log_info "Publishing enclave application..."
    dotnet publish src/Tee/NeoServiceLayer.Tee.Enclave/NeoServiceLayer.Tee.Enclave.csproj \
        -c "$BUILD_TYPE" \
        --no-restore \
        --nologo \
        -o "$BUILD_DIR/dotnet-app" \
        --self-contained false \
        --use-current-runtime
    
    log_success ".NET components built successfully"
}

# Build native components (if Rust is available)
build_native_components() {
    if command -v cargo >/dev/null 2>&1; then
        log_info "Building native Rust components..."
        
        if [ -d "$PROJECT_ROOT/rust-components" ]; then
            cd "$PROJECT_ROOT/rust-components"
            cargo build --release
            
            # Copy native libraries to build directory
            mkdir -p "$BUILD_DIR/native-libs"
            cp target/release/*.so "$BUILD_DIR/native-libs/" 2>/dev/null || true
            
            log_success "Native components built successfully"
        else
            log_info "No native Rust components found, skipping..."
        fi
    else
        log_info "Rust not available, skipping native components..."
    fi
}

# Initialize Occlum application
init_occlum_app() {
    log_info "Initializing Occlum application..."
    
    mkdir -p "$BUILD_DIR"
    cd "$BUILD_DIR"
    
    # Initialize Occlum application
    occlum init neo-service-enclave
    cd neo-service-enclave
    
    log_success "Occlum application initialized"
}

# Configure Occlum application
configure_occlum() {
    log_info "Configuring Occlum application..."
    
    cd "$OCCLUM_APP_DIR"
    
    # Copy configuration file
    if [ -f "$SCRIPT_DIR/Occlum.json" ]; then
        cp "$SCRIPT_DIR/Occlum.json" .
        log_info "Using custom Occlum configuration"
    else
        log_info "Using default Occlum configuration"
    fi
    
    # Update configuration based on build parameters
    jq --arg size "$ENCLAVE_SIZE" '.sgx.enclave_size = $size' Occlum.json > tmp.json && mv tmp.json Occlum.json
    jq --arg threads "$THREAD_NUM" '.sgx.thread_num = ($threads | tonumber)' Occlum.json > tmp.json && mv tmp.json Occlum.json
    
    # Set debug mode based on build type
    if [ "$BUILD_TYPE" = "Debug" ]; then
        jq '.sgx.debug = true' Occlum.json > tmp.json && mv tmp.json Occlum.json
        jq '.development.allow_debug = true' Occlum.json > tmp.json && mv tmp.json Occlum.json
    else
        jq '.sgx.debug = false' Occlum.json > tmp.json && mv tmp.json Occlum.json
        jq '.development.allow_debug = false' Occlum.json > tmp.json && mv tmp.json Occlum.json
    fi
    
    log_success "Occlum configuration updated"
}

# Prepare Occlum image
prepare_occlum_image() {
    log_info "Preparing Occlum image..."
    
    cd "$OCCLUM_APP_DIR"
    
    # Create directory structure
    mkdir -p image/opt/neo-service-layer/bin
    mkdir -p image/opt/neo-service-layer/lib
    mkdir -p image/data/secure
    mkdir -p image/data/logs
    mkdir -p image/tmp
    
    # Copy .NET application
    log_info "Copying .NET application files..."
    cp -r "$BUILD_DIR/dotnet-app"/* image/opt/neo-service-layer/bin/
    
    # Copy native libraries if available
    if [ -d "$BUILD_DIR/native-libs" ]; then
        log_info "Copying native libraries..."
        cp -r "$BUILD_DIR/native-libs"/* image/opt/neo-service-layer/lib/
    fi
    
    # Copy system libraries
    log_info "Copying system libraries..."
    copy_system_libs() {
        local lib_dir="image/lib"
        mkdir -p "$lib_dir"
        
        # Essential libraries for .NET runtime
        local libs=(
            "/lib/x86_64-linux-gnu/libdl.so.2"
            "/lib/x86_64-linux-gnu/libc.so.6"
            "/lib/x86_64-linux-gnu/libm.so.6"
            "/lib/x86_64-linux-gnu/libpthread.so.0"
            "/lib/x86_64-linux-gnu/librt.so.1"
            "/lib/x86_64-linux-gnu/libssl.so.1.1"
            "/lib/x86_64-linux-gnu/libcrypto.so.1.1"
        )
        
        for lib in "${libs[@]}"; do
            if [ -f "$lib" ]; then
                cp "$lib" "$lib_dir/"
            else
                log_warning "Library not found: $lib"
            fi
        done
        
        # Copy dynamic linker
        mkdir -p image/lib64
        if [ -f "/lib64/ld-linux-x86-64.so.2" ]; then
            cp "/lib64/ld-linux-x86-64.so.2" image/lib64/
        fi
    }
    
    copy_system_libs
    
    # Create entry point script
    log_info "Creating entry point script..."
    cat > image/opt/neo-service-layer/bin/entrypoint.sh << 'EOF'
#!/bin/bash
set -e

# Initialize logging
mkdir -p /data/logs
exec > >(tee -a /data/logs/neo-service.log)
exec 2>&1

echo "Starting Neo Service Layer Enclave $(date)"
echo "Build Type: $BUILD_TYPE"
echo "SGX Mode: $SGX_MODE"

# Set environment variables
export DOTNET_RUNNING_IN_CONTAINER=true
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Verify required files
echo "Verifying application files..."
if [ ! -f "/opt/neo-service-layer/bin/NeoServiceLayer.Tee.Enclave.dll" ]; then
    echo "ERROR: Application DLL not found"
    exit 1
fi

# Start the Neo Service Layer application
echo "Starting Neo Service Layer application..."
cd /opt/neo-service-layer/bin

# Use exec to replace the shell with the application
exec dotnet NeoServiceLayer.Tee.Enclave.dll "$@"
EOF
    
    chmod +x image/opt/neo-service-layer/bin/entrypoint.sh
    
    # Set proper permissions
    chmod 700 image/data/secure
    chmod 755 image/data/logs
    chmod 1777 image/tmp
    
    log_success "Occlum image prepared"
}

# Build Occlum enclave
build_occlum_enclave() {
    log_info "Building Occlum enclave..."
    
    cd "$OCCLUM_APP_DIR"
    
    # Build the enclave
    log_info "Compiling enclave..."
    occlum build
    
    # Sign the enclave
    log_info "Signing enclave..."
    if [ -f "/opt/occlum/etc/template/Enclave.pem" ]; then
        occlum package --sign-key /opt/occlum/etc/template/Enclave.pem
    else
        log_warning "Default signing key not found, using occlum package without explicit key"
        occlum package
    fi
    
    log_success "Occlum enclave built and signed successfully"
}

# Generate deployment artifacts
generate_deployment_artifacts() {
    log_info "Generating deployment artifacts..."
    
    cd "$BUILD_DIR"
    
    # Create deployment package
    local deployment_dir="deployment"
    mkdir -p "$deployment_dir"
    
    # Copy built enclave
    cp -r neo-service-enclave "$deployment_dir/"
    
    # Create deployment scripts
    cat > "$deployment_dir/run.sh" << EOF
#!/bin/bash
# Neo Service Layer Occlum Deployment Script

set -e

SCRIPT_DIR="\$(cd "\$(dirname "\${BASH_SOURCE[0]}")" && pwd)"
cd "\$SCRIPT_DIR/neo-service-enclave"

# Check SGX availability
if [ "$SGX_MODE" = "HW" ] && [ ! -e /dev/sgx_enclave ]; then
    echo "ERROR: SGX hardware not available"
    exit 1
fi

# Run the enclave
echo "Starting Neo Service Layer Enclave..."
echo "SGX Mode: $SGX_MODE"
echo "Build Type: $BUILD_TYPE"

exec occlum run /opt/neo-service-layer/bin/entrypoint.sh "\$@"
EOF
    
    chmod +x "$deployment_dir/run.sh"
    
    # Create configuration template
    cat > "$deployment_dir/config.env" << EOF
# Neo Service Layer Occlum Configuration
# Copy this file and customize for your deployment

# SGX Configuration
SGX_MODE=$SGX_MODE
ENCLAVE_SIZE=$ENCLAVE_SIZE
THREAD_NUM=$THREAD_NUM

# Application Configuration
BUILD_TYPE=$BUILD_TYPE
LOG_LEVEL=info
NEO_SERVICE_MODE=production

# Network Configuration (adjust as needed)
# LISTEN_PORT=8080
# HTTPS_PORT=8443

# Storage Configuration (adjust paths as needed)
# DATA_DIR=/var/lib/neo-service
# LOG_DIR=/var/log/neo-service
EOF
    
    # Create Docker Compose file for easy deployment
    cat > "$deployment_dir/docker-compose.yml" << EOF
version: '3.8'

services:
  neo-service-enclave:
    build:
      context: ../..
      dockerfile: src/Tee/NeoServiceLayer.Tee.Enclave/Dockerfile.occlum
    container_name: neo-service-enclave
    restart: unless-stopped
    
    # SGX device access
    devices:
      - /dev/sgx_enclave:/dev/sgx_enclave
      - /dev/sgx_provision:/dev/sgx_provision
    
    # Environment variables
    environment:
      - SGX_MODE=$SGX_MODE
      - NEO_SERVICE_MODE=production
      - OCCLUM_LOG_LEVEL=info
    
    # Port mapping (adjust as needed)
    ports:
      - "8080:8080"
      - "8443:8443"
    
    # Volume mounts
    volumes:
      - neo-service-data:/var/lib/neo-service
      - neo-service-logs:/var/log/neo-service
    
    # Security settings
    security_opt:
      - no-new-privileges:true
    
    # Resource limits
    mem_limit: 4g
    memswap_limit: 4g
    
    # Health check
    healthcheck:
      test: ["/usr/local/bin/health-check.sh"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s

volumes:
  neo-service-data:
    driver: local
  neo-service-logs:
    driver: local
EOF
    
    # Create README for deployment
    cat > "$deployment_dir/README.md" << EOF
# Neo Service Layer Occlum Deployment

This directory contains the built Neo Service Layer Occlum enclave and deployment artifacts.

## Files

- \`neo-service-enclave/\` - Built Occlum enclave application
- \`run.sh\` - Simple deployment script
- \`config.env\` - Configuration template
- \`docker-compose.yml\` - Docker Compose deployment configuration

## Prerequisites

- Linux system with SGX support (for hardware mode)
- Occlum LibOS runtime installed
- SGX driver and PSW installed (for hardware mode)

## Quick Start

### Direct Execution
\`\`\`bash
./run.sh
\`\`\`

### Docker Deployment
\`\`\`bash
docker-compose up -d
\`\`\`

## Configuration

Copy \`config.env\` to customize your deployment:
\`\`\`bash
cp config.env .env
# Edit .env with your configuration
\`\`\`

## Build Information

- Build Type: $BUILD_TYPE
- SGX Mode: $SGX_MODE
- Enclave Size: $ENCLAVE_SIZE
- Thread Count: $THREAD_NUM
- Build Date: $(date)
- Occlum Version: $OCCLUM_VERSION
EOF
    
    log_success "Deployment artifacts generated in $deployment_dir"
}

# Main build function
main() {
    local start_time=$(date +%s)
    
    log_info "Starting Neo Service Layer Occlum build..."
    log_info "Configuration: Build=$BUILD_TYPE, SGX=$SGX_MODE, Enclave=$ENCLAVE_SIZE, Threads=$THREAD_NUM"
    
    check_prerequisites
    clean_build
    build_dotnet_components
    build_native_components
    init_occlum_app
    configure_occlum
    prepare_occlum_image
    build_occlum_enclave
    generate_deployment_artifacts
    
    local end_time=$(date +%s)
    local build_time=$((end_time - start_time))
    
    log_success "Build completed successfully in ${build_time} seconds"
    log_info "Deployment artifacts available in: $BUILD_DIR/deployment"
    log_info "To run the enclave: cd $BUILD_DIR/deployment && ./run.sh"
}

# Handle script arguments
case "${1:-}" in
    "clean")
        clean_build
        ;;
    "check")
        check_prerequisites
        ;;
    "help"|"-h"|"--help")
        cat << EOF
Neo Service Layer Occlum Build Script

Usage: $0 [command]

Commands:
  (no args)  Full build process
  clean      Clean previous builds
  check      Check prerequisites only
  help       Show this help message

Environment Variables:
  BUILD_TYPE     Build configuration (Debug|Release) [default: Release]
  SGX_MODE       SGX mode (HW|SIM) [default: HW]
  ENCLAVE_SIZE   Enclave size [default: 2GB]
  THREAD_NUM     Number of threads [default: 32]

Examples:
  $0                              # Full build with defaults
  BUILD_TYPE=Debug $0             # Debug build
  SGX_MODE=SIM $0                 # Simulation mode build
  ENCLAVE_SIZE=4GB THREAD_NUM=64 $0  # Custom resource limits
EOF
        ;;
    *)
        main
        ;;
esac 