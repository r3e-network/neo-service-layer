using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Repository interface for User aggregate
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Gets a user by ID
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The user, or null if not found</returns>
        Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a user by username
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The user, or null if not found</returns>
        Task<User?> GetByUsernameAsync(Username username, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a user by email address
        /// </summary>
        /// <param name="email">The email address</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The user, or null if not found</returns>
        Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a username already exists
        /// </summary>
        /// <param name="username">The username to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the username exists</returns>
        Task<bool> ExistsAsync(Username username, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an email address already exists
        /// </summary>
        /// <param name="email">The email to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the email exists</returns>
        Task<bool> EmailExistsAsync(EmailAddress email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new user to the repository
        /// </summary>
        /// <param name="user">The user to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the operation</returns>
        Task AddAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing user
        /// </summary>
        /// <param name="user">The user to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the operation</returns>
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a user from the repository
        /// </summary>
        /// <param name="user">The user to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the operation</returns>
        Task RemoveAsync(User user, CancellationToken cancellationToken = default);
    }
}