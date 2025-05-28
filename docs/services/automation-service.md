# Neo Service Layer - Automation Service

## Overview

The Automation Service provides reliable, decentralized smart contract automation for the Neo N3 and NeoX blockchains. It leverages Intel SGX with Occlum LibOS enclaves to ensure secure and trustworthy execution of automated tasks, similar to Chainlink Automation (formerly Keepers) but optimized for the Neo ecosystem.

## Features

- **Smart Contract Automation**: Automatically executes smart contract functions based on conditions
- **Time-Based Scheduling**: Schedules contract executions at specific times or intervals
- **Condition-Based Triggers**: Executes contracts when specific conditions are met
- **High Reliability**: Ensures consistent execution with redundancy and failover mechanisms
- **Gas Optimization**: Optimizes gas usage for automated transactions
- **Multi-Blockchain Support**: Supports both Neo N3 and NeoX blockchains
- **Custom Logic**: Supports complex custom logic for automation triggers

## Architecture

The Automation Service consists of the following components:

### Service Layer

- **IAutomationService**: Interface defining the Automation service operations
- **AutomationService**: Implementation of the Automation service, inheriting from EnclaveBlockchainServiceBase

### Enclave Layer

- **Automation Engine**: C++ code running within Intel SGX with Occlum LibOS enclaves to securely manage automation
- **Condition Evaluator**: Evaluates trigger conditions within the secure enclave
- **Scheduler**: Manages time-based and condition-based scheduling

### Blockchain Integration

- **Neo N3 Integration**: Integration with the Neo N3 blockchain for contract execution
- **NeoX Integration**: Integration with the NeoX blockchain (EVM-compatible) for contract execution

## Automation Types

### 1. Time-Based Automation
- **Cron-style Scheduling**: Execute contracts at specific times using cron expressions
- **Interval-based**: Execute contracts at regular intervals
- **One-time Execution**: Execute contracts once at a specific time

### 2. Condition-Based Automation
- **Price Triggers**: Execute when asset prices reach certain thresholds
- **Balance Triggers**: Execute when account balances change
- **Event Triggers**: Execute when specific blockchain events occur
- **Custom Logic**: Execute based on complex custom conditions

### 3. Hybrid Automation
- **Combined Triggers**: Combine time-based and condition-based triggers
- **Sequential Execution**: Execute multiple contracts in sequence
- **Conditional Chains**: Execute different contracts based on previous results

## API Reference

### IAutomationService Interface

```csharp
public interface IAutomationService : IEnclaveService, IBlockchainService
{
    Task<string> CreateUpkeepAsync(UpkeepRequest request, BlockchainType blockchainType);
    Task<bool> UpdateUpkeepAsync(string upkeepId, UpkeepUpdate update, BlockchainType blockchainType);
    Task<bool> CancelUpkeepAsync(string upkeepId, BlockchainType blockchainType);
    Task<bool> PauseUpkeepAsync(string upkeepId, BlockchainType blockchainType);
    Task<bool> ResumeUpkeepAsync(string upkeepId, BlockchainType blockchainType);
    Task<UpkeepStatus> GetUpkeepStatusAsync(string upkeepId, BlockchainType blockchainType);
    Task<IEnumerable<UpkeepInfo>> GetUpkeepsAsync(string owner, BlockchainType blockchainType);
    Task<ExecutionHistory> GetExecutionHistoryAsync(string upkeepId, int limit, BlockchainType blockchainType);
    Task<bool> FundUpkeepAsync(string upkeepId, decimal amount, BlockchainType blockchainType);
    Task<decimal> GetUpkeepBalanceAsync(string upkeepId, BlockchainType blockchainType);
}
```

#### Methods

- **CreateUpkeepAsync**: Creates a new automation upkeep
  - Parameters:
    - `request`: Upkeep creation request with configuration
    - `blockchainType`: The blockchain type
  - Returns: The upkeep ID

- **UpdateUpkeepAsync**: Updates an existing upkeep configuration
  - Parameters:
    - `upkeepId`: The ID of the upkeep to update
    - `update`: Update configuration
    - `blockchainType`: The blockchain type
  - Returns: True if the update was successful

- **CancelUpkeepAsync**: Cancels an upkeep permanently
  - Parameters:
    - `upkeepId`: The ID of the upkeep to cancel
    - `blockchainType`: The blockchain type
  - Returns: True if the cancellation was successful

- **PauseUpkeepAsync**: Temporarily pauses an upkeep
  - Parameters:
    - `upkeepId`: The ID of the upkeep to pause
    - `blockchainType`: The blockchain type
  - Returns: True if the pause was successful

- **ResumeUpkeepAsync**: Resumes a paused upkeep
  - Parameters:
    - `upkeepId`: The ID of the upkeep to resume
    - `blockchainType`: The blockchain type
  - Returns: True if the resume was successful

- **GetUpkeepStatusAsync**: Gets the current status of an upkeep
  - Parameters:
    - `upkeepId`: The ID of the upkeep
    - `blockchainType`: The blockchain type
  - Returns: Current upkeep status

- **GetUpkeepsAsync**: Gets all upkeeps for a specific owner
  - Parameters:
    - `owner`: The owner address
    - `blockchainType`: The blockchain type
  - Returns: Collection of upkeep information

- **GetExecutionHistoryAsync**: Gets the execution history for an upkeep
  - Parameters:
    - `upkeepId`: The ID of the upkeep
    - `limit`: Maximum number of history entries to return
    - `blockchainType`: The blockchain type
  - Returns: Execution history

- **FundUpkeepAsync**: Adds funds to an upkeep for gas payments
  - Parameters:
    - `upkeepId`: The ID of the upkeep
    - `amount`: Amount to fund
    - `blockchainType`: The blockchain type
  - Returns: True if the funding was successful

- **GetUpkeepBalanceAsync**: Gets the current balance of an upkeep
  - Parameters:
    - `upkeepId`: The ID of the upkeep
    - `blockchainType`: The blockchain type
  - Returns: Current balance

### Data Models

#### UpkeepRequest Class

```csharp
public class UpkeepRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string ContractAddress { get; set; }
    public string FunctionName { get; set; }
    public object[] Parameters { get; set; }
    public UpkeepType Type { get; set; }
    public TriggerConfig TriggerConfig { get; set; }
    public decimal InitialFunding { get; set; }
    public string Owner { get; set; }
    public GasConfig GasConfig { get; set; }
}
```

#### TriggerConfig Class

```csharp
public class TriggerConfig
{
    public TriggerType Type { get; set; }
    public string CronExpression { get; set; }
    public int IntervalSeconds { get; set; }
    public DateTime? ExecuteAt { get; set; }
    public ConditionConfig[] Conditions { get; set; }
    public string CustomLogic { get; set; }
}
```

#### ConditionConfig Class

```csharp
public class ConditionConfig
{
    public ConditionType Type { get; set; }
    public string Parameter { get; set; }
    public ComparisonOperator Operator { get; set; }
    public object Value { get; set; }
    public LogicalOperator LogicalOperator { get; set; }
}
```

#### UpkeepStatus Class

```csharp
public class UpkeepStatus
{
    public string UpkeepId { get; set; }
    public UpkeepState State { get; set; }
    public DateTime LastExecution { get; set; }
    public DateTime NextExecution { get; set; }
    public int ExecutionCount { get; set; }
    public decimal Balance { get; set; }
    public string LastError { get; set; }
    public PerformanceMetrics Performance { get; set; }
}
```

#### ExecutionHistory Class

```csharp
public class ExecutionHistory
{
    public string UpkeepId { get; set; }
    public ExecutionRecord[] Records { get; set; }
    public int TotalCount { get; set; }
}

public class ExecutionRecord
{
    public DateTime Timestamp { get; set; }
    public string TransactionHash { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; }
    public decimal GasUsed { get; set; }
    public decimal GasCost { get; set; }
}
```

#### Enums

```csharp
public enum UpkeepType
{
    TimeBased,
    ConditionBased,
    Hybrid,
    Custom
}

public enum TriggerType
{
    Cron,
    Interval,
    OneTime,
    Condition,
    Event,
    Custom
}

public enum ConditionType
{
    Price,
    Balance,
    BlockNumber,
    Timestamp,
    ContractState,
    Custom
}

public enum ComparisonOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual
}

public enum LogicalOperator
{
    And,
    Or,
    Not
}

public enum UpkeepState
{
    Active,
    Paused,
    Cancelled,
    OutOfFunds,
    Error
}
```

## Smart Contract Integration

### Neo N3

```csharp
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;

namespace AutomationConsumer
{
    [Contract("0x0123456789abcdef0123456789abcdef")]
    public class AutomationConsumer : SmartContract
    {
        [InitialValue("0xabcdef0123456789abcdef0123456789", ContractParameterType.Hash160)]
        private static readonly UInt160 AutomationContractAddress = default;

        // Function to be called by automation
        public static bool PerformUpkeep(byte[] performData)
        {
            // Verify the call is from the automation contract
            if (Runtime.CallingScriptHash != AutomationContractAddress)
                return false;

            // Perform the automated task
            // Implementation specific to your use case
            
            return true;
        }

        // Function to check if upkeep is needed
        public static object CheckUpkeep(byte[] checkData)
        {
            // Check conditions to determine if upkeep is needed
            bool upkeepNeeded = false;
            byte[] performData = new byte[0];

            // Your condition checking logic here
            // For example, check if a certain time has passed or condition is met

            return new object[] { upkeepNeeded, performData };
        }

        // Register upkeep with automation service
        public static string RegisterUpkeep(string name, string triggerConfig, int initialFunding)
        {
            var result = (string)Contract.Call(AutomationContractAddress, "createUpkeep", CallFlags.All, 
                new object[] { name, Runtime.ExecutingScriptHash, "performUpkeep", triggerConfig, initialFunding });
            return result;
        }
    }
}
```

### NeoX (EVM)

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface IAutomationConsumer {
    function createUpkeep(
        string calldata name,
        address target,
        string calldata functionName,
        string calldata triggerConfig,
        uint256 initialFunding
    ) external returns (string memory);
}

contract AutomationConsumer {
    address private automationContract;
    
    constructor(address _automationContract) {
        automationContract = _automationContract;
    }
    
    // Function to be called by automation
    function performUpkeep(bytes calldata performData) external {
        require(msg.sender == automationContract, "Only automation contract can call");
        
        // Perform the automated task
        // Implementation specific to your use case
    }
    
    // Function to check if upkeep is needed
    function checkUpkeep(bytes calldata checkData) external view returns (bool upkeepNeeded, bytes memory performData) {
        // Check conditions to determine if upkeep is needed
        upkeepNeeded = false;
        performData = "";
        
        // Your condition checking logic here
        // For example, check if a certain time has passed or condition is met
        
        return (upkeepNeeded, performData);
    }
    
    // Register upkeep with automation service
    function registerUpkeep(
        string calldata name,
        string calldata triggerConfig,
        uint256 initialFunding
    ) external returns (string memory) {
        return IAutomationConsumer(automationContract).createUpkeep(
            name,
            address(this),
            "performUpkeep",
            triggerConfig,
            initialFunding
        );
    }
}
```

## Use Cases

### DeFi Protocols
- **Liquidation Automation**: Automatically liquidate undercollateralized positions
- **Yield Harvesting**: Automatically compound yields in farming protocols
- **Rebalancing**: Automatically rebalance portfolio allocations

### Gaming
- **Reward Distribution**: Automatically distribute rewards to players
- **Game State Updates**: Update game states at regular intervals
- **Tournament Management**: Automatically manage tournament phases

### NFT and Collectibles
- **Auction Management**: Automatically end auctions and transfer NFTs
- **Royalty Distribution**: Distribute royalties to creators automatically
- **Metadata Updates**: Update NFT metadata based on conditions

### Infrastructure
- **Oracle Updates**: Trigger oracle data updates when needed
- **Contract Upgrades**: Automatically deploy contract upgrades
- **Maintenance Tasks**: Perform regular maintenance operations

## Security Considerations

- **Enclave Security**: All automation logic runs within secure Intel SGX with Occlum LibOS enclaves
- **Access Control**: Strict access controls for upkeep management
- **Gas Management**: Automatic gas optimization and monitoring
- **Failure Handling**: Robust error handling and retry mechanisms
- **Audit Trail**: Complete audit trail of all executions

## Deployment

The Automation Service is deployed as part of the Neo Service Layer:

- **Service Layer**: Deployed as a .NET service with high availability
- **Enclave Layer**: Deployed within Intel SGX with Occlum LibOS enclaves
- **Scheduler**: Distributed scheduling system for reliability
- **Smart Contracts**: Deployed on Neo N3 and NeoX blockchains

## Conclusion

The Automation Service enables reliable, secure, and efficient smart contract automation for the Neo ecosystem. By leveraging Intel SGX with Occlum LibOS enclaves and providing flexible trigger mechanisms, it empowers developers to build sophisticated automated applications on both Neo N3 and NeoX blockchains.
