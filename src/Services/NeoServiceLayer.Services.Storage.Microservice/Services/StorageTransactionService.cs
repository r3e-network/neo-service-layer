using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Models;
using Neo.Storage.Service.Services;
using System.Text.Json;

namespace Neo.Storage.Service.Services;

public class StorageTransactionService : IStorageTransactionService
{
    private readonly StorageDbContext _context;
    private readonly ILogger<StorageTransactionService> _logger;
    private readonly TimeSpan _defaultTransactionTimeout = TimeSpan.FromMinutes(30);

    public StorageTransactionService(
        StorageDbContext context,
        ILogger<StorageTransactionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StorageTransaction> BeginTransactionAsync(string userId, TransactionType type)
    {
        try
        {
            var transaction = new StorageTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Status = TransactionStatus.Active,
                StartedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_defaultTransactionTimeout),
                Operations = new List<StorageOperation>(),
                TotalCost = 0,
                Metadata = new Dictionary<string, object>()
            };

            _context.StorageTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Started transaction {TransactionId} for user {UserId} of type {Type}",
                transaction.Id, userId, type);

            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin transaction for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> CommitTransactionAsync(Guid transactionId)
    {
        try
        {
            var transaction = await _context.StorageTransactions
                .Include(t => t.Operations)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction {TransactionId} not found for commit", transactionId);
                return false;
            }

            if (transaction.Status != TransactionStatus.Active)
            {
                _logger.LogWarning("Cannot commit transaction {TransactionId} with status {Status}",
                    transactionId, transaction.Status);
                return false;
            }

            if (DateTime.UtcNow > transaction.ExpiresAt)
            {
                _logger.LogWarning("Cannot commit expired transaction {TransactionId}", transactionId);
                transaction.Status = TransactionStatus.Expired;
                await _context.SaveChangesAsync();
                return false;
            }

            // Validate all operations in the transaction
            if (!await ValidateTransactionOperationsAsync(transaction))
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.CompletedAt = DateTime.UtcNow;
                transaction.FailureReason = "Transaction validation failed";
                await _context.SaveChangesAsync();
                return false;
            }

            // Apply all operations atomically
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var operation in transaction.Operations)
                {
                    await ExecuteOperationAsync(operation);
                }

                transaction.Status = TransactionStatus.Committed;
                transaction.CompletedAt = DateTime.UtcNow;
                transaction.TotalCost = await CalculateTransactionCostInternalAsync(transaction);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("Committed transaction {TransactionId} with {OperationCount} operations",
                    transactionId, transaction.Operations.Count);

                return true;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Failed to commit transaction {TransactionId}", transactionId);

                transaction.Status = TransactionStatus.Failed;
                transaction.CompletedAt = DateTime.UtcNow;
                transaction.FailureReason = ex.Message;
                await _context.SaveChangesAsync();

                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during transaction commit {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> RollbackTransactionAsync(Guid transactionId)
    {
        try
        {
            var transaction = await _context.StorageTransactions
                .Include(t => t.Operations)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                return false;
            }

            if (transaction.Status != TransactionStatus.Active)
            {
                return false;
            }

            transaction.Status = TransactionStatus.RolledBack;
            transaction.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Rolled back transaction {TransactionId}", transactionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<StorageTransaction?> GetTransactionAsync(Guid transactionId)
    {
        return await _context.StorageTransactions
            .Include(t => t.Operations)
            .FirstOrDefaultAsync(t => t.Id == transactionId);
    }

    public async Task<List<StorageTransaction>> GetUserTransactionsAsync(string userId, TransactionStatus? status = null)
    {
        var query = _context.StorageTransactions
            .Include(t => t.Operations)
            .Where(t => t.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return await query
            .OrderByDescending(t => t.StartedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<bool> AddOperationToTransactionAsync(Guid transactionId, string operation, Dictionary<string, object> parameters)
    {
        try
        {
            var transaction = await _context.StorageTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.Status == TransactionStatus.Active);

            if (transaction == null)
            {
                return false;
            }

            if (DateTime.UtcNow > transaction.ExpiresAt)
            {
                transaction.Status = TransactionStatus.Expired;
                await _context.SaveChangesAsync();
                return false;
            }

            var storageOperation = new StorageOperation
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                Operation = operation,
                Parameters = JsonSerializer.Serialize(parameters),
                Status = OperationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                EstimatedCost = CalculateOperationCost(operation, parameters)
            };

            _context.StorageOperations.Add(storageOperation);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Added operation {Operation} to transaction {TransactionId}",
                operation, transactionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add operation to transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<decimal> CalculateTransactionCostAsync(Guid transactionId)
    {
        try
        {
            var transaction = await _context.StorageTransactions
                .Include(t => t.Operations)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                return 0;
            }

            return await CalculateTransactionCostInternalAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate transaction cost for {TransactionId}", transactionId);
            return 0;
        }
    }

    public async Task<bool> IsTransactionActiveAsync(Guid transactionId)
    {
        var transaction = await _context.StorageTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        return transaction != null && 
               transaction.Status == TransactionStatus.Active && 
               DateTime.UtcNow <= transaction.ExpiresAt;
    }

    public async Task CleanupExpiredTransactionsAsync()
    {
        try
        {
            var expiredTransactions = await _context.StorageTransactions
                .Where(t => t.Status == TransactionStatus.Active && DateTime.UtcNow > t.ExpiresAt)
                .ToListAsync();

            foreach (var transaction in expiredTransactions)
            {
                transaction.Status = TransactionStatus.Expired;
                transaction.CompletedAt = DateTime.UtcNow;
            }

            if (expiredTransactions.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired transactions", expiredTransactions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired transactions");
        }
    }

    private async Task<bool> ValidateTransactionOperationsAsync(StorageTransaction transaction)
    {
        try
        {
            foreach (var operation in transaction.Operations)
            {
                if (!await ValidateOperationAsync(operation))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate transaction operations for {TransactionId}", transaction.Id);
            return false;
        }
    }

    private async Task<bool> ValidateOperationAsync(StorageOperation operation)
    {
        try
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(operation.Parameters) 
                ?? new Dictionary<string, object>();

            switch (operation.Operation.ToLower())
            {
                case "upload":
                    return await ValidateUploadOperationAsync(parameters);
                case "delete":
                    return await ValidateDeleteOperationAsync(parameters);
                case "copy":
                    return await ValidateCopyOperationAsync(parameters);
                case "move":
                    return await ValidateMoveOperationAsync(parameters);
                default:
                    _logger.LogWarning("Unknown operation type: {Operation}", operation.Operation);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate operation {OperationId}", operation.Id);
            return false;
        }
    }

    private async Task<bool> ValidateUploadOperationAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("bucketName", out var bucketNameObj) ||
            !parameters.TryGetValue("key", out var keyObj))
        {
            return false;
        }

        var bucketName = bucketNameObj.ToString();
        var bucket = await _context.StorageBuckets
            .FirstOrDefaultAsync(b => b.Name == bucketName && b.Status == BucketStatus.Active);

        return bucket != null;
    }

    private async Task<bool> ValidateDeleteOperationAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("objectId", out var objectIdObj) ||
            !Guid.TryParse(objectIdObj.ToString(), out var objectId))
        {
            return false;
        }

        var obj = await _context.StorageObjects
            .FirstOrDefaultAsync(o => o.Id == objectId && o.Status == ObjectStatus.Active);

        return obj != null;
    }

    private async Task<bool> ValidateCopyOperationAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("sourceObjectId", out var sourceIdObj) ||
            !parameters.TryGetValue("targetBucketName", out var targetBucketObj) ||
            !Guid.TryParse(sourceIdObj.ToString(), out var sourceId))
        {
            return false;
        }

        var sourceObject = await _context.StorageObjects
            .FirstOrDefaultAsync(o => o.Id == sourceId && o.Status == ObjectStatus.Active);

        var targetBucket = await _context.StorageBuckets
            .FirstOrDefaultAsync(b => b.Name == targetBucketObj.ToString() && b.Status == BucketStatus.Active);

        return sourceObject != null && targetBucket != null;
    }

    private async Task<bool> ValidateMoveOperationAsync(Dictionary<string, object> parameters)
    {
        // Move is essentially copy + delete, so validate both
        return await ValidateCopyOperationAsync(parameters) && await ValidateDeleteOperationAsync(parameters);
    }

    private async Task ExecuteOperationAsync(StorageOperation operation)
    {
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(operation.Parameters) 
            ?? new Dictionary<string, object>();

        switch (operation.Operation.ToLower())
        {
            case "upload":
                await ExecuteUploadOperationAsync(operation, parameters);
                break;
            case "delete":
                await ExecuteDeleteOperationAsync(operation, parameters);
                break;
            case "copy":
                await ExecuteCopyOperationAsync(operation, parameters);
                break;
            case "move":
                await ExecuteMoveOperationAsync(operation, parameters);
                break;
            default:
                throw new InvalidOperationException($"Unknown operation: {operation.Operation}");
        }

        operation.Status = OperationStatus.Completed;
        operation.CompletedAt = DateTime.UtcNow;
    }

    private async Task ExecuteUploadOperationAsync(StorageOperation operation, Dictionary<string, object> parameters)
    {
        // Implementation would integrate with StorageObjectService
        // For now, just mark as executed
        await Task.CompletedTask;
        _logger.LogDebug("Executed upload operation {OperationId}", operation.Id);
    }

    private async Task ExecuteDeleteOperationAsync(StorageOperation operation, Dictionary<string, object> parameters)
    {
        // Implementation would integrate with StorageObjectService
        await Task.CompletedTask;
        _logger.LogDebug("Executed delete operation {OperationId}", operation.Id);
    }

    private async Task ExecuteCopyOperationAsync(StorageOperation operation, Dictionary<string, object> parameters)
    {
        // Implementation would integrate with StorageObjectService
        await Task.CompletedTask;
        _logger.LogDebug("Executed copy operation {OperationId}", operation.Id);
    }

    private async Task ExecuteMoveOperationAsync(StorageOperation operation, Dictionary<string, object> parameters)
    {
        // Implementation would integrate with StorageObjectService
        await Task.CompletedTask;
        _logger.LogDebug("Executed move operation {OperationId}", operation.Id);
    }

    private async Task<decimal> CalculateTransactionCostInternalAsync(StorageTransaction transaction)
    {
        decimal totalCost = 0;

        foreach (var operation in transaction.Operations)
        {
            totalCost += operation.EstimatedCost;
        }

        // Add transaction overhead cost
        totalCost += CalculateTransactionOverheadCost(transaction);

        await Task.CompletedTask;
        return totalCost;
    }

    private static decimal CalculateOperationCost(string operation, Dictionary<string, object> parameters)
    {
        // Simple cost calculation - in reality this would be much more sophisticated
        return operation.ToLower() switch
        {
            "upload" => GetSizeCost(parameters) * 0.01m,
            "delete" => 0.001m,
            "copy" => GetSizeCost(parameters) * 0.005m,
            "move" => GetSizeCost(parameters) * 0.006m,
            _ => 0.001m
        };
    }

    private static decimal GetSizeCost(Dictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("size", out var sizeObj) && 
            long.TryParse(sizeObj.ToString(), out var size))
        {
            return (decimal)size / (1024 * 1024); // Cost per MB
        }
        return 1.0m; // Default 1MB assumption
    }

    private static decimal CalculateTransactionOverheadCost(StorageTransaction transaction)
    {
        // Base transaction cost + cost per operation
        return 0.01m + (transaction.Operations.Count * 0.001m);
    }
}