#!/bin/bash

# Script to verify compiled contracts and prepare deployment summary

echo "Neo Smart Contracts - Deployment Readiness Check"
echo "==============================================="
echo ""

# Count contracts
TOTAL_NEF=$(ls -1 build/*.nef 2>/dev/null | wc -l)
TOTAL_MANIFEST=$(ls -1 build/*.manifest.json 2>/dev/null | wc -l)

echo "Compiled Contracts Summary:"
echo "- NEF files: $TOTAL_NEF"
echo "- Manifest files: $TOTAL_MANIFEST"
echo ""

if [ $TOTAL_NEF -ne $TOTAL_MANIFEST ]; then
    echo "⚠️  Warning: NEF and manifest counts don't match!"
fi

echo "Contract List:"
echo "--------------"
for nef in build/*.nef; do
    if [ -f "$nef" ]; then
        contract=$(basename "$nef" .nef)
        manifest="build/${contract}.manifest.json"
        
        if [ -f "$manifest" ]; then
            size=$(stat -c%s "$nef" 2>/dev/null || stat -f%z "$nef" 2>/dev/null)
            echo "✓ $contract (${size} bytes)"
        else
            echo "✗ $contract (missing manifest)"
        fi
    fi
done

echo ""
echo "Wallet Information:"
echo "------------------"
echo "Wallet File: deployment-testnet.json"
echo "Address: NTmHjwiadq4g3VHpJ5FQigQcD4fF5m8TyX"
echo "Password: testnet123"
echo ""

echo "Deployment Commands:"
echo "-------------------"
echo "1. To deploy all contracts, run each command in DEPLOYMENT_GUIDE.md"
echo "2. Enter password 'testnet123' when prompted"
echo "3. Save the contract hashes after each deployment"
echo ""

echo "Total Contracts Ready for Deployment: $TOTAL_NEF"