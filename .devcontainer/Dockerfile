# Neo Service Layer Development Container
# Ubuntu 24.04 + SGX SDK + Occlum LibOS + .NET 9.0 + Rust
FROM ubuntu:24.04

# Avoid prompts from apt
ENV DEBIAN_FRONTEND=noninteractive

# Set timezone
ENV TZ=UTC
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# Create vscode user early
RUN groupadd --gid 1000 vscode \
    && useradd --uid 1000 --gid vscode --shell /bin/bash --create-home vscode \
    && echo "vscode ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers

# Install base packages and build tools
RUN apt-get update && apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release \
    software-properties-common \
    wget \
    build-essential \
    cmake \
    make \
    git \
    vim \
    nano \
    htop \
    tree \
    jq \
    unzip \
    zip \
    tar \
    gzip \
    pkg-config \
    libssl-dev \
    libprotobuf-dev \
    protobuf-compiler \
    libc6-dev \
    libncurses5-dev \
    libffi-dev \
    python3 \
    python3-pip \
    python3-dev \
    nodejs \
    npm \
    autoconf \
    automake \
    libtool \
    nasm \
    yasm \
    flex \
    bison \
    gdb \
    valgrind \
    strace \
    ltrace \
    && rm -rf /var/lib/apt/lists/*

# Install .NET 9.0 SDK
RUN wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y dotnet-sdk-9.0 aspnetcore-runtime-9.0 \
    && rm -rf /var/lib/apt/lists/*

# Install Rust and Cargo
USER vscode
RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y \
    && echo 'source /home/vscode/.cargo/env' >> /home/vscode/.bashrc
ENV PATH="/home/vscode/.cargo/bin:${PATH}"
RUN /home/vscode/.cargo/bin/rustup default stable \
    && /home/vscode/.cargo/bin/rustup component add rustfmt clippy \
    && /home/vscode/.cargo/bin/cargo install cargo-audit cargo-watch

USER root

# Intel SGX SDK Installation
RUN apt-get update && apt-get install -y \
    libprotobuf23 \
    libprotobuf-dev \
    alien \
    kmod \
    && rm -rf /var/lib/apt/lists/*

# Download and install Intel SGX SDK
WORKDIR /tmp
RUN wget https://download.01.org/intel-sgx/sgx-linux/2.23/distro/ubuntu24.04-server/sgx_linux_x64_sdk_2.23.100.2.bin \
    && chmod +x sgx_linux_x64_sdk_2.23.100.2.bin \
    && echo -e 'no\n/opt/intel' | ./sgx_linux_x64_sdk_2.23.100.2.bin \
    && rm sgx_linux_x64_sdk_2.23.100.2.bin \
    && ln -sf /opt/intel/sgxsdk /opt/intel/sgxsdk

# Install SGX driver and platform software (simulation mode)
RUN apt-get update && apt-get install -y \
    libsgx-launch \
    libsgx-urts \
    libsgx-epid \
    libsgx-quote-ex \
    libsgx-dcap-ql \
    sgx-aesm-service \
    && rm -rf /var/lib/apt/lists/*

# Install additional SGX development packages
RUN apt-get update && apt-get install -y \
    libsgx-headers \
    libsgx-ae-le \
    libsgx-ae-pce \
    libsgx-enclave-common-dev \
    libsgx-dcap-ql-dev \
    libsgx-dcap-default-qpl-dev \
    && rm -rf /var/lib/apt/lists/*

# Occlum LibOS Installation
WORKDIR /tmp

# Install Occlum dependencies
RUN apt-get update && apt-get install -y \
    python3-pyelftools \
    python3-cryptography \
    python3-toml \
    python3-jinja2 \
    python3-click \
    && rm -rf /var/lib/apt/lists/*

# Install Intel MPX (required for Occlum)
RUN apt-get update && apt-get install -y \
    gcc-multilib \
    libc6-dev-i386 \
    && rm -rf /var/lib/apt/lists/*

# Download and install Occlum
RUN git clone --recursive https://github.com/occlum/occlum.git /opt/occlum \
    && cd /opt/occlum \
    && git checkout v0.29.6 \
    && git submodule update --init --recursive

WORKDIR /opt/occlum

# Build Occlum LibOS
RUN make submodule || true \
    && OCCLUM_RELEASE_BUILD=1 make install || echo "Occlum build failed - continuing with simulation mode" \
    && echo 'source /opt/occlum/build/bin/occlum_bashrc' >> /etc/bash.bashrc || true

# Install additional Rust targets needed for SGX
USER vscode
RUN /home/vscode/.cargo/bin/rustup target add x86_64-unknown-linux-musl \
    && /home/vscode/.cargo/bin/rustup component add rust-src

# Install Fortanix EDP utilities for SGX
RUN /home/vscode/.cargo/bin/cargo install fortanix-sgx-tools sgxs-tools

USER root

# Install Protocol Buffers compiler (latest version)
WORKDIR /tmp
RUN PB_REL="https://github.com/protocolbuffers/protobuf/releases" \
    && curl -LO $PB_REL/download/v25.1/protoc-25.1-linux-x86_64.zip \
    && unzip protoc-25.1-linux-x86_64.zip -d /usr/local \
    && chmod +x /usr/local/bin/protoc \
    && rm protoc-25.1-linux-x86_64.zip

# Install additional development tools
RUN npm install -g typescript @types/node ts-node nodemon

# Install Docker Compose
RUN curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose \
    && chmod +x /usr/local/bin/docker-compose

# Create directories for development
RUN mkdir -p /opt/intel/sgxsdk \
    && mkdir -p /workspace \
    && chown -R vscode:vscode /workspace

# Set up environment variables
ENV SGX_SDK=/opt/intel/sgxsdk
ENV SGX_MODE=SIM
ENV PATH="/opt/intel/sgxsdk/bin/x64:/home/vscode/.cargo/bin:/home/vscode/.dotnet:/home/vscode/.dotnet/tools:${PATH}"
ENV LD_LIBRARY_PATH="/opt/intel/sgxsdk/sdk_libs:/opt/occlum/build/lib:${LD_LIBRARY_PATH}"
ENV PKG_CONFIG_PATH="/opt/intel/sgxsdk/pkgconfig:${PKG_CONFIG_PATH}"

# Source SGX environment
RUN echo 'source /opt/intel/sgxsdk/environment' >> /etc/bash.bashrc \
    && echo 'source /opt/occlum/build/bin/occlum_bashrc' >> /etc/bash.bashrc

# Fix permissions for vscode user
RUN chown -R vscode:vscode /home/vscode \
    && chmod -R 755 /home/vscode/.cargo \
    && mkdir -p /home/vscode/.dotnet \
    && chown -R vscode:vscode /home/vscode/.dotnet

# Install dotnet global tools as vscode user
USER vscode
WORKDIR /home/vscode

# Install useful .NET global tools
RUN dotnet tool install -g dotnet-ef \
    && dotnet tool install -g dotnet-aspnet-codegenerator \
    && dotnet tool install -g dotnet-dev-certs \
    && dotnet tool install -g dotnet-watch \
    && dotnet tool install -g Microsoft.Web.LibraryManager.Cli

# Generate development certificates
RUN dotnet dev-certs https --trust

# Set up Rust environment for vscode user
ENV CARGO_HOME=/home/vscode/.cargo
ENV RUSTUP_HOME=/home/vscode/.rustup
ENV PATH="/home/vscode/.cargo/bin:${PATH}"

# Verify installations
RUN dotnet --version \
    && rustc --version \
    && cargo --version \
    && protoc --version \
    && node --version \
    && npm --version

USER root

# Clean up
RUN apt-get autoremove -y \
    && apt-get autoclean \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf /tmp/*

# Set working directory
WORKDIR /workspace

# Switch to vscode user
USER vscode

# Set default command
CMD ["/bin/bash"] 