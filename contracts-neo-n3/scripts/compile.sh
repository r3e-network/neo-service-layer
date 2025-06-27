#!/bin/bash

# Neo Service Layer Smart Contract Compilation Script
# Uses official Neo C# Compiler from neo-devpack-dotnet

set -e

# Configuration
NEO_COMPILER_VERSION="3.6.3"
OUTPUT_DIR="./bin/contracts"
SOURCE_DIR="./src"
MANIFEST_DIR="./manifests"

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

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK is required but not installed"
        log_info "Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download"
        exit 1
    fi
    
    # Check .NET version
    DOTNET_VERSION=$(dotnet --version)
    log_info "Found .NET version: $DOTNET_VERSION"
    
    if [[ ! "$DOTNET_VERSION" =~ ^8\. ]]; then
        log_warning ".NET 8.0 is recommended for Neo smart contract development"
    fi
    
    log_success "Prerequisites check passed"
}

# Setup directories
setup_directories() {
    log_info "Setting up output directories..."
    
    # Create output directories
    mkdir -p "$OUTPUT_DIR"
    mkdir -p "$MANIFEST_DIR"
    
    # Clean previous builds
    if [ -d "./bin" ]; then
        rm -rf ./bin/Debug ./bin/Release
    fi
    
    if [ -d "./obj" ]; then
        rm -rf ./obj
    fi
    
    log_success "Directories setup completed"
}

# Restore NuGet packages
restore_packages() {
    log_info "Restoring NuGet packages..."
    
    dotnet restore --verbosity minimal
    
    if [ $? -eq 0 ]; then
        log_success "Package restoration completed"
    else
        log_error "Failed to restore packages"
        exit 1
    fi
}

# Compile individual contract
compile_contract() {
    local contract_name=$1
    local contract_file=$2
    
    log_info "Compiling $contract_name..."
    
    # Create temporary project for individual contract compilation
    local temp_dir="./temp_$contract_name"
    mkdir -p "$temp_dir"
    
    # Create individual contract project
    cat > "$temp_dir/$contract_name.csproj" << EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>NeoServiceLayer</RootNamespace>
    <AssemblyName>$contract_name</AssemblyName>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Neo.SmartContract.Framework" Version="$NEO_COMPILER_VERSION" />
  </ItemGroup>
  
  <ItemGroup>
    <Compile Include="../$contract_file" />
    <Compile Include="../src/Core/IServiceContract.cs" />
  </ItemGroup>
</Project>
EOF
    
    # Compile the contract
    cd "$temp_dir"
    dotnet build --configuration Release --verbosity minimal
    
    if [ $? -eq 0 ]; then
        # Use Neo compiler to generate .nef and .manifest files
        dotnet run --project . --configuration Release -- compile "$contract_name.dll" --output "../$OUTPUT_DIR"
        
        if [ $? -eq 0 ]; then
            log_success "$contract_name compiled successfully"
            
            # Move manifest to manifests directory
            if [ -f "../$OUTPUT_DIR/$contract_name.manifest.json" ]; then
                cp "../$OUTPUT_DIR/$contract_name.manifest.json" "../$MANIFEST_DIR/"
            fi
        else
            log_error "Failed to generate NEF file for $contract_name"
        fi
    else
        log_error "Failed to compile $contract_name"
    fi
    
    cd ..
    rm -rf "$temp_dir"
}

# Compile all contracts using Neo C# Compiler
compile_all_contracts() {
    log_info "Starting compilation of all Neo Service Layer contracts..."
    
    # Array of all contracts to compile
    declare -a contracts=(
        # Core Infrastructure
        "ServiceRegistry:src/Core/ServiceRegistry.cs"
        
        # Service Contracts
        "RandomnessContract:src/Services/RandomnessContract.cs"
        "OracleContract:src/Services/OracleContract.cs"
        "AbstractAccountContract:src/Services/AbstractAccountContract.cs"
        "StorageContract:src/Services/StorageContract.cs"
        "ComputeContract:src/Services/ComputeContract.cs"
        "CrossChainContract:src/Services/CrossChainContract.cs"
        "MonitoringContract:src/Services/MonitoringContract.cs"
        "VotingContract:src/Services/VotingContract.cs"
        "ComplianceContract:src/Services/ComplianceContract.cs"
        "KeyManagementContract:src/Services/KeyManagementContract.cs"
        "AutomationContract:src/Services/AutomationContract.cs"
        "IdentityManagementContract:src/Services/IdentityManagementContract.cs"
        "PaymentProcessingContract:src/Services/PaymentProcessingContract.cs"
        "NotificationContract:src/Services/NotificationContract.cs"
        "AnalyticsContract:src/Services/AnalyticsContract.cs"
        "MarketplaceContract:src/Services/MarketplaceContract.cs"
        "InsuranceContract:src/Services/InsuranceContract.cs"
        "LendingContract:src/Services/LendingContract.cs"
        "TokenizationContract:src/Services/TokenizationContract.cs"
        "SupplyChainContract:src/Services/SupplyChainContract.cs"
        "EnergyManagementContract:src/Services/EnergyManagementContract.cs"
        "HealthcareContract:src/Services/HealthcareContract.cs"
        "GameContract:src/Services/GameContract.cs"
    )
    
    local success_count=0
    local total_count=${#contracts[@]}
    
    # Compile each contract
    for contract_info in "${contracts[@]}"; do
        IFS=':' read -r contract_name contract_file <<< "$contract_info"
        
        if [ -f "$contract_file" ]; then
            compile_contract "$contract_name" "$contract_file"
            ((success_count++))
        else
            log_error "Contract file not found: $contract_file"
        fi
    done
    
    log_info "Compilation Summary:"
    log_info "Total contracts: $total_count"
    log_success "Successfully compiled: $success_count"
    
    if [ $success_count -eq $total_count ]; then
        log_success "All contracts compiled successfully!"
    else
        log_warning "Some contracts failed to compile"
    fi
}

# Alternative: Use nccs (Neo C# Compiler Service) directly
compile_with_nccs() {
    log_info "Compiling contracts using Neo C# Compiler Service..."
    
    # Install nccs if not available
    if ! command -v nccs &> /dev/null; then
        log_info "Installing Neo C# Compiler Service..."
        dotnet tool install --global Neo.Compiler.CSharp --version $NEO_COMPILER_VERSION
    fi
    
    # Compile each contract file directly
    find "$SOURCE_DIR" -name "*.cs" -not -path "*/Core/IServiceContract.cs" | while read -r contract_file; do
        local contract_name=$(basename "$contract_file" .cs)
        log_info "Compiling $contract_name with nccs..."
        
        nccs "$contract_file" --output "$OUTPUT_DIR" --base64
        
        if [ $? -eq 0 ]; then
            log_success "$contract_name compiled with nccs"
        else
            log_error "Failed to compile $contract_name with nccs"
        fi
    done
}

# Generate compilation report
generate_report() {
    local report_file="compilation-report-$(date +%Y%m%d-%H%M%S).json"
    
    log_info "Generating compilation report: $report_file"
    
    cat > "$report_file" << EOF
{
  "compilation": {
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "compiler_version": "$NEO_COMPILER_VERSION",
    "dotnet_version": "$(dotnet --version)",
    "total_contracts": $(find "$OUTPUT_DIR" -name "*.nef" | wc -l),
    "contracts": [
EOF

    local first=true
    find "$OUTPUT_DIR" -name "*.nef" | while read -r nef_file; do
        local contract_name=$(basename "$nef_file" .nef)
        local manifest_file="$MANIFEST_DIR/$contract_name.manifest.json"
        
        if [ "$first" = true ]; then
            first=false
        else
            echo "," >> "$report_file"
        fi
        
        echo "      {" >> "$report_file"
        echo "        \"name\": \"$contract_name\"," >> "$report_file"
        echo "        \"nef_file\": \"$nef_file\"," >> "$report_file"
        echo "        \"manifest_file\": \"$manifest_file\"," >> "$report_file"
        echo "        \"size\": $(stat -f%z "$nef_file" 2>/dev/null || stat -c%s "$nef_file")" >> "$report_file"
        echo "      }" >> "$report_file"
    done

    cat >> "$report_file" << EOF
    ],
    "output_directory": "$OUTPUT_DIR",
    "manifest_directory": "$MANIFEST_DIR"
  }
}
EOF

    log_success "Compilation report generated: $report_file"
}

# Verify compiled contracts
verify_contracts() {
    log_info "Verifying compiled contracts..."
    
    local nef_count=$(find "$OUTPUT_DIR" -name "*.nef" | wc -l)
    local manifest_count=$(find "$MANIFEST_DIR" -name "*.manifest.json" | wc -l)
    
    log_info "Found $nef_count NEF files"
    log_info "Found $manifest_count manifest files"
    
    if [ $nef_count -gt 0 ]; then
        log_success "Contract compilation verification passed"
        
        # List all compiled contracts
        log_info "Compiled contracts:"
        find "$OUTPUT_DIR" -name "*.nef" -exec basename {} .nef \; | sort | while read -r contract; do
            log_info "  ✓ $contract"
        done
    else
        log_error "No compiled contracts found"
        exit 1
    fi
}

# Main compilation function
main() {
    echo "=================================================="
    echo "Neo Service Layer Smart Contract Compilation"
    echo "Using Neo C# Compiler v$NEO_COMPILER_VERSION"
    echo "=================================================="
    
    check_prerequisites
    setup_directories
    restore_packages
    
    # Try different compilation methods
    if command -v nccs &> /dev/null; then
        compile_with_nccs
    else
        compile_all_contracts
    fi
    
    verify_contracts
    generate_report
    
    echo "=================================================="
    log_success "Compilation completed successfully!"
    echo "=================================================="
    
    log_info "Next steps:"
    log_info "1. Review compiled contracts in: $OUTPUT_DIR"
    log_info "2. Check contract manifests in: $MANIFEST_DIR"
    log_info "3. Deploy contracts using: ./scripts/deploy.sh deploy"
    log_info "4. Run tests using: dotnet test"
}

# Handle script arguments
case "${1:-}" in
    "clean")
        log_info "Cleaning build artifacts..."
        rm -rf ./bin ./obj ./temp_* ./manifests
        log_success "Clean completed"
        ;;
    "restore")
        check_prerequisites
        restore_packages
        ;;
    "compile"|"")
        main
        ;;
    "verify")
        verify_contracts
        ;;
    *)
        echo "Usage: $0 {clean|restore|compile|verify}"
        echo ""
        echo "Commands:"
        echo "  clean    - Clean build artifacts"
        echo "  restore  - Restore NuGet packages only"
        echo "  compile  - Compile all contracts (default)"
        echo "  verify   - Verify compiled contracts"
        echo ""
        echo "Environment variables:"
        echo "  NEO_COMPILER_VERSION - Neo compiler version (default: $NEO_COMPILER_VERSION)"
        exit 1
        ;;
esac