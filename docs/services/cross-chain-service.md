# Neo Service Layer - Cross-Chain Service

## Overview

The Cross-Chain Service provides secure and reliable cross-chain interoperability for the Neo ecosystem, enabling seamless communication and asset transfers between Neo N3, NeoX, and other supported blockchains. It leverages Intel SGX with Occlum LibOS enclaves to ensure the security and integrity of cross-chain operations, similar to Chainlink CCIP but optimized for the Neo ecosystem.

## Features

- **Cross-Chain Messaging**: Send arbitrary data between different blockchains
- **Token Transfers**: Secure transfer of tokens across chains
- **Smart Contract Calls**: Execute smart contract functions on remote chains
- **Message Verification**: Cryptographic verification of cross-chain messages
- **Multi-Blockchain Support**: Supports Neo N3, NeoX, Ethereum, and other EVM chains
- **Programmable Transfers**: Combine token transfers with smart contract execution
- **Rate Limiting**: Built-in rate limiting and security controls

## Architecture

The Cross-Chain Service consists of the following components:

### Service Layer

- **ICrossChainService**: Interface defining the Cross-Chain service operations
- **CrossChainService**: Implementation of the Cross-Chain service, inheriting from EnclaveBlockchainServiceBase

### Enclave Layer

- **Message Router**: C++ code running within Intel SGX with Occlum LibOS enclaves to securely route messages
- **Cryptographic Verifier**: Verifies message authenticity and integrity within the enclave
- **State Manager**: Manages cross-chain state and transaction tracking

### Blockchain Integration

- **Neo N3 Integration**: Native integration with Neo N3 blockchain
- **NeoX Integration**: Native integration with NeoX blockchain (EVM-compatible)
- **External Chain Support**: Support for Ethereum, Polygon, BSC, and other EVM chains

## Cross-Chain Operations

### 1. Message Passing
- **Arbitrary Data**: Send any data structure between chains
- **Event Notifications**: Notify contracts on other chains about events
- **State Synchronization**: Keep state synchronized across chains

### 2. Token Transfers
- **Native Tokens**: Transfer native tokens (NEO, GAS, ETH, etc.)
- **Wrapped Tokens**: Create and manage wrapped token representations
- **Multi-Hop Transfers**: Transfer tokens through multiple chains

### 3. Smart Contract Execution
- **Remote Calls**: Execute functions on contracts on other chains
- **Conditional Execution**: Execute based on conditions from other chains
- **Atomic Operations**: Ensure atomicity across multiple chains

## API Reference

### ICrossChainService Interface

```csharp
public interface ICrossChainService : IEnclaveService, IBlockchainService
{
    Task<string> SendMessageAsync(CrossChainMessage message);
    Task<string> TransferTokensAsync(CrossChainTransfer transfer);
    Task<string> ExecuteRemoteCallAsync(RemoteCall call);
    Task<MessageStatus> GetMessageStatusAsync(string messageId);
    Task<IEnumerable<CrossChainMessage>> GetPendingMessagesAsync(BlockchainType destinationChain);
    Task<bool> VerifyMessageAsync(string messageId, string proof);
    Task<CrossChainRoute> GetOptimalRouteAsync(BlockchainType source, BlockchainType destination);
    Task<decimal> EstimateFeesAsync(CrossChainOperation operation);
    Task<IEnumerable<SupportedChain>> GetSupportedChainsAsync();
    Task<bool> RegisterTokenMappingAsync(TokenMapping mapping);
}
```

#### Methods

- **SendMessageAsync**: Sends a message to another blockchain
  - Parameters:
    - `message`: Cross-chain message to send
  - Returns: Message ID for tracking

- **TransferTokensAsync**: Transfers tokens to another blockchain
  - Parameters:
    - `transfer`: Cross-chain transfer details
  - Returns: Transfer ID for tracking

- **ExecuteRemoteCallAsync**: Executes a smart contract function on another blockchain
  - Parameters:
    - `call`: Remote call details
  - Returns: Call ID for tracking

- **GetMessageStatusAsync**: Gets the status of a cross-chain message
  - Parameters:
    - `messageId`: The ID of the message
  - Returns: Current message status

- **GetPendingMessagesAsync**: Gets pending messages for a destination chain
  - Parameters:
    - `destinationChain`: The destination blockchain
  - Returns: Collection of pending messages

- **VerifyMessageAsync**: Verifies a cross-chain message
  - Parameters:
    - `messageId`: The ID of the message
    - `proof`: Cryptographic proof
  - Returns: True if the message is valid

- **GetOptimalRouteAsync**: Gets the optimal route between two chains
  - Parameters:
    - `source`: Source blockchain
    - `destination`: Destination blockchain
  - Returns: Optimal route information

- **EstimateFeesAsync**: Estimates fees for a cross-chain operation
  - Parameters:
    - `operation`: Cross-chain operation details
  - Returns: Estimated fees

- **GetSupportedChainsAsync**: Gets all supported blockchains
  - Returns: Collection of supported chains

- **RegisterTokenMappingAsync**: Registers a token mapping between chains
  - Parameters:
    - `mapping`: Token mapping details
  - Returns: True if registration was successful

### Data Models

#### CrossChainMessage Class

```csharp
public class CrossChainMessage
{
    public string MessageId { get; set; }
    public BlockchainType SourceChain { get; set; }
    public BlockchainType DestinationChain { get; set; }
    public string Sender { get; set; }
    public string Receiver { get; set; }
    public byte[] Data { get; set; }
    public decimal Fee { get; set; }
    public DateTime Timestamp { get; set; }
    public int Nonce { get; set; }
    public string Signature { get; set; }
}
```

#### CrossChainTransfer Class

```csharp
public class CrossChainTransfer
{
    public string TransferId { get; set; }
    public BlockchainType SourceChain { get; set; }
    public BlockchainType DestinationChain { get; set; }
    public string TokenAddress { get; set; }
    public decimal Amount { get; set; }
    public string Sender { get; set; }
    public string Receiver { get; set; }
    public decimal Fee { get; set; }
    public byte[] AdditionalData { get; set; }
}
```

#### RemoteCall Class

```csharp
public class RemoteCall
{
    public string CallId { get; set; }
    public BlockchainType SourceChain { get; set; }
    public BlockchainType DestinationChain { get; set; }
    public string ContractAddress { get; set; }
    public string FunctionName { get; set; }
    public object[] Parameters { get; set; }
    public decimal GasLimit { get; set; }
    public decimal Fee { get; set; }
    public string Caller { get; set; }
}
```

#### MessageStatus Class

```csharp
public class MessageStatus
{
    public string MessageId { get; set; }
    public CrossChainStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string TransactionHash { get; set; }
    public string Error { get; set; }
    public int Confirmations { get; set; }
    public int RequiredConfirmations { get; set; }
}
```

#### CrossChainRoute Class

```csharp
public class CrossChainRoute
{
    public BlockchainType Source { get; set; }
    public BlockchainType Destination { get; set; }
    public BlockchainType[] IntermediateChains { get; set; }
    public decimal EstimatedFee { get; set; }
    public int EstimatedTimeMinutes { get; set; }
    public decimal SecurityScore { get; set; }
}
```

#### TokenMapping Class

```csharp
public class TokenMapping
{
    public string SourceChain { get; set; }
    public string SourceTokenAddress { get; set; }
    public string DestinationChain { get; set; }
    public string DestinationTokenAddress { get; set; }
    public decimal ConversionRate { get; set; }
    public bool IsActive { get; set; }
}
```

#### Enums

```csharp
public enum CrossChainStatus
{
    Pending,
    Confirmed,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public enum CrossChainOperationType
{
    Message,
    TokenTransfer,
    RemoteCall,
    AtomicSwap
}
```

## Smart Contract Integration

### Neo N3

```csharp
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;

namespace CrossChainConsumer
{
    [Contract("0x0123456789abcdef0123456789abcdef")]
    public class CrossChainConsumer : SmartContract
    {
        [InitialValue("0xabcdef0123456789abcdef0123456789", ContractParameterType.Hash160)]
        private static readonly UInt160 CrossChainContractAddress = default;

        // Send a message to another chain
        public static string SendCrossChainMessage(string destinationChain, string receiver, byte[] data)
        {
            var result = (string)Contract.Call(CrossChainContractAddress, "sendMessage", CallFlags.All, 
                new object[] { destinationChain, receiver, data });
            return result;
        }

        // Transfer tokens to another chain
        public static string TransferTokens(string destinationChain, string tokenAddress, decimal amount, string receiver)
        {
            var result = (string)Contract.Call(CrossChainContractAddress, "transferTokens", CallFlags.All, 
                new object[] { destinationChain, tokenAddress, amount, receiver });
            return result;
        }

        // Receive a cross-chain message
        public static bool ReceiveCrossChainMessage(string messageId, string sourceChain, string sender, byte[] data)
        {
            // Verify the call is from the cross-chain contract
            if (Runtime.CallingScriptHash != CrossChainContractAddress)
                return false;

            // Process the received message
            // Implementation specific to your use case
            
            return true;
        }

        // Execute a remote call on another chain
        public static string ExecuteRemoteCall(string destinationChain, string contractAddress, string functionName, object[] parameters)
        {
            var result = (string)Contract.Call(CrossChainContractAddress, "executeRemoteCall", CallFlags.All, 
                new object[] { destinationChain, contractAddress, functionName, parameters });
            return result;
        }
    }
}
```

### NeoX (EVM)

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface ICrossChainConsumer {
    function sendMessage(string calldata destinationChain, address receiver, bytes calldata data) external returns (string memory);
    function transferTokens(string calldata destinationChain, address tokenAddress, uint256 amount, address receiver) external returns (string memory);
    function executeRemoteCall(string calldata destinationChain, address contractAddress, string calldata functionName, bytes calldata parameters) external returns (string memory);
}

contract CrossChainConsumer {
    address private crossChainContract;
    
    event CrossChainMessageReceived(string messageId, string sourceChain, address sender, bytes data);
    
    constructor(address _crossChainContract) {
        crossChainContract = _crossChainContract;
    }
    
    // Send a message to another chain
    function sendCrossChainMessage(string calldata destinationChain, address receiver, bytes calldata data) external returns (string memory) {
        return ICrossChainConsumer(crossChainContract).sendMessage(destinationChain, receiver, data);
    }
    
    // Transfer tokens to another chain
    function transferTokens(string calldata destinationChain, address tokenAddress, uint256 amount, address receiver) external returns (string memory) {
        return ICrossChainConsumer(crossChainContract).transferTokens(destinationChain, tokenAddress, amount, receiver);
    }
    
    // Receive a cross-chain message
    function receiveCrossChainMessage(string calldata messageId, string calldata sourceChain, address sender, bytes calldata data) external {
        require(msg.sender == crossChainContract, "Only cross-chain contract can call");
        
        // Process the received message
        // Implementation specific to your use case
        
        emit CrossChainMessageReceived(messageId, sourceChain, sender, data);
    }
    
    // Execute a remote call on another chain
    function executeRemoteCall(string calldata destinationChain, address contractAddress, string calldata functionName, bytes calldata parameters) external returns (string memory) {
        return ICrossChainConsumer(crossChainContract).executeRemoteCall(destinationChain, contractAddress, functionName, parameters);
    }
}
```

## Supported Blockchains

### Primary Chains
- **Neo N3**: Native support with full feature set
- **NeoX**: Native support with EVM compatibility
- **Ethereum**: Full support for mainnet and testnets
- **Polygon**: Layer 2 scaling solution support

### Additional EVM Chains
- **Binance Smart Chain (BSC)**
- **Avalanche C-Chain**
- **Fantom Opera**
- **Arbitrum**
- **Optimism**

### Future Support
- **Bitcoin** (via wrapped tokens)
- **Polkadot** (via parachains)
- **Cosmos** (via IBC protocol)
- **Solana** (via Wormhole integration)

## Use Cases

### DeFi Applications
- **Cross-Chain Lending**: Lend assets on one chain, borrow on another
- **Arbitrage Trading**: Execute arbitrage opportunities across chains
- **Yield Farming**: Optimize yields across multiple chains
- **Liquidity Provision**: Provide liquidity across different DEXs

### Gaming and NFTs
- **Cross-Chain Gaming**: Move game assets between different blockchain games
- **NFT Bridging**: Transfer NFTs between marketplaces on different chains
- **Multi-Chain Rewards**: Distribute rewards across multiple chains

### Enterprise Solutions
- **Supply Chain**: Track goods across different blockchain networks
- **Identity Management**: Manage identity across multiple chains
- **Document Verification**: Verify documents across different networks

## Security Considerations

- **Enclave Security**: All cross-chain operations are processed within secure Intel SGX with Occlum LibOS enclaves
- **Message Verification**: Cryptographic verification of all cross-chain messages
- **Rate Limiting**: Built-in rate limiting to prevent spam and attacks
- **Multi-Signature**: Multi-signature requirements for high-value transfers
- **Time Locks**: Time locks for large transfers to allow for dispute resolution
- **Audit Trail**: Complete audit trail of all cross-chain operations

## Deployment

The Cross-Chain Service is deployed as part of the Neo Service Layer:

- **Service Layer**: Deployed as a .NET service with high availability
- **Enclave Layer**: Deployed within Intel SGX with Occlum LibOS enclaves
- **Relay Network**: Distributed relay network for message passing
- **Smart Contracts**: Deployed on all supported blockchains

## Conclusion

The Cross-Chain Service enables secure and efficient interoperability between the Neo ecosystem and other major blockchains. By leveraging Intel SGX with Occlum LibOS enclaves and providing comprehensive cross-chain capabilities, it empowers developers to build truly multi-chain applications that can leverage the best features of different blockchain networks.
