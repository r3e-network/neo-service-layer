using NeoServiceLayer.Core.Models;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for interacting with the Neo N3 blockchain.
    /// </summary>
    public interface INeoN3BlockchainService
    {
        /// <summary>
        /// Gets the current blockchain height.
        /// </summary>
        /// <returns>The blockchain height as a string.</returns>
        Task<string> GetBlockchainHeightAsync();

        /// <summary>
        /// Gets a transaction by its hash.
        /// </summary>
        /// <param name="txHash">The transaction hash.</param>
        /// <returns>The transaction details.</returns>
        Task<BlockchainTransaction> GetTransactionAsync(string txHash);

        /// <summary>
        /// Invokes a smart contract method.
        /// </summary>
        /// <param name="scriptHash">The script hash of the contract.</param>
        /// <param name="operation">The operation to invoke.</param>
        /// <param name="args">The arguments to pass to the operation.</param>
        /// <returns>The transaction hash of the invocation.</returns>
        Task<string> InvokeContractAsync(string scriptHash, string operation, params object[] args);

        /// <summary>
        /// Test invokes a smart contract method without submitting a transaction.
        /// </summary>
        /// <param name="scriptHash">The script hash of the contract.</param>
        /// <param name="operation">The operation to invoke.</param>
        /// <param name="args">The arguments to pass to the operation.</param>
        /// <returns>The result of the test invocation.</returns>
        Task<ContractInvocationResult> TestInvokeContractAsync(string scriptHash, string operation, params object[] args);

        /// <summary>
        /// Gets events emitted by a smart contract.
        /// </summary>
        /// <param name="scriptHash">The script hash of the contract.</param>
        /// <param name="fromBlock">The block index to start from.</param>
        /// <param name="count">The maximum number of events to return.</param>
        /// <returns>An array of blockchain events.</returns>
        Task<BlockchainEvent[]> GetContractEventsAsync(string scriptHash, int fromBlock = 0, int count = 100);
    }
}
