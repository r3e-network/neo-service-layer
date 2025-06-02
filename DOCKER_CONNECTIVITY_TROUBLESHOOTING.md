# Docker Connectivity Troubleshooting Guide

## Overview

This guide addresses Docker connectivity issues when building the Neo Service Layer devcontainer, specifically the error:

```
ERROR: failed to authorize: failed to fetch oauth token: Post "https://auth.docker.io/token": dial tcp [2a03:2880:f11f:83:face:b00c:0:25de]:443: connectex: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond.
```

## Root Cause Analysis

The error indicates network connectivity issues when accessing Docker Hub (`docker.io`). This can be caused by:

1. **Network Connectivity Issues**: Firewall, proxy, or DNS resolution problems
2. **Docker Hub Rate Limiting**: Exceeded pull limits for anonymous users
3. **IPv6 Connectivity Issues**: Problems with IPv6 routing to Docker Hub
4. **Corporate Network Restrictions**: Blocked access to container registries
5. **DNS Resolution Issues**: Unable to resolve Docker Hub domains

## Immediate Solutions

### Solution 1: Use IPv4 Only
Add the following to your Docker daemon configuration (`daemon.json`):

```json
{
  "ipv6": false,
  "fixed-cidr-v6": "",
  "experimental": false,
  "ip-forward": true
}
```

**Location of daemon.json:**
- Windows: `%USERPROFILE%\.docker\daemon.json`
- macOS: `~/.docker/daemon.json`
- Linux: `/etc/docker/daemon.json`

### Solution 2: Configure DNS
Update Docker daemon configuration to use reliable DNS servers:

```json
{
  "dns": ["8.8.8.8", "8.8.4.4", "1.1.1.1"],
  "dns-search": [],
  "dns-opts": []
}
```

### Solution 3: Use Docker Hub Authentication
Create a Docker Hub account and login to avoid rate limiting:

```bash
docker login
```

### Solution 4: Alternative Registry Configuration
Configure Docker to use alternative registries:

```json
{
  "registry-mirrors": [
    "https://mirror.gcr.io",
    "https://daocloud.io",
    "https://c.163.com"
  ]
}
```

## Advanced Solutions

### Solution 5: Offline Docker Images
Pre-pull required images when connectivity is available:

```bash
# Pull base images
docker pull ubuntu:24.04
docker pull mcr.microsoft.com/dotnet/sdk:8.0
docker pull mcr.microsoft.com/dotnet/aspnet:8.0

# Pull devcontainer feature images
docker pull ghcr.io/devcontainers/features/common-utils:2
docker pull ghcr.io/devcontainers/features/git:1
docker pull ghcr.io/devcontainers/features/github-cli:1
docker pull ghcr.io/devcontainers/features/docker-in-docker:2
```

### Solution 6: Custom Base Image
Create a pre-built base image with all dependencies:

```dockerfile
FROM ubuntu:24.04

# Install system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    git \
    build-essential \
    ca-certificates \
    gnupg \
    lsb-release \
    && rm -rf /var/lib/apt/lists/*

# Install .NET SDK
RUN wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y dotnet-sdk-8.0 \
    && rm -rf /var/lib/apt/lists/*

# Install Docker CLI
RUN curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu jammy stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null \
    && apt-get update \
    && apt-get install -y docker-ce-cli \
    && rm -rf /var/lib/apt/lists/*

# Install GitHub CLI
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
    && chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && apt-get update \
    && apt-get install -y gh \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /workspace
```

## Network Diagnostics

### Check Docker Hub Connectivity
```bash
# Test basic connectivity
ping docker.io

# Test HTTPS connectivity
curl -I https://docker.io

# Test Docker Registry API
curl https://registry-1.docker.io/v2/

# Check DNS resolution
nslookup docker.io
nslookup registry-1.docker.io
```

### Check Docker Configuration
```bash
# Check Docker info
docker info

# Check Docker version
docker version

# Check if Docker daemon is running
docker ps
```

## Corporate Network Solutions

### Proxy Configuration
If behind a corporate proxy, configure Docker proxy settings:

```json
{
  "proxies": {
    "http-proxy": "http://proxy.company.com:8080",
    "https-proxy": "https://proxy.company.com:8080",
    "no-proxy": "localhost,127.0.0.1,.company.com"
  }
}
```

### Certificate Issues
For corporate certificates, add them to Docker:

```bash
# Copy corporate certificates
sudo cp corporate-cert.crt /usr/local/share/ca-certificates/
sudo update-ca-certificates

# For Windows, add to certificate store
certlm.msc
```

## Quick Restart Steps

After making configuration changes:

```bash
# Restart Docker service
# Windows/macOS: Restart Docker Desktop
# Linux:
sudo systemctl restart docker

# Clear Docker cache
docker system prune -a

# Test connectivity
docker run hello-world
```

## Alternative Development Setup

If devcontainer continues to fail, consider these alternatives:

### Option 1: Local Development
Install dependencies directly on your host machine:
- .NET 8.0 SDK
- Docker Desktop
- Git
- GitHub CLI

### Option 2: Manual Container Setup
```bash
# Run Ubuntu container manually
docker run -it --name neo-dev \
  -v "${PWD}:/workspace" \
  -w /workspace \
  ubuntu:24.04 bash

# Install dependencies inside container
apt-get update && apt-get install -y \
  curl wget git build-essential ca-certificates

# Install .NET SDK manually
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
apt-get update && apt-get install -y dotnet-sdk-8.0
```

### Option 3: Use Gitpod or GitHub Codespaces
Consider cloud-based development environments that handle connectivity issues:
- GitHub Codespaces
- Gitpod
- Replit

## Prevention Strategies

1. **Regular Image Updates**: Keep base images updated during good connectivity
2. **Image Caching**: Use Docker layer caching strategies
3. **Registry Mirrors**: Configure multiple registry mirrors
4. **Offline Development**: Maintain offline-capable development setups
5. **Network Monitoring**: Monitor network connectivity patterns

## Support Resources

- **Docker Documentation**: https://docs.docker.com/config/daemon/
- **Devcontainer Documentation**: https://containers.dev/
- **Network Troubleshooting**: Contact your network administrator
- **Docker Hub Status**: https://status.docker.com/

## Next Steps

1. Try Solution 1 (IPv4 only) first as it addresses the most common cause
2. Restart Docker Desktop/daemon after configuration changes
3. Test with `docker run hello-world`
4. If issues persist, try the manual container setup
5. Consider using alternative development environments

## Emergency Workaround

If you need to work immediately:

```bash
# Clone repository locally
git clone <repository-url>
cd neo-service-layer

# Install .NET SDK locally
# Build and run without containers
dotnet restore
dotnet build
dotnet test
```

This allows development to continue while resolving container connectivity issues.