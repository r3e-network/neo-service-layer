using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;

#region Event Sourcing Entities

/// <summary>
/// Domain events for event sourcing pattern
/// </summary>
public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid AggregateId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string AggregateType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    public string EventData { get; set; } = string.Empty; // JSON serialized event data
    
    [Required]
    public string Metadata { get; set; } = string.Empty; // JSON metadata (correlation ID, causation ID, etc.)
    
    public long Version { get; set; } // Aggregate version
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public Guid? UserId { get; set; } // Who triggered the event
    
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
    
    [MaxLength(100)]
    public string? CausationId { get; set; }
    
    // Navigation properties
    public virtual AggregateRoot? Aggregate { get; set; }
    public virtual User? User { get; set; }
}

/// <summary>
/// Event snapshots for aggregate reconstruction optimization
/// </summary>
public class EventSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid AggregateId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string AggregateType { get; set; } = string.Empty;
    
    [Required]
    public string SnapshotData { get; set; } = string.Empty; // JSON serialized aggregate state
    
    public long Version { get; set; } // Aggregate version at snapshot time
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(255)]
    public string? SnapshotType { get; set; } // Full, Incremental, etc.
    
    public long? EventCount { get; set; } // Number of events since last snapshot
    
    // Navigation properties
    public virtual AggregateRoot? Aggregate { get; set; }
}

/// <summary>
/// Aggregate root tracking for event sourcing
/// </summary>
public class AggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(255)]
    public string AggregateType { get; set; } = string.Empty;
    
    public long Version { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    public long EventCount { get; set; } = 0;
    
    public DateTime? LastSnapshotAt { get; set; }
    
    public long? LastSnapshotVersion { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Deleted, Archived
    
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
    
    // Navigation properties
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    public virtual ICollection<EventSnapshot> Snapshots { get; set; } = new List<EventSnapshot>();
}

#endregion