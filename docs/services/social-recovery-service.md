# Social Recovery Service

## Overview

The Social Recovery Service provides a decentralized account recovery mechanism with a reputation-based guardian network. It enables secure recovery of lost accounts through a network of trusted guardians, multi-factor authentication, and economic incentives.

## Key Features

- **Reputation-Based Guardian System**: Guardians build reputation through successful recoveries
- **Multi-Factor Authentication**: Support for email, SMS, TOTP, and biometric factors
- **Trust Network**: Establish trust relationships between guardians
- **Economic Incentives**: Staking and rewards for guardian participation
- **Multiple Recovery Strategies**: Standard, emergency, and multi-factor recovery options
- **Cross-Chain Support**: Available on both Neo N3 and Neo X

## Architecture

```
┌─────────────────────────────────────────────────┐
│            Social Recovery Network              │
├─────────────────────────────────────────────────┤
│  Guardian Registry │ Recovery Engine │ Trust    │
│                   │                 │ Graph    │
├─────────────────────────────────────────────────┤
│  Reputation      │ Multi-Factor    │ Incentive │
│  System          │ Auth            │ Layer     │
└─────────────────────────────────────────────────┘
```

## Guardian System

### Enrollment

Guardians must stake a minimum of 100 GAS to participate:

```csharp
// Enroll as guardian
var guardian = await socialRecoveryService.EnrollGuardianAsync(
    address: "0x123...",
    stakeAmount: BigInteger.Parse("10000000000"), // 100 GAS
    blockchain: "neo-n3"
);
```

### Reputation Scoring

Guardian reputation is calculated based on:
- Successful recovery participation (+50 points)
- Failed recovery attempts (-100 points)
- Community endorsements
- Time in network
- Stake amount

### Trust Relationships

Guardians can establish trust relationships:

```csharp
// Establish trust
await socialRecoveryService.EstablishTrustAsync(
    trustee: "0x456...",
    trustLevel: 80, // 0-100 scale
    blockchain: "neo-n3"
);
```

## Recovery Process

### 1. Initiate Recovery

Account owners or authorized guardians can initiate recovery:

```csharp
var recovery = await socialRecoveryService.InitiateRecoveryAsync(
    accountAddress: "0xabc...",
    newOwner: "0xdef...",
    strategyId: "standard",
    isEmergency: false,
    recoveryFee: BigInteger.Parse("100000000"), // 1 GAS
    authFactors: new List<AuthFactor>
    {
        new AuthFactor
        {
            FactorType = "email",
            FactorHash = "hash...",
            Proof = emailProof
        }
    },
    blockchain: "neo-n3"
);
```

### 2. Guardian Confirmations

Other guardians confirm the recovery request:

```csharp
// Confirm recovery
await socialRecoveryService.ConfirmRecoveryAsync(
    recoveryId: "0x789...",
    blockchain: "neo-n3"
);
```

### 3. Recovery Execution

When threshold is met, recovery executes automatically:
- Ownership transfers to new address
- All session keys are revoked
- Recovery fees distributed to participants

## Recovery Strategies

### Standard Recovery
- **Timeout**: 7 days
- **Min Guardians**: 3
- **Min Reputation**: 100
- **Use Case**: Regular account recovery

### Emergency Recovery
- **Timeout**: 24 hours
- **Min Guardians**: 5
- **Min Reputation**: 500
- **Use Case**: Urgent situations

### Multi-Factor Recovery
- **Timeout**: 3 days
- **Min Guardians**: 2
- **Min Reputation**: 200
- **Requires**: Additional authentication factors

## Multi-Factor Authentication

### Supported Factors

1. **Email Verification**
   - Hash-based verification
   - Time-limited codes

2. **SMS/Phone**
   - OTP verification
   - Carrier validation

3. **TOTP**
   - Google Authenticator compatible
   - 30-second windows

4. **Attestations**
   - Zero-knowledge proofs
   - Identity attestations

### Adding Auth Factors

```csharp
// Add email factor
await socialRecoveryService.AddAuthFactorAsync(
    factorType: "email",
    factorHash: Hash256("user@example.com"),
    blockchain: "neo-n3"
);
```

## Configuration

### Account Configuration

```csharp
// Configure recovery preferences
await socialRecoveryService.ConfigureAccountRecoveryAsync(
    accountAddress: "0xabc...",
    preferredStrategy: "standard",
    recoveryThreshold: 3,
    allowNetworkGuardians: true,
    minGuardianReputation: 500,
    blockchain: "neo-n3"
);
```

### Trusted Guardians

```csharp
// Add trusted guardian
await socialRecoveryService.AddTrustedGuardianAsync(
    accountAddress: "0xabc...",
    guardian: "0x123...",
    blockchain: "neo-n3"
);
```

## Economic Model

### Guardian Incentives

- **Staking Rewards**: Annual percentage yield on staked GAS
- **Recovery Fees**: Distributed among participating guardians
- **Reputation Rewards**: Higher reputation = higher fee share

### Slashing Conditions

Guardians can be slashed for:
- Malicious recovery attempts
- Repeated failures
- Inactivity (reputation decay)

## Security Considerations

### Sybil Resistance
- Minimum stake requirement (100 GAS)
- Reputation building over time
- Identity attestations for high-value accounts

### Attack Mitigation
- **Collusion**: High guardian requirements, reputation weighting
- **Timing Attacks**: Random confirmation weights, time delays
- **Social Engineering**: Multi-factor auth, trusted guardian lists

## Best Practices

### For Account Owners
1. Choose at least 5 trusted guardians
2. Set appropriate recovery thresholds (3-5)
3. Enable multi-factor authentication
4. Regularly review guardian list
5. Test recovery process periodically

### For Guardians
1. Maintain adequate stake (>100 GAS)
2. Actively participate in recoveries
3. Build trust relationships
4. Keep authentication factors updated
5. Monitor reputation score

## API Reference

### Service Methods

```csharp
public interface ISocialRecoveryService
{
    // Guardian Management
    Task<GuardianInfo> EnrollGuardianAsync(string address, BigInteger stakeAmount, string blockchain);
    Task<bool> SlashGuardianAsync(string guardian, string reason, string blockchain);
    
    // Recovery Operations
    Task<RecoveryRequest> InitiateRecoveryAsync(...);
    Task<bool> ConfirmRecoveryAsync(string recoveryId, string blockchain);
    Task<bool> CancelRecoveryAsync(string recoveryId, string blockchain);
    
    // Trust Management
    Task<bool> EstablishTrustAsync(string trustee, int trustLevel, string blockchain);
    Task<int> GetTrustLevelAsync(string truster, string trustee, string blockchain);
    
    // Configuration
    Task<bool> ConfigureAccountRecoveryAsync(...);
    Task<bool> AddTrustedGuardianAsync(string accountAddress, string guardian, string blockchain);
    
    // Multi-Factor Auth
    Task<bool> AddAuthFactorAsync(string factorType, string factorHash, string blockchain);
    Task<bool> VerifyMultiFactorAuthAsync(string accountAddress, List<AuthFactor> authFactors, string blockchain);
    
    // Queries
    Task<GuardianInfo> GetGuardianInfoAsync(string address, string blockchain);
    Task<RecoveryInfo> GetRecoveryInfoAsync(string recoveryId, string blockchain);
    Task<List<RecoveryStrategy>> GetAvailableStrategiesAsync(string blockchain);
    Task<NetworkStats> GetNetworkStatsAsync(string blockchain);
}
```

## Smart Contract Integration

### Neo N3 Contract
- Location: `/contracts-neo-n3/src/Services/SocialRecoveryContract.cs`
- Inherits from `BaseServiceContract`
- Full SGX enclave support

### Solidity Contract (Neo X)
- Location: `/contracts/SocialRecoveryNetwork.sol`
- EIP-4337 compatible
- Integrates with AbstractAccount

## Monitoring & Analytics

### Network Statistics
```csharp
var stats = await socialRecoveryService.GetNetworkStatsAsync("neo-n3");
// Returns:
// - Total guardians
// - Total/successful recoveries
// - Total staked amount
// - Average reputation score
// - Success rate
```

### Events
- `GuardianEnrolled`
- `RecoveryInitiated`
- `RecoveryConfirmed`
- `RecoveryExecuted`
- `GuardianSlashed`
- `TrustEstablished`
- `ReputationUpdated`

## Testing

### Unit Tests
```bash
dotnet test tests/SocialRecoveryServiceTests.cs
```

### Integration Tests
```bash
dotnet test tests/SocialRecoveryIntegrationTests.cs
```

### Contract Tests
```bash
# Neo N3
neo-express test SocialRecoveryContract.test.cs

# Solidity
npx hardhat test test/SocialRecoveryNetwork.test.js
```

## Deployment

### Configuration
```json
{
  "SocialRecovery": {
    "ContractHash": {
      "neo-n3": "0x...",
      "neo-x": "0x..."
    },
    "DefaultCacheDuration": "00:05:00",
    "MaxRecoveryAttempts": 3,
    "RecoveryAttemptWindow": "1.00:00:00"
  }
}
```

### Environment Variables
```bash
SOCIAL_RECOVERY_CONTRACT_NEO_N3=0x...
SOCIAL_RECOVERY_CONTRACT_NEO_X=0x...
SOCIAL_RECOVERY_MIN_STAKE=10000000000
```

## Troubleshooting

### Common Issues

1. **"Insufficient stake"**
   - Ensure guardian has staked at least 100 GAS
   - Check stake amount in contract

2. **"Guardian not active"**
   - Verify guardian enrollment status
   - Check if guardian was slashed

3. **"Recovery expired"**
   - Recovery requests have time limits
   - Use emergency recovery for urgent cases

4. **"Insufficient reputation"**
   - Build reputation through successful recoveries
   - Get endorsements from other guardians

## Support

- **Documentation**: [Social Recovery Network Guide](../social-recovery-network.md)
- **API Reference**: `/api/social-recovery/swagger`
- **Community**: Discord #social-recovery channel
- **Issues**: GitHub Issues