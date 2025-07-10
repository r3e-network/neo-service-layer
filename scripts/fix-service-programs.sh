#!/bin/bash

# Script to fix missing System.Linq in service Program.cs files

set -e

echo "Adding System.Linq to service Program.cs files..."

# List of services that need System.Linq added
services=(
    "AbstractAccount"
    "Automation"
    "Backup"
    "Compliance"
    "Compute"
    "Configuration"
    "EnclaveStorage"
    "EventSubscription"
    "Health"
    "Monitoring"
    "NetworkSecurity"
    "Notification"
    "Oracle"
    "ProofOfReserve"
    "Randomness"
    "SecretsManagement"
    "SocialRecovery"
    "Voting"
    "ZeroKnowledge"
)

for service in "${services[@]}"; do
    file="src/Services/NeoServiceLayer.Services.$service/Program.cs"
    if [ -f "$file" ]; then
        echo "Fixing $service..."
        # Add System.Linq after the last using NeoServiceLayer line
        sed -i '/using NeoServiceLayer.Services.'$service';/a using System.Linq;' "$file"
    fi
done

echo "Done! All service Program.cs files have been updated."