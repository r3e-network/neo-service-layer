using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Advanced.FairOrdering.Models;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Advanced.FairOrdering;

/// <summary>
/// Interface for the Fair Ordering Service that provides transaction fairness and MEV protection capabilities.
/// </summary>
public interface IFairOrderingService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Creates a new ordering pool for fair transaction processing.
    /// </summary>
    /// <param name="config">The pool configuration.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The pool ID.</returns>
    Task<string> CreateOrderingPoolAsync(OrderingPoolConfig config, BlockchainType blockchainType);

    /// <summary>
    /// Submits a transaction for fair ordering processing.
    /// </summary>
    /// <param name="request">The fair transaction request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The transaction ID.</returns>
    Task<string> SubmitFairTransactionAsync(Models.FairTransactionRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Analyzes the fairness risk of a transaction.
    /// </summary>
    /// <param name="request">The transaction analysis request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The fairness risk analysis result.</returns>
    Task<FairnessRiskAnalysisResult> AnalyzeFairnessRiskAsync(Models.TransactionAnalysisRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Analyzes MEV (Maximal Extractable Value) risk for a transaction.
    /// </summary>
    /// <param name="request">The MEV analysis request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The MEV protection result.</returns>
    Task<MevProtectionResult> AnalyzeMevRiskAsync(MevAnalysisRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Submits a transaction with fairness protection.
    /// </summary>
    /// <param name="submission">The transaction submission.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The transaction ID.</returns>
    Task<string> SubmitTransactionAsync(TransactionSubmission submission, BlockchainType blockchainType);

    /// <summary>
    /// Gets fairness metrics for a specific ordering pool.
    /// </summary>
    /// <param name="poolId">The pool ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The fairness metrics.</returns>
    Task<FairnessMetrics> GetFairnessMetricsAsync(string poolId, BlockchainType blockchainType);

    /// <summary>
    /// Gets all ordering pools for a blockchain.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The ordering pools.</returns>
    Task<IEnumerable<OrderingPool>> GetOrderingPoolsAsync(BlockchainType blockchainType);

    /// <summary>
    /// Updates the configuration of an ordering pool.
    /// </summary>
    /// <param name="poolId">The pool ID.</param>
    /// <param name="config">The new configuration.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> UpdatePoolConfigAsync(string poolId, OrderingPoolConfig config, BlockchainType blockchainType);

    /// <summary>
    /// Gets the ordering result for a specific transaction.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The ordering result.</returns>
    Task<OrderingResult> GetOrderingResultAsync(string transactionId, BlockchainType blockchainType);
}
