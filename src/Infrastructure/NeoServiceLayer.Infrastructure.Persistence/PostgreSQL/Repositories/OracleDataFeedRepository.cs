using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Entities.OracleEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories
{
    /// <summary>
    /// Oracle data feed repository interface.
    /// </summary>
    public interface IOracleDataFeedRepository
    {
        Task<OracleDataFeed?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<OracleDataFeed?> GetByFeedIdAsync(string feedId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OracleDataFeed>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<OracleDataFeed>> GetActiveAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<OracleDataFeed>> GetByTypeAsync(string feedType, CancellationToken cancellationToken = default);
        Task<OracleDataFeed> CreateAsync(OracleDataFeed feed, CancellationToken cancellationToken = default);
        Task<OracleDataFeed> UpdateAsync(OracleDataFeed feed, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<FeedHistory>> GetHistoryAsync(string feedId, int limit = 100, CancellationToken cancellationToken = default);
        Task<FeedHistory> AddHistoryAsync(FeedHistory history, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// PostgreSQL implementation of Oracle data feed repository.
    /// </summary>
    public class OracleDataFeedRepository : IOracleDataFeedRepository
    {
        private readonly NeoServiceLayerDbContext _context;
        private readonly ILogger<OracleDataFeedRepository> _logger;

        public OracleDataFeedRepository(
            NeoServiceLayerDbContext context,
            ILogger<OracleDataFeedRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OracleDataFeed?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.OracleDataFeeds
                    .Include(f => f.History.OrderByDescending(h => h.RecordedAt).Take(10))
                    .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get oracle data feed by ID {Id}", id);
                throw;
            }
        }

        public async Task<OracleDataFeed?> GetByFeedIdAsync(string feedId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.OracleDataFeeds
                    .Include(f => f.History.OrderByDescending(h => h.RecordedAt).Take(10))
                    .FirstOrDefaultAsync(f => f.FeedId == feedId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get oracle data feed by FeedId {FeedId}", feedId);
                throw;
            }
        }

        public async Task<IEnumerable<OracleDataFeed>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.OracleDataFeeds
                    .Include(f => f.History.OrderByDescending(h => h.RecordedAt).Take(5))
                    .OrderBy(f => f.Name)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all oracle data feeds");
                throw;
            }
        }

        public async Task<IEnumerable<OracleDataFeed>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.OracleDataFeeds
                    .Include(f => f.History.OrderByDescending(h => h.RecordedAt).Take(5))
                    .Where(f => f.IsActive && (f.ExpiresAt == null || f.ExpiresAt > DateTime.UtcNow))
                    .OrderBy(f => f.Name)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active oracle data feeds");
                throw;
            }
        }

        public async Task<IEnumerable<OracleDataFeed>> GetByTypeAsync(string feedType, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.OracleDataFeeds
                    .Include(f => f.History.OrderByDescending(h => h.RecordedAt).Take(5))
                    .Where(f => f.FeedType == feedType && f.IsActive)
                    .OrderBy(f => f.Name)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get oracle data feeds by type {FeedType}", feedType);
                throw;
            }
        }

        public async Task<OracleDataFeed> CreateAsync(OracleDataFeed feed, CancellationToken cancellationToken = default)
        {
            try
            {
                feed.Id = Guid.NewGuid();
                feed.CreatedAt = DateTime.UtcNow;
                feed.UpdatedAt = DateTime.UtcNow;

                _context.OracleDataFeeds.Add(feed);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created oracle data feed {FeedId}", feed.FeedId);
                return feed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create oracle data feed {FeedId}", feed.FeedId);
                throw;
            }
        }

        public async Task<OracleDataFeed> UpdateAsync(OracleDataFeed feed, CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _context.OracleDataFeeds
                    .FirstOrDefaultAsync(f => f.Id == feed.Id, cancellationToken);

                if (existing == null)
                {
                    throw new InvalidOperationException($"Oracle data feed with ID {feed.Id} not found");
                }

                // Update properties
                existing.Name = feed.Name;
                existing.Description = feed.Description;
                existing.FeedType = feed.FeedType;
                existing.Value = feed.Value;
                existing.ValueString = feed.ValueString;
                existing.ConfidenceScore = feed.ConfidenceScore;
                existing.Source = feed.Source;
                existing.ExpiresAt = feed.ExpiresAt;
                existing.IsActive = feed.IsActive;
                existing.Metadata = feed.Metadata;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated oracle data feed {FeedId}", existing.FeedId);
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update oracle data feed {Id}", feed.Id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var feed = await _context.OracleDataFeeds
                    .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

                if (feed != null)
                {
                    // Soft delete
                    feed.IsActive = false;
                    feed.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Deleted oracle data feed {FeedId}", feed.FeedId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete oracle data feed {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<FeedHistory>> GetHistoryAsync(string feedId, int limit = 100, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.FeedHistory
                    .Where(h => h.FeedId == feedId)
                    .OrderByDescending(h => h.RecordedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get history for oracle data feed {FeedId}", feedId);
                throw;
            }
        }

        public async Task<FeedHistory> AddHistoryAsync(FeedHistory history, CancellationToken cancellationToken = default)
        {
            try
            {
                history.Id = Guid.NewGuid();
                history.RecordedAt = DateTime.UtcNow;

                _context.FeedHistory.Add(history);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Added history entry for oracle data feed {FeedId}", history.FeedId);
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add history for oracle data feed {FeedId}", history.FeedId);
                throw;
            }
        }
    }
}