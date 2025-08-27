using Neo.Storage.Service.Models;

namespace Neo.Storage.Service.Services;

public interface IStorageTransactionService
{
    Task<StorageTransaction> BeginTransactionAsync(string userId, TransactionType type);
    Task<bool> CommitTransactionAsync(Guid transactionId);
    Task<bool> RollbackTransactionAsync(Guid transactionId);
    Task<StorageTransaction?> GetTransactionAsync(Guid transactionId);
    Task<List<StorageTransaction>> GetUserTransactionsAsync(string userId, TransactionStatus? status = null);
    Task<bool> AddOperationToTransactionAsync(Guid transactionId, string operation, Dictionary<string, object> parameters);
    Task<decimal> CalculateTransactionCostAsync(Guid transactionId);
    Task<bool> IsTransactionActiveAsync(Guid transactionId);
    Task CleanupExpiredTransactionsAsync();
}