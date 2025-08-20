using System.Threading;
using System.Threading.Tasks;
using NeoServiceLayer.ServiceFramework.Models;

namespace NeoServiceLayer.ServiceFramework.Interfaces
{
    /// <summary>
    /// Basic interface for enclave storage service.
    /// This is a minimal interface to avoid circular dependencies.
    /// </summary>
    public interface IEnclaveStorageService
    {
        /// <summary>
        /// Stores data in the enclave.
        /// </summary>
        Task StoreDataAsync(object data, CancellationToken cancellationToken);
        
        /// <summary>
        /// Retrieves data from the enclave.
        /// </summary>
        Task<object?> RetrieveDataAsync(string key, CancellationToken cancellationToken);
        
        /// <summary>
        /// Seals data in the enclave.
        /// </summary>
        Task<byte[]> SealDataAsync(SealDataRequest request, CancellationToken cancellationToken);
        
        /// <summary>
        /// Unseals data from the enclave.
        /// </summary>
        Task<byte[]> UnsealDataAsync(string key, CancellationToken cancellationToken);
        
        /// <summary>
        /// Deletes sealed data from the enclave.
        /// </summary>
        Task<bool> DeleteSealedDataAsync(string key, CancellationToken cancellationToken);
        
        /// <summary>
        /// Lists sealed items in the enclave.
        /// </summary>
        Task<SealedItemsList> ListSealedItemsAsync(ListSealedItemsRequest request, CancellationToken cancellationToken);
    }
}