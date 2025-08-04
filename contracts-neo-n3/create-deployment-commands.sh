#!/bin/bash

# Configuration
TESTNET_RPC="https://testnet1.neo.coz.io:443"
WALLET_FILE="deployment-testnet.json"
DEPLOYER_ADDRESS="NTmHjwiadq4g3VHpJ5FQigQcD4fF5m8TyX"

echo "Neo Smart Contract Deployment Commands"
echo "======================================"
echo ""
echo "Your wallet is encrypted. You need to know the password you used when creating it."
echo "The WIF key you provided (KzjaqMvqzF1uup6KrTKRxTgjcXE7PbKLRH84e6ckyXDt3fu7afUb) was encrypted"
echo "into: 6PYLHmDf6AjF4J1z4k8VoF7k8JbHKzF1uup6KrTKRxTgjcXE7PbKLRH84e6ckyXDt3fu7afUb"
echo ""
echo "To deploy contracts, you have two options:"
echo ""
echo "OPTION 1: Deploy with your existing wallet (requires password)"
echo "--------------------------------------------------------"
echo "Run each command below and enter your wallet password when prompted:"
echo ""

# Generate deployment commands
for nef in build/*.nef; do
    if [ -f "$nef" ]; then
        manifest="${nef%.nef}.manifest.json"
        contract=$(basename "$nef" .nef)
        echo "# Deploy $contract"
        echo "neo-go contract deploy -i $nef -m $manifest -r $TESTNET_RPC -w $WALLET_FILE -a $DEPLOYER_ADDRESS --force"
        echo ""
    fi
done

echo ""
echo "OPTION 2: Create a new unencrypted wallet (for automation)"
echo "--------------------------------------------------------"
echo "# First, create a new wallet from your WIF key:"
echo "neo-go wallet init -w unencrypted-wallet.json --account"
echo "neo-go wallet import -w unencrypted-wallet.json --wif KzjaqMvqzF1uup6KrTKRxTgjcXE7PbKLRH84e6ckyXDt3fu7afUb"
echo ""
echo "# Then deploy using the unencrypted wallet:"
echo "for nef in build/*.nef; do"
echo "    manifest=\"\${nef%.nef}.manifest.json\""
echo "    neo-go contract deploy -i \"\$nef\" -m \"\$manifest\" -r $TESTNET_RPC -w unencrypted-wallet.json -a $DEPLOYER_ADDRESS --force"
echo "done"
echo ""
echo "OPTION 3: Test your password"
echo "---------------------------"
echo "# Try this command to test if your password works:"
echo "neo-go wallet dump -w $WALLET_FILE"
echo ""
echo "Common passwords to try:"
echo "- The password you set when creating the wallet"
echo "- 'testnet123'"
echo "- 'password'"
echo "- Your WIF key itself"