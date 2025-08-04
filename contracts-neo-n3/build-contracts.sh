#!/bin/bash

# Build script for Neo N3 Smart Contracts using nccs

NCCS=~/.dotnet/tools/nccs
CONTRACT_DIR="src/Services"
OUTPUT_DIR="bin"

# Create output directory if it doesn't exist
mkdir -p $OUTPUT_DIR

echo "Building Neo N3 Smart Contracts..."
echo "================================"

# List of contracts to build
CONTRACTS=(
    "RandomnessContract"
    "OracleContract"
    "AbstractAccountContract"
    "StorageContract"
    "ComputeContract"
    "CrossChainContract"
    "MonitoringContract"
    "VotingContract"
    "ComplianceContract"
    "KeyManagementContract"
    "AutomationContract"
    "NotificationContract"
    "AnalyticsContract"
    "IdentityManagementContract"
    "HealthcareContract"
    "GameContract"
    "EnergyManagementContract"
    "PaymentProcessingContract"
    "MarketplaceContract"
    "LendingContract"
    "InsuranceContract"
    "TokenizationContract"
    "SupplyChainContract"
    "SocialRecoveryContract"
)

# Build each contract
TOTAL=${#CONTRACTS[@]}
SUCCESS=0
FAILED=0

for contract in "${CONTRACTS[@]}"; do
    echo -n "Building $contract... "
    
    if $NCCS "$CONTRACT_DIR/$contract.cs" \
        -o "$OUTPUT_DIR/$contract" \
        --debug \
        2>"$OUTPUT_DIR/$contract.build.log"; then
        echo "✅ SUCCESS"
        ((SUCCESS++))
    else
        echo "❌ FAILED (see $OUTPUT_DIR/$contract.build.log)"
        ((FAILED++))
    fi
done

echo "================================"
echo "Build Summary:"
echo "Total contracts: $TOTAL"
echo "✅ Successful: $SUCCESS"
echo "❌ Failed: $FAILED"

if [ $FAILED -gt 0 ]; then
    echo ""
    echo "Check the build logs in the $OUTPUT_DIR directory for error details."
    exit 1
else
    echo ""
    echo "All contracts built successfully! 🎉"
    echo "Output files are in the $OUTPUT_DIR directory."
fi