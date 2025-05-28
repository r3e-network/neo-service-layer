# Compliance Service

## Overview

The Compliance Service is a secure, enclave-based service for verifying compliance with regulatory requirements. It provides a framework for defining compliance rules, verifying transactions, addresses, and contracts against these rules, and generating compliance reports.

## Features

- **Rule-Based Compliance**: Define compliance rules that can be applied to transactions, addresses, and contracts.
- **Transaction Verification**: Verify transactions against compliance rules.
- **Address Verification**: Verify addresses against compliance rules.
- **Contract Verification**: Verify contracts against compliance rules.
- **Risk Scoring**: Assign risk scores to transactions, addresses, and contracts based on compliance rules.
- **Compliance Reports**: Generate compliance reports for audit purposes.
- **Blockchain Support**: Support for both Neo N3 and NeoX blockchains.

## Architecture

The Compliance Service is built on the Neo Service Layer framework and uses the Trusted Execution Environment (TEE) to provide secure compliance verification. The service consists of the following components:

- **IComplianceService**: The interface that defines the operations supported by the service.
- **ComplianceService**: The implementation of the service that uses the enclave to perform compliance verification.
- **EnclaveManager**: The component that manages the communication with the enclave.

## Usage

### Service Registration

```csharp
// Register the service
services.AddNeoService<IComplianceService, ComplianceService>();

// Register the service with the service registry
serviceProvider.RegisterAllNeoServices();
```

### Adding a Compliance Rule

```csharp
// Add a new compliance rule
var rule = new ComplianceRule
{
    RuleId = "rule-1",
    RuleName = "Blacklist Check",
    RuleDescription = "Check if an address is on the blacklist",
    RuleType = "Address",
    Parameters = new Dictionary<string, string>
    {
        { "blacklist", "address1,address2,address3" }
    },
    Severity = 100,
    Enabled = true
};

bool success = await complianceService.AddComplianceRuleAsync(
    rule,
    BlockchainType.NeoN3);
```

### Verifying an Address

```csharp
// Verify an address
var result = await complianceService.VerifyAddressAsync(
    "address1",
    BlockchainType.NeoN3);

if (result.Passed)
{
    Console.WriteLine("Address passed compliance checks.");
}
else
{
    Console.WriteLine($"Address failed compliance checks. Risk score: {result.RiskScore}");
    foreach (var violation in result.Violations)
    {
        Console.WriteLine($"Rule: {violation.RuleName}, Severity: {violation.Severity}, Details: {violation.Details}");
    }
}
```

### Verifying a Transaction

```csharp
// Verify a transaction
var result = await complianceService.VerifyTransactionAsync(
    transactionData,
    BlockchainType.NeoN3);
```

### Verifying a Contract

```csharp
// Verify a contract
var result = await complianceService.VerifyContractAsync(
    contractData,
    BlockchainType.NeoN3);
```

## Security Considerations

- All compliance rules and verification logic are executed within the secure enclave.
- Compliance results are cryptographically signed to ensure authenticity.
- All operations are logged for audit purposes.

## API Reference

### AddComplianceRuleAsync

Adds a compliance rule.

```csharp
Task<bool> AddComplianceRuleAsync(
    ComplianceRule rule,
    BlockchainType blockchainType);
```

### RemoveComplianceRuleAsync

Removes a compliance rule.

```csharp
Task<bool> RemoveComplianceRuleAsync(
    string ruleId,
    BlockchainType blockchainType);
```

### UpdateComplianceRuleAsync

Updates a compliance rule.

```csharp
Task<bool> UpdateComplianceRuleAsync(
    ComplianceRule rule,
    BlockchainType blockchainType);
```

### GetComplianceRulesAsync

Gets the compliance rules.

```csharp
Task<IEnumerable<ComplianceRule>> GetComplianceRulesAsync(
    BlockchainType blockchainType);
```

### VerifyTransactionAsync

Verifies a transaction.

```csharp
Task<VerificationResult> VerifyTransactionAsync(
    string transactionData,
    BlockchainType blockchainType);
```

### VerifyAddressAsync

Verifies an address.

```csharp
Task<VerificationResult> VerifyAddressAsync(
    string address,
    BlockchainType blockchainType);
```

### VerifyContractAsync

Verifies a contract.

```csharp
Task<VerificationResult> VerifyContractAsync(
    string contractData,
    BlockchainType blockchainType);
```
