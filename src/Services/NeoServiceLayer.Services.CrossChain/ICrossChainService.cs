using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.CrossChain.Models;
using CoreModels = NeoServiceLayer.Core.Models;
using ServiceCrossChainMessage = NeoServiceLayer.Services.CrossChain.Models.CrossChainMessage;
using ServiceCrossChainMessageRequest = NeoServiceLayer.Services.CrossChain.Models.CrossChainMessageRequest;
using ServiceCrossChainMessageStatus = NeoServiceLayer.Services.CrossChain.Models.CrossChainMessageStatus;
using ServiceCrossChainRoute = NeoServiceLayer.Services.CrossChain.Models.CrossChainRoute;
using ServiceCrossChainOperation = NeoServiceLayer.Services.CrossChain.Models.CrossChainOperation;

namespace NeoServiceLayer.Services.CrossChain;

/// <summary>
/// Interface for Cross-Chain Service operations.
/// </summary>
public interface ICrossChainService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Sends a cross-chain message between blockchains.
    /// </summary>
    Task<string> SendMessageAsync(ServiceCrossChainMessageRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain);

    /// <summary>
    /// Transfers tokens between blockchains.
    /// </summary>
    Task<string> TransferTokensAsync(CoreModels.CrossChainTransferRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain);

    /// <summary>
    /// Executes a remote call on another blockchain.
    /// </summary>
    Task<string> ExecuteRemoteCallAsync(CoreModels.RemoteCallRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain);

    /// <summary>
    /// Gets the status of a cross-chain message.
    /// </summary>
    Task<CrossChainMessageStatus> GetMessageStatusAsync(string messageId, BlockchainType blockchainType);

    /// <summary>
    /// Gets pending messages for a destination chain.
    /// </summary>
    Task<IEnumerable<CrossChainMessage>> GetPendingMessagesAsync(BlockchainType destinationChain);

    /// <summary>
    /// Verifies a cross-chain message proof.
    /// </summary>
    Task<bool> VerifyMessageAsync(string messageId, string proof, BlockchainType blockchainType);

    /// <summary>
    /// Gets the optimal route between two blockchains.
    /// </summary>
    Task<CrossChainRoute> GetOptimalRouteAsync(BlockchainType source, BlockchainType destination);

    /// <summary>
    /// Estimates fees for a cross-chain operation.
    /// </summary>
    Task<decimal> EstimateFeesAsync(CrossChainOperation operation, BlockchainType blockchainType);

    /// <summary>
    /// Gets supported chains for cross-chain operations.
    /// </summary>
    Task<IEnumerable<SupportedChain>> GetSupportedChainsAsync();

    /// <summary>
    /// Registers a token mapping between blockchains.
    /// </summary>
    Task<bool> RegisterTokenMappingAsync(CoreModels.TokenMapping mapping, BlockchainType blockchainType);

    /// <summary>
    /// Gets transaction history for an address.
    /// </summary>
    Task<IEnumerable<CrossChainTransaction>> GetTransactionHistoryAsync(string address, BlockchainType blockchainType);

    /// <summary>
    /// Executes a contract call on another blockchain.
    /// </summary>
    Task<CrossChainExecutionResult> ExecuteContractCallAsync(CrossChainContractCallRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain);

    /// <summary>
    /// Verifies a message proof.
    /// </summary>
    Task<bool> VerifyMessageProofAsync(CrossChainMessageProof proof, BlockchainType blockchainType);
}
