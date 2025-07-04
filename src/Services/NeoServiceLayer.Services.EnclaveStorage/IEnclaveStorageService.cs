using System.Threading.Tasks;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.EnclaveStorage.Models;

namespace NeoServiceLayer.Services.EnclaveStorage;

/// <summary>
/// Interface for enclave storage service providing secure persistent storage within SGX.
/// </summary>
public interface IEnclaveStorageService : IService
{
    /// <summary>
    /// Seals and stores data within the enclave.
    /// </summary>
    /// <param name="request">The seal data request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The seal result.</returns>
    Task<SealDataResult> SealDataAsync(SealDataRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Unseals previously stored data.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The unsealed data.</returns>
    Task<UnsealDataResult> UnsealDataAsync(string key, BlockchainType blockchainType);

    /// <summary>
    /// Lists all sealed items for a service.
    /// </summary>
    /// <param name="request">The list request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of sealed items.</returns>
    Task<SealedItemsList> ListSealedItemsAsync(ListSealedItemsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Deletes sealed data.
    /// </summary>
    /// <param name="key">The data key to delete.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The deletion result.</returns>
    Task<DeleteSealedDataResult> DeleteSealedDataAsync(string key, BlockchainType blockchainType);

    /// <summary>
    /// Gets storage statistics.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The storage statistics.</returns>
    Task<EnclaveStorageStatistics> GetStorageStatisticsAsync(BlockchainType blockchainType);

    /// <summary>
    /// Backs up sealed data with re-sealing.
    /// </summary>
    /// <param name="request">The backup request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The backup result.</returns>
    Task<BackupResult> BackupSealedDataAsync(BackupRequest request, BlockchainType blockchainType);
}
