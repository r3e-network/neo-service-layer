using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.CQRS.Abstractions
{
    /// <summary>
    /// Minimal user repository interface for CQRS operations
    /// </summary>
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(Guid userId);
        Task<User> GetByEmailAsync(string email);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task AssignRoleAsync(Guid userId, Guid roleId);
        Task InvalidateAllSessionsAsync(Guid userId);
        Task<List<Role>> GetUserRolesAsync(Guid userId);
    }

    /// <summary>
    /// Minimal role repository interface for CQRS operations
    /// </summary>
    public interface IRoleRepository
    {
        Task<Role> GetByIdAsync(Guid roleId);
    }

    /// <summary>
    /// Minimal password hasher interface for CQRS operations
    /// </summary>
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    /// <summary>
    /// Minimal user model for CQRS operations
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public Guid? TenantId { get; set; }
        public UserStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PasswordChangedAt { get; set; }
        public DateTime? LockedUntil { get; set; }
        public string LockedReason { get; set; }
        public string LockedBy { get; set; }
        public DateTime? LockedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Minimal role model for CQRS operations
    /// </summary>
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// User status enumeration
    /// </summary>
    public enum UserStatus
    {
        Active = 0,
        Inactive = 1,
        Locked = 2,
        Suspended = 3,
        Deleted = 4
    }
}