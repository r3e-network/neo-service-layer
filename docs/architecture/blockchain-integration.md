# Neo Service Layer - Blockchain Integration

## Overview

The Neo Service Layer provides integration with both Neo N3 and NeoX blockchains, allowing services to interact with these blockchains in a consistent way. This document describes the blockchain integration architecture and how to use it.

## Core Components

### Blockchain Client Interface

The `IBlockchainClient` interface defines the operations that can be performed on a blockchain:

```csharp
public interface IBlockchainClient
{
    BlockchainType BlockchainType { get; }
    Task<long> GetBlockHeightAsync();
    Task<Block> GetBlockAsync(long height);
    Task<Block> GetBlockAsync(string hash);
    Task<Transaction> GetTransactionAsync(string hash);
    Task<string> SendTransactionAsync(Transaction transaction);
    Task<string> SubscribeToBlocksAsync(Func<Block, Task> callback);
    Task<bool> UnsubscribeFromBlocksAsync(string subscriptionId);
    Task<string> SubscribeToTransactionsAsync(Func<Transaction, Task> callback);
    Task<bool> UnsubscribeFromTransactionsAsync(string subscriptionId);
    Task<string> SubscribeToContractEventsAsync(string contractAddress, string eventName, Func<ContractEvent, Task> callback);
    Task<bool> UnsubscribeFromContractEventsAsync(string subscriptionId);
    Task<string> CallContractMethodAsync(string contractAddress, string method, params object[] args);
    Task<string> InvokeContractMethodAsync(string contractAddress, string method, params object[] args);
}
```

### Blockchain Client Implementations

The Neo Service Layer provides implementations of the `IBlockchainClient` interface for both Neo N3 and NeoX blockchains:

- **NeoN3Client**: Implementation for the Neo N3 blockchain.
- **NeoXClient**: Implementation for the NeoX blockchain (EVM-compatible).

### Blockchain Client Factory

The `IBlockchainClientFactory` interface and its implementation `BlockchainClientFactory` provide a way to create blockchain clients for different blockchain types:

```csharp
public interface IBlockchainClientFactory
{
    IBlockchainClient CreateClient(BlockchainType blockchainType);
    IEnumerable<BlockchainType> GetSupportedBlockchainTypes();
}
```

## Using Blockchain Integration

### Registering Blockchain Clients

To use blockchain integration in your application, you need to register the blockchain clients with the dependency injection system:

```csharp
services.AddBlockchainClients(new Dictionary<BlockchainType, string>
{
    { BlockchainType.NeoN3, "http://localhost:10332" },
    { BlockchainType.NeoX, "http://localhost:8545" }
});
```

Or, if you only need one blockchain type:

```csharp
services.AddBlockchainClient(BlockchainType.NeoN3, "http://localhost:10332");
```

### Using Blockchain Clients

Once registered, you can inject the `IBlockchainClientFactory` into your services and use it to create blockchain clients:

```csharp
public class MyService
{
    private readonly IBlockchainClientFactory _blockchainClientFactory;

    public MyService(IBlockchainClientFactory blockchainClientFactory)
    {
        _blockchainClientFactory = blockchainClientFactory;
    }

    public async Task DoSomethingAsync()
    {
        // Create a Neo N3 client
        var neoN3Client = _blockchainClientFactory.CreateClient(BlockchainType.NeoN3);

        // Get the current block height
        var blockHeight = await neoN3Client.GetBlockHeightAsync();

        // Get a block by height
        var block = await neoN3Client.GetBlockAsync(blockHeight);

        // Call a smart contract method
        var result = await neoN3Client.CallContractMethodAsync("0x1234567890abcdef", "balanceOf", "NeoN3Address");

        // Create a NeoX client
        var neoXClient = _blockchainClientFactory.CreateClient(BlockchainType.NeoX);

        // Call a smart contract method
        var result2 = await neoXClient.CallContractMethodAsync("0x1234567890abcdef", "balanceOf", "0x1234567890abcdef");
    }
}
```

### Implementing Blockchain-Aware Services

Services that need to support multiple blockchain types can implement the `IBlockchainService` interface and inherit from the `BlockchainServiceBase` class:

```csharp
public class MyBlockchainService : BlockchainServiceBase
{
    private readonly IBlockchainClientFactory _blockchainClientFactory;

    public MyBlockchainService(
        ILogger<MyBlockchainService> logger,
        IBlockchainClientFactory blockchainClientFactory)
        : base("MyBlockchainService", "My blockchain service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        _blockchainClientFactory = blockchainClientFactory;
    }

    public async Task<string> GetBalanceAsync(string address, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        var client = _blockchainClientFactory.CreateClient(blockchainType);
        var contractAddress = blockchainType == BlockchainType.NeoN3 ? "NeoN3ContractAddress" : "0x1234567890abcdef";
        return await client.CallContractMethodAsync(contractAddress, "balanceOf", address);
    }
}
```

## Smart Contract Integration

### Neo N3 Smart Contracts

For Neo N3 smart contracts, you can use the Neo Smart Contract Framework to interact with the Neo Service Layer:

```csharp
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;

namespace MyContract
{
    [Contract("0x0123456789abcdef0123456789abcdef")]
    public class MyContract : SmartContract
    {
        [InitialValue("0xabcdef0123456789abcdef0123456789", ContractParameterType.Hash160)]
        private static readonly UInt160 OracleContractAddress = default;

        public static object GetExternalData(string url, string path)
        {
            var result = (string)Contract.Call(OracleContractAddress, "getOracleData", CallFlags.All, new object[] { url, path });
            return result;
        }
    }
}
```

### NeoX Smart Contracts

For NeoX smart contracts, you can use Solidity to interact with the Neo Service Layer:

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface IOracleConsumer {
    function getOracleData(string calldata url, string calldata path) external view returns (string memory);
}

contract MyContract {
    address private oracleContract;

    constructor(address _oracleContract) {
        oracleContract = _oracleContract;
    }

    function getExternalData(string calldata url, string calldata path) external view returns (string memory) {
        return IOracleConsumer(oracleContract).getOracleData(url, path);
    }
}
```

## Conclusion

The Neo Service Layer provides a consistent way to interact with both Neo N3 and NeoX blockchains, allowing services to support multiple blockchain types with minimal code duplication. By using the blockchain integration components, you can easily add blockchain support to your services and create blockchain-aware applications.
