# Neo Service Layer Devcontainer Setup Guide

## Overview

This guide provides multiple devcontainer configurations for the Neo Service Layer project, designed to handle various network connectivity scenarios and development environments.

## Available Configurations

### 1. Standard Configuration (`devcontainer.json`)
- **Best for**: Stable network connectivity
- **Features**: Full .NET SDK, Docker CLI, GitHub CLI, all development tools
- **Base Image**: `mcr.microsoft.com/dotnet/sdk:8.0-jammy`
- **Setup Time**: ~3-5 minutes

### 2. Fallback Configuration (`devcontainer.fallback.json`)
- **Best for**: Poor network connectivity or corporate firewalls
- **Features**: Minimal setup with .NET SDK, basic tools installed via script
- **Base Image**: `ubuntu:22.04`
- **Setup Time**: ~2-3 minutes

## Quick Start

### Method 1: Using Standard Configuration

1. **Prerequisites Check**:
   ```bash
   # Test Docker Hub connectivity
   docker run hello-world
   
   # If this fails, see troubleshooting section
   ```

2. **Open in devcontainer**:
   - Open VS Code in your Neo Service Layer directory
   - Command Palette (`Ctrl+Shift+P`) → "Dev Containers: Reopen in Container"
   - Select `devcontainer.json` when prompted

### Method 2: Using Fallback Configuration

1. **When to use**: If standard configuration fails or you have network issues

2. **Setup**:
   - Rename `.devcontainer/devcontainer.fallback.json` to `.devcontainer/devcontainer.json`
   - Open VS Code in your project directory
   - Command Palette → "Dev Containers: Reopen in Container"

### Method 3: Manual Docker Setup (Emergency)

If devcontainers fail completely:

```bash
# Pull base image manually
docker pull ubuntu:22.04

# Run container manually
docker run -it --name neo-dev \
  -v "${PWD}:/workspace" \
  -w /workspace \
  -p 5000:5000 \
  -p 5001:5001 \
  -p 8080:8080 \
  ubuntu:22.04 bash

# Inside container, run setup
bash .devcontainer/setup.sh
```

## Network Connectivity Issues

### Common Error: Docker Hub Connection Failed

**Error Message**:
```
ERROR: failed to authorize: failed to fetch oauth token: Post "https://auth.docker.io/token"
```

**Solutions** (in order of recommendation):

#### Solution 1: Configure Docker for IPv4 Only
Create/edit `~/.docker/daemon.json` (Windows: `%USERPROFILE%\.docker\daemon.json`):

```json
{
  "ipv6": false,
  "fixed-cidr-v6": "",
  "experimental": false,
  "ip-forward": true
}
```

**Restart Docker Desktop after making changes.**

#### Solution 2: Use Alternative DNS
Add to `daemon.json`:

```json
{
  "dns": ["8.8.8.8", "8.8.4.4", "1.1.1.1"],
  "ipv6": false
}
```

#### Solution 3: Pre-pull Images
When you have connectivity, pre-pull required images:

```bash
docker pull mcr.microsoft.com/dotnet/sdk:8.0-jammy
docker pull ubuntu:22.04
docker pull mcr.microsoft.com/dotnet/aspnet:8.0
```

#### Solution 4: Use Registry Mirrors
Add to `daemon.json`:

```json
{
  "registry-mirrors": [
    "https://mirror.gcr.io"
  ],
  "ipv6": false
}
```

#### Solution 5: Corporate Network Setup
For corporate networks, add proxy configuration to `daemon.json`:

```json
{
  "proxies": {
    "http-proxy": "http://proxy.company.com:8080",
    "https-proxy": "https://proxy.company.com:8080",
    "no-proxy": "localhost,127.0.0.1,.company.com"
  },
  "ipv6": false
}
```

## Development Workflow

### Initial Setup

1. **Start Container**:
   ```bash
   # Automatic with devcontainer
   # Or manual setup as shown above
   ```

2. **Verify Installation**:
   ```bash
   dotnet --version  # Should show 8.0.x
   git --version
   docker --version  # If Docker CLI installed
   ```

3. **Build Project**:
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Run Tests**:
   ```bash
   dotnet test
   ```

### Daily Development

1. **Start Services**:
   ```bash
   # Web application
   cd src/Web/NeoServiceLayer.Web
   dotnet run
   
   # API
   cd src/Api/NeoServiceLayer.Api
   dotnet run
   ```

2. **Access Services**:
   - Web UI: http://localhost:5000
   - API: http://localhost:5001
   - Additional services: http://localhost:8080

3. **SGX Enclave Development**:
   ```bash
   # Navigate to enclave projects
   cd src/Tee/NeoServiceLayer.Tee.Enclave
   
   # Build enclave (if SGX is available)
   make
   
   # Run tests
   dotnet test
   ```

## Troubleshooting

### Container Won't Start

**Problem**: Devcontainer fails to build or start

**Solutions**:
1. Check Docker is running: `docker ps`
2. Try fallback configuration
3. Clear Docker cache: `docker system prune -a`
4. Restart Docker Desktop
5. Check disk space: `docker system df`

### Network Issues

**Problem**: Can't access external resources from container

**Solutions**:
1. Check DNS: `nslookup google.com`
2. Test connectivity: `curl -I https://api.nuget.org`
3. Configure proxy if behind corporate firewall
4. Use offline package sources

### Build Failures

**Problem**: .NET build or restore fails

**Solutions**:
1. Clear NuGet cache: `dotnet nuget locals all --clear`
2. Restore packages: `dotnet restore --force`
3. Check .NET version: `dotnet --info`
4. Update packages: `dotnet outdated`

### Permission Issues

**Problem**: File permission errors

**Solutions**:
1. Check user in container: `whoami`
2. Fix ownership: `sudo chown -R vscode:vscode /workspace`
3. Check mount permissions
4. Use root user temporarily

## Advanced Configuration

### Custom Base Image

If you frequently face network issues, create a custom base image:

```dockerfile
# Dockerfile.custom
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy

# Install all your tools here
RUN apt-get update && apt-get install -y \
    git curl wget docker.io gh \
    && rm -rf /var/lib/apt/lists/*

# Pre-install .NET tools
RUN dotnet tool install --global dotnet-ef
```

Build and use:
```bash
docker build -f Dockerfile.custom -t neo-dev-base .
```

Update `devcontainer.json`:
```json
{
  "image": "neo-dev-base",
  ...
}
```

### Offline Development

For completely offline development:

1. **Pre-download packages**:
   ```bash
   # When online, cache packages
   dotnet restore --packages .nuget-cache
   ```

2. **Configure offline sources**:
   ```xml
   <!-- nuget.config -->
   <packageSources>
     <add key="local" value="./.nuget-cache" />
   </packageSources>
   ```

### Performance Optimization

**For faster container builds**:

1. **Use multi-stage builds**:
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
   # Build steps
   
   FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
   # Runtime setup
   ```

2. **Cache layers effectively**:
   - Copy project files first
   - Restore packages
   - Copy source code last

3. **Use .dockerignore**:
   ```
   **/bin
   **/obj
   **/.git
   **/node_modules
   ```

## Environment Variables

Set these in your devcontainer for optimal development:

```json
{
  "containerEnv": {
    "DOTNET_UsePollingFileWatcher": "true",
    "DOTNET_CLI_TELEMETRY_OPTOUT": "true",
    "ASPNETCORE_ENVIRONMENT": "Development",
    "ASPNETCORE_URLS": "http://+:5000"
  }
}
```

## Support and Resources

- **Docker Issues**: See `DOCKER_CONNECTIVITY_TROUBLESHOOTING.md`
- **Project Documentation**: See `docs/` directory
- **SGX Setup**: See `ENCLAVE_INTEGRATION_VERIFICATION.md`
- **Production Deployment**: See `FINAL_DEPLOYMENT_STATUS.md`

## Quick Commands Reference

```bash
# Container management
docker ps                          # List running containers
docker exec -it neo-dev bash      # Enter running container
docker logs neo-dev               # View container logs

# Development
dotnet restore                     # Restore packages
dotnet build                       # Build solution
dotnet test                        # Run all tests
dotnet run --project src/Web/NeoServiceLayer.Web  # Start web app

# Troubleshooting
docker system prune -a            # Clean up Docker
dotnet clean && dotnet restore    # Clean and restore .NET
git status                         # Check git status
```

## Next Steps

1. **Start with the standard configuration** if you have good network connectivity
2. **Use the fallback configuration** if you encounter network issues
3. **Follow the troubleshooting guide** for specific problems
4. **Consider the offline setup** for development in restricted environments
5. **Check the service documentation** in the `docs/services/` directory for specific service development

This setup ensures you can develop the Neo Service Layer regardless of your network environment or connectivity constraints.