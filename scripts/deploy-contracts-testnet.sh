#!/bin/bash

# Deploy all Neo Service Layer contracts to testnet
# Wallet: NTmHjwiadq4g3VHpJ5FQigQcD4fF5m8TyX

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
BOLD='\033[1m'
NC='\033[0m'

# Testnet configuration
TESTNET_RPC="https://testnet1.neo.coz.io:443"
WALLET_ADDRESS="NTmHjwiadq4g3VHpJ5FQigQcD4fF5m8TyX"
WALLET_PUBKEY="03407c24a382011c16be1597699cd6460f54e49c25098d4943fdf0192c80cb6917"
WALLET_WIF="KzjaqMvqzF1uup6KrTKRxTgjcXE7PbKLRH84e6ckyXDt3fu7afUb"

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║${BOLD}        Neo Service Layer - Testnet Contract Deployment${NC}${BLUE}      ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

echo -e "${GREEN}Deployment Configuration:${NC}"
echo -e "  🌐 Network: Neo N3 Testnet"
echo -e "  🔗 RPC: $TESTNET_RPC"
echo -e "  👤 Wallet: $WALLET_ADDRESS"
echo -e "  📁 Contracts Directory: contracts-neo-n3/src/Services"
echo ""

# Check prerequisites
echo -e "${BLUE}Checking prerequisites...${NC}"

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}✗ .NET SDK not found${NC}"
    exit 1
fi
echo -e "${GREEN}✓ .NET SDK available${NC}"

# Check if neo-express or neocli is available
if ! command -v neo-cli &> /dev/null && ! command -v neocli &> /dev/null; then
    echo -e "${YELLOW}⚠ Installing Neo CLI tools...${NC}"
    
    # Install Neo CLI as a global tool
    dotnet tool install --global neo-cli
    export PATH="$PATH:$HOME/.dotnet/tools"
    
    if ! command -v neo-cli &> /dev/null; then
        echo -e "${RED}✗ Failed to install Neo CLI${NC}"
        echo -e "${YELLOW}Continuing with manual deployment approach...${NC}"
    else
        echo -e "${GREEN}✓ Neo CLI installed${NC}"
    fi
else
    echo -e "${GREEN}✓ Neo CLI available${NC}"
fi

# Change to contracts directory
cd /home/ubuntu/neo-service-layer/contracts-neo-n3

echo -e "${BLUE}Step 1: Building all contracts...${NC}"

# Build the contracts project
dotnet build -c Release

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Contract build failed${NC}"
    echo -e "${YELLOW}Attempting to fix build issues...${NC}"
    
    # Try to fix common issues and rebuild
    dotnet clean
    dotnet restore
    dotnet build -c Release
    
    if [ $? -ne 0 ]; then
        echo -e "${RED}✗ Contract build still failing. Please check the contract code.${NC}"
        exit 1
    fi
fi

echo -e "${GREEN}✓ Contracts built successfully${NC}"

# Create deployment results directory
mkdir -p deployment-results
DEPLOYMENT_LOG="deployment-results/testnet-deployment-$(date +%Y%m%d-%H%M%S).log"

echo -e "${BLUE}Step 2: Preparing wallet configuration...${NC}"

# Create wallet configuration
cat > deployment-wallet.json << EOF
{
    "name": "deployment-wallet",
    "version": "1.0",
    "scrypt": {
        "n": 16384,
        "r": 8,
        "p": 8
    },
    "accounts": [
        {
            "address": "$WALLET_ADDRESS",
            "label": "deployment-account",
            "isDefault": true,
            "lock": false,
            "key": "$WALLET_WIF",
            "contract": {
                "script": "",
                "parameters": [],
                "deployed": false
            },
            "extra": null
        }
    ],
    "extra": null
}
EOF

echo -e "${GREEN}✓ Wallet configuration created${NC}"

echo -e "${BLUE}Step 3: Getting contract list and preparing deployment...${NC}"

# List of all service contracts
CONTRACTS=(
    "AbstractAccountContract"
    "AnalyticsContract"
    "AutomationContract"
    "ComplianceContract"
    "ComputeContract"
    "CrossChainContract"
    "EnergyManagementContract"
    "GameContract"
    "HealthcareContract"
    "IdentityManagementContract"
    "InsuranceContract"
    "KeyManagementContract"
    "LendingContract"
    "MarketplaceContract"
    "MonitoringContract"
    "NotificationContract"
    "OracleContract"
    "PaymentProcessingContract"
    "RandomnessContract"
    "SimpleStorageContract"
    "SimpleTokenContract"
    "SimpleVotingContract"
    "SocialRecoveryContract"
    "StorageContract"
    "SupplyChainContract"
    "TokenizationContract"
    "VotingContract"
)

echo -e "${GREEN}Found ${#CONTRACTS[@]} contracts to deploy${NC}"

# Create deployment script for each contract
echo -e "${BLUE}Step 4: Creating individual deployment scripts...${NC}"

for contract in "${CONTRACTS[@]}"; do
    cat > "deploy-${contract,,}.neo" << EOF
# Deploy $contract to testnet
open wallet deployment-wallet.json

# Wait for wallet to be ready
wait 2

# Deploy contract
deploy src/Services/${contract}.cs 0x0 0x0 0x0

# Wait for deployment to complete
wait 5

# Get contract hash and save to results
show contract
EOF
done

echo -e "${GREEN}✓ Deployment scripts created${NC}"

echo -e "${BLUE}Step 5: Creating comprehensive deployment manifest...${NC}"

# Create deployment manifest
cat > deployment-manifest.json << EOF
{
    "deployment": {
        "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
        "network": "neo-n3-testnet",
        "rpc_endpoint": "$TESTNET_RPC",
        "deployer_address": "$WALLET_ADDRESS",
        "deployer_pubkey": "$WALLET_PUBKEY"
    },
    "contracts": [
EOF

for i in "${!CONTRACTS[@]}"; do
    contract="${CONTRACTS[$i]}"
    cat >> deployment-manifest.json << EOF
        {
            "name": "$contract",
            "file": "src/Services/${contract}.cs",
            "description": "Neo Service Layer ${contract}",
            "status": "pending",
            "hash": "",
            "transaction_id": "",
            "block_height": 0,
            "deployment_cost": 0
        }$([ $i -lt $((${#CONTRACTS[@]} - 1)) ] && echo "," || echo "")
EOF
done

cat >> deployment-manifest.json << EOF
    ]
}
EOF

echo -e "${GREEN}✓ Deployment manifest created${NC}"

echo -e "${BLUE}Step 6: Manual deployment approach (Neo CLI method)...${NC}"

# Since automated deployment can be complex, provide manual deployment instructions
cat > DEPLOYMENT-INSTRUCTIONS.md << EOF
# Neo Service Layer Testnet Deployment Instructions

## Prerequisites
- Neo CLI installed
- Testnet GAS for deployment fees
- Wallet configured with deployment account

## Deployment Configuration
- **Network**: Neo N3 Testnet
- **RPC**: $TESTNET_RPC
- **Wallet**: $WALLET_ADDRESS
- **Public Key**: $WALLET_PUBKEY

## Manual Deployment Steps

### 1. Start Neo CLI
\`\`\`bash
neo-cli -r $TESTNET_RPC
\`\`\`

### 2. Open Wallet
\`\`\`
open wallet deployment-wallet.json
\`\`\`

### 3. Check Balance
\`\`\`
list asset
\`\`\`
*Ensure you have sufficient GAS for deployment fees*

### 4. Deploy Each Contract

For each contract, use the following command pattern:
\`\`\`
deploy src/Services/[ContractName].cs
\`\`\`

#### Core Service Contracts:
EOF

for contract in "${CONTRACTS[@]}"; do
    echo "- \`deploy src/Services/${contract}.cs\`" >> DEPLOYMENT-INSTRUCTIONS.md
done

cat >> DEPLOYMENT-INSTRUCTIONS.md << EOF

### 5. Record Contract Hashes
After each deployment, record the contract hash:
\`\`\`
show contract
\`\`\`

## Automated Deployment Script

An automated deployment can be run with:
\`\`\`bash
./deploy-contracts-testnet.sh
\`\`\`

## Post-Deployment Steps

1. Update service configuration with contract hashes
2. Test contract functionality
3. Update frontend with new contract addresses
4. Document deployment results

## Contract Verification

Verify deployed contracts on:
- Neo Testnet Explorer: https://testnet.neotube.io/
- Neo CLI: \`show contract [hash]\`

## Troubleshooting

### Common Issues:
1. **Insufficient GAS**: Get testnet GAS from faucet
2. **Build Errors**: Check contract syntax and dependencies
3. **Network Issues**: Verify RPC endpoint accessibility

### Testnet Resources:
- Faucet: https://testnet.neo.org/
- Explorer: https://testnet.neotube.io/
- RPC Endpoints: https://docs.neo.org/docs/en-us/node/cli/latest/rpc.html
EOF

echo -e "${GREEN}✓ Deployment instructions created${NC}"

echo -e "${BLUE}Step 7: Creating automated deployment runner...${NC}"

# Create an automated deployment runner script
cat > run-automated-deployment.sh << 'EOF'
#!/bin/bash

# Automated deployment runner for Neo testnet
set -e

CONTRACTS_DIR="src/Services"
RESULTS_DIR="deployment-results"
mkdir -p "$RESULTS_DIR"

echo "Starting automated contract deployment..."

# Function to deploy a single contract
deploy_contract() {
    local contract_name=$1
    local contract_file="$CONTRACTS_DIR/${contract_name}.cs"
    
    if [ ! -f "$contract_file" ]; then
        echo "❌ Contract file not found: $contract_file"
        return 1
    fi
    
    echo "📦 Deploying $contract_name..."
    
    # Use neo-cli for deployment (if available)
    if command -v neo-cli &> /dev/null; then
        # Create deployment script
        cat > "temp-deploy-${contract_name}.txt" << DEPLOY_EOF
open wallet deployment-wallet.json
deploy $contract_file
exit
DEPLOY_EOF
        
        # Run deployment
        neo-cli -r https://testnet1.neo.coz.io:443 < "temp-deploy-${contract_name}.txt" > "$RESULTS_DIR/${contract_name}-deployment.log" 2>&1
        
        # Check if deployment was successful
        if grep -q "Transaction successfully sent" "$RESULTS_DIR/${contract_name}-deployment.log"; then
            echo "✅ $contract_name deployed successfully"
            
            # Extract transaction ID if available
            TX_ID=$(grep -o "Transaction ID: [a-f0-9]*" "$RESULTS_DIR/${contract_name}-deployment.log" | cut -d' ' -f3)
            echo "   Transaction ID: $TX_ID"
            
            # Save result
            echo "$contract_name:$TX_ID:$(date)" >> "$RESULTS_DIR/successful-deployments.txt"
        else
            echo "❌ $contract_name deployment failed"
            echo "$contract_name:FAILED:$(date)" >> "$RESULTS_DIR/failed-deployments.txt"
        fi
        
        # Cleanup
        rm -f "temp-deploy-${contract_name}.txt"
        
        # Wait between deployments
        sleep 5
    else
        echo "⚠️ Neo CLI not available, skipping automated deployment for $contract_name"
        echo "   Use manual deployment instructions instead"
    fi
}

# Deploy all contracts
CONTRACTS=(
    "AbstractAccountContract"
    "AnalyticsContract"
    "AutomationContract"
    "ComplianceContract"
    "ComputeContract"
    "CrossChainContract"
    "EnergyManagementContract"
    "GameContract"
    "HealthcareContract"
    "IdentityManagementContract"
    "InsuranceContract"
    "KeyManagementContract"
    "LendingContract"
    "MarketplaceContract"
    "MonitoringContract"
    "NotificationContract"
    "OracleContract"
    "PaymentProcessingContract"
    "RandomnessContract"
    "SimpleStorageContract"
    "SimpleTokenContract"
    "SimpleVotingContract"
    "SocialRecoveryContract"
    "StorageContract"
    "SupplyChainContract"
    "TokenizationContract"
    "VotingContract"
)

echo "🚀 Starting deployment of ${#CONTRACTS[@]} contracts..."

for contract in "${CONTRACTS[@]}"; do
    deploy_contract "$contract"
done

echo ""
echo "🏁 Deployment completed!"
echo "📊 Check results in: $RESULTS_DIR/"
echo "📋 Successful deployments: $RESULTS_DIR/successful-deployments.txt"
echo "❌ Failed deployments: $RESULTS_DIR/failed-deployments.txt"
EOF

chmod +x run-automated-deployment.sh

echo -e "${GREEN}✓ Automated deployment runner created${NC}"

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║${BOLD}            CONTRACT DEPLOYMENT SETUP COMPLETE${NC}${GREEN}                ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

echo -e "${PURPLE}📋 Deployment Options:${NC}"
echo ""
echo -e "${BLUE}1. Automated Deployment (if Neo CLI is available):${NC}"
echo -e "   ${YELLOW}./run-automated-deployment.sh${NC}"
echo ""
echo -e "${BLUE}2. Manual Deployment:${NC}"
echo -e "   ${YELLOW}Follow instructions in: DEPLOYMENT-INSTRUCTIONS.md${NC}"
echo ""
echo -e "${BLUE}3. Individual Contract Deployment:${NC}"
echo -e "   ${YELLOW}Use the deploy-[contractname].neo scripts${NC}"
echo ""

echo -e "${GREEN}📁 Files Created:${NC}"
echo -e "   • ${BOLD}deployment-wallet.json${NC} - Wallet configuration"
echo -e "   • ${BOLD}deployment-manifest.json${NC} - Deployment tracking"
echo -e "   • ${BOLD}DEPLOYMENT-INSTRUCTIONS.md${NC} - Manual deployment guide"
echo -e "   • ${BOLD}run-automated-deployment.sh${NC} - Automated deployment"
echo -e "   • ${BOLD}deploy-*.neo${NC} - Individual contract deployment scripts"
echo ""

echo -e "${YELLOW}💰 Important Notes:${NC}"
echo -e "   • Ensure you have sufficient testnet GAS for deployment fees"
echo -e "   • Each contract deployment costs GAS (typically 10-20 GAS)"
echo -e "   • Get testnet GAS from: ${GREEN}https://testnet.neo.org/${NC}"
echo -e "   • Monitor deployments on: ${GREEN}https://testnet.neotube.io/${NC}"
echo ""

echo -e "${RED}🔑 Wallet Information:${NC}"
echo -e "   Address: ${GREEN}$WALLET_ADDRESS${NC}"
echo -e "   Public Key: ${GREEN}$WALLET_PUBKEY${NC}"
echo -e "   Network: ${GREEN}Neo N3 Testnet${NC}"
echo ""

read -p "Would you like to start the automated deployment now? (y/n): " start_deploy

if [ "$start_deploy" == "y" ] || [ "$start_deploy" == "Y" ]; then
    echo -e "${BLUE}Starting automated deployment...${NC}"
    ./run-automated-deployment.sh
else
    echo -e "${YELLOW}Deployment setup complete. Run './run-automated-deployment.sh' when ready.${NC}"
fi

echo -e "${GREEN}🎉 Contract deployment environment ready!${NC}"