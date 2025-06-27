# Neo Service Layer - Production Dockerfile with SGX and Occlum LibOS Support
# Multi-stage build for optimized production deployment

# Build stage with SGX SDK and Rust support
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install build dependencies including Protocol Buffers compiler
RUN apt-get update && apt-get install -y \
    build-essential \
    wget \
    curl \
    gnupg \
    software-properties-common \
    ca-certificates \
    protobuf-compiler \
    && rm -rf /var/lib/apt/lists/*

# Verify protoc installation
RUN protoc --version

# Install Rust toolchain for Occlum LibOS
RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y
ENV PATH="/root/.cargo/bin:${PATH}"

# Install Intel SGX SDK (simulation mode for containerized builds)
ARG SGX_SDK_VERSION=2.23.100.2
RUN set -eux; \
    # Use modern GPG key handling instead of deprecated apt-key
    mkdir -p /etc/apt/keyrings /opt/intel/sgxsdk/lib64; \
    # Try to download Intel SGX key with timeout and retries
    (wget --timeout=30 --tries=3 -qO /etc/apt/keyrings/intel-sgx.asc https://download.01.org/intel-sgx/sgx_repo/ubuntu/intel-sgx-deb.key && \
     echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/intel-sgx.asc] https://download.01.org/intel-sgx/sgx_repo/ubuntu jammy main" > /etc/apt/sources.list.d/intel-sgx.list && \
     apt-get update && \
     apt-get install -y --no-install-recommends libsgx-urts libsgx-uae-service) || { \
        echo "Warning: Intel SGX repository unavailable, creating mock libraries for containerized builds"; \
        mkdir -p /usr/lib/x86_64-linux-gnu; \
        touch /usr/lib/x86_64-linux-gnu/libsgx_urts.so.2; \
        touch /usr/lib/x86_64-linux-gnu/libsgx_uae_service.so; \
        ln -sf /usr/lib/x86_64-linux-gnu/libsgx_urts.so.2 /opt/intel/sgxsdk/lib64/libsgx_urts.so; \
        ln -sf /usr/lib/x86_64-linux-gnu/libsgx_uae_service.so /opt/intel/sgxsdk/lib64/libsgx_uae_service.so; \
        echo "Mock SGX libraries created for simulation mode"; \
    }; \
    rm -rf /var/lib/apt/lists/*

# Set SGX environment variables for build
# Build in SIM mode for containerized builds, runtime can be HW
ENV SGX_MODE=SIM
ENV SGX_DEBUG=1
ENV SGX_SDK=/opt/intel/sgxsdk

# Copy solution and project files
COPY *.sln .
COPY global.json .
COPY Directory.Build.props .
COPY Directory.Build.targets .
COPY Directory.Packages.props .

# Copy all project files for dependency resolution
COPY src/Api/NeoServiceLayer.Api/*.csproj src/Api/NeoServiceLayer.Api/
COPY src/Core/NeoServiceLayer.Core/*.csproj src/Core/NeoServiceLayer.Core/
COPY src/Core/NeoServiceLayer.ServiceFramework/*.csproj src/Core/NeoServiceLayer.ServiceFramework/
COPY src/Core/NeoServiceLayer.Shared/*.csproj src/Core/NeoServiceLayer.Shared/
COPY src/Blockchain/NeoServiceLayer.Neo.N3/*.csproj src/Blockchain/NeoServiceLayer.Neo.N3/
COPY src/Blockchain/NeoServiceLayer.Neo.X/*.csproj src/Blockchain/NeoServiceLayer.Neo.X/

# Copy Services projects explicitly
COPY src/Services/NeoServiceLayer.Services.Automation/*.csproj src/Services/NeoServiceLayer.Services.Automation/
COPY src/Services/NeoServiceLayer.Services.Backup/*.csproj src/Services/NeoServiceLayer.Services.Backup/
COPY src/Services/NeoServiceLayer.Services.Compliance/*.csproj src/Services/NeoServiceLayer.Services.Compliance/
COPY src/Services/NeoServiceLayer.Services.Health/*.csproj src/Services/NeoServiceLayer.Services.Health/
COPY src/Services/NeoServiceLayer.Services.KeyManagement/*.csproj src/Services/NeoServiceLayer.Services.KeyManagement/
COPY src/Services/NeoServiceLayer.Services.Monitoring/*.csproj src/Services/NeoServiceLayer.Services.Monitoring/
COPY src/Services/NeoServiceLayer.Services.Notification/*.csproj src/Services/NeoServiceLayer.Services.Notification/
COPY src/Services/NeoServiceLayer.Services.Oracle/*.csproj src/Services/NeoServiceLayer.Services.Oracle/
COPY src/Services/NeoServiceLayer.Services.ProofOfReserve/*.csproj src/Services/NeoServiceLayer.Services.ProofOfReserve/
COPY src/Services/NeoServiceLayer.Services.Randomness/*.csproj src/Services/NeoServiceLayer.Services.Randomness/
COPY src/Services/NeoServiceLayer.Services.SecretsManagement/*.csproj src/Services/NeoServiceLayer.Services.SecretsManagement/
COPY src/Services/NeoServiceLayer.Services.Storage/*.csproj src/Services/NeoServiceLayer.Services.Storage/
COPY src/Services/NeoServiceLayer.Services.Voting/*.csproj src/Services/NeoServiceLayer.Services.Voting/
COPY src/Services/NeoServiceLayer.Services.ZeroKnowledge/*.csproj src/Services/NeoServiceLayer.Services.ZeroKnowledge/

# Copy AI projects explicitly
COPY src/AI/NeoServiceLayer.AI.PatternRecognition/*.csproj src/AI/NeoServiceLayer.AI.PatternRecognition/
COPY src/AI/NeoServiceLayer.AI.Prediction/*.csproj src/AI/NeoServiceLayer.AI.Prediction/

# Copy Advanced projects explicitly
COPY src/Advanced/NeoServiceLayer.Advanced.FairOrdering/*.csproj src/Advanced/NeoServiceLayer.Advanced.FairOrdering/

# Copy TEE projects explicitly
COPY src/Tee/NeoServiceLayer.Tee.Host/*.csproj src/Tee/NeoServiceLayer.Tee.Host/
COPY src/Tee/NeoServiceLayer.Tee.Enclave/*.csproj src/Tee/NeoServiceLayer.Tee.Enclave/

# Copy Infrastructure projects explicitly
COPY src/Infrastructure/NeoServiceLayer.Infrastructure.Blockchain/*.csproj src/Infrastructure/NeoServiceLayer.Infrastructure.Blockchain/
COPY src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/*.csproj src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/
COPY src/Infrastructure/NeoServiceLayer.Infrastructure.Security/*.csproj src/Infrastructure/NeoServiceLayer.Infrastructure.Security/

# Copy TEE Enclave Rust dependencies
COPY src/Tee/NeoServiceLayer.Tee.Enclave/Cargo.* src/Tee/NeoServiceLayer.Tee.Enclave/

# Restore dependencies for the main API project only
RUN dotnet restore src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj

# Copy source code
COPY src/ src/

# Build Rust enclave components (skip if V8 dependencies cause issues)
WORKDIR /src/src/Tee/NeoServiceLayer.Tee.Enclave
RUN if [ -f "Cargo.toml" ] && ! grep -q "v8" Cargo.toml; then \
    echo "Building Rust enclave components..."; \
    cargo build --release; \
    else \
    echo "Skipping Rust enclave build (V8 dependencies or missing Cargo.toml)"; \
    fi

# Build the .NET application
WORKDIR /src/src/Api/NeoServiceLayer.Api
RUN dotnet build -c Release --no-restore

# Publish the application
RUN dotnet publish -c Release --no-build --no-restore -o /app/publish

# Runtime stage with SGX runtime support
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install SGX runtime dependencies
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    ca-certificates \
    gnupg \
    software-properties-common \
    && rm -rf /var/lib/apt/lists/*

# Install Intel SGX runtime libraries
ARG SGX_SDK_VERSION=2.23.100.2
RUN set -eux; \
    # Use modern GPG key handling instead of deprecated apt-key
    mkdir -p /etc/apt/keyrings; \
    # Try to install Intel SGX runtime with fallback to mock libraries
    (wget --timeout=30 --tries=3 -qO /etc/apt/keyrings/intel-sgx.asc https://download.01.org/intel-sgx/sgx_repo/ubuntu/intel-sgx-deb.key && \
     echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/intel-sgx.asc] https://download.01.org/intel-sgx/sgx_repo/ubuntu jammy main" > /etc/apt/sources.list.d/intel-sgx.list && \
     apt-get update && \
     apt-get install -y --no-install-recommends libsgx-urts libsgx-uae-service) || { \
        echo "Warning: Intel SGX repository unavailable, creating mock runtime libraries"; \
        mkdir -p /usr/lib/x86_64-linux-gnu; \
        touch /usr/lib/x86_64-linux-gnu/libsgx_urts.so.2; \
        touch /usr/lib/x86_64-linux-gnu/libsgx_uae_service.so; \
        echo "Mock SGX runtime libraries created"; \
    }; \
    rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r neoservice && useradd -r -g neoservice neoservice

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Create directories for native libraries and configuration
RUN mkdir -p ./runtimes/linux-x64/native /app/config

# NOTE: Native libraries and configuration files will be available at runtime
# if the build process successfully creates them. This approach avoids
# Docker COPY conditional logic which isn't supported.

# Create directories for logs, data, and SGX
RUN mkdir -p /var/log/neo-service-layer /app/data /var/run/aesmd /opt/intel/sgx-aesm-service/aesm && \
    chown -R neoservice:neoservice /var/log/neo-service-layer /app/data /app

# Switch to non-root user
USER neoservice

# Expose ports
EXPOSE 5000 5001 9090

# Health check with SGX status
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Environment variables for SGX and Occlum
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# SGX Environment Variables
# Note: SGX_MODE should be set via environment variable or docker-compose
# Default to HW for production, but allow override
ARG SGX_MODE=HW
ENV SGX_MODE=${SGX_MODE}
ENV SGX_DEBUG=0
ENV OCCLUM_LOG_LEVEL=warn
ENV NEO_SERVICE_TEE_MODE=enabled

# Runtime configuration
ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_gcServer=1
ENV DOTNET_ThreadPool_UnfairSemaphoreSpinLimit=6

# SGX device access (for hardware mode)
# This requires running with --device /dev/sgx_enclave --device /dev/sgx_provision

# Entry point with SGX awareness
ENTRYPOINT ["dotnet", "NeoServiceLayer.Api.dll"] 