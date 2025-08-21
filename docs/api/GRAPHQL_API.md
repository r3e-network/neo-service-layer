# Neo Service Layer - GraphQL API

## Overview

The Neo Service Layer now provides a comprehensive GraphQL API alongside the existing REST endpoints. GraphQL offers clients the flexibility to query exactly the data they need with a single request.

## Endpoints

- **GraphQL Endpoint**: `/graphql`
- **GraphQL Voyager (Development)**: `/graphql-voyager`
- **Schema Introspection**: Available via standard GraphQL introspection queries

## Features

### Core Capabilities

- **Flexible Queries**: Request exactly the data you need
- **Real-time Subscriptions**: WebSocket-based subscriptions for live updates
- **Type Safety**: Strongly typed schema with full introspection support
- **Authentication**: JWT-based authentication with role-based authorization
- **Performance**: Built-in DataLoader support for efficient data fetching
- **Filtering & Pagination**: Advanced filtering, sorting, and pagination capabilities

### Supported Operations

#### Queries
- **Key Management**: Retrieve key metadata, statistics, and search capabilities
- **Authentication**: User information and authentication status
- **Oracle Services**: Price data and market information
- **AI Services**: Pattern recognition and prediction results
- **Proof of Reserve**: Asset reserve attestations
- **Voting**: Governance proposals and voting data
- **Storage**: Object metadata and storage statistics
- **Configuration**: System configuration (admin only)
- **Health Monitoring**: Service health and system status

#### Mutations
- **Key Management**: Generate, rotate, sign, verify, and delete keys
- **Authentication**: Login, logout, and user management
- **Voting**: Create proposals and cast votes
- **Configuration**: Update system settings (admin only)
- **AI Services**: Train models and trigger analysis

#### Subscriptions
- **Key Events**: Real-time key creation, rotation, and expiry notifications
- **Price Updates**: Live price feed from oracle services
- **Voting Updates**: Real-time voting progress and results
- **Health Monitoring**: Service health change notifications
- **System Events**: General system event stream

## Authentication

GraphQL endpoints use the same JWT authentication as REST endpoints:

```http
Authorization: Bearer <jwt-token>
```

### Role-Based Access Control

- **Admin**: Full access to all operations
- **KeyManager**: Key management operations
- **User**: Read operations and basic functionality
- **Monitor**: Health and monitoring data access

## Example Queries

### Get Key Information
```graphql
query GetKey($keyId: String!, $blockchainType: BlockchainType!) {
  getKey(keyId: $keyId, blockchainType: $blockchainType) {
    keyId
    keyType
    keyUsage
    publicKeyHex
    createdAt
    age
    isExpired
  }
}
```

### Get Key Statistics
```graphql
query GetKeyStats($blockchainType: BlockchainType!) {
  getKeyStatistics(blockchainType: $blockchainType) {
    totalKeys
    activeKeys
    expiredKeys
    keysByType
    keysExpiringWithin24Hours
    lastKeyCreated
  }
}
```

### Get System Health
```graphql
query GetSystemHealth {
  getSystemHealth {
    status
    timestamp
    services {
      serviceName
      status
      statusColor
      checkDuration
      lastChecked
      message
    }
  }
}
```

### Get Current User
```graphql
query GetCurrentUser {
  getCurrentUser {
    id
    username
    email
    roles
    isActive
    isTwoFactorEnabled
    lastLoginAt
    isOnline
    accountAge
  }
}
```

## Example Mutations

### Generate a New Key
```graphql
mutation GenerateKey($input: GenerateKeyInput!) {
  generateKey(input: $input) {
    keyId
    keyType
    publicKeyHex
    createdAt
    exportable
  }
}
```

Variables:
```json
{
  "input": {
    "keyId": "user-key-001",
    "keyType": "Secp256k1",
    "keyUsage": "Sign,Verify",
    "blockchainType": "NEO_N3",
    "exportable": false,
    "tags": {
      "environment": "production",
      "purpose": "user-signing"
    }
  }
}
```

### Sign Data
```graphql
mutation SignData($input: SignDataInput!) {
  signData(input: $input) {
    signatureHex
    publicKeyHex
    algorithm
    timestamp
  }
}
```

### User Login
```graphql
mutation Login($username: String!, $password: String!) {
  login(username: $username, password: $password) {
    token
    refreshToken
    expiresAt
    user {
      id
      username
      roles
    }
  }
}
```

## Example Subscriptions

### Subscribe to Key Events
```graphql
subscription OnKeyCreated($blockchainType: BlockchainType) {
  onKeyCreated(blockchainType: $blockchainType) {
    eventType
    keyMetadata {
      keyId
      keyType
      createdAt
    }
    blockchainType
    timestamp
  }
}
```

### Subscribe to Price Updates
```graphql
subscription OnPriceUpdate($assetId: String) {
  onPriceUpdate(assetId: $assetId) {
    assetId
    price
    timestamp
  }
}
```

### Subscribe to Service Health Changes
```graphql
subscription OnServiceHealthChange {
  onServiceHealthChange {
    serviceName
    oldStatus
    newStatus
    timestamp
  }
}
```

## Error Handling

GraphQL errors follow a standardized format:

```json
{
  "errors": [
    {
      "message": "Unauthorized access",
      "locations": [{"line": 2, "column": 3}],
      "path": ["getKey"],
      "extensions": {
        "code": "UNAUTHORIZED"
      }
    }
  ]
}
```

### Error Codes

- `UNAUTHENTICATED`: Authentication required
- `FORBIDDEN`: Insufficient permissions
- `VALIDATION_ERROR`: Input validation failed
- `INVALID_OPERATION`: Operation not allowed in current state
- `INTERNAL_ERROR`: Server error

## Schema Introspection

The GraphQL schema supports full introspection for development tools:

```graphql
query IntrospectionQuery {
  __schema {
    types {
      name
      kind
      description
    }
  }
}
```

## Performance Considerations

### DataLoader

The GraphQL implementation uses DataLoader to optimize database queries and prevent N+1 problems. This is automatically handled for related data fetching.

### Query Complexity

Complex queries are automatically limited to prevent abuse:

- Maximum query depth: 10 levels
- Maximum query complexity: 1000 points
- Execution timeout: 30 seconds

### Caching

- Query results can be cached based on field-level cache hints
- Subscription results are delivered in real-time via WebSocket connections

## Development Tools

### GraphQL Voyager (Development Only)

Access the interactive schema explorer at `/graphql-voyager` to visualize the complete GraphQL schema and relationships.

### IDE Integration

Popular GraphQL IDEs and tools that work with this endpoint:

- **GraphQL Playground**: Built into development mode
- **Insomnia**: REST/GraphQL client
- **Postman**: GraphQL support available
- **Apollo Studio**: Advanced GraphQL tooling

## Comparison with REST API

| Feature | REST | GraphQL |
|---------|------|---------|
| Data Fetching | Multiple requests | Single request |
| Over-fetching | Common | Eliminated |
| Under-fetching | Requires multiple calls | Not applicable |
| Real-time | WebHooks/Polling | Built-in subscriptions |
| Type Safety | OpenAPI schema | Native type system |
| Caching | HTTP caching | Field-level caching |
| Learning Curve | Familiar | Higher initially |

## Migration Guide

### From REST to GraphQL

1. **Authentication**: Use the same JWT tokens
2. **Endpoints**: Replace REST calls with GraphQL queries
3. **Error Handling**: Adapt to GraphQL error format
4. **Real-time**: Replace WebHooks with GraphQL subscriptions

### Gradual Adoption

Both REST and GraphQL APIs can be used simultaneously, allowing for gradual migration:

- Keep existing REST integrations
- Use GraphQL for new features
- Migrate high-value use cases first
- Deprecate REST endpoints gradually

## Best Practices

### Query Design

1. **Request only needed fields**: Leverage GraphQL's selective fetching
2. **Use fragments**: Reuse common field selections
3. **Implement proper pagination**: Use cursor-based pagination for large datasets
4. **Handle errors gracefully**: Check both data and errors in responses

### Security

1. **Always authenticate**: Most operations require valid JWT tokens
2. **Validate permissions**: Respect role-based access controls
3. **Rate limiting**: Be mindful of query complexity and frequency
4. **Sanitize inputs**: Validate all input data

### Performance

1. **Use subscriptions wisely**: Subscribe only to necessary real-time updates
2. **Batch operations**: Combine related operations in single requests
3. **Monitor query performance**: Use introspection to analyze query costs
4. **Implement caching**: Cache query results where appropriate

---

The GraphQL API provides a powerful, flexible alternative to REST while maintaining full backward compatibility. It's particularly valuable for frontend applications that need efficient data loading and real-time capabilities.