#!/bin/bash

# Neo Service Layer Devcontainer Setup Script
# This script sets up the development environment for the fallback devcontainer

set -e

echo "Setting up Neo Service Layer development environment..."

# Update package lists
apt-get update

# Install basic utilities
apt-get install -y \
    curl \
    wget \
    git \
    unzip \
    ca-certificates \
    gnupg \
    lsb-release

# Install .NET SDK 8.0
echo "Installing .NET SDK 8.0..."
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

apt-get update
apt-get install -y dotnet-sdk-8.0

# Verify .NET installation
echo "Verifying .NET installation..."
dotnet --version

# Install additional tools if network allows
echo "Installing additional development tools..."

# Try to install Docker CLI (optional)
if curl -fsSL https://get.docker.com -o get-docker.sh 2>/dev/null; then
    echo "Installing Docker CLI..."
    sh get-docker.sh
    rm get-docker.sh
else
    echo "Skipping Docker CLI installation due to network issues"
fi

# Try to install GitHub CLI (optional)
if curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg -o githubcli-archive-keyring.gpg 2>/dev/null; then
    echo "Installing GitHub CLI..."
    dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg < githubcli-archive-keyring.gpg
    chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null
    apt-get update
    apt-get install -y gh || echo "Failed to install GitHub CLI, continuing..."
    rm githubcli-archive-keyring.gpg
else
    echo "Skipping GitHub CLI installation due to network issues"
fi

# Clean up
apt-get clean
rm -rf /var/lib/apt/lists/*

# Set up workspace
cd /workspace

# Try to restore .NET packages
echo "Attempting to restore .NET packages..."
if [ -f "NeoServiceLayer.sln" ]; then
    dotnet restore || echo "Package restore failed, but continuing..."
    dotnet build || echo "Build failed, but environment is ready for development"
else
    echo "Solution file not found, skipping package restore"
fi

echo "Setup complete! You can now start developing."
echo "If some tools failed to install due to network issues, you can:"
echo "1. Try running this script again when connectivity improves"
echo "2. Install tools manually as needed"
echo "3. Use the main devcontainer configuration when network is stable"