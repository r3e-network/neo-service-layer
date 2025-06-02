# Use Microsoft's official .NET SDK image which includes all necessary tools
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy

# Set non-interactive mode for apt
ENV DEBIAN_FRONTEND=noninteractive

# Create vscode user
RUN groupadd --gid 1000 vscode \
    && useradd --uid 1000 --gid vscode --shell /bin/bash --create-home vscode

# Install system dependencies
RUN apt-get update && apt-get install -y \
    # Basic utilities
    curl \
    wget \
    unzip \
    zip \
    git \
    openssh-client \
    gnupg2 \
    iproute2 \
    procps \
    lsof \
    htop \
    net-tools \
    psmisc \
    # Build tools
    build-essential \
    # Certificate handling
    ca-certificates \
    # For Intel SGX (if needed)
    libssl-dev \
    libcurl4-openssl-dev \
    libprotobuf-dev \
    # Clean up
    && rm -rf /var/lib/apt/lists/*

# Install Docker CLI (for docker-in-docker scenarios)
RUN curl -fsSL https://get.docker.com | sh \
    && usermod -aG docker vscode

# Install GitHub CLI
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
    && chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && apt-get update \
    && apt-get install -y gh \
    && rm -rf /var/lib/apt/lists/*

# Install additional .NET tools
RUN dotnet tool install --global dotnet-ef \
    && dotnet tool install --global dotnet-outdated-tool \
    && dotnet tool install --global dotnet-reportgenerator-globaltool

# Set up workspace
WORKDIR /workspace

# Switch to vscode user
USER vscode

# Ensure dotnet tools are in PATH for vscode user
ENV PATH="${PATH}:/home/vscode/.dotnet/tools"

# Set git safe directory (for when workspace is mounted)
RUN git config --global --add safe.directory /workspace

# Default command
CMD ["sleep", "infinity"]