using System.Threading.Tasks;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for the compliance service.
    /// </summary>
    public interface IComplianceService
    {
        /// <summary>
        /// Verifies an identity.
        /// </summary>
        /// <param name="identityData">The encrypted identity data to verify.</param>
        /// <param name="verificationType">The type of verification to perform.</param>
        /// <returns>The ID of the verification request.</returns>
        Task<string> VerifyIdentityAsync(string identityData, string verificationType);

        /// <summary>
        /// Gets the result of an identity verification.
        /// </summary>
        /// <param name="verificationId">The ID of the verification request.</param>
        /// <returns>The verification result.</returns>
        Task<VerificationResult> GetVerificationResultAsync(string verificationId);

        /// <summary>
        /// Checks if a transaction complies with regulations.
        /// </summary>
        /// <param name="transactionData">The encrypted transaction data to check.</param>
        /// <returns>The compliance check result.</returns>
        Task<ComplianceCheckResult> CheckTransactionComplianceAsync(string transactionData);
    }

    /// <summary>
    /// Represents the result of a compliance check.
    /// </summary>
    public class ComplianceCheckResult
    {
        /// <summary>
        /// Gets or sets whether the transaction complies with regulations.
        /// </summary>
        public bool Compliant { get; set; }

        /// <summary>
        /// Gets or sets the reason for the compliance check result.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the risk score of the transaction.
        /// </summary>
        public double RiskScore { get; set; }
    }
}
