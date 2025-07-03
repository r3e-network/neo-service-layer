# Network Security Service

## Overview

The Network Security Service provides secure network communication capabilities for SGX enclaves, enabling encrypted and authenticated communication channels between enclaves and external services. It implements secure tunneling, traffic encryption, and network isolation within the trusted execution environment.

## Features

- **Secure Channels**: TLS 1.3 encrypted communication channels
- **Enclave-to-Enclave**: Direct secure communication between enclaves
- **Certificate Management**: Automated certificate generation and validation
- **Traffic Isolation**: Network segmentation within enclave boundaries
- **Protocol Support**: HTTP/HTTPS, WebSocket, and TCP protocols
- **Firewall Rules**: Configurable network access policies
- **Traffic Monitoring**: Real-time network activity logging
- **DDoS Protection**: Rate limiting and traffic filtering

## API Reference

### Create Secure Channel

Establishes a new secure communication channel.

**Endpoint**: `POST /api/v1/network/channel/create/{blockchainType}`

**Request Body**:
```json
{
  "channelName": "service-channel-01",
  "targetEndpoint": "https://remote-service.example.com",
  "protocol": "HTTPS",
  "authentication": {
    "type": "MUTUAL_TLS",
    "clientCertificate": "base64_encoded_cert"
  },
  "encryptionPolicy": {
    "algorithm": "AES-256-GCM",
    "keyRotationHours": 24
  }
}
```

**Response**:
```json
{
  "success": true,
  "channelId": "ch_123abc...",
  "status": "ACTIVE",
  "endpoint": "https://enclave.local:8443/channel/ch_123abc",
  "publicKey": "base64_encoded_public_key",
  "validUntil": "2025-01-02T00:00:00Z"
}
```

### Send Encrypted Message

Sends an encrypted message through a secure channel.

**Endpoint**: `POST /api/v1/network/channel/{channelId}/send/{blockchainType}`

**Request Body**:
```json
{
  "payload": "base64_encoded_data",
  "headers": {
    "Content-Type": "application/json",
    "X-Request-ID": "req_123"
  },
  "timeout": 5000
}
```

**Response**:
```json
{
  "success": true,
  "messageId": "msg_456def...",
  "response": "base64_encoded_response",
  "latency": 125,
  "encrypted": true
}
```

### Configure Firewall Rules

Sets network access policies for the enclave.

**Endpoint**: `PUT /api/v1/network/firewall/rules/{blockchainType}`

**Request Body**:
```json
{
  "rules": [
    {
      "name": "allow-api-server",
      "action": "ALLOW",
      "source": "enclave",
      "destination": "api.example.com",
      "port": 443,
      "protocol": "TCP"
    },
    {
      "name": "block-external",
      "action": "DENY",
      "source": "0.0.0.0/0",
      "destination": "enclave",
      "port": "*",
      "protocol": "*"
    }
  ],
  "defaultAction": "DENY"
}
```

### Monitor Network Traffic

Retrieves network traffic statistics and logs.

**Endpoint**: `GET /api/v1/network/monitor/{blockchainType}`

**Query Parameters**:
- `startTime`: Start timestamp for logs
- `endTime`: End timestamp for logs
- `channelId`: Filter by specific channel

**Response**:
```json
{
  "statistics": {
    "totalRequests": 10000,
    "successfulRequests": 9950,
    "failedRequests": 50,
    "averageLatency": 45,
    "bandwidthUsed": 1048576
  },
  "activeChannels": 5,
  "securityEvents": [
    {
      "timestamp": "2025-01-01T00:00:00Z",
      "type": "INVALID_CERTIFICATE",
      "source": "192.168.1.100",
      "action": "BLOCKED"
    }
  ]
}
```

## Configuration

Add to your `appsettings.json`:

```json
{
  "NetworkSecurityService": {
    "Enabled": true,
    "DefaultProtocol": "TLS1.3",
    "CertificateGeneration": {
      "Algorithm": "ECDSA",
      "KeySize": 384,
      "ValidityDays": 365
    },
    "Firewall": {
      "Enabled": true,
      "DefaultPolicy": "DENY",
      "LogBlocked": true
    },
    "RateLimiting": {
      "RequestsPerMinute": 1000,
      "BurstSize": 100,
      "EnableDDoSProtection": true
    },
    "Monitoring": {
      "RetentionDays": 7,
      "SampleRate": 0.1
    }
  }
}
```

## Security Architecture

### Network Isolation

```
┌─────────────────────────────────────────┐
│          SGX Enclave Boundary           │
│  ┌─────────────────────────────────┐   │
│  │   Network Security Service       │   │
│  │  ┌──────────┐  ┌─────────────┐ │   │
│  │  │ Firewall │  │ TLS Engine   │ │   │
│  │  └──────────┘  └─────────────┘ │   │
│  │  ┌──────────────────────────┐  │   │
│  │  │ Secure Channel Manager   │  │   │
│  │  └──────────────────────────┘  │   │
│  └─────────────────────────────────┘   │
│         ↓ Encrypted Traffic ↓           │
└─────────────────────────────────────────┘
                    ↓
            External Network
```

## Usage Examples

### Establish Secure API Connection

```csharp
var client = new NetworkSecurityServiceClient(apiKey);

// Create secure channel to external API
var channelRequest = new CreateChannelRequest
{
    ChannelName = "payment-api-channel",
    TargetEndpoint = "https://payment.provider.com/api",
    Protocol = NetworkProtocol.Https,
    Authentication = new MutualTlsAuth
    {
        ClientCertificate = certificate
    }
};

var channel = await client.CreateChannelAsync(channelRequest, BlockchainType.NeoN3);
```

### Send Encrypted Request

```csharp
// Send encrypted API request
var message = new NetworkMessage
{
    Payload = JsonSerializer.Serialize(paymentRequest),
    Headers = new Dictionary<string, string>
    {
        ["Authorization"] = "Bearer " + token
    }
};

var response = await client.SendMessageAsync(channel.ChannelId, message, BlockchainType.NeoN3);
var paymentResponse = JsonSerializer.Deserialize<PaymentResponse>(response.Response);
```

### Configure Network Security

```csharp
// Set up firewall rules
var rules = new FirewallRuleSet
{
    Rules = new[]
    {
        new FirewallRule
        {
            Name = "allow-blockchain-rpc",
            Action = FirewallAction.Allow,
            Destination = "neo-node.example.com",
            Port = 10332
        }
    },
    DefaultAction = FirewallAction.Deny
};

await client.ConfigureFirewallAsync(rules, BlockchainType.NeoN3);
```

## Best Practices

1. **Certificate Rotation**: Regularly rotate TLS certificates
2. **Channel Lifecycle**: Close unused channels to free resources
3. **Rate Limiting**: Implement appropriate rate limits
4. **Monitoring**: Actively monitor for security events
5. **Encryption**: Use strong encryption algorithms
6. **Access Control**: Implement least-privilege network policies

## Performance Considerations

- Channel creation: ~200ms
- Message encryption overhead: ~5-10ms
- Firewall rule evaluation: <1ms per rule
- Maximum concurrent channels: 1000
- Traffic monitoring overhead: ~2% of bandwidth

## Limitations

- Maximum message size: 10MB
- Concurrent connections per channel: 100
- Firewall rules: 1000 maximum
- Certificate cache: 10000 entries
- Monitor log retention: 7 days default

## Troubleshooting

### Common Issues

1. **Certificate Validation Failures**
   - Verify certificate chain is complete
   - Check certificate expiration
   - Ensure proper CA configuration

2. **Connection Timeouts**
   - Check firewall rules
   - Verify network connectivity
   - Review rate limiting settings

3. **Performance Degradation**
   - Monitor active channel count
   - Check for rule evaluation overhead
   - Review encryption algorithm choice

## Related Services

- [Attestation Service](attestation-service.md) - For secure attestation
- [Key Management Service](key-management-service.md) - For certificate management
- [Monitoring Service](monitoring-service.md) - For network monitoring