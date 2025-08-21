using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Entities.CrossChainEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories
{
    /// <summary>
    /// Cross-chain repository interface.
    /// </summary>
    public interface ICrossChainRepository
    {
        Task<CrossChainOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<CrossChainOperation>> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CrossChainOperation>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<CrossChainOperation>> GetRecentAsync(TimeSpan timeRange, CancellationToken cancellationToken = default);
        Task<IEnumerable<CrossChainOperation>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
        Task<CrossChainOperation> CreateAsync(CrossChainOperation operation, CancellationToken cancellationToken = default);
        Task<CrossChainOperation> UpdateAsync(CrossChainOperation operation, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
        Task<IEnumerable<TokenTransfer>> GetTokenTransfersAsync(Guid operationId, CancellationToken cancellationToken = default);
        Task<TokenTransfer> CreateTokenTransferAsync(TokenTransfer transfer, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// PostgreSQL implementation of cross-chain repository.
    /// </summary>
    public class CrossChainRepository : ICrossChainRepository
    {
        private readonly NeoServiceLayerDbContext _context;
        private readonly ILogger<CrossChainRepository> _logger;

        public CrossChainRepository(
            NeoServiceLayerDbContext context,
            ILogger<CrossChainRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CrossChainOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.CrossChainOperations
                    .Include(o => o.TokenTransfers)
                    .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cross-chain operation by ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<CrossChainOperation>> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.CrossChainOperations
                    .Include(o => o.TokenTransfers)
                    .Where(o => o.OperationId == operationId)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cross-chain operations by OperationId {OperationId}", operationId);
                throw;
            }
        }

        public async Task<IEnumerable<CrossChainOperation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.CrossChainOperations
                    .Include(o => o.TokenTransfers)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all cross-chain operations");
                throw;
            }
        }

        public async Task<IEnumerable<CrossChainOperation>> GetRecentAsync(TimeSpan timeRange, CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow - timeRange;
                return await _context.CrossChainOperations
                    .Include(o => o.TokenTransfers)
                    .Where(o => o.CreatedAt >= cutoffTime)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent cross-chain operations for timeRange {TimeRange}", timeRange);
                throw;
            }
        }

        public async Task<IEnumerable<CrossChainOperation>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.CrossChainOperations
                    .Include(o => o.TokenTransfers)
                    .Where(o => o.Status == status)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cross-chain operations by status {Status}", status);
                throw;
            }
        }

        public async Task<CrossChainOperation> CreateAsync(CrossChainOperation operation, CancellationToken cancellationToken = default)
        {
            try
            {
                operation.Id = Guid.NewGuid();
                operation.CreatedAt = DateTime.UtcNow;
                operation.UpdatedAt = DateTime.UtcNow;

                _context.CrossChainOperations.Add(operation);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created cross-chain operation {OperationId}", operation.OperationId);
                return operation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create cross-chain operation {OperationId}", operation.OperationId);
                throw;
            }
        }

        public async Task<CrossChainOperation> UpdateAsync(CrossChainOperation operation, CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _context.CrossChainOperations
                    .FirstOrDefaultAsync(o => o.Id == operation.Id, cancellationToken);

                if (existing == null)
                {
                    throw new InvalidOperationException($"Cross-chain operation with ID {operation.Id} not found");
                }

                // Update properties
                existing.OperationType = operation.OperationType;
                existing.SourceChain = operation.SourceChain;
                existing.TargetChain = operation.TargetChain;
                existing.Status = operation.Status;
                existing.TransactionHash = operation.TransactionHash;
                existing.BlockNumber = operation.BlockNumber;
                existing.GasUsed = operation.GasUsed;
                existing.GasPrice = operation.GasPrice;
                existing.CompletedAt = operation.CompletedAt;
                existing.RetryCount = operation.RetryCount;
                existing.MaxRetries = operation.MaxRetries;
                existing.Metadata = operation.Metadata;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated cross-chain operation {OperationId}", existing.OperationId);
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update cross-chain operation {Id}", operation.Id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var operation = await _context.CrossChainOperations
                    .Include(o => o.TokenTransfers)
                    .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

                if (operation != null)
                {
                    _context.CrossChainOperations.Remove(operation);
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Deleted cross-chain operation {OperationId}", operation.OperationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete cross-chain operation {Id}", id);
                throw;
            }
        }

        public async Task DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var oldOperations = await _context.CrossChainOperations
                    .Where(o => o.CreatedAt < cutoffDate)
                    .ToListAsync(cancellationToken);

                if (oldOperations.Any())
                {
                    _context.CrossChainOperations.RemoveRange(oldOperations);
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Deleted {Count} cross-chain operations older than {CutoffDate}", 
                        oldOperations.Count, cutoffDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete cross-chain operations older than {CutoffDate}", cutoffDate);
                throw;
            }
        }

        public async Task<IEnumerable<TokenTransfer>> GetTokenTransfersAsync(Guid operationId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.TokenTransfers
                    .Include(t => t.Operation)
                    .Where(t => t.OperationId == operationId)
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get token transfers for operation {OperationId}", operationId);
                throw;
            }
        }

        public async Task<TokenTransfer> CreateTokenTransferAsync(TokenTransfer transfer, CancellationToken cancellationToken = default)
        {
            try
            {
                transfer.Id = Guid.NewGuid();
                transfer.CreatedAt = DateTime.UtcNow;

                _context.TokenTransfers.Add(transfer);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created token transfer from {FromAddress} to {ToAddress} for operation {OperationId}", 
                    transfer.FromAddress, transfer.ToAddress, transfer.OperationId);
                return transfer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create token transfer for operation {OperationId}", transfer.OperationId);
                throw;
            }
        }
    }
}