using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data.Entities;

namespace NeoServiceLayer.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Interface for the TEE account repository.
    /// </summary>
    public interface ITeeAccountRepository : IRepository<TeeAccountEntity, string>
    {
        /// <summary>
        /// Gets all accounts for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The accounts for the user.</returns>
        Task<IEnumerable<TeeAccount>> GetByUserIdAsync(string userId);

        /// <summary>
        /// Gets an account by its ID.
        /// </summary>
        /// <param name="accountId">The account ID.</param>
        /// <returns>The account, or null if not found.</returns>
        Task<TeeAccount> GetAccountByIdAsync(string accountId);

        /// <summary>
        /// Adds an account.
        /// </summary>
        /// <param name="account">The account to add.</param>
        /// <returns>The added account.</returns>
        Task<TeeAccount> AddAccountAsync(TeeAccount account);

        /// <summary>
        /// Updates an account.
        /// </summary>
        /// <param name="account">The account to update.</param>
        /// <returns>The updated account.</returns>
        Task<TeeAccount> UpdateAccountAsync(TeeAccount account);

        /// <summary>
        /// Deletes an account.
        /// </summary>
        /// <param name="accountId">The account ID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        System.Threading.Tasks.Task DeleteAccountAsync(string accountId);
    }
}
