# Social Recovery Network

## Overview

The Social Recovery Network is a decentralized account recovery system that enhances the security and usability of abstract accounts on Neo N3 and Neo X blockchains. It provides a reputation-based guardian network with multi-factor authentication support, enabling secure and flexible account recovery options.

## Key Features

### 1. **Reputation-Based Guardian System**
- Guardians build reputation through successful recoveries
- Reputation decay prevents inactive guardians from maintaining high scores
- Weighted voting based on reputation and stake
- Slashing mechanism for malicious behavior

### 2. **Multi-Strategy Recovery**
- **Standard Recovery**: 3+ guardians, 7-day timeout
- **Emergency Recovery**: 5+ high-reputation guardians, 24-hour timeout  
- **Multi-Factor Recovery**: Combines guardian approval with authentication factors

### 3. **Trust Network**
- Guardians can establish trust relationships
- Trust levels influence confirmation weights
- Network effects strengthen security

### 4. **Economic Incentives**
- Guardians stake tokens to participate
- Recovery fees distributed to participating guardians
- Slashing for malicious behavior
- Reputation rewards for successful recoveries

### 5. **Multi-Factor Authentication**
- Email verification
- SMS verification
- TOTP (Time-based One-Time Password)
- Biometric attestations
- Zero-knowledge proof attestations

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
                         │
              ┌──────────┴──────────┐
              │  Abstract Accounts  │
              └────────────────────┘
```

## Smart Contract Integration

### Neo N3 Contract
Located at: `/contracts-neo-n3/src/Services/SocialRecoveryContract.cs`

Key methods:
- `EnrollGuardian(address, stakeAmount)`
- `InitiateRecovery(account, newOwner, strategy, isEmergency, fee)`
- `ConfirmRecovery(recoveryId)`
- `EstablishTrust(trustee, trustLevel)`
- `AddAuthFactor(factorType, factorHash)`

### Solidity Contract (Neo X)
Located at: `/contracts/SocialRecoveryNetwork.sol`

Implements EIP-4337 compatible recovery with:
- Guardian staking in GAS tokens
- Multi-factor authentication support
- Attestation service integration
- Weighted confirmation system

## Service Layer API

### Endpoints

#### Guardian Management
```http
POST /api/social-recovery/guardians/enroll
{
  "address": "0x...",
  "stakeAmount": "100000000000",
  "blockchain": "neo-n3"
}
```

#### Recovery Operations
```http
POST /api/social-recovery/recovery/initiate
{
  "accountAddress": "0x...",
  "newOwner": "0x...",
  "strategyId": "standard",
  "isEmergency": false,
  "recoveryFee": "1000000000",
  "authFactors": [
    {
      "factorType": "email",
      "proof": "..."
    }
  ]
}
```

```http
POST /api/social-recovery/recovery/{recoveryId}/confirm
```

#### Trust Management
```http
POST /api/social-recovery/trust/establish
{
  "trustee": "0x...",
  "trustLevel": 75
}
```

## Usage Examples

### 1. Setting Up Social Recovery for an Account

```csharp
// Enable social recovery on abstract account
await account.EnableSocialRecovery(socialRecoveryNetworkAddress);

// Configure recovery preferences
await account.ConfigureRecoveryPreferences(
    preferredStrategy: "standard",
    recoveryThreshold: 3,
    allowNetworkGuardians: true,
    minGuardianReputation: 500
);

// Add trusted guardians
await account.AddTrustedGuardianToNetwork(guardian1Address);
await account.AddTrustedGuardianToNetwork(guardian2Address);

// Add multi-factor authentication
await account.AddAuthFactor(
    factorType: keccak256("email"),
    factorHash: keccak256("user@example.com")
);
```

### 2. Becoming a Guardian

```csharp
// Enroll as guardian with 100 GAS stake
var guardianInfo = await socialRecoveryService.EnrollGuardianAsync(
    address: myAddress,
    stakeAmount: BigInteger.Parse("10000000000"), // 100 GAS
    blockchain: "neo-n3"
);

// Establish trust with other guardians
await socialRecoveryService.EstablishTrustAsync(
    trustee: otherGuardianAddress,
    trustLevel: 80,
    blockchain: "neo-n3"
);
```

### 3. Initiating Account Recovery

```csharp
// Standard recovery
var recovery = await socialRecoveryService.InitiateRecoveryAsync(
    accountAddress: lostAccountAddress,
    newOwner: newOwnerAddress,
    strategyId: "standard",
    isEmergency: false,
    recoveryFee: BigInteger.Parse("100000000"), // 1 GAS
    authFactors: null,
    blockchain: "neo-n3"
);

// Emergency recovery with multi-factor
var emergencyRecovery = await socialRecoveryService.InitiateRecoveryAsync(
    accountAddress: lostAccountAddress,
    newOwner: newOwnerAddress,
    strategyId: "emergency",
    isEmergency: true,
    recoveryFee: BigInteger.Parse("500000000"), // 5 GAS
    authFactors: new List<AuthFactor>
    {
        new AuthFactor
        {
            FactorType = "email",
            Proof = emailVerificationProof
        }
    },
    blockchain: "neo-n3"
);
```

## Security Considerations

### 1. **Sybil Resistance**
- Minimum stake requirement (100 GAS)
- Reputation building over time
- Identity attestations for high-value accounts

### 2. **Economic Security**
- Slashing for malicious guardians
- Recovery fees incentivize honest behavior
- Time-locked stakes prevent quick exit scams

### 3. **Privacy**
- Zero-knowledge proofs for sensitive attestations
- Encrypted recovery metadata
- Private guardian lists option

### 4. **Attack Vectors & Mitigations**
- **Collusion**: High guardian requirements, reputation weighting
- **Timing attacks**: Random confirmation weights, time delays
- **Social engineering**: Multi-factor auth, trusted guardian lists

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

## Monitoring & Analytics

The service provides comprehensive metrics:
- Total guardians in network
- Active recovery requests
- Success/failure rates
- Average recovery time
- Guardian reputation distribution
- Network stake levels

Access analytics at: `/api/social-recovery/stats`

## Future Enhancements

1. **Cross-chain recovery coordination**
2. **Machine learning for anomaly detection**
3. **Decentralized identity integration**
4. **Insurance pools for failed recoveries**
5. **Mobile app for guardian notifications**
6. **Hardware wallet integration**

## Support

For technical support or questions:
- Documentation: `/docs/social-recovery-network.md`
- API Reference: `/api/social-recovery/swagger`
- Smart Contract Audits: `/audits/social-recovery/`
- Community: Discord #social-recovery channel