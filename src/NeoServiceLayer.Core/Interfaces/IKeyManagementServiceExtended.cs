using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Extended interface for key management service with additional operations.
    /// </summary>
    public interface IKeyManagementServiceExtended : IKeyManagementService
    {
        /// <summary>
        /// Deletes an account by ID.
        /// </summary>
        /// <param name="accountId">The ID of the account to delete.</param>
        /// <returns>True if the account was deleted, false otherwise.</returns>
        Task<bool> DeleteAccountAsync(string accountId);
    }
}
