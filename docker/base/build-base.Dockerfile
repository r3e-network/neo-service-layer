# Build base image for Neo Service Layer
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-base

# Install system dependencies
RUN apt-get update && apt-get install -y \
    git \
    curl \
    wget \
    unzip \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

# Set up SGX SDK (for Intel SGX support) - Optional
# Note: SGX support is disabled in this build
# To enable SGX, uncomment the following lines and ensure SGX is available on your system
# RUN apt-get update && apt-get install -y lsb-release \
#     && wget -q -O - https://download.01.org/intel-sgx/sgx_repo/ubuntu/intel-sgx-deb.key | apt-key add - \
#     && echo "deb [arch=amd64] https://download.01.org/intel-sgx/sgx_repo/ubuntu $(lsb_release -cs) main" > /etc/apt/sources.list.d/intel-sgx.list \
#     && apt-get update \
#     && apt-get install -y \
#         libsgx-enclave-common \
#         libsgx-quote-ex \
#         libsgx-urts \
#     && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /src

# This image will be used as base for service-specific builds
LABEL name="Neo Service Layer Build Base"
LABEL version="1.0.0"
LABEL description="Base image for building Neo Service Layer microservices"