# Neo Service Layer API SDKs

## Overview

The Neo Service Layer provides Software Development Kits (SDKs) for various programming languages to make it easier to integrate with the API. This document provides information about the available SDKs and how to use them.

## Available SDKs

The Neo Service Layer provides SDKs for the following programming languages:

- [.NET SDK](#net-sdk)
- [JavaScript SDK](#javascript-sdk)
- [Python SDK](#python-sdk)
- [Java SDK](#java-sdk)
- [Go SDK](#go-sdk)

## .NET SDK

The .NET SDK provides a strongly-typed client for the Neo Service Layer API.

### Installation

Install the .NET SDK using NuGet:

```bash
dotnet add package NeoServiceLayer.Client
```

### Usage

```csharp
using NeoServiceLayer.Client;
using NeoServiceLayer.Client.Models;

// Create a client
var client = new NeoServiceLayerClient("your-api-key");

// Use the randomness service
var randomResult = await client.Randomness.GenerateAsync(new RandomnessGenerateRequest
{
    Blockchain = BlockchainType.NeoN3,
    Min = 1,
    Max = 100
});

Console.WriteLine($"Random number: {randomResult.Value}");

// Use the oracle service
var oracleResult = await client.Oracle.FetchAsync(new OracleFetchRequest
{
    Blockchain = BlockchainType.NeoN3,
    Url = "https://api.example.com/data",
    Path = "$.data.value"
});

Console.WriteLine($"Oracle data: {oracleResult.Value}");

// Use the key management service
var keyResult = await client.Keys.GenerateAsync(new KeyGenerateRequest
{
    Blockchain = BlockchainType.NeoN3,
    KeyType = KeyType.Secp256r1,
    KeyUsage = KeyUsage.Signing,
    Exportable = false
});

Console.WriteLine($"Key ID: {keyResult.KeyId}");
```

### Documentation

For more information about the .NET SDK, see the [.NET SDK Documentation](https://github.com/neo-project/neo-service-layer-dotnet-sdk).

## JavaScript SDK

The JavaScript SDK provides a client for the Neo Service Layer API that can be used in Node.js and browser environments.

### Installation

Install the JavaScript SDK using npm:

```bash
npm install neo-service-layer-client
```

Or using yarn:

```bash
yarn add neo-service-layer-client
```

### Usage

```javascript
import { NeoServiceLayerClient } from 'neo-service-layer-client';

// Create a client
const client = new NeoServiceLayerClient('your-api-key');

// Use the randomness service
client.randomness.generate({
  blockchain: 'neo-n3',
  min: 1,
  max: 100
})
.then(result => {
  console.log(`Random number: ${result.value}`);
})
.catch(error => {
  console.error('Error:', error);
});

// Use the oracle service
client.oracle.fetch({
  blockchain: 'neo-n3',
  url: 'https://api.example.com/data',
  path: '$.data.value'
})
.then(result => {
  console.log(`Oracle data: ${result.value}`);
})
.catch(error => {
  console.error('Error:', error);
});

// Use the key management service
client.keys.generate({
  blockchain: 'neo-n3',
  keyType: 'secp256r1',
  keyUsage: 'signing',
  exportable: false
})
.then(result => {
  console.log(`Key ID: ${result.keyId}`);
})
.catch(error => {
  console.error('Error:', error);
});
```

### Documentation

For more information about the JavaScript SDK, see the [JavaScript SDK Documentation](https://github.com/neo-project/neo-service-layer-js-sdk).

## Python SDK

The Python SDK provides a client for the Neo Service Layer API.

### Installation

Install the Python SDK using pip:

```bash
pip install neo-service-layer-client
```

### Usage

```python
from neo_service_layer_client import NeoServiceLayerClient

# Create a client
client = NeoServiceLayerClient('your-api-key')

# Use the randomness service
random_result = client.randomness.generate(
    blockchain='neo-n3',
    min=1,
    max=100
)

print(f"Random number: {random_result['value']}")

# Use the oracle service
oracle_result = client.oracle.fetch(
    blockchain='neo-n3',
    url='https://api.example.com/data',
    path='$.data.value'
)

print(f"Oracle data: {oracle_result['value']}")

# Use the key management service
key_result = client.keys.generate(
    blockchain='neo-n3',
    key_type='secp256r1',
    key_usage='signing',
    exportable=False
)

print(f"Key ID: {key_result['keyId']}")
```

### Documentation

For more information about the Python SDK, see the [Python SDK Documentation](https://github.com/neo-project/neo-service-layer-python-sdk).

## Java SDK

The Java SDK provides a client for the Neo Service Layer API.

### Installation

Add the Java SDK to your Maven project:

```xml
<dependency>
    <groupId>org.neo</groupId>
    <artifactId>neo-service-layer-client</artifactId>
    <version>1.0.0</version>
</dependency>
```

Or to your Gradle project:

```groovy
implementation 'org.neo:neo-service-layer-client:1.0.0'
```

### Usage

```java
import org.neo.servicelayer.client.NeoServiceLayerClient;
import org.neo.servicelayer.client.models.*;

// Create a client
NeoServiceLayerClient client = new NeoServiceLayerClient("your-api-key");

// Use the randomness service
RandomnessGenerateRequest randomRequest = new RandomnessGenerateRequest();
randomRequest.setBlockchain(BlockchainType.NEO_N3);
randomRequest.setMin(1);
randomRequest.setMax(100);

RandomnessGenerateResponse randomResult = client.getRandomnessService().generate(randomRequest);
System.out.println("Random number: " + randomResult.getValue());

// Use the oracle service
OracleFetchRequest oracleRequest = new OracleFetchRequest();
oracleRequest.setBlockchain(BlockchainType.NEO_N3);
oracleRequest.setUrl("https://api.example.com/data");
oracleRequest.setPath("$.data.value");

OracleFetchResponse oracleResult = client.getOracleService().fetch(oracleRequest);
System.out.println("Oracle data: " + oracleResult.getValue());

// Use the key management service
KeyGenerateRequest keyRequest = new KeyGenerateRequest();
keyRequest.setBlockchain(BlockchainType.NEO_N3);
keyRequest.setKeyType(KeyType.SECP256R1);
keyRequest.setKeyUsage(KeyUsage.SIGNING);
keyRequest.setExportable(false);

KeyGenerateResponse keyResult = client.getKeyService().generate(keyRequest);
System.out.println("Key ID: " + keyResult.getKeyId());
```

### Documentation

For more information about the Java SDK, see the [Java SDK Documentation](https://github.com/neo-project/neo-service-layer-java-sdk).

## Go SDK

The Go SDK provides a client for the Neo Service Layer API.

### Installation

Install the Go SDK using go get:

```bash
go get github.com/neo-project/neo-service-layer-go-sdk
```

### Usage

```go
package main

import (
    "fmt"
    "log"

    "github.com/neo-project/neo-service-layer-go-sdk/client"
    "github.com/neo-project/neo-service-layer-go-sdk/models"
)

func main() {
    // Create a client
    client, err := client.NewNeoServiceLayerClient("your-api-key")
    if err != nil {
        log.Fatal(err)
    }

    // Use the randomness service
    randomResult, err := client.Randomness.Generate(&models.RandomnessGenerateRequest{
        Blockchain: models.BlockchainTypeNeoN3,
        Min:        1,
        Max:        100,
    })
    if err != nil {
        log.Fatal(err)
    }

    fmt.Printf("Random number: %d\n", randomResult.Value)

    // Use the oracle service
    oracleResult, err := client.Oracle.Fetch(&models.OracleFetchRequest{
        Blockchain: models.BlockchainTypeNeoN3,
        Url:        "https://api.example.com/data",
        Path:       "$.data.value",
    })
    if err != nil {
        log.Fatal(err)
    }

    fmt.Printf("Oracle data: %s\n", oracleResult.Value)

    // Use the key management service
    keyResult, err := client.Keys.Generate(&models.KeyGenerateRequest{
        Blockchain:  models.BlockchainTypeNeoN3,
        KeyType:     models.KeyTypeSecp256r1,
        KeyUsage:    models.KeyUsageSigning,
        Exportable:  false,
    })
    if err != nil {
        log.Fatal(err)
    }

    fmt.Printf("Key ID: %s\n", keyResult.KeyId)
}
```

### Documentation

For more information about the Go SDK, see the [Go SDK Documentation](https://github.com/neo-project/neo-service-layer-go-sdk).

## References

- [Neo Service Layer API](README.md)
- [Neo Service Layer API Endpoints](endpoints.md)
- [Neo Service Layer API Authentication](authentication.md)
