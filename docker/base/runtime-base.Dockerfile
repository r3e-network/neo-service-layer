# Runtime base image for Neo Service Layer
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime-base

# Install system dependencies for SGX runtime
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Set up SGX runtime (for Intel SGX support) - Optional
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
#         libsgx-aesm-service \
#     && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r neoservice && useradd -r -g neoservice neoservice

# Set up directories
RUN mkdir -p /app /var/log/neoservice /var/lib/neoservice \
    && chown -R neoservice:neoservice /app /var/log/neoservice /var/lib/neoservice

# Common environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# This image will be used as base for service-specific runtimes
LABEL name="Neo Service Layer Runtime Base"
LABEL version="1.0.0"
LABEL description="Base runtime image for Neo Service Layer microservices"

# Switch to non-root user
USER neoservice

WORKDIR /app