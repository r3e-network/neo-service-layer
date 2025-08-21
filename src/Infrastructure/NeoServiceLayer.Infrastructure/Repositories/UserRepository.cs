using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Core.Domain;
using NeoServiceLayer.Infrastructure.Data;

namespace NeoServiceLayer.Infrastructure.Repositories
{
    /// <summary>
    /// Entity Framework implementation of IUserRepository
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of UserRepository
        /// </summary>
        /// <param name="context">The database context</param>
        public UserRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public async Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            if (userId == null)
                return null;

            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<User?> GetByUsernameAsync(Username username, CancellationToken cancellationToken = default)
        {
            if (username == null)
                return null;

            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
        {
            if (email == null)
                return null;

            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(Username username, CancellationToken cancellationToken = default)
        {
            if (username == null)
                return false;

            return await _context.Users
                .AnyAsync(u => u.Username == username, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> EmailExistsAsync(EmailAddress email, CancellationToken cancellationToken = default)
        {
            if (email == null)
                return false;

            return await _context.Users
                .AnyAsync(u => u.Email == email, cancellationToken);
        }

        /// <inheritdoc />
        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            await _context.Users.AddAsync(user, cancellationToken);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            _context.Users.Update(user);
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task RemoveAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            _context.Users.Remove(user);
            await Task.CompletedTask;
        }
    }
}