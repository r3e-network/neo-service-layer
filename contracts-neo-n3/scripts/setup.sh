#!/bin/bash

# Neo Service Layer Setup Script
# Installs all prerequisites and dependencies

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Detect OS
detect_os() {
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        OS="linux"
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        OS="macos"
    elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]]; then
        OS="windows"
    else
        log_error "Unsupported operating system: $OSTYPE"
        exit 1
    fi
    log_info "Detected OS: $OS"
}

# Install .NET 8.0 SDK
install_dotnet() {
    log_info "Installing .NET 8.0 SDK..."
    
    if command -v dotnet &> /dev/null; then
        DOTNET_VERSION=$(dotnet --version)
        log_info "Found existing .NET version: $DOTNET_VERSION"
        
        if [[ "$DOTNET_VERSION" =~ ^8\. ]]; then
            log_success ".NET 8.0 is already installed"
            return 0
        fi
    fi
    
    case $OS in
        "linux")
            curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 8.0.0
            echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
            export PATH=$PATH:$HOME/.dotnet
            ;;
        "macos")
            if command -v brew &> /dev/null; then
                brew install --cask dotnet
            else
                curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 8.0.0
                echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.zshrc
                export PATH=$PATH:$HOME/.dotnet
            fi
            ;;
        "windows")
            log_info "Please download and install .NET 8.0 SDK from:"
            log_info "https://dotnet.microsoft.com/download/dotnet/8.0"
            ;;
    esac
    
    log_success ".NET 8.0 SDK installation completed"
}

# Install Neo C# Compiler
install_neo_compiler() {
    log_info "Installing Neo C# Compiler..."
    
    if command -v nccs &> /dev/null; then
        log_success "Neo C# Compiler is already installed"
        return 0
    fi
    
    dotnet tool install --global Neo.Compiler.CSharp --version 3.6.3
    
    if [ $? -eq 0 ]; then
        log_success "Neo C# Compiler installed successfully"
    else
        log_error "Failed to install Neo C# Compiler"
        exit 1
    fi
}

# Setup project dependencies
setup_project() {
    log_info "Setting up Neo Service Layer project..."
    
    # Navigate to project root
    cd "$(dirname "$0")/.."
    
    # Restore NuGet packages
    log_info "Restoring NuGet packages..."
    dotnet restore
    
    if [ $? -eq 0 ]; then
        log_success "NuGet packages restored successfully"
    else
        log_error "Failed to restore NuGet packages"
        exit 1
    fi
    
    # Create necessary directories
    mkdir -p bin/contracts
    mkdir -p manifests
    mkdir -p logs
    mkdir -p temp
    
    log_success "Project setup completed"
}

# Main setup function
main() {
    echo "=================================================="
    echo "Neo Service Layer Setup"
    echo "Setting up development environment..."
    echo "=================================================="
    
    detect_os
    install_dotnet
    install_neo_compiler
    setup_project
    
    echo "=================================================="
    log_success "Setup completed successfully!"
    echo "=================================================="
    
    log_info "Next steps:"
    log_info "1. Compile contracts: ./scripts/compile.sh"
    log_info "2. Run tests: dotnet test"
    log_info "3. Deploy contracts: ./scripts/deploy.sh deploy"
}

# Handle script arguments
case "${1:-}" in
    "dotnet")
        detect_os
        install_dotnet
        ;;
    "compiler")
        install_neo_compiler
        ;;
    "project")
        setup_project
        ;;
    *)
        main
        ;;
esac