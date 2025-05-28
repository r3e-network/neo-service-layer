# Oracle Service

## Overview

The Oracle Service is a secure, enclave-based service for fetching data from external sources and delivering it to smart contracts. It provides a framework for defining data sources, fetching data, and delivering it to the blockchain with cryptographic proofs.

## Features

- **Data Fetching**: Fetch data from external sources such as APIs, websites, and databases.
- **Data Verification**: Verify the authenticity and integrity of fetched data.
- **Data Delivery**: Deliver data to smart contracts on the blockchain.
- **Batch Processing**: Process multiple data requests in a single batch for efficiency.
- **Cryptographic Proofs**: Generate cryptographic proofs for fetched data, allowing smart contracts to verify the authenticity of the data.
- **Blockchain Support**: Support for both Neo N3 and NeoX blockchains.

## Architecture

The Oracle Service is built on the Neo Service Layer framework and uses the Trusted Execution Environment (TEE) to provide secure data fetching and delivery. The service consists of the following components:

- **IOracleService**: The interface that defines the operations supported by the service.
- **OracleService**: The implementation of the service that uses the enclave to fetch and deliver data.
- **EnclaveManager**: The component that manages the communication with the enclave.

## Usage

### Service Registration

```csharp
// Register the service
services.AddNeoService<IOracleService, OracleService>();

// Register the service with the service registry
serviceProvider.RegisterAllNeoServices();
```

### Fetching Data

```csharp
// Create a data request
var request = new OracleRequest
{
    RequestId = Guid.NewGuid().ToString(),
    Url = "https://api.example.com/data",
    Path = "$.data.value",
    Headers = new Dictionary<string, string>
    {
        { "Authorization", "Bearer token123" }
    }
};

// Fetch data
var response = await oracleService.FetchDataAsync(
    request,
    BlockchainType.NeoN3);

Console.WriteLine($"Data: {response.Data}");
```

### Fetching Data in Batch

```csharp
// Create multiple data requests
var requests = new List<OracleRequest>
{
    new OracleRequest
    {
        RequestId = Guid.NewGuid().ToString(),
        Url = "https://api.example.com/data1",
        Path = "$.data.value1"
    },
    new OracleRequest
    {
        RequestId = Guid.NewGuid().ToString(),
        Url = "https://api.example.com/data2",
        Path = "$.data.value2"
    }
};

// Fetch data in batch
var responses = await oracleService.FetchDataBatchAsync(
    requests,
    BlockchainType.NeoN3);

foreach (var response in responses)
{
    Console.WriteLine($"Request ID: {response.RequestId}, Data: {response.Data}");
}
```

### Verifying Data

```csharp
// Verify data
bool isValid = await oracleService.VerifyDataAsync(
    response,
    BlockchainType.NeoN3);

if (isValid)
{
    Console.WriteLine("Data is valid.");
}
else
{
    Console.WriteLine("Data is invalid.");
}
```

## Security Considerations

- All data fetching and verification is performed within the secure enclave.
- Data is cryptographically signed to ensure authenticity.
- All operations are logged for audit purposes.

## API Reference

### FetchDataAsync

Fetches data from an external source.

```csharp
Task<OracleResponse> FetchDataAsync(
    OracleRequest request,
    BlockchainType blockchainType);
```

### FetchDataBatchAsync

Fetches data from multiple external sources in a single batch.

```csharp
Task<IEnumerable<OracleResponse>> FetchDataBatchAsync(
    IEnumerable<OracleRequest> requests,
    BlockchainType blockchainType);
```

### VerifyDataAsync

Verifies the authenticity of fetched data.

```csharp
Task<bool> VerifyDataAsync(
    OracleResponse response,
    BlockchainType blockchainType);
```

### GetSupportedDataSourcesAsync

Gets the list of supported data sources.

```csharp
Task<IEnumerable<string>> GetSupportedDataSourcesAsync(
    BlockchainType blockchainType);
```
