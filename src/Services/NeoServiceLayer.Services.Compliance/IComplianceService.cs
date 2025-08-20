using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Compliance.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


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

    // New methods for controller compatibility

    /// <summary>
    /// Checks compliance for a transaction or entity.
    /// </summary>
    /// <param name="request">The compliance check request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The compliance check result.</returns>
    Task<ComplianceCheckResult> CheckComplianceAsync(ComplianceCheckRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Generates a compliance report.
    /// </summary>
    /// <param name="request">The compliance report request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The compliance report result.</returns>
    Task<ComplianceReportResult> GenerateComplianceReportAsync(ComplianceReportRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates a compliance rule.
    /// </summary>
    /// <param name="request">The create compliance rule request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The compliance rule result.</returns>
    Task<ComplianceRuleResult> CreateComplianceRuleAsync(CreateComplianceRuleRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Updates a compliance rule.
    /// </summary>
    /// <param name="request">The update compliance rule request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The compliance rule result.</returns>
    Task<ComplianceRuleResult> UpdateComplianceRuleAsync(UpdateComplianceRuleRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a compliance rule.
    /// </summary>
    /// <param name="request">The delete compliance rule request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The compliance rule result.</returns>
    Task<ComplianceRuleResult> DeleteComplianceRuleAsync(DeleteComplianceRuleRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets compliance rules with pagination and filtering.
    /// </summary>
    /// <param name="request">The get compliance rules request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The compliance rules result.</returns>
    Task<GetComplianceRulesResult> GetComplianceRulesAsync(GetComplianceRulesRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Starts an audit.
    /// </summary>
    /// <param name="request">The start audit request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The audit result.</returns>
    Task<AuditResult> StartAuditAsync(StartAuditRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets audit status.
    /// </summary>
    /// <param name="request">The get audit status request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The audit result.</returns>
    Task<AuditResult> GetAuditStatusAsync(GetAuditStatusRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Reports a violation.
    /// </summary>
    /// <param name="request">The report violation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The violation result.</returns>
    Task<ViolationResult> ReportViolationAsync(ReportViolationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets violations with pagination and filtering.
    /// </summary>
    /// <param name="request">The get violations request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The violations result.</returns>
    Task<GetViolationsResult> GetViolationsAsync(GetViolationsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates a remediation plan.
    /// </summary>
    /// <param name="request">The create remediation plan request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The remediation plan result.</returns>
    Task<RemediationPlanResult> CreateRemediationPlanAsync(CreateRemediationPlanRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets compliance dashboard data.
    /// </summary>
    /// <param name="request">The compliance dashboard request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The compliance dashboard result.</returns>
    Task<ComplianceDashboardResult> GetComplianceDashboardAsync(ComplianceDashboardRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Requests certification.
    /// </summary>
    /// <param name="request">The request certification request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The certification result.</returns>
    Task<CertificationResult> RequestCertificationAsync(RequestCertificationRequest request, BlockchainType blockchainType);

    // AML/KYC Methods

    /// <summary>
    /// Verifies KYC for a user.
    /// </summary>
    /// <param name="request">The KYC verification request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The KYC verification result.</returns>
    Task<KycVerificationResult> VerifyKycAsync(KycVerificationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets KYC status for a user.
    /// </summary>
    /// <param name="request">The get KYC status request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The KYC status result.</returns>
    Task<KycStatusResult> GetKycStatusAsync(GetKycStatusRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Screens a transaction for AML compliance.
    /// </summary>
    /// <param name="request">The AML screening request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The AML screening result.</returns>
    Task<AmlScreeningResult> ScreenTransactionAsync(AmlScreeningRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Reports suspicious activity.
    /// </summary>
    /// <param name="request">The suspicious activity request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The suspicious activity report result.</returns>
    Task<SuspiciousActivityResult> ReportSuspiciousActivityAsync(SuspiciousActivityRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets the AML watchlist.
    /// </summary>
    /// <param name="request">The get watchlist request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The watchlist result.</returns>
    Task<WatchlistResult> GetWatchlistAsync(GetWatchlistRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Adds an address to the watchlist.
    /// </summary>
    /// <param name="request">The add to watchlist request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The watchlist operation result.</returns>
    Task<WatchlistOperationResult> AddToWatchlistAsync(AddToWatchlistRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Removes an address from the watchlist.
    /// </summary>
    /// <param name="request">The remove from watchlist request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The watchlist operation result.</returns>
    Task<WatchlistOperationResult> RemoveFromWatchlistAsync(RemoveFromWatchlistRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Assesses risk for an entity or transaction.
    /// </summary>
    /// <param name="request">The risk assessment request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The risk assessment result.</returns>
    Task<Models.RiskAssessmentResult> AssessRiskAsync(Models.RiskAssessmentRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets the risk profile for an entity.
    /// </summary>
    /// <param name="request">The get risk profile request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The risk profile result.</returns>
    Task<RiskProfileResult> GetRiskProfileAsync(GetRiskProfileRequest request, BlockchainType blockchainType);
}


