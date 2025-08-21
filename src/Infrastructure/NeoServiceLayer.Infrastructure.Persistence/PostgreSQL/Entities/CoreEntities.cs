using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;

#region Core System Entities

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? FullName { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<AuthenticationSession> Sessions { get; set; } = new List<AuthenticationSession>();
}

public class Service
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Version { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Inactive";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastHealthCheck { get; set; }
    
    // Navigation properties
    public virtual ICollection<ServiceConfiguration> Configurations { get; set; } = new List<ServiceConfiguration>();
    public virtual ICollection<HealthCheckResult> HealthChecks { get; set; } = new List<HealthCheckResult>();
}

public class ServiceConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ServiceId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ConfigKey { get; set; } = string.Empty;
    
    [Required]
    public string ConfigValue { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? Description { get; set; }
    
    public bool IsSecure { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Service Service { get; set; } = null!;
}

public class HealthCheckResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ServiceId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty; // Healthy, Degraded, Unhealthy
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public TimeSpan ResponseTime { get; set; }
    
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Service Service { get; set; } = null!;
}

#endregion