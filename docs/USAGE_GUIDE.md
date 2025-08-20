# Neo Service Layer - Usage Guide

This comprehensive guide demonstrates how to use the Neo Service Layer for smart contract development and deployment across Neo N3 and Neo X networks.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Authentication & Setup](#authentication--setup)
3. [Neo N3 Smart Contracts](#neo-n3-smart-contracts)
4. [Neo X Smart Contracts](#neo-x-smart-contracts)
5. [Cross-Chain Operations](#cross-chain-operations)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)
8. [Example Applications](#example-applications)

## Quick Start

### 1. Environment Setup

```bash
# Clone the repository
git clone https://github.com/neo/neo-service-layer.git
cd neo-service-layer

# Set up environment variables
export JWT_SECRET_KEY=$(openssl rand -base64 32)
export NEO_ALLOW_SGX_SIMULATION=true

# Start the service
docker-compose up -d
```

### 2. Verify Installation

```bash
# Check service health
curl http://localhost:8080/health

# Get service information
curl http://localhost:8080/api/info
```

### 3. Obtain Authentication Token

```bash
# Login (replace with your credentials)
curl -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "your-secure-password"
  }'
```

## Authentication & Setup

### 1. User Registration

```bash
curl -X POST http://localhost:8080/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "developer",
    "email": "developer@example.com",
    "password": "SecurePassword123!",
    "role": "KeyManager"
  }'
```

### 2. Token Management

```bash
# Login and save token
TOKEN=$(curl -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "developer",
    "password": "SecurePassword123!"
  }' | jq -r '.token')

# Use token in requests
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/api/v1/smart-contracts/neo-n3/contracts
```

### 3. Service Configuration

```bash
# Check service configuration
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/api/v1/configuration/blockchain/neo-n3

# Update configuration (Admin only)
curl -X PUT http://localhost:8080/api/v1/configuration/blockchain/neo-n3 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "rpcUrl": "https://mainnet1.neo.coz.io:443",
    "networkMagic": 860833102
  }'
```

## Neo N3 Smart Contracts

### 1. Deploying a NEP-17 Token Contract

#### Prepare Contract Code

First, compile your C# contract using Neo DevPack:

```csharp
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.Numerics;

[DisplayName("MyToken")]
[ManifestExtra("Author", "Your Name")]
[ManifestExtra("Description", "My NEP-17 Token")]
[SupportedStandards("NEP-17")]
[ContractPermission("*", "onNEP17Payment")]
public class MyToken : SmartContract
{
    private const byte Prefix_TotalSupply = 0x00;
    private const byte Prefix_Balance = 0x01;

    public static string Symbol() => "MTK";
    public static byte Decimals() => 8;

    [DisplayName("Transfer")]
    public static event Action<UInt160, UInt160, BigInteger> OnTransfer;

    public static bool Transfer(UInt160 from, UInt160 to, BigInteger amount, object data)
    {
        if (from is null || !from.IsValid)
            throw new Exception("The argument 'from' is invalid.");
        if (to is null || !to.IsValid)
            throw new Exception("The argument 'to' is invalid.");
        if (amount < 0)
            throw new Exception("The amount must be a positive number.");

        if (!Runtime.CheckWitness(from)) return false;

        if (from != to && amount > 0)
        {
            if (BalanceOf(from) < amount) return false;
            
            if (from != UInt160.Zero)
                UpdateBalance(from, BalanceOf(from) - amount);
            
            if (to != UInt160.Zero)
                UpdateBalance(to, BalanceOf(to) + amount);
        }

        OnTransfer(from, to, amount);
        return true;
    }

    public static BigInteger BalanceOf(UInt160 owner)
    {
        if (owner is null || !owner.IsValid)
            throw new Exception("The argument 'owner' is invalid");
        return (BigInteger)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_Balance }.Concat(owner));
    }

    public static BigInteger TotalSupply()
    {
        return (BigInteger)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_TotalSupply });
    }

    private static void UpdateBalance(UInt160 owner, BigInteger balance)
    {
        if (balance > 0)
            Storage.Put(Storage.CurrentContext, new byte[] { Prefix_Balance }.Concat(owner), balance);
        else
            Storage.Delete(Storage.CurrentContext, new byte[] { Prefix_Balance }.Concat(owner));
    }
}
```

#### Deploy the Contract

```bash
# Prepare contract code (base64 encoded NEF + manifest)
CONTRACT_CODE=$(cat mytoken.nef mytoken.manifest.json | base64 -w 0)

# Deploy contract
curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-n3/deploy \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"contractCode\": \"$CONTRACT_CODE\",
    \"name\": \"MyToken\",
    \"version\": \"1.0.0\",
    \"author\": \"Your Name\",
    \"description\": \"My NEP-17 token contract\",
    \"gasLimit\": 10000000
  }"
```

#### Response

```json
{
  "contractHash": "0x1234567890abcdef1234567890abcdef12345678",
  "transactionHash": "0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890",
  "blockNumber": 1234567,
  "gasConsumed": 8500000,
  "isSuccess": true,
  "contractManifest": "{\"name\":\"MyToken\",\"groups\":[],\"features\":{},\"supportedStandards\":[\"NEP-17\"],...}"
}
```

### 2. Interacting with Contracts

#### Check Token Balance

```bash
# Call read-only method
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8080/api/v1/smart-contracts/neo-n3/0x1234567890abcdef1234567890abcdef12345678/call/balanceOf?parameters=[\"NbnjKGMBJzJ6j5PHeYhjJDaQ5Vy5UYu4Fv\"]"
```

#### Transfer Tokens

```bash
# Invoke state-changing method
curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-n3/0x1234567890abcdef1234567890abcdef12345678/invoke \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "method": "transfer",
    "parameters": [
      "NbnjKGMBJzJ6j5PHeYhjJDaQ5Vy5UYu4Fv",
      "NTmpbW7Vt8i7LvS4cmx1SJLFLXmkFHyAGm",
      100000000
    ],
    "gasLimit": 1000000
  }'
```

### 3. Monitoring Contract Events

```bash
# Get Transfer events from last 100 blocks
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8080/api/v1/smart-contracts/neo-n3/0x1234567890abcdef1234567890abcdef12345678/events?eventName=Transfer&fromBlock=1234400&toBlock=1234500"
```

## Neo X Smart Contracts

### 1. Deploying an ERC20 Token Contract

#### Prepare Solidity Contract

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/token/ERC20/ERC20.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

contract MyERC20Token is ERC20, Ownable {
    constructor(
        string memory name,
        string memory symbol,
        uint256 initialSupply
    ) ERC20(name, symbol) {
        _mint(msg.sender, initialSupply * 10**decimals());
    }

    function mint(address to, uint256 amount) public onlyOwner {
        _mint(to, amount);
    }

    function burn(uint256 amount) public {
        _burn(msg.sender, amount);
    }
}
```

#### Compile and Deploy

```bash
# Compile contract with Hardhat/Truffle and get bytecode + ABI
# Create deployment payload
cat > contract_payload.json << 'EOF'
{
  "bytecode": "0x608060405234801561001057600080fd5b50...",
  "abi": [
    {
      "inputs": [
        {"name": "name", "type": "string"},
        {"name": "symbol", "type": "string"},
        {"name": "initialSupply", "type": "uint256"}
      ],
      "stateMutability": "nonpayable",
      "type": "constructor"
    }
  ]
}
EOF

CONTRACT_CODE=$(echo '{"bytecode":"'$(cat contract.bin)'","abi":'$(cat contract.abi)'}' | base64 -w 0)

# Deploy to Neo X
curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-x/deploy \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"contractCode\": \"$CONTRACT_CODE\",
    \"constructorParameters\": [\"MyToken\", \"MTK\", 1000000],
    \"name\": \"MyERC20Token\",
    \"version\": \"1.0.0\",
    \"author\": \"Your Name\",
    \"gasLimit\": 2000000
  }"
```

### 2. ERC20 Token Operations

#### Check Token Balance

```bash
# Call view function
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8080/api/v1/smart-contracts/neo-x/0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234/call/balanceOf?parameters=[\"0x8ba1f109551bD432803012645Hac136c30F0B97c\"]"
```

#### Transfer Tokens

```bash
# Execute transaction
curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-x/0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234/invoke \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "method": "transfer",
    "parameters": [
      "0x8ba1f109551bD432803012645Hac136c30F0B97c",
      "1000000000000000000"
    ],
    "gasLimit": 100000
  }'
```

#### Approve Token Spending

```bash
# Approve another address to spend tokens
curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-x/0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234/invoke \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "method": "approve",
    "parameters": [
      "0x1234567890123456789012345678901234567890",
      "5000000000000000000"
    ],
    "gasLimit": 80000
  }'
```

## Cross-Chain Operations

### 1. Token Bridge Setup

#### Configure Bridge Contracts

```bash
# Update bridge configuration (Admin only)
curl -X PUT http://localhost:8080/api/v1/smart-contracts/cross-chain/bridge-config/NeoN3/NeoX \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceBridgeAddress": "0x1234567890abcdef1234567890abcdef12345678",
    "targetBridgeAddress": "0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234",
    "minConfirmations": 6,
    "signatureThreshold": 2,
    "feePercentage": 0.001,
    "operators": [
      "0xoperator1address",
      "0xoperator2address",
      "0xoperator3address"
    ]
  }'
```

### 2. Execute Cross-Chain Transfer

```bash
# Transfer tokens from Neo N3 to Neo X
curl -X POST http://localhost:8080/api/v1/smart-contracts/cross-chain/execute \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceBlockchain": "NeoN3",
    "targetBlockchain": "NeoX",
    "sourceContract": "0x1234567890abcdef1234567890abcdef12345678",
    "targetContract": "0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234",
    "method": "mint",
    "parameters": [
      "0x8ba1f109551bD432803012645Hac136c30F0B97c",
      "1000000000000000000"
    ],
    "value": 0,
    "gasLimit": 2000000
  }'
```

### 3. Monitor Cross-Chain Transaction

```bash
# Check transaction status
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8080/api/v1/smart-contracts/cross-chain/status/0xabc1234567890def1234567890abc1234567890def1234567890abc1234567890"
```

## Best Practices

### 1. Security Best Practices

#### Secure Key Management

```bash
# Use hardware security modules for production
export JWT_SECRET_KEY=$(vault kv get -field=jwt_key secret/neo-service-layer)

# Rotate keys regularly
vault kv put secret/neo-service-layer jwt_key="$(openssl rand -base64 32)"
```

#### Input Validation

```bash
# Always validate contract addresses
validate_address() {
  if [[ ! $1 =~ ^0x[a-fA-F0-9]{40}$ ]]; then
    echo "Invalid contract address: $1"
    exit 1
  fi
}

validate_address "0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234"
```

#### Gas Estimation

```bash
# Always estimate gas before invoking
estimate_gas() {
  local contract=$1
  local method=$2
  local params=$3
  
  curl -s -H "Authorization: Bearer $TOKEN" \
    "http://localhost:8080/api/v1/smart-contracts/neo-x/$contract/estimate-gas/$method?parameters=$params" \
    | jq -r '.gasEstimate'
}

GAS_ESTIMATE=$(estimate_gas "0x742d35cc..." "transfer" "[\"0x123...\",1000]")
echo "Estimated gas: $GAS_ESTIMATE"
```

### 2. Error Handling

```bash
# Robust error handling
invoke_contract() {
  local response=$(curl -s -X POST http://localhost:8080/api/v1/smart-contracts/neo-x/0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234/invoke \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{
      "method": "transfer",
      "parameters": ["0x123...", "1000000000000000000"],
      "gasLimit": 100000
    }')
  
  local status=$(echo "$response" | jq -r '.isSuccess // false')
  
  if [ "$status" = "true" ]; then
    echo "Transaction successful: $(echo "$response" | jq -r '.transactionHash')"
  else
    echo "Transaction failed: $(echo "$response" | jq -r '.errorMessage')"
    return 1
  fi
}
```

### 3. Monitoring and Logging

```bash
# Set up monitoring
monitor_contract() {
  local contract=$1
  local last_block=0
  
  while true; do
    local current_block=$(curl -s -H "Authorization: Bearer $TOKEN" \
      "http://localhost:8080/api/v1/blockchain/neo-x/block/latest" | jq -r '.number')
    
    if [ "$current_block" -gt "$last_block" ]; then
      local events=$(curl -s -H "Authorization: Bearer $TOKEN" \
        "http://localhost:8080/api/v1/smart-contracts/neo-x/$contract/events?fromBlock=$last_block&toBlock=$current_block")
      
      if [ "$(echo "$events" | jq '.events | length')" -gt 0 ]; then
        echo "New events detected: $events"
      fi
      
      last_block=$current_block
    fi
    
    sleep 10
  done
}
```

### 4. Batch Operations

```bash
# Process multiple transactions efficiently
batch_transfer() {
  local contract=$1
  shift
  local recipients=("$@")
  
  for recipient in "${recipients[@]}"; do
    echo "Transferring to $recipient..."
    invoke_contract "$contract" "transfer" "[\"$recipient\",\"1000000000000000000\"]" &
  done
  
  wait  # Wait for all transfers to complete
}

batch_transfer "0x742d35cc..." "0xrecipient1" "0xrecipient2" "0xrecipient3"
```

## Troubleshooting

### 1. Common Issues

#### Authentication Errors

```bash
# Token expired
if curl -s -H "Authorization: Bearer $TOKEN" \
   http://localhost:8080/api/v1/smart-contracts/neo-n3/contracts | \
   grep -q "authentication-required"; then
  echo "Token expired, refreshing..."
  TOKEN=$(refresh_token)
fi
```

#### Network Connectivity

```bash
# Test blockchain connectivity
test_connectivity() {
  local network=$1
  local endpoint="http://localhost:8080/api/v1/blockchain/$network/status"
  
  if curl -s -f -H "Authorization: Bearer $TOKEN" "$endpoint" > /dev/null; then
    echo "$network: Connected"
  else
    echo "$network: Connection failed"
  fi
}

test_connectivity "neo-n3"
test_connectivity "neo-x"
```

#### Gas Estimation Failures

```bash
# Retry gas estimation with higher limits
estimate_gas_with_retry() {
  local contract=$1
  local method=$2
  local params=$3
  local multipliers=(1.2 1.5 2.0)
  
  for mult in "${multipliers[@]}"; do
    local base_estimate=$(estimate_gas "$contract" "$method" "$params")
    local adjusted_estimate=$(echo "$base_estimate * $mult" | bc -l)
    
    echo "Trying gas limit: $adjusted_estimate"
    if invoke_with_gas "$contract" "$method" "$params" "$adjusted_estimate"; then
      return 0
    fi
  done
  
  echo "All gas estimates failed"
  return 1
}
```

### 2. Debugging Tools

#### Transaction Tracing

```bash
# Enable detailed logging
export NEO_SERVICE_LAYER_DEBUG=true

# Trace transaction execution
trace_transaction() {
  local tx_hash=$1
  
  curl -H "Authorization: Bearer $TOKEN" \
    "http://localhost:8080/api/v1/debug/transaction/$tx_hash/trace"
}
```

#### Contract State Inspection

```bash
# Inspect contract storage
inspect_storage() {
  local contract=$1
  local key=$2
  
  curl -H "Authorization: Bearer $TOKEN" \
    "http://localhost:8080/api/v1/debug/contract/$contract/storage/$key"
}
```

## Example Applications

### 1. DeFi Yield Farming

```bash
#!/bin/bash
# DeFi yield farming example

STAKING_CONTRACT="0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234"
TOKEN_CONTRACT="0x1234567890abcdef1234567890abcdef12345678"
AMOUNT="1000000000000000000"

# Approve staking contract
echo "Approving tokens for staking..."
curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-x/$TOKEN_CONTRACT/invoke \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"method\": \"approve\",
    \"parameters\": [\"$STAKING_CONTRACT\", \"$AMOUNT\"],
    \"gasLimit\": 80000
  }"

# Stake tokens
echo "Staking tokens..."
curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-x/$STAKING_CONTRACT/invoke \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"method\": \"stake\",
    \"parameters\": [\"$AMOUNT\"],
    \"gasLimit\": 150000
  }"

# Check rewards periodically
while true; do
  REWARDS=$(curl -s -H "Authorization: Bearer $TOKEN" \
    "http://localhost:8080/api/v1/smart-contracts/neo-x/$STAKING_CONTRACT/call/pendingRewards?parameters=[\"$(get_user_address)\"]")
  
  echo "Pending rewards: $REWARDS"
  sleep 300  # Check every 5 minutes
done
```

### 2. NFT Marketplace

```bash
#!/bin/bash
# NFT marketplace operations

NFT_CONTRACT="0x567890abcdef1234567890abcdef1234567890ab"
MARKETPLACE_CONTRACT="0xabcdef1234567890abcdef1234567890abcdef12"

# Mint NFT
mint_nft() {
  local to_address=$1
  local token_uri=$2
  
  curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-x/$NFT_CONTRACT/invoke \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{
      \"method\": \"mint\",
      \"parameters\": [\"$to_address\", \"$token_uri\"],
      \"gasLimit\": 200000
    }"
}

# List NFT for sale
list_nft() {
  local token_id=$1
  local price=$2
  
  # Approve marketplace
  curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-x/$NFT_CONTRACT/invoke \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{
      \"method\": \"approve\",
      \"parameters\": [\"$MARKETPLACE_CONTRACT\", \"$token_id\"],
      \"gasLimit\": 100000
    }"
  
  # List for sale
  curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-x/$MARKETPLACE_CONTRACT/invoke \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{
      \"method\": \"listItem\",
      \"parameters\": [\"$NFT_CONTRACT\", \"$token_id\", \"$price\"],
      \"gasLimit\": 150000
    }"
}

# Usage
mint_nft "0x8ba1f109551bD432803012645Hac136c30F0B97c" "https://metadata.example.com/1"
list_nft "1" "1000000000000000000"
```

### 3. Multi-Signature Wallet

```bash
#!/bin/bash
# Multi-signature wallet operations

MULTISIG_CONTRACT="0xdef1234567890abcdef1234567890abcdef123456"

# Propose transaction
propose_transaction() {
  local to=$1
  local value=$2
  local data=$3
  
  curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-x/$MULTISIG_CONTRACT/invoke \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{
      \"method\": \"proposeTransaction\",
      \"parameters\": [\"$to\", \"$value\", \"$data\"],
      \"gasLimit\": 200000
    }"
}

# Confirm transaction
confirm_transaction() {
  local tx_id=$1
  
  curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-x/$MULTISIG_CONTRACT/invoke \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{
      \"method\": \"confirmTransaction\",
      \"parameters\": [\"$tx_id\"],
      \"gasLimit\": 150000
    }"
}

# Execute transaction (after enough confirmations)
execute_transaction() {
  local tx_id=$1
  
  curl -X POST http://localhost:8080/api/v1/smart-contracts/neo-x/$MULTISIG_CONTRACT/invoke \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{
      \"method\": \"executeTransaction\",
      \"parameters\": [\"$tx_id\"],
      \"gasLimit\": 300000
    }"
}
```

## Next Steps

1. **Explore Advanced Features**: Check out the [Advanced Features Documentation](./ADVANCED_FEATURES.md)
2. **Join the Community**: Connect with other developers on our [Discord](https://discord.gg/neo-service-layer)
3. **Contribute**: Help improve the platform by contributing to our [GitHub repository](https://github.com/neo/neo-service-layer)
4. **Stay Updated**: Follow our [blog](https://blog.neoservicelayer.io) for the latest updates and tutorials

## Support

Need help? We're here for you:

- **Documentation**: https://docs.neoservicelayer.io
- **API Reference**: https://api.neoservicelayer.io/docs
- **GitHub Issues**: https://github.com/neo/neo-service-layer/issues
- **Discord Community**: https://discord.gg/neo-service-layer
- **Email Support**: support@neoservicelayer.io

---

Happy building with Neo Service Layer! 🚀