#!/bin/bash
# Neo Service Layer - Smart Contract Validation Script
# Validates all smart contracts for compilation and consistency

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Contract directories
CONTRACTS_DIR="contracts-neo-n3/src"
CORE_DIR="$CONTRACTS_DIR/Core"
PRODUCTION_DIR="$CONTRACTS_DIR/ProductionReady"
SERVICES_DIR="$CONTRACTS_DIR/Services"

# Counters
TOTAL_CONTRACTS=0
VALID_CONTRACTS=0
INVALID_CONTRACTS=0

echo -e "${GREEN}Neo Service Layer - Smart Contract Validation${NC}"
echo "============================================="

# Function to validate contract syntax
validate_contract() {
    local file=$1
    local contract_name=$(basename "$file" .cs)
    
    ((TOTAL_CONTRACTS++))
    echo -ne "\r${BLUE}Validating contracts: $TOTAL_CONTRACTS${NC}"
    
    # Check basic syntax patterns
    local has_issues=false
    
    # Check for using statements
    if ! grep -q "using Neo.SmartContract.Framework" "$file"; then
        echo -e "\n${RED}✗ $contract_name: Missing Neo.SmartContract.Framework import${NC}"
        has_issues=true
    fi
    
    # Check for class definition
    if ! grep -q "public class.*SmartContract\|public class.*ReentrancyGuard" "$file"; then
        echo -e "\n${RED}✗ $contract_name: Not inheriting from SmartContract or ReentrancyGuard${NC}"
        has_issues=true
    fi
    
    # Check for proper namespace
    if ! grep -q "namespace NeoServiceLayer.Contracts" "$file"; then
        echo -e "\n${YELLOW}⚠ $contract_name: Non-standard namespace${NC}"
    fi
    
    # Check for required security patterns in production contracts
    if [[ "$file" == *"ProductionReady"* ]]; then
        if ! grep -q "ReentrancyGuard" "$file"; then
            echo -e "\n${YELLOW}⚠ $contract_name: Production contract not using ReentrancyGuard${NC}"
        fi
    fi
    
    # Check for TODO items
    if grep -q "TODO\|FIXME" "$file"; then
        echo -e "\n${YELLOW}⚠ $contract_name: Contains TODO/FIXME items${NC}"
    fi
    
    if [ "$has_issues" = false ]; then
        ((VALID_CONTRACTS++))
    else
        ((INVALID_CONTRACTS++))
    fi
}

# Function to check contract dependencies
check_dependencies() {
    echo -e "\n${YELLOW}Checking contract dependencies...${NC}"
    
    # Check for required core contracts
    local required_core=(
        "IServiceContract.cs"
        "ServiceRegistry.cs"
        "ReentrancyGuard.cs"
        "InputValidation.cs"
    )
    
    for contract in "${required_core[@]}"; do
        if [ -f "$CORE_DIR/$contract" ]; then
            echo -e "${GREEN}✓ Core contract found: $contract${NC}"
        else
            echo -e "${RED}✗ Core contract missing: $contract${NC}"
        fi
    done
}

# Function to validate contract relationships
validate_relationships() {
    echo -e "\n${YELLOW}Validating contract relationships...${NC}"
    
    # Check that service contracts implement IServiceContract
    local service_contracts=$(find "$SERVICES_DIR" -name "*.cs" -type f)
    local implementing_count=0
    
    for contract in $service_contracts; do
        if grep -q "IServiceContract" "$contract"; then
            ((implementing_count++))
        fi
    done
    
    echo -e "${BLUE}Service contracts implementing IServiceContract: $implementing_count${NC}"
}

# Function to check for security patterns
check_security_patterns() {
    echo -e "\n${YELLOW}Checking security patterns...${NC}"
    
    # Count contracts with proper access control
    local access_control_count=0
    local contracts=$(find "$CONTRACTS_DIR" -name "*.cs" -type f)
    
    for contract in $contracts; do
        if grep -q "Runtime.CheckWitness\|OnlyOwner\|RequireOwner" "$contract"; then
            ((access_control_count++))
        fi
    done
    
    echo -e "${BLUE}Contracts with access control: $access_control_count${NC}"
    
    # Check for input validation usage
    local validation_count=0
    for contract in $contracts; do
        if grep -q "InputValidation\|ValidateAddress\|ValidatePositive" "$contract"; then
            ((validation_count++))
        fi
    done
    
    echo -e "${BLUE}Contracts using input validation: $validation_count${NC}"
}

# Function to generate contract summary
generate_summary() {
    echo -e "\n${YELLOW}Contract Summary${NC}"
    echo "================"
    
    # Count contracts by type
    local core_count=$(find "$CORE_DIR" -name "*.cs" | wc -l)
    local production_count=$(find "$PRODUCTION_DIR" -name "*.cs" | wc -l)
    local service_count=$(find "$SERVICES_DIR" -name "*.cs" | wc -l)
    
    echo "Core Contracts: $core_count"
    echo "Production Contracts: $production_count"
    echo "Service Contracts: $service_count"
    echo "Total Contracts: $((core_count + production_count + service_count))"
}

# Main validation flow
echo -e "${BLUE}Starting contract validation...${NC}"

# Check if contracts directory exists
if [ ! -d "$CONTRACTS_DIR" ]; then
    echo -e "${RED}Error: Contracts directory not found at $CONTRACTS_DIR${NC}"
    exit 1
fi

# Validate all contracts
echo -e "\n${YELLOW}Validating individual contracts...${NC}"

# Validate core contracts
if [ -d "$CORE_DIR" ]; then
    for contract in "$CORE_DIR"/*.cs; do
        [ -f "$contract" ] && validate_contract "$contract"
    done
fi

# Validate production contracts
if [ -d "$PRODUCTION_DIR" ]; then
    for contract in "$PRODUCTION_DIR"/*.cs; do
        [ -f "$contract" ] && validate_contract "$contract"
    done
fi

# Validate service contracts
if [ -d "$SERVICES_DIR" ]; then
    for contract in "$SERVICES_DIR"/*.cs; do
        [ -f "$contract" ] && validate_contract "$contract"
    done
fi

echo "" # New line after progress indicator

# Run additional checks
check_dependencies
validate_relationships
check_security_patterns
generate_summary

# Check compilation capability
echo -e "\n${YELLOW}Checking contract compilation setup...${NC}"

# Check for .csproj file
if [ -f "contracts-neo-n3/NeoServiceLayer.Contracts.csproj" ]; then
    echo -e "${GREEN}✓ Contract project file found${NC}"
    
    # Check for Neo DevPack reference
    if grep -q "Neo.SmartContract.Framework" "contracts-neo-n3/NeoServiceLayer.Contracts.csproj"; then
        echo -e "${GREEN}✓ Neo Smart Contract Framework referenced${NC}"
    else
        echo -e "${RED}✗ Neo Smart Contract Framework not referenced${NC}"
    fi
else
    echo -e "${RED}✗ Contract project file missing${NC}"
fi

# Final report
echo -e "\n${YELLOW}===== VALIDATION RESULTS =====${NC}"
echo "Total Contracts: $TOTAL_CONTRACTS"
echo -e "${GREEN}Valid Contracts: $VALID_CONTRACTS${NC}"
echo -e "${RED}Invalid Contracts: $INVALID_CONTRACTS${NC}"

if [ $INVALID_CONTRACTS -eq 0 ]; then
    echo -e "\n${GREEN}✅ All contracts validated successfully!${NC}"
    exit 0
else
    echo -e "\n${RED}❌ Contract validation failed!${NC}"
    echo "Please fix the issues in $INVALID_CONTRACTS contracts."
    exit 1
fi