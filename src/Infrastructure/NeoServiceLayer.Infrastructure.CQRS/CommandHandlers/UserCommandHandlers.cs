using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Infrastructure.CQRS.Commands;
using NeoServiceLayer.Core.Events;
using NeoServiceLayer.Infrastructure.CQRS.Abstractions;

namespace NeoServiceLayer.Infrastructure.CQRS.CommandHandlers
{
    /// <summary>
    /// Handler for user-related commands
    /// </summary>
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
    {
        private readonly ILogger<CreateUserCommandHandler> _logger;
        private readonly IEventBus _eventBus;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public CreateUserCommandHandler(
            ILogger<CreateUserCommandHandler> logger,
            IEventBus eventBus,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher)
        {
            _logger = logger;
            _eventBus = eventBus;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating user with email {Email}", command.Email);

                // Check if user already exists
                var existingUser = await _userRepository.GetByEmailAsync(command.Email);
                if (existingUser != null)
                {
                    throw new InvalidOperationException("User with this email already exists");
                }

                // Hash password
                var passwordHash = _passwordHasher.HashPassword(command.Password);

                // Create user entity
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = command.Email,
                    Username = command.Username,
                    PasswordHash = passwordHash,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    TenantId = command.TenantId,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                // Save to repository
                await _userRepository.CreateAsync(user);

                // Assign roles
                foreach (var roleId in command.RoleIds)
                {
                    await _userRepository.AssignRoleAsync(user.Id, roleId);
                }

                // Publish event
                await _eventBus.PublishAsync(new UserCreatedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    TenantId = command.TenantId,
                    CreatedAt = user.CreatedAt,
                    CorrelationId = command.CorrelationId
                });

                _logger.LogInformation("User {UserId} created successfully", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }
    }

    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
    {
        private readonly ILogger<UpdateUserCommandHandler> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IEventBus _eventBus;

        public UpdateUserCommandHandler(
            ILogger<UpdateUserCommandHandler> logger,
            IUserRepository userRepository,
            IEventBus eventBus)
        {
            _logger = logger;
            _userRepository = userRepository;
            _eventBus = eventBus;
        }

        public async Task HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(command.UserId);
                if (user == null)
                {
                    return CommandResult.Failure("User not found");
                }

                // Track changes for event
                var changes = new Dictionary<string, object>();
                
                if (!string.IsNullOrEmpty(command.FirstName) && user.FirstName != command.FirstName)
                {
                    changes["FirstName"] = command.FirstName;
                    user.FirstName = command.FirstName;
                }

                if (!string.IsNullOrEmpty(command.LastName) && user.LastName != command.LastName)
                {
                    changes["LastName"] = command.LastName;
                    user.LastName = command.LastName;
                }

                if (!string.IsNullOrEmpty(command.PhoneNumber) && user.PhoneNumber != command.PhoneNumber)
                {
                    changes["PhoneNumber"] = command.PhoneNumber;
                    user.PhoneNumber = command.PhoneNumber;
                }

                if (command.Metadata != null)
                {
                    user.Metadata = command.Metadata;
                    changes["Metadata"] = command.Metadata;
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                // Publish event
                await _eventBus.PublishAsync(new UserUpdatedEvent
                {
                    UserId = user.Id,
                    Changes = changes,
                    UpdatedAt = user.UpdatedAt.Value,
                    CorrelationId = command.CorrelationId
                });

                _logger.LogInformation("User {UserId} updated successfully", user.Id);

                return CommandResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", command.UserId);
                return CommandResult.Failure($"Failed to update user: {ex.Message}");
            }
        }
    }

    public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
    {
        private readonly ILogger<ChangePasswordCommandHandler> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEventBus _eventBus;

        public ChangePasswordCommandHandler(
            ILogger<ChangePasswordCommandHandler> logger,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IEventBus eventBus)
        {
            _logger = logger;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _eventBus = eventBus;
        }

        public async Task HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(command.UserId);
                if (user == null)
                {
                    return CommandResult.Failure("User not found");
                }

                // Verify current password
                if (!_passwordHasher.VerifyPassword(command.CurrentPassword, user.PasswordHash))
                {
                    return CommandResult.Failure("Current password is incorrect");
                }

                // Hash new password
                user.PasswordHash = _passwordHasher.HashPassword(command.NewPassword);
                user.PasswordChangedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                // Invalidate all existing sessions
                await _userRepository.InvalidateAllSessionsAsync(user.Id);

                // Publish event
                await _eventBus.PublishAsync(new PasswordChangedEvent
                {
                    UserId = user.Id,
                    ChangedAt = user.PasswordChangedAt.Value,
                    CorrelationId = command.CorrelationId
                });

                _logger.LogInformation("Password changed for user {UserId}", user.Id);

                return CommandResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", command.UserId);
                return CommandResult.Failure($"Failed to change password: {ex.Message}");
            }
        }
    }

    public class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand>
    {
        private readonly ILogger<AssignRoleCommandHandler> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IEventBus _eventBus;

        public AssignRoleCommandHandler(
            ILogger<AssignRoleCommandHandler> logger,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IEventBus eventBus)
        {
            _logger = logger;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _eventBus = eventBus;
        }

        public async Task HandleAsync(AssignRoleCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate user exists
                var user = await _userRepository.GetByIdAsync(command.UserId);
                if (user == null)
                {
                    return CommandResult.Failure("User not found");
                }

                // Validate role exists
                var role = await _roleRepository.GetByIdAsync(command.RoleId);
                if (role == null)
                {
                    return CommandResult.Failure("Role not found");
                }

                // Check if already assigned
                var existingRoles = await _userRepository.GetUserRolesAsync(command.UserId);
                if (existingRoles.Any(r => r.Id == command.RoleId))
                {
                    return CommandResult.Failure("Role already assigned to user");
                }

                // Assign role
                await _userRepository.AssignRoleAsync(command.UserId, command.RoleId, command.AssignedBy, command.ExpiresAt);

                // Publish event
                await _eventBus.PublishAsync(new RoleAssignedEvent
                {
                    UserId = command.UserId,
                    RoleId = command.RoleId,
                    RoleName = role.Name,
                    AssignedBy = command.AssignedBy,
                    AssignedAt = DateTime.UtcNow,
                    ExpiresAt = command.ExpiresAt,
                    CorrelationId = command.CorrelationId
                });

                _logger.LogInformation("Role {RoleId} assigned to user {UserId}", command.RoleId, command.UserId);

                return CommandResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role to user");
                return CommandResult.Failure($"Failed to assign role: {ex.Message}");
            }
        }
    }

    public class LockUserCommandHandler : ICommandHandler<LockUserCommand>
    {
        private readonly ILogger<LockUserCommandHandler> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IEventBus _eventBus;

        public LockUserCommandHandler(
            ILogger<LockUserCommandHandler> logger,
            IUserRepository userRepository,
            IEventBus eventBus)
        {
            _logger = logger;
            _userRepository = userRepository;
            _eventBus = eventBus;
        }

        public async Task HandleAsync(LockUserCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(command.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                if (user.Status == UserStatus.Locked)
                {
                    throw new InvalidOperationException("User is already locked");
                }

                user.Status = UserStatus.Locked;
                user.LockedUntil = command.LockedUntil;
                user.LockedReason = command.Reason;
                user.LockedBy = command.LockedBy;
                user.LockedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                // Invalidate all sessions
                await _userRepository.InvalidateAllSessionsAsync(user.Id);

                // Publish event
                await _eventBus.PublishAsync(new UserLockedEvent
                {
                    UserId = user.Id,
                    Reason = command.Reason,
                    LockedBy = command.LockedBy,
                    LockedAt = user.LockedAt.Value,
                    LockedUntil = command.LockedUntil,
                    CorrelationId = command.CorrelationId
                });

                _logger.LogInformation("User {UserId} locked by {LockedBy}", command.UserId, command.LockedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking user {UserId}", command.UserId);
                throw;
            }
        }
    }

    // Domain events
    public class UserCreatedEvent : DomainEventBase
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public Guid TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserUpdatedEvent : DomainEventBase
    {
        public Guid UserId { get; set; }
        public Dictionary<string, object> Changes { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PasswordChangedEvent : DomainEventBase
    {
        public Guid UserId { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    public class RoleAssignedEvent : DomainEventBase
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public Guid AssignedBy { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class UserLockedEvent : DomainEventBase
    {
        public Guid UserId { get; set; }
        public string Reason { get; set; }
        public Guid LockedBy { get; set; }
        public DateTime LockedAt { get; set; }
        public DateTime? LockedUntil { get; set; }
    }

    // Command result
    public class CommandResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public object Data { get; set; }

        public static CommandResult Successful() => new CommandResult { Success = true };
        public static CommandResult Successful(object data) => new CommandResult { Success = true, Data = data };
        public static CommandResult Failure(string error) => new CommandResult { Success = false, Error = error };
    }
}