using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Persistence;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;

/// <summary>
/// PostgreSQL-specific implementation of Unit of Work pattern
/// Provides transaction management and change tracking for all Neo Service Layer entities
/// </summary>
public class PostgreSQLUnitOfWork : IUnitOfWork, IDisposable
{
    private readonly NeoServiceLayerDbContext _context;
    private readonly ILogger<PostgreSQLUnitOfWork> _logger;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public PostgreSQLUnitOfWork(
        NeoServiceLayerDbContext context,
        ILogger<PostgreSQLUnitOfWork> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Saving changes to PostgreSQL database");
            
            // Add audit information for tracked entities
            AddAuditInformation();
            
            var changes = await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Successfully saved {ChangeCount} changes to database", changes);
            
            return changes;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to save changes to PostgreSQL database");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction already started");
        }

        try
        {
            _logger.LogDebug("Beginning PostgreSQL database transaction");
            
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            
            _logger.LogDebug("Database transaction started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin database transaction");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to commit");
        }

        try
        {
            _logger.LogDebug("Committing PostgreSQL database transaction");
            
            await _transaction.CommitAsync(cancellationToken);
            
            _logger.LogDebug("Database transaction committed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit database transaction");
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to rollback");
        }

        try
        {
            _logger.LogDebug("Rolling back PostgreSQL database transaction");
            
            await _transaction.RollbackAsync(cancellationToken);
            
            _logger.LogDebug("Database transaction rolled back successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback database transaction");
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Adds audit information to tracked entities before saving
    /// </summary>
    private void AddAuditInformation()
    {
        var entries = _context.ChangeTracker.Entries();
        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Set creation timestamps
                    if (entry.Entity is IAuditableEntity auditableEntity)
                    {
                        auditableEntity.CreatedAt = utcNow;
                        auditableEntity.UpdatedAt = utcNow;
                    }
                    else if (HasProperty(entry, "CreatedAt"))
                    {
                        entry.Property("CreatedAt").CurrentValue = utcNow;
                    }
                    break;

                case EntityState.Modified:
                    // Set update timestamps
                    if (entry.Entity is IAuditableEntity modifiedEntity)
                    {
                        modifiedEntity.UpdatedAt = utcNow;
                    }
                    else if (HasProperty(entry, "UpdatedAt"))
                    {
                        entry.Property("UpdatedAt").CurrentValue = utcNow;
                    }
                    
                    // Prevent modification of CreatedAt
                    if (HasProperty(entry, "CreatedAt"))
                    {
                        entry.Property("CreatedAt").IsModified = false;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Checks if an entity has a specific property
    /// </summary>
    private static bool HasProperty(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName)
    {
        return entry.Properties.Any(p => p.Metadata.Name == propertyName);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Interface for entities that support audit information
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}