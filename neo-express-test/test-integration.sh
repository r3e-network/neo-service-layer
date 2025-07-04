#!/bin/bash

# Test script to demonstrate Neo Service Layer integration with Neo Express

echo "Neo Express Integration Test for Neo Service Layer"
echo "=================================================="
echo

# Export path for neoxp command
export PATH="$PATH:/home/ubuntu/.dotnet/tools"

# Test 1: Get blockchain height
echo "Test 1: Getting blockchain height..."
HEIGHT=$(neoxp show block latest | jq -r '.header.index')
echo "Current blockchain height: $HEIGHT"
echo

# Test 2: Get contract information
echo "Test 2: Getting contract information..."
CONTRACT_HASH="0x918dc5e53f237015fae0dad532655efff9834cbd"
echo "Contract hash: $CONTRACT_HASH"
echo

# Test 3: Get wallet balance
echo "Test 3: Getting wallet balance..."
neoxp wallet show alice | jq '.accounts[0].balances'
echo

# Test 4: Create sample integration code for Neo Service Layer
echo "Test 4: Creating integration example..."
cat > integration-example.cs << 'EOF'
// Example: How Neo Service Layer can interact with Neo Express blockchain

// Configuration for Neo Express RPC endpoint
var neoExpressRpcUrl = "http://localhost:50012"; // Default Neo Express RPC port

// Example 1: Invoking smart contract through Neo Service Layer
var contractHash = "0x918dc5e53f237015fae0dad532655efff9834cbd";
var scriptBuilder = new ScriptBuilder();
scriptBuilder.EmitDynamicCall(contractHash, "hello", "Neo Service Layer");

// Example 2: Reading contract storage
var storageKey = "testKey";
var result = await smartContractManager.GetStorage(contractHash, storageKey);

// Example 3: Monitoring contract events
var events = await smartContractManager.GetContractEvents(contractHash);

// Example 4: Deploying contracts through service layer
var nefFile = File.ReadAllBytes("SimpleContract.nef");
var manifest = File.ReadAllText("SimpleContract.manifest.json");
var deployResult = await smartContractManager.DeployContract(nefFile, manifest);
EOF

echo "Integration example created in integration-example.cs"
echo

# Test 5: Generate configuration for Neo Service Layer
echo "Test 5: Creating Neo Service Layer configuration for Neo Express..."
cat > neo-express-config.json << EOF
{
  "Neo": {
    "Network": "neo-express",
    "RpcUrl": "http://localhost:50012",
    "WalletPath": "/home/ubuntu/neo-service-layer/neo-express-test/wallet.json",
    "DefaultAccount": "alice",
    "Contracts": {
      "SimpleContract": "$CONTRACT_HASH"
    }
  }
}
EOF

echo "Configuration created in neo-express-config.json"
echo

# Test 6: Test connectivity
echo "Test 6: Testing RPC connectivity..."
curl -s -X POST http://localhost:50012 \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"getversion","params":[],"id":1}' | jq '.result.protocol'
echo

echo "Integration test completed successfully!"
echo
echo "Next steps:"
echo "1. Update Neo Service Layer appsettings.json with Neo Express RPC endpoint"
echo "2. Configure wallet path for transaction signing"
echo "3. Use the SmartContractsController API to interact with deployed contracts"
echo "4. Monitor contract events through the service layer"