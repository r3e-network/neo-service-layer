using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;

#region Authentication & Authorization Entities

/// <summary>
/// Authentication sessions for user login tracking
/// </summary>
public class AuthenticationSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string SessionToken { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? RefreshToken { get; set; }
    
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public DateTime? LastActivityAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(50)]
    public string? DeviceId { get; set; }
    
    [MaxLength(100)]
    public string? Location { get; set; }
    
    public bool IsExpired => ExpiresAt < DateTime.UtcNow;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// System permissions for fine-grained access control
/// </summary>
public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // e.g., "storage:read", "oracle:write"
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(50)]
    public string Category { get; set; } = "General"; // Storage, Oracle, Voting, etc.
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// User roles for role-based access control (RBAC)
/// </summary>
public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Many-to-many relationship between users and roles
/// </summary>
public class UserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    public Guid RoleId { get; set; }
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    public Guid? AssignedBy { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
    public virtual User? AssignedByUser { get; set; }
}

/// <summary>
/// Many-to-many relationship between roles and permissions
/// </summary>
public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid RoleId { get; set; }
    
    public Guid PermissionId { get; set; }
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    public Guid? AssignedBy { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
    public virtual User? AssignedByUser { get; set; }
}

#endregion