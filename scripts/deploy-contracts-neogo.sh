#!/bin/bash

# Deploy Neo Service Layer contracts using neo-go
# More reliable alternative deployment method

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
TESTNET_RPC="https://testnet1.neo.coz.io:443"
WALLET_ADDRESS="NTmHjwiadq4g3VHpJ5FQigQcD4fF5m8TyX"
WALLET_WIF="KzjaqMvqzF1uup6KrTKRxTgjcXE7PbKLRH84e6ckyXDt3fu7afUb"

echo -e "${BLUE}Neo Service Layer - Contract Deployment (neo-go method)${NC}"
echo -e "${BLUE}=====================================================${NC}"
echo ""

# Install neo-go if not available
if ! command -v neo-go &> /dev/null; then
    echo -e "${YELLOW}Installing neo-go...${NC}"
    
    # Download and install neo-go
    wget -q https://github.com/nspcc-dev/neo-go/releases/download/v0.105.1/neo-go-linux-amd64 -O neo-go
    chmod +x neo-go
    sudo mv neo-go /usr/local/bin/
    
    if command -v neo-go &> /dev/null; then
        echo -e "${GREEN}✓ neo-go installed successfully${NC}"
    else
        echo -e "${RED}✗ Failed to install neo-go${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✓ neo-go is available${NC}"
fi

# Change to contracts directory
cd /home/ubuntu/neo-service-layer/contracts-neo-n3

# Create deployment wallet
echo -e "${BLUE}Creating deployment wallet...${NC}"

cat > deployment.wallet << EOF
{
    "version": "1.0",
    "scrypt": {
        "n": 16384,
        "r": 8,
        "p": 8
    },
    "accounts": [
        {
            "address": "$WALLET_ADDRESS",
            "key": "$WALLET_WIF",
            "label": "deployment",
            "isDefault": true,
            "lock": false,
            "contract": {
                "script": "",
                "parameters": [],
                "deployed": false
            }
        }
    ]
}
EOF

echo -e "${GREEN}✓ Wallet created${NC}"

# Create deployment results directory
mkdir -p deployment-results
RESULTS_FILE="deployment-results/testnet-deployment-$(date +%Y%m%d-%H%M%S).json"

# Start results file
cat > "$RESULTS_FILE" << EOF
{
    "deployment_session": {
        "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
        "network": "neo-n3-testnet",
        "rpc": "$TESTNET_RPC",
        "deployer": "$WALLET_ADDRESS"
    },
    "contracts": [
EOF

echo -e "${BLUE}Building contracts...${NC}"
dotnet build -c Release

# List of contracts to deploy
CONTRACTS=(
    "SimpleStorageContract"
    "SimpleTokenContract"
    "SimpleVotingContract"
    "KeyManagementContract"
    "OracleContract"
    "CrossChainContract"
    "StorageContract"
    "NotificationContract"
    "MonitoringContract"
    "AnalyticsContract"
    "AutomationContract"
    "RandomnessContract"
    "VotingContract"
    "SocialRecoveryContract"
)

echo -e "${GREEN}Found ${#CONTRACTS[@]} contracts to deploy${NC}"

# Deploy each contract
deployed_count=0
failed_count=0

for i in "${!CONTRACTS[@]}"; do
    contract="${CONTRACTS[$i]}"
    echo ""
    echo -e "${BLUE}[$((i+1))/${#CONTRACTS[@]}] Deploying $contract...${NC}"
    
    contract_file="src/Services/${contract}.cs"
    
    if [ ! -f "$contract_file" ]; then
        echo -e "${RED}✗ Contract file not found: $contract_file${NC}"
        failed_count=$((failed_count + 1))
        continue
    fi
    
    # Compile contract first
    echo -e "${YELLOW}  Compiling $contract...${NC}"
    
    # Create a simple deployment script for this contract
    cat > "temp-${contract}.yml" << EOF
name: $contract
sourceroot: .
supportedstandards: []
events:
  - name: ContractDeployed
    parameters:
      - name: sender
        type: Hash160
permissions:
  - contract: "*"
    methods: "*"
EOF
    
    # Try to deploy using neo-go
    if neo-go contract compile -i "$contract_file" -c "temp-${contract}.yml" -m "temp-${contract}.manifest.json" -o "temp-${contract}.nef" 2>/dev/null; then
        echo -e "${GREEN}  ✓ Compiled successfully${NC}"
        
        # Deploy to testnet
        echo -e "${YELLOW}  Deploying to testnet...${NC}"
        
        deploy_result=$(neo-go contract deploy \
            -i "temp-${contract}.nef" \
            -manifest "temp-${contract}.manifest.json" \
            -r "$TESTNET_RPC" \
            -w deployment.wallet \
            -a "$WALLET_ADDRESS" \
            --force 2>&1 || echo "DEPLOY_FAILED")
        
        if echo "$deploy_result" | grep -q "DEPLOY_FAILED\|error\|Error"; then
            echo -e "${RED}  ✗ Deployment failed${NC}"
            failed_count=$((failed_count + 1))
            
            # Add to results file
            cat >> "$RESULTS_FILE" << EOF
        {
            "name": "$contract",
            "status": "failed",
            "error": "Deployment failed",
            "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
        }$([ $i -lt $((${#CONTRACTS[@]} - 1)) ] && echo "," || echo "")
EOF
        else
            echo -e "${GREEN}  ✓ Deployed successfully${NC}"
            deployed_count=$((deployed_count + 1))
            
            # Extract transaction hash if available
            tx_hash=$(echo "$deploy_result" | grep -o "0x[a-f0-9]\{64\}" | head -1 || echo "unknown")
            
            echo -e "${GREEN}    Transaction: $tx_hash${NC}"
            
            # Add to results file
            cat >> "$RESULTS_FILE" << EOF
        {
            "name": "$contract",
            "status": "deployed",
            "transaction_hash": "$tx_hash",
            "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
        }$([ $i -lt $((${#CONTRACTS[@]} - 1)) ] && echo "," || echo "")
EOF
        fi
        
        # Cleanup temporary files
        rm -f "temp-${contract}.yml" "temp-${contract}.manifest.json" "temp-${contract}.nef"
    else
        echo -e "${RED}  ✗ Compilation failed${NC}"
        failed_count=$((failed_count + 1))
        
        # Add to results file
        cat >> "$RESULTS_FILE" << EOF
        {
            "name": "$contract",
            "status": "failed",
            "error": "Compilation failed",
            "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
        }$([ $i -lt $((${#CONTRACTS[@]} - 1)) ] && echo "," || echo "")
EOF
    fi
    
    # Small delay between deployments
    sleep 2
done

# Close results file
cat >> "$RESULTS_FILE" << EOF
    ],
    "summary": {
        "total_contracts": ${#CONTRACTS[@]},
        "deployed": $deployed_count,
        "failed": $failed_count,
        "completion_time": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    }
}
EOF

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║                DEPLOYMENT COMPLETED                          ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

echo -e "${BLUE}📊 Deployment Summary:${NC}"
echo -e "  Total Contracts: ${#CONTRACTS[@]}"
echo -e "  Successfully Deployed: ${GREEN}$deployed_count${NC}"
echo -e "  Failed: ${RED}$failed_count${NC}"
echo ""

echo -e "${BLUE}📋 Results saved to: ${GREEN}$RESULTS_FILE${NC}"
echo ""

if [ $deployed_count -gt 0 ]; then
    echo -e "${GREEN}🎉 Successfully deployed $deployed_count contracts to testnet!${NC}"
    echo ""
    echo -e "${BLUE}📍 Verify deployments on testnet explorer:${NC}"
    echo -e "   ${YELLOW}https://testnet.neotube.io/address/$WALLET_ADDRESS${NC}"
fi

if [ $failed_count -gt 0 ]; then
    echo -e "${YELLOW}⚠️  $failed_count contracts failed to deploy${NC}"
    echo -e "${YELLOW}   Check contract code and try manual deployment${NC}"
fi

echo ""
echo -e "${BLUE}🔍 Next Steps:${NC}"
echo -e "  1. Verify deployments on testnet explorer"
echo -e "  2. Test contract functionality"
echo -e "  3. Update service configuration with contract hashes"
echo -e "  4. Configure frontend with deployed contract addresses"

# Cleanup
rm -f deployment.wallet

echo -e "${GREEN}✅ Deployment process completed!${NC}"