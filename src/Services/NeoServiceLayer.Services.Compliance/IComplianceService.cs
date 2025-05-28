using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Compliance;

/// <summary>
/// Interface for the Compliance service.
/// </summary>
public interface IComplianceService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Verifies a transaction.
    /// </summary>
    /// <param name="transactionData">The transaction data.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The verification result.</returns>
    Task<VerificationResult> VerifyTransactionAsync(string transactionData, BlockchainType blockchainType);

    /// <summary>
    /// Verifies an address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The verification result.</returns>
    Task<VerificationResult> VerifyAddressAsync(string address, BlockchainType blockchainType);

    /// <summary>
    /// Verifies a contract.
    /// </summary>
    /// <param name="contractData">The contract data.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The verification result.</returns>
    Task<VerificationResult> VerifyContractAsync(string contractData, BlockchainType blockchainType);

    /// <summary>
    /// Gets the compliance rules.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of compliance rules.</returns>
    Task<IEnumerable<ComplianceRule>> GetComplianceRulesAsync(BlockchainType blockchainType);

    /// <summary>
    /// Adds a compliance rule.
    /// </summary>
    /// <param name="rule">The compliance rule.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the rule was added successfully, false otherwise.</returns>
    Task<bool> AddComplianceRuleAsync(ComplianceRule rule, BlockchainType blockchainType);

    /// <summary>
    /// Removes a compliance rule.
    /// </summary>
    /// <param name="ruleId">The rule ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the rule was removed successfully, false otherwise.</returns>
    Task<bool> RemoveComplianceRuleAsync(string ruleId, BlockchainType blockchainType);

    /// <summary>
    /// Updates a compliance rule.
    /// </summary>
    /// <param name="rule">The compliance rule.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the rule was updated successfully, false otherwise.</returns>
    Task<bool> UpdateComplianceRuleAsync(ComplianceRule rule, BlockchainType blockchainType);
}

/// <summary>
/// Verification result.
/// </summary>
public class VerificationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the verification passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Gets or sets the list of rule violations.
    /// </summary>
    public List<RuleViolation> Violations { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk score (0-100, where 0 is no risk and 100 is highest risk).
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the verification timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the verification ID.
    /// </summary>
    public string VerificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the proof.
    /// </summary>
    public string Proof { get; set; } = string.Empty;
}

/// <summary>
/// Rule violation.
/// </summary>
public class RuleViolation
{
    /// <summary>
    /// Gets or sets the rule ID.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule description.
    /// </summary>
    public string RuleDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity (0-100, where 0 is lowest severity and 100 is highest severity).
    /// </summary>
    public int Severity { get; set; }

    /// <summary>
    /// Gets or sets the violation details.
    /// </summary>
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Compliance rule.
/// </summary>
public class ComplianceRule
{
    /// <summary>
    /// Gets or sets the rule ID.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule description.
    /// </summary>
    public string RuleDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule type.
    /// </summary>
    public string RuleType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule parameters.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the severity (0-100, where 0 is lowest severity and 100 is highest severity).
    /// </summary>
    public int Severity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the rule is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modified date.
    /// </summary>
    public DateTime LastModifiedAt { get; set; }
}
