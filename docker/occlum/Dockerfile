FROM ubuntu:20.04

# Set environment variables
ENV DEBIAN_FRONTEND=noninteractive
ENV PATH="/opt/occlum/build/bin:/usr/local/occlum/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"
ENV LD_LIBRARY_PATH="/opt/occlum/build/lib:/usr/local/occlum/lib"
ENV OCCLUM_VERSION=0.29.5

# Install dependencies
RUN apt-get update && apt-get install -y \
    build-essential \
    cmake \
    git \
    libssl-dev \
    libcurl4-openssl-dev \
    libprotobuf-dev \
    protobuf-compiler \
    pkg-config \
    wget \
    curl \
    gnupg \
    lsb-release \
    software-properties-common \
    python3 \
    python3-pip \
    unzip \
    vim \
    jq \
    libmbedtls-dev \
    nlohmann-json3-dev \
    && rm -rf /var/lib/apt/lists/*

# Install Node.js
RUN curl -fsSL https://deb.nodesource.com/setup_16.x | bash - \
    && apt-get install -y nodejs \
    && npm install -g npm@latest

# Install Intel SGX SDK
RUN wget https://download.01.org/intel-sgx/sgx-linux/2.15.1/distro/ubuntu20.04-server/sgx_linux_x64_sdk_2.15.101.1.bin \
    && chmod +x sgx_linux_x64_sdk_2.15.101.1.bin \
    && echo "yes" | ./sgx_linux_x64_sdk_2.15.101.1.bin \
    && rm sgx_linux_x64_sdk_2.15.101.1.bin \
    && . /opt/intel/sgxsdk/environment

# Install Occlum
RUN wget https://github.com/occlum/occlum/releases/download/${OCCLUM_VERSION}/occlum-${OCCLUM_VERSION}-ubuntu20.04-x86_64.tar.gz \
    && tar -xzf occlum-${OCCLUM_VERSION}-ubuntu20.04-x86_64.tar.gz \
    && cd occlum-${OCCLUM_VERSION} \
    && ./install.sh \
    && cd .. \
    && rm -rf occlum-${OCCLUM_VERSION} occlum-${OCCLUM_VERSION}-ubuntu20.04-x86_64.tar.gz

# Install .NET SDK
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y dotnet-sdk-9.0 \
    && rm -rf /var/lib/apt/lists/*

# Create Occlum instance directory
RUN mkdir -p /occlum_instance

# Set up Occlum instance
WORKDIR /occlum_instance
RUN occlum init \
    && mkdir -p image/bin image/lib image/etc image/node_modules image/tmp \
    && cp $(which node) image/bin/ \
    && cp -r /usr/lib/x86_64-linux-gnu/libnode* image/lib/ \
    && cp /etc/hosts image/etc/ \
    && cp /etc/resolv.conf image/etc/

# Install Node.js modules
WORKDIR /tmp
RUN npm init -y \
    && npm install fs-extra crypto-js uuid axios \
    && cp -r node_modules/* /occlum_instance/image/node_modules/

# Configure Occlum instance
WORKDIR /occlum_instance
RUN echo '{\n\
  "resource_limits": {\n\
    "user_space_size": "1GB",\n\
    "kernel_space_heap_size": "64MB",\n\
    "kernel_space_stack_size": "1MB",\n\
    "max_num_of_threads": 32\n\
  },\n\
  "process": {\n\
    "default_stack_size": "4MB",\n\
    "default_heap_size": "32MB",\n\
    "default_mmap_size": "500MB"\n\
  },\n\
  "entry_points": ["/bin/node"],\n\
  "env": {\n\
    "LD_LIBRARY_PATH": "/lib:/usr/lib:/usr/local/lib",\n\
    "PATH": "/bin:/usr/bin",\n\
    "OCCLUM": "yes"\n\
  }\n\
}' > Occlum.json

# Set working directory
WORKDIR /app

# Copy the source code
COPY . /app

# Build the project
RUN dotnet build src/NeoServiceLayer.sln -c Release

# Build the enclave
WORKDIR /app/src/NeoServiceLayer.Tee.Enclave/Enclave/Build
RUN mkdir -p build && cd build && cmake .. && make

# Copy the enclave to the Occlum instance
RUN cp /app/src/NeoServiceLayer.Tee.Enclave/Enclave/Build/build/libneoserviceenclave.so /occlum_instance/image/lib/

# Build the Occlum instance
WORKDIR /occlum_instance
RUN occlum build

# Set the working directory
WORKDIR /app

# Set environment variables for the Neo Service Layer
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Tee__Type=Occlum
ENV Tee__SimulationMode=true
ENV Tee__EnclavePath=/occlum_instance/image/lib/libneoserviceenclave.so
ENV Tee__Debug=true
ENV Tee__Occlum__InstanceDir=/occlum_instance
ENV Tee__Occlum__LogLevel=debug
ENV Tee__Occlum__NodeJsPath=/bin/node
ENV Tee__Occlum__TempDir=/tmp
ENV Tee__Occlum__EnableDebugMode=true
ENV Tee__Occlum__MaxMemoryMB=1024
ENV Tee__Occlum__MaxThreads=32
ENV Tee__Occlum__MaxProcesses=16
ENV Tee__Occlum__MaxExecutionTimeSeconds=60

# Expose ports
EXPOSE 5000 5001

# Set the entrypoint
ENTRYPOINT ["dotnet", "run", "--project", "src/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj", "--no-build", "--configuration", "Release"]
