# Neo Service Layer - Compliance Service

## Overview

The Compliance Service provides regulatory compliance verification for blockchain transactions, addresses, and smart contracts. It enables blockchain applications to comply with regulatory requirements such as Anti-Money Laundering (AML), Know Your Customer (KYC), and sanctions screening.

## Features

- **Address Verification**: Verify blockchain addresses against compliance rules.
- **Transaction Verification**: Verify blockchain transactions against compliance rules.
- **Smart Contract Verification**: Verify smart contracts against compliance rules.
- **Risk Scoring**: Assign risk scores to addresses, transactions, and smart contracts.
- **Compliance Rules Management**: Manage and update compliance rules.
- **Compliance Reporting**: Generate compliance reports for regulatory purposes.
- **Compliance Auditing**: Audit compliance activities for internal and external review.
- **Multiple Blockchain Support**: Support for both Neo N3 and NeoX blockchains.

## Architecture

The Compliance Service consists of the following components:

### Service Layer

- **IComplianceService**: Interface defining the Compliance service operations.
- **ComplianceService**: Implementation of the Compliance service, inheriting from EnclaveBlockchainServiceBase.

### Enclave Layer

- **Enclave Implementation**: C++ code running within Occlum LibOS enclaves to securely process compliance checks.
- **Secure Communication**: Encrypted communication between the service layer and the enclave.

### Compliance Rules Engine

- **Rules Engine**: Engine for evaluating compliance rules.
- **Rules Repository**: Repository for storing and managing compliance rules.
- **Rules Updater**: Component for updating compliance rules.

### Blockchain Integration

- **Neo N3 Integration**: Integration with the Neo N3 blockchain.
- **NeoX Integration**: Integration with the NeoX blockchain (EVM-compatible).

## Data Flow

1. **Request Initiation**: A client requests a compliance check for an address, transaction, or smart contract.
2. **Data Collection**: The service collects relevant data from the blockchain and other sources.
3. **Enclave Processing**: The data is processed within Occlum LibOS enclaves to ensure confidentiality.
4. **Rules Evaluation**: The compliance rules are evaluated against the data.
5. **Result Generation**: A compliance result is generated, including a pass/fail status and risk score.
6. **Response**: The compliance result is returned to the client.

## API Reference

### IComplianceService Interface

```csharp
public interface IComplianceService : IEnclaveService, IBlockchainService
{
    Task<ComplianceResult> VerifyAddressAsync(string address, BlockchainType blockchainType);
    Task<ComplianceResult> VerifyTransactionAsync(string transactionData, BlockchainType blockchainType);
    Task<ComplianceResult> VerifySmartContractAsync(string contractData, BlockchainType blockchainType);
    Task<bool> AddComplianceRuleAsync(ComplianceRule rule, BlockchainType blockchainType);
    Task<bool> UpdateComplianceRuleAsync(string ruleId, ComplianceRule rule, BlockchainType blockchainType);
    Task<bool> DeleteComplianceRuleAsync(string ruleId, BlockchainType blockchainType);
    Task<IEnumerable<ComplianceRule>> GetComplianceRulesAsync(BlockchainType blockchainType);
    Task<ComplianceReport> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, BlockchainType blockchainType);
}
```

#### Methods

- **VerifyAddressAsync**: Verifies an address against compliance rules.
  - Parameters:
    - `address`: The address to verify.
    - `blockchainType`: The blockchain type.
  - Returns: A compliance result.

- **VerifyTransactionAsync**: Verifies a transaction against compliance rules.
  - Parameters:
    - `transactionData`: The transaction data to verify.
    - `blockchainType`: The blockchain type.
  - Returns: A compliance result.

- **VerifySmartContractAsync**: Verifies a smart contract against compliance rules.
  - Parameters:
    - `contractData`: The contract data to verify.
    - `blockchainType`: The blockchain type.
  - Returns: A compliance result.

- **AddComplianceRuleAsync**: Adds a compliance rule.
  - Parameters:
    - `rule`: The compliance rule to add.
    - `blockchainType`: The blockchain type.
  - Returns: True if the rule was added successfully.

- **UpdateComplianceRuleAsync**: Updates a compliance rule.
  - Parameters:
    - `ruleId`: The ID of the rule to update.
    - `rule`: The updated compliance rule.
    - `blockchainType`: The blockchain type.
  - Returns: True if the rule was updated successfully.

- **DeleteComplianceRuleAsync**: Deletes a compliance rule.
  - Parameters:
    - `ruleId`: The ID of the rule to delete.
    - `blockchainType`: The blockchain type.
  - Returns: True if the rule was deleted successfully.

- **GetComplianceRulesAsync**: Gets all compliance rules.
  - Parameters:
    - `blockchainType`: The blockchain type.
  - Returns: All compliance rules.

- **GenerateComplianceReportAsync**: Generates a compliance report.
  - Parameters:
    - `startDate`: The start date for the report.
    - `endDate`: The end date for the report.
    - `blockchainType`: The blockchain type.
  - Returns: A compliance report.

### ComplianceResult Class

```csharp
public class ComplianceResult
{
    public bool Passed { get; set; }
    public int RiskScore { get; set; }
    public List<ComplianceViolation> Violations { get; set; }
    public DateTime Timestamp { get; set; }
}
```

#### Properties

- **Passed**: Whether the compliance check passed.
- **RiskScore**: The risk score assigned to the address, transaction, or smart contract.
- **Violations**: A list of compliance violations.
- **Timestamp**: The timestamp of the compliance check.

### ComplianceViolation Class

```csharp
public class ComplianceViolation
{
    public string RuleId { get; set; }
    public string RuleName { get; set; }
    public string Description { get; set; }
    public int Severity { get; set; }
}
```

#### Properties

- **RuleId**: The ID of the violated rule.
- **RuleName**: The name of the violated rule.
- **Description**: A description of the violation.
- **Severity**: The severity of the violation.

### ComplianceRule Class

```csharp
public class ComplianceRule
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string RuleType { get; set; }
    public string RuleDefinition { get; set; }
    public int Severity { get; set; }
    public bool Enabled { get; set; }
}
```

#### Properties

- **Id**: The ID of the rule.
- **Name**: The name of the rule.
- **Description**: A description of the rule.
- **RuleType**: The type of the rule (e.g., "Address", "Transaction", "SmartContract").
- **RuleDefinition**: The definition of the rule in a rule language.
- **Severity**: The severity of the rule.
- **Enabled**: Whether the rule is enabled.

### ComplianceReport Class

```csharp
public class ComplianceReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalChecks { get; set; }
    public int PassedChecks { get; set; }
    public int FailedChecks { get; set; }
    public List<ComplianceViolation> TopViolations { get; set; }
    public Dictionary<string, int> ViolationsByRule { get; set; }
    public Dictionary<string, int> ViolationsByAddress { get; set; }
}
```

#### Properties

- **StartDate**: The start date of the report.
- **EndDate**: The end date of the report.
- **TotalChecks**: The total number of compliance checks.
- **PassedChecks**: The number of passed compliance checks.
- **FailedChecks**: The number of failed compliance checks.
- **TopViolations**: The top compliance violations.
- **ViolationsByRule**: Compliance violations grouped by rule.
- **ViolationsByAddress**: Compliance violations grouped by address.

## Smart Contract Integration

### Neo N3 Smart Contract

```csharp
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;

namespace ComplianceExample
{
    [DisplayName("ComplianceExample")]
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "Compliance Example")]
    public class ComplianceExample : SmartContract
    {
        [DisplayName("AddressVerified")]
        public static event Action<UInt160, bool> OnAddressVerified;

        [DisplayName("TransactionVerified")]
        public static event Action<string, bool> OnTransactionVerified;

        [InitialValue("0xabcdef0123456789abcdef0123456789", ContractParameterType.Hash160)]
        private static readonly UInt160 ComplianceContractAddress = default;

        public static bool VerifyAddress(UInt160 address)
        {
            // Call the Compliance Service to verify the address
            var result = (string)Contract.Call(ComplianceContractAddress, "verifyAddress", CallFlags.All, new object[] { address.ToString() });
            var passed = result == "true";
            
            // Emit event
            OnAddressVerified(address, passed);
            
            return passed;
        }

        public static bool VerifyTransaction(string transactionData)
        {
            // Call the Compliance Service to verify the transaction
            var result = (string)Contract.Call(ComplianceContractAddress, "verifyTransaction", CallFlags.All, new object[] { transactionData });
            var passed = result == "true";
            
            // Emit event
            OnTransactionVerified(transactionData, passed);
            
            return passed;
        }
    }
}
```

### NeoX Smart Contract

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface IComplianceConsumer {
    function verifyAddress(address addr) external view returns (bool);
    function verifyTransaction(bytes calldata transactionData) external view returns (bool);
}

contract ComplianceExample {
    address private complianceContract;
    
    event AddressVerified(address addr, bool passed);
    event TransactionVerified(bytes transactionData, bool passed);
    
    constructor(address _complianceContract) {
        complianceContract = _complianceContract;
    }
    
    function verifyAddress(address addr) external returns (bool) {
        // Call the Compliance Service to verify the address
        bool passed = IComplianceConsumer(complianceContract).verifyAddress(addr);
        
        // Emit event
        emit AddressVerified(addr, passed);
        
        return passed;
    }
    
    function verifyTransaction(bytes calldata transactionData) external returns (bool) {
        // Call the Compliance Service to verify the transaction
        bool passed = IComplianceConsumer(complianceContract).verifyTransaction(transactionData);
        
        // Emit event
        emit TransactionVerified(transactionData, passed);
        
        return passed;
    }
}
```

## Security Considerations

- **Enclave Security**: All compliance checks occur within secure Occlum LibOS enclaves.
- **Data Confidentiality**: Sensitive compliance data is encrypted during transmission and processing.
- **Rules Integrity**: Compliance rules are protected against unauthorized modification.
- **Audit Trail**: All compliance activities are logged for audit purposes.
- **Access Control**: Access to compliance functions is restricted to authorized users.

## Deployment

The Compliance Service is deployed as part of the Neo Service Layer, with the following components:

- **Service Layer**: Deployed as a .NET service.
- **Enclave Layer**: Deployed within Occlum LibOS enclaves.
- **Smart Contracts**: Deployed on the Neo N3 and NeoX blockchains.

## Conclusion

The Compliance Service provides a secure and reliable way to ensure regulatory compliance for blockchain applications. By leveraging Occlum LibOS enclaves, it ensures the confidentiality and integrity of compliance checks, enabling blockchain applications to meet regulatory requirements while maintaining privacy and security.
