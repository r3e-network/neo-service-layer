#!/bin/bash

# Simple deployment script - just run this and enter password when prompted
# Password: testnet123

echo "🚀 Neo Smart Contracts Deployment"
echo "================================="
echo "Password when prompted: testnet123"
echo ""

total=0
deployed=0

for nef in build/*.nef; do
    if [ -f "$nef" ]; then
        manifest="${nef%.nef}.manifest.json"
        if [ -f "$manifest" ]; then
            ((total++))
            contract=$(basename "$nef" .nef)
            echo "[$total/28] Deploying $contract..."
            echo "Password: testnet123"
            
            if neo-go contract deploy -i "$nef" -m "$manifest" -r https://testnet1.neo.coz.io:443 -w deployment-testnet.json -a NTmHjwiadq4g3VHpJ5FQigQcD4fF5m8TyX --force; then
                echo "✅ $contract deployed!"
                ((deployed++))
            else
                echo "❌ $contract failed"
            fi
            echo ""
        fi
    fi
done

echo "=== Final Results ==="
echo "Total: $total"
echo "Deployed: $deployed"
echo "Failed: $((total - deployed))"