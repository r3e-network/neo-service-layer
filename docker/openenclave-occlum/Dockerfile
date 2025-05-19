# Base image with Ubuntu 20.04
FROM ubuntu:20.04 AS base

# Avoid prompts from apt
ENV DEBIAN_FRONTEND=noninteractive

# Install common dependencies
RUN apt-get update && apt-get install -y \
    build-essential \
    cmake \
    wget \
    git \
    pkg-config \
    apt-transport-https \
    curl \
    gnupg \
    lsb-release \
    software-properties-common \
    libssl-dev \
    libcurl4-openssl-dev \
    libprotobuf-dev \
    protobuf-compiler \
    libboost-all-dev \
    python3 \
    python3-pip \
    ninja-build \
    && rm -rf /var/lib/apt/lists/*

# Install .NET SDK
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y dotnet-sdk-9.0 \
    && rm -rf /var/lib/apt/lists/*

# Install Intel SGX dependencies
RUN apt-get update && apt-get install -y \
    libsgx-enclave-common \
    libsgx-enclave-common-dev \
    libsgx-dcap-ql \
    libsgx-dcap-ql-dev \
    libsgx-quote-ex \
    libsgx-quote-ex-dev \
    libsgx-urts \
    libsgx-uae-service \
    && rm -rf /var/lib/apt/lists/*

# Install Open Enclave SDK dependencies
RUN apt-get update && apt-get install -y \
    clang-11 \
    llvm-11 \
    && rm -rf /var/lib/apt/lists/*

# Build Open Enclave SDK from source
FROM base AS openenclave-builder

WORKDIR /opt
RUN git clone --recursive https://github.com/openenclave/openenclave.git
WORKDIR /opt/openenclave
RUN git checkout v0.19.3
RUN mkdir build
WORKDIR /opt/openenclave/build
RUN cmake -DCMAKE_INSTALL_PREFIX=/opt/openenclave/install -DHAS_QUOTE_PROVIDER=OFF -DBUILD_TESTS=OFF ..
RUN make -j$(nproc)
RUN make install

# Build Occlum from source
FROM base AS occlum-builder

# Install Rust
RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y
ENV PATH="/root/.cargo/bin:${PATH}"

# Install Occlum dependencies
RUN apt-get update && apt-get install -y \
    libseccomp-dev \
    libsgx-dcap-default-qpl \
    && rm -rf /var/lib/apt/lists/*

# Clone and build Occlum
WORKDIR /opt
RUN git clone https://github.com/occlum/occlum.git
WORKDIR /opt/occlum
RUN git checkout v0.30.0

# Apply patch to support Open Enclave
COPY patches/occlum-openenclave.patch /opt/occlum/
RUN git apply /opt/occlum/occlum-openenclave.patch

# Build Occlum
ENV OCCLUM_RELEASE_BUILD=1
RUN cd build && make
RUN make install

# Install Node.js
WORKDIR /opt
RUN wget https://nodejs.org/dist/v16.20.2/node-v16.20.2-linux-x64.tar.xz
RUN tar -xf node-v16.20.2-linux-x64.tar.xz
RUN mv node-v16.20.2-linux-x64 node
ENV PATH="/opt/node/bin:${PATH}"

# Final image
FROM base

# Copy Open Enclave SDK from builder
COPY --from=openenclave-builder /opt/openenclave/install /opt/openenclave/install

# Copy Occlum from builder
COPY --from=occlum-builder /opt/occlum/build/install /opt/occlum
COPY --from=occlum-builder /opt/node /opt/node

# Set environment variables
ENV PATH="/opt/occlum/bin:/opt/node/bin:/opt/openenclave/install/bin:${PATH}"
ENV PKG_CONFIG_PATH="/opt/openenclave/install/share/pkgconfig:${PKG_CONFIG_PATH}"
ENV CMAKE_PREFIX_PATH="/opt/openenclave/install/lib/openenclave/cmake:${CMAKE_PREFIX_PATH}"
ENV LD_LIBRARY_PATH="/opt/openenclave/install/lib:/opt/openenclave/install/lib/openenclave:${LD_LIBRARY_PATH}"
ENV OCCLUM_PATH="/opt/occlum"

# Create a working directory
WORKDIR /app

# Copy the Neo Service Layer source code
COPY . .

# Build the Neo Service Layer
RUN dotnet build

# Create an Occlum instance
RUN mkdir -p occlum_instance
WORKDIR /app/occlum_instance
RUN occlum init
RUN mkdir -p image/bin image/lib image/etc image/node_modules

# Copy Node.js and dependencies to the Occlum instance
RUN cp /opt/node/bin/node image/bin/
RUN cp -r /opt/node/lib/* image/lib/
RUN cp /etc/hosts image/etc/
RUN cp /etc/resolv.conf image/etc/

# Build the Occlum instance
RUN occlum build

# Build the enclave
WORKDIR /app
RUN mkdir -p build
WORKDIR /app/build
RUN cmake .. -DOCCLUM_SUPPORT=ON -DOE_SIMULATION=1
RUN make

# Set the working directory back to the app root
WORKDIR /app

# Expose the API port
EXPOSE 5000

# Set the entry point
ENTRYPOINT ["dotnet", "run", "--project", "src/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj", "--urls", "http://0.0.0.0:5000"]
