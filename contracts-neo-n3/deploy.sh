#!/bin/bash
echo "Password: testnet123"
for nef in build/*.nef; do 
    manifest="${nef%.nef}.manifest.json"
    contract=$(basename "$nef" .nef)
    echo "Deploying $contract..."
    neo-go contract deploy -i "$nef" -m "$manifest" -r https://testnet1.neo.coz.io:443 -w deployment-testnet.json -a NTmHjwiadq4g3VHpJ5FQigQcD4fF5m8TyX --force
    echo ""
done