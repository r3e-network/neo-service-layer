using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.DataArchival;

public static class DataArchivalExtensions
{
    public static IServiceCollection AddDataArchival(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DataArchivalOptions>(configuration.GetSection("DataArchival"));

        // Add archival services
        services.AddScoped<IArchivalService, ArchivalService>();
        services.AddScoped<IArchivalStorage, CloudArchivalStorage>();
        services.AddScoped<IArchivalPolicy, TimeBasedArchivalPolicy>();
        services.AddScoped<IArchivalCompression, GzipArchivalCompression>();
        
        // Add archival strategies
        services.AddScoped<IArchivalStrategy, PartitionArchivalStrategy>();
        services.AddScoped<IArchivalStrategy, DeleteArchivalStrategy>();
        services.AddScoped<IArchivalStrategy, CompressArchivalStrategy>();

        // Add restoration service
        services.AddScoped<IRestorationService, RestorationService>();

        // Add archival job
        services.AddHostedService<DataArchivalBackgroundService>();

        return services;
    }

    public static IApplicationBuilder UseDataArchival(this IApplicationBuilder app)
    {
        // Initialize archival storage
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var storage = scope.ServiceProvider.GetRequiredService<IArchivalStorage>();
            storage.InitializeAsync().GetAwaiter().GetResult();
        }

        return app;
    }
}

// Archival service interface
public interface IArchivalService
{
    Task<ArchivalResult> ArchiveDataAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate, 
        ArchivalOptions options = null,
        CancellationToken cancellationToken = default) where TEntity : class;
    
    Task<ArchivalResult> ArchiveDataAsync(
        string tableName,
        string whereClause,
        ArchivalOptions options = null,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<ArchivalJob>> GetPendingArchivalJobsAsync(CancellationToken cancellationToken = default);
    Task<ArchivalStatistics> GetStatisticsAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
}

// Archival service implementation
public class ArchivalService : IArchivalService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IArchivalStorage _storage;
    private readonly IEnumerable<IArchivalStrategy> _strategies;
    private readonly IArchivalPolicy _policy;
    private readonly ILogger<ArchivalService> _logger;
    private readonly DataArchivalOptions _options;

    public ArchivalService(
        IServiceProvider serviceProvider,
        IArchivalStorage storage,
        IEnumerable<IArchivalStrategy> strategies,
        IArchivalPolicy policy,
        ILogger<ArchivalService> logger,
        IOptions<DataArchivalOptions> options)
    {
        _serviceProvider = serviceProvider;
        _storage = storage;
        _strategies = strategies;
        _policy = policy;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<ArchivalResult> ArchiveDataAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate,
        ArchivalOptions options = null,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity));
        var tableName = entityType.GetTableName();

        // Get data to archive
        var query = dbContext.Set<TEntity>().Where(predicate);
        var count = await query.CountAsync(cancellationToken);

        if (count == 0)
        {
            return new ArchivalResult { Success = true, RecordsArchived = 0 };
        }

        _logger.LogInformation("Starting archival of {Count} records from {Table}", count, tableName);

        var result = new ArchivalResult
        {
            TableName = tableName,
            StartTime = DateTimeOffset.UtcNow
        };

        try
        {
            // Determine archival strategy
            var strategy = DetermineStrategy(options ?? new ArchivalOptions());
            
            // Execute archival
            var batchSize = options?.BatchSize ?? _options.DefaultBatchSize;
            var totalProcessed = 0;

            while (totalProcessed < count)
            {
                var batch = await query
                    .Skip(totalProcessed)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (!batch.Any())
                {
                    break;
                }

                var archivalData = new ArchivalData
                {
                    Id = Guid.NewGuid(),
                    TableName = tableName,
                    Records = batch.Cast<object>().ToList(),
                    ArchivalDate = DateTimeOffset.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["RecordCount"] = batch.Count,
                        ["BatchNumber"] = totalProcessed / batchSize + 1
                    }
                };

                // Apply strategy
                await strategy.ExecuteAsync(archivalData, dbContext, _storage, cancellationToken);

                totalProcessed += batch.Count;
                result.RecordsArchived += batch.Count;

                _logger.LogDebug("Archived batch {BatchNumber} with {Count} records", 
                    archivalData.Metadata["BatchNumber"], batch.Count);
            }

            result.Success = true;
            result.EndTime = DateTimeOffset.UtcNow;
            result.Duration = result.EndTime.Value - result.StartTime;

            _logger.LogInformation("Completed archival of {Count} records from {Table} in {Duration}",
                result.RecordsArchived, tableName, result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving data from {Table}", tableName);
            result.Success = false;
            result.Error = ex.Message;
            result.EndTime = DateTimeOffset.UtcNow;
            return result;
        }
    }

    public async Task<ArchivalResult> ArchiveDataAsync(
        string tableName,
        string whereClause,
        ArchivalOptions options = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();

        var result = new ArchivalResult
        {
            TableName = tableName,
            StartTime = DateTimeOffset.UtcNow
        };

        try
        {
            // Execute raw SQL to get count
            var countSql = $"SELECT COUNT(*) FROM {tableName} WHERE {whereClause}";
            using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = countSql;
            
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

            if (count == 0)
            {
                return new ArchivalResult { Success = true, RecordsArchived = 0 };
            }

            // Archive in batches
            var batchSize = options?.BatchSize ?? _options.DefaultBatchSize;
            var offset = 0;

            while (offset < count)
            {
                var selectSql = $@"
                    SELECT * FROM {tableName} 
                    WHERE {whereClause}
                    ORDER BY 1
                    LIMIT {batchSize} OFFSET {offset}";

                var records = await dbContext.Database
                    .SqlQueryRaw<Dictionary<string, object>>(selectSql)
                    .ToListAsync(cancellationToken);

                if (!records.Any())
                {
                    break;
                }

                var archivalData = new ArchivalData
                {
                    Id = Guid.NewGuid(),
                    TableName = tableName,
                    Records = records.Cast<object>().ToList(),
                    ArchivalDate = DateTimeOffset.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["WhereClause"] = whereClause,
                        ["RecordCount"] = records.Count
                    }
                };

                // Store archived data
                await _storage.StoreAsync(archivalData, cancellationToken);

                // Delete archived records if configured
                if (options?.DeleteAfterArchival ?? _options.DeleteAfterArchival)
                {
                    var deleteSql = $@"
                        DELETE FROM {tableName} 
                        WHERE {whereClause}
                        AND ctid IN (
                            SELECT ctid FROM {tableName}
                            WHERE {whereClause}
                            ORDER BY 1
                            LIMIT {batchSize}
                        )";

                    await dbContext.Database.ExecuteSqlRawAsync(deleteSql, cancellationToken);
                }

                offset += batchSize;
                result.RecordsArchived += records.Count;
            }

            result.Success = true;
            result.EndTime = DateTimeOffset.UtcNow;
            result.Duration = result.EndTime.Value - result.StartTime;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving data from {Table}", tableName);
            result.Success = false;
            result.Error = ex.Message;
            result.EndTime = DateTimeOffset.UtcNow;
            return result;
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    public async Task<IEnumerable<ArchivalJob>> GetPendingArchivalJobsAsync(CancellationToken cancellationToken = default)
    {
        var jobs = new List<ArchivalJob>();

        foreach (var config in _options.ArchivalConfigurations)
        {
            if (await _policy.ShouldArchiveAsync(config, cancellationToken))
            {
                jobs.Add(new ArchivalJob
                {
                    Id = Guid.NewGuid(),
                    TableName = config.TableName,
                    WhereClause = config.WhereClause,
                    ScheduledDate = DateTimeOffset.UtcNow,
                    Priority = config.Priority
                });
            }
        }

        return jobs.OrderBy(j => j.Priority).ThenBy(j => j.ScheduledDate);
    }

    public async Task<ArchivalStatistics> GetStatisticsAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        var manifests = await _storage.GetManifestsAsync(startDate, endDate);
        
        return new ArchivalStatistics
        {
            TotalArchives = manifests.Count(),
            TotalRecords = manifests.Sum(m => m.RecordCount),
            TotalSize = manifests.Sum(m => m.CompressedSize),
            TableStatistics = manifests
                .GroupBy(m => m.TableName)
                .Select(g => new TableArchivalStatistics
                {
                    TableName = g.Key,
                    ArchiveCount = g.Count(),
                    RecordCount = g.Sum(m => m.RecordCount),
                    TotalSize = g.Sum(m => m.CompressedSize),
                    OldestArchive = g.Min(m => m.ArchivalDate),
                    NewestArchive = g.Max(m => m.ArchivalDate)
                })
                .ToList()
        };
    }

    private IArchivalStrategy DetermineStrategy(ArchivalOptions options)
    {
        var strategyName = options.Strategy ?? _options.DefaultStrategy;
        return _strategies.FirstOrDefault(s => s.Name.Equals(strategyName, StringComparison.OrdinalIgnoreCase))
            ?? _strategies.First();
    }
}

// Archival storage interface
public interface IArchivalStorage
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<string> StoreAsync(ArchivalData data, CancellationToken cancellationToken = default);
    Task<ArchivalData> RetrieveAsync(string archiveId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string archiveId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ArchivalManifest>> GetManifestsAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
}

// Cloud archival storage implementation
public class CloudArchivalStorage : IArchivalStorage
{
    private readonly IArchivalCompression _compression;
    private readonly ILogger<CloudArchivalStorage> _logger;
    private readonly DataArchivalOptions _options;
    private readonly string _basePath;

    public CloudArchivalStorage(
        IArchivalCompression compression,
        ILogger<CloudArchivalStorage> logger,
        IOptions<DataArchivalOptions> options)
    {
        _compression = compression;
        _logger = logger;
        _options = options.Value;
        _basePath = _options.StoragePath ?? Path.Combine(Path.GetTempPath(), "archives");
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }

        return Task.CompletedTask;
    }

    public async Task<string> StoreAsync(ArchivalData data, CancellationToken cancellationToken = default)
    {
        var archiveId = GenerateArchiveId(data);
        var archivePath = GetArchivePath(archiveId);

        // Serialize data
        var json = JsonSerializer.Serialize(data);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        // Compress data
        var compressedData = await _compression.CompressAsync(jsonBytes, cancellationToken);

        // Store compressed data
        await File.WriteAllBytesAsync(archivePath, compressedData, cancellationToken);

        // Create manifest
        var manifest = new ArchivalManifest
        {
            ArchiveId = archiveId,
            TableName = data.TableName,
            RecordCount = data.Records.Count,
            OriginalSize = jsonBytes.Length,
            CompressedSize = compressedData.Length,
            CompressionRatio = (double)compressedData.Length / jsonBytes.Length,
            ArchivalDate = data.ArchivalDate,
            StoragePath = archivePath
        };

        await StoreManifestAsync(manifest, cancellationToken);

        _logger.LogInformation("Stored archive {ArchiveId} with {RecordCount} records ({CompressedSize} bytes)",
            archiveId, data.Records.Count, compressedData.Length);

        return archiveId;
    }

    public async Task<ArchivalData> RetrieveAsync(string archiveId, CancellationToken cancellationToken = default)
    {
        var archivePath = GetArchivePath(archiveId);
        
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException($"Archive {archiveId} not found");
        }

        // Read compressed data
        var compressedData = await File.ReadAllBytesAsync(archivePath, cancellationToken);

        // Decompress data
        var jsonBytes = await _compression.DecompressAsync(compressedData, cancellationToken);

        // Deserialize data
        var json = Encoding.UTF8.GetString(jsonBytes);
        var data = JsonSerializer.Deserialize<ArchivalData>(json);

        _logger.LogInformation("Retrieved archive {ArchiveId} with {RecordCount} records",
            archiveId, data.Records.Count);

        return data;
    }

    public async Task<bool> DeleteAsync(string archiveId, CancellationToken cancellationToken = default)
    {
        var archivePath = GetArchivePath(archiveId);
        
        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
            await DeleteManifestAsync(archiveId, cancellationToken);
            
            _logger.LogInformation("Deleted archive {ArchiveId}", archiveId);
            return true;
        }

        return false;
    }

    public async Task<IEnumerable<ArchivalManifest>> GetManifestsAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        var manifestPath = Path.Combine(_basePath, "manifests");
        if (!Directory.Exists(manifestPath))
        {
            return Enumerable.Empty<ArchivalManifest>();
        }

        var manifests = new List<ArchivalManifest>();

        foreach (var file in Directory.GetFiles(manifestPath, "*.manifest"))
        {
            var json = await File.ReadAllTextAsync(file);
            var manifest = JsonSerializer.Deserialize<ArchivalManifest>(json);

            if (startDate.HasValue && manifest.ArchivalDate < startDate.Value)
                continue;

            if (endDate.HasValue && manifest.ArchivalDate > endDate.Value)
                continue;

            manifests.Add(manifest);
        }

        return manifests;
    }

    private string GenerateArchiveId(ArchivalData data)
    {
        return $"{data.TableName}_{data.ArchivalDate:yyyyMMddHHmmss}_{data.Id:N}";
    }

    private string GetArchivePath(string archiveId)
    {
        var year = archiveId.Split('_')[1].Substring(0, 4);
        var month = archiveId.Split('_')[1].Substring(4, 2);
        var directory = Path.Combine(_basePath, year, month);
        
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return Path.Combine(directory, $"{archiveId}.archive");
    }

    private async Task StoreManifestAsync(ArchivalManifest manifest, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(_basePath, "manifests");
        if (!Directory.Exists(manifestPath))
        {
            Directory.CreateDirectory(manifestPath);
        }

        var filePath = Path.Combine(manifestPath, $"{manifest.ArchiveId}.manifest");
        var json = JsonSerializer.Serialize(manifest);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private Task DeleteManifestAsync(string archiveId, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_basePath, "manifests", $"{archiveId}.manifest");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}

// Archival strategies
public interface IArchivalStrategy
{
    string Name { get; }
    Task ExecuteAsync(ArchivalData data, DbContext dbContext, IArchivalStorage storage, CancellationToken cancellationToken);
}

public class PartitionArchivalStrategy : IArchivalStrategy
{
    public string Name => "Partition";

    public async Task ExecuteAsync(ArchivalData data, DbContext dbContext, IArchivalStorage storage, CancellationToken cancellationToken)
    {
        // Store data in archive
        await storage.StoreAsync(data, cancellationToken);

        // Keep data in original table (partitioned)
        _logger.LogDebug("Partition strategy: Data archived but kept in original table");
    }

    private readonly ILogger<PartitionArchivalStrategy> _logger;

    public PartitionArchivalStrategy(ILogger<PartitionArchivalStrategy> logger)
    {
        _logger = logger;
    }
}

public class DeleteArchivalStrategy : IArchivalStrategy
{
    public string Name => "Delete";

    public async Task ExecuteAsync(ArchivalData data, DbContext dbContext, IArchivalStorage storage, CancellationToken cancellationToken)
    {
        // Store data in archive
        await storage.StoreAsync(data, cancellationToken);

        // Delete from original table
        dbContext.RemoveRange(data.Records);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Delete strategy: Data archived and removed from original table");
    }

    private readonly ILogger<DeleteArchivalStrategy> _logger;

    public DeleteArchivalStrategy(ILogger<DeleteArchivalStrategy> logger)
    {
        _logger = logger;
    }
}

public class CompressArchivalStrategy : IArchivalStrategy
{
    public string Name => "Compress";

    public async Task ExecuteAsync(ArchivalData data, DbContext dbContext, IArchivalStorage storage, CancellationToken cancellationToken)
    {
        // Store data in archive
        var archiveId = await storage.StoreAsync(data, cancellationToken);

        // Update original records with archive reference
        foreach (var record in data.Records)
        {
            // This is a simplified example
            // In practice, you'd update a specific field or related table
            _logger.LogDebug("Compress strategy: Record archived with ID {ArchiveId}", archiveId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private readonly ILogger<CompressArchivalStrategy> _logger;

    public CompressArchivalStrategy(ILogger<CompressArchivalStrategy> logger)
    {
        _logger = logger;
    }
}

// Archival policy
public interface IArchivalPolicy
{
    Task<bool> ShouldArchiveAsync(ArchivalConfiguration config, CancellationToken cancellationToken = default);
}

public class TimeBasedArchivalPolicy : IArchivalPolicy
{
    public async Task<bool> ShouldArchiveAsync(ArchivalConfiguration config, CancellationToken cancellationToken = default)
    {
        // Check if enough time has passed since last archival
        var now = DateTimeOffset.UtcNow;
        
        if (config.LastArchivalDate.HasValue)
        {
            var timeSinceLastArchival = now - config.LastArchivalDate.Value;
            if (timeSinceLastArchival < config.ArchivalInterval)
            {
                return false;
            }
        }

        // Check if it's within the archival window
        if (config.ArchivalWindow != null)
        {
            var currentHour = now.Hour;
            if (currentHour < config.ArchivalWindow.StartHour || currentHour > config.ArchivalWindow.EndHour)
            {
                return false;
            }
        }

        return true;
    }
}

// Compression
public interface IArchivalCompression
{
    Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default);
    Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default);
}

public class GzipArchivalCompression : IArchivalCompression
{
    public async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            await gzip.WriteAsync(data, 0, data.Length, cancellationToken);
        }
        return output.ToArray();
    }

    public async Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default)
    {
        using var input = new MemoryStream(compressedData);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
        {
            await gzip.CopyToAsync(output, cancellationToken);
        }
        return output.ToArray();
    }
}

// Restoration service
public interface IRestorationService
{
    Task<RestorationResult> RestoreAsync(string archiveId, RestoreOptions options = null, CancellationToken cancellationToken = default);
    Task<RestorationResult> RestoreRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate, RestoreOptions options = null, CancellationToken cancellationToken = default);
}

public class RestorationService : IRestorationService
{
    private readonly IArchivalStorage _storage;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RestorationService> _logger;

    public RestorationService(
        IArchivalStorage storage,
        IServiceProvider serviceProvider,
        ILogger<RestorationService> logger)
    {
        _storage = storage;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<RestorationResult> RestoreAsync(string archiveId, RestoreOptions options = null, CancellationToken cancellationToken = default)
    {
        var result = new RestorationResult
        {
            ArchiveId = archiveId,
            StartTime = DateTimeOffset.UtcNow
        };

        try
        {
            // Retrieve archived data
            var data = await _storage.RetrieveAsync(archiveId, cancellationToken);

            // Restore to database
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();

            // Begin transaction
            using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Add records back to database
                await dbContext.AddRangeAsync(data.Records, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                result.Success = true;
                result.RecordsRestored = data.Records.Count;

                // Delete archive if requested
                if (options?.DeleteArchiveAfterRestore ?? false)
                {
                    await _storage.DeleteAsync(archiveId, cancellationToken);
                }
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring archive {ArchiveId}", archiveId);
            result.Success = false;
            result.Error = ex.Message;
        }
        finally
        {
            result.EndTime = DateTimeOffset.UtcNow;
            result.Duration = result.EndTime.Value - result.StartTime;
        }

        return result;
    }

    public async Task<RestorationResult> RestoreRangeAsync(
        DateTimeOffset startDate, 
        DateTimeOffset endDate, 
        RestoreOptions options = null, 
        CancellationToken cancellationToken = default)
    {
        var result = new RestorationResult
        {
            StartTime = DateTimeOffset.UtcNow
        };

        try
        {
            var manifests = await _storage.GetManifestsAsync(startDate, endDate);
            
            foreach (var manifest in manifests)
            {
                var restoreResult = await RestoreAsync(manifest.ArchiveId, options, cancellationToken);
                result.RecordsRestored += restoreResult.RecordsRestored;
                
                if (!restoreResult.Success)
                {
                    result.Success = false;
                    result.Error = $"Failed to restore {manifest.ArchiveId}: {restoreResult.Error}";
                    break;
                }
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring archives in range {StartDate} to {EndDate}", startDate, endDate);
            result.Success = false;
            result.Error = ex.Message;
        }
        finally
        {
            result.EndTime = DateTimeOffset.UtcNow;
            result.Duration = result.EndTime.Value - result.StartTime;
        }

        return result;
    }
}

// Background service
public class DataArchivalBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataArchivalBackgroundService> _logger;
    private readonly DataArchivalOptions _options;

    public DataArchivalBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DataArchivalBackgroundService> logger,
        IOptions<DataArchivalOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableAutomaticArchival)
        {
            _logger.LogInformation("Automatic data archival is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessArchivalJobsAsync(stoppingToken);
                await Task.Delay(_options.ArchivalCheckInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing archival jobs");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task ProcessArchivalJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var archivalService = scope.ServiceProvider.GetRequiredService<IArchivalService>();

        var jobs = await archivalService.GetPendingArchivalJobsAsync(cancellationToken);

        foreach (var job in jobs)
        {
            try
            {
                _logger.LogInformation("Processing archival job for table {TableName}", job.TableName);

                var options = new ArchivalOptions
                {
                    BatchSize = _options.DefaultBatchSize,
                    DeleteAfterArchival = _options.DeleteAfterArchival
                };

                var result = await archivalService.ArchiveDataAsync(
                    job.TableName,
                    job.WhereClause,
                    options,
                    cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation("Archived {RecordCount} records from {TableName} in {Duration}",
                        result.RecordsArchived, job.TableName, result.Duration);
                }
                else
                {
                    _logger.LogError("Failed to archive data from {TableName}: {Error}",
                        job.TableName, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing archival job for table {TableName}", job.TableName);
            }
        }
    }
}

// Models
public class ArchivalData
{
    public Guid Id { get; set; }
    public string TableName { get; set; }
    public List<object> Records { get; set; }
    public DateTimeOffset ArchivalDate { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

public class ArchivalManifest
{
    public string ArchiveId { get; set; }
    public string TableName { get; set; }
    public int RecordCount { get; set; }
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public double CompressionRatio { get; set; }
    public DateTimeOffset ArchivalDate { get; set; }
    public string StoragePath { get; set; }
}

public class ArchivalResult
{
    public bool Success { get; set; }
    public string TableName { get; set; }
    public int RecordsArchived { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public string Error { get; set; }
}

public class RestorationResult
{
    public bool Success { get; set; }
    public string ArchiveId { get; set; }
    public int RecordsRestored { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public string Error { get; set; }
}

public class ArchivalJob
{
    public Guid Id { get; set; }
    public string TableName { get; set; }
    public string WhereClause { get; set; }
    public DateTimeOffset ScheduledDate { get; set; }
    public int Priority { get; set; }
}

public class ArchivalStatistics
{
    public int TotalArchives { get; set; }
    public long TotalRecords { get; set; }
    public long TotalSize { get; set; }
    public List<TableArchivalStatistics> TableStatistics { get; set; }
}

public class TableArchivalStatistics
{
    public string TableName { get; set; }
    public int ArchiveCount { get; set; }
    public long RecordCount { get; set; }
    public long TotalSize { get; set; }
    public DateTimeOffset OldestArchive { get; set; }
    public DateTimeOffset NewestArchive { get; set; }
}

public class ArchivalOptions
{
    public string Strategy { get; set; }
    public int? BatchSize { get; set; }
    public bool? DeleteAfterArchival { get; set; }
}

public class RestoreOptions
{
    public bool DeleteArchiveAfterRestore { get; set; }
}

// Configuration
public class DataArchivalOptions
{
    public bool EnableAutomaticArchival { get; set; } = true;
    public TimeSpan ArchivalCheckInterval { get; set; } = TimeSpan.FromHours(1);
    public string StoragePath { get; set; }
    public string DefaultStrategy { get; set; } = "Delete";
    public int DefaultBatchSize { get; set; } = 1000;
    public bool DeleteAfterArchival { get; set; } = true;
    public List<ArchivalConfiguration> ArchivalConfigurations { get; set; } = new();
}

public class ArchivalConfiguration
{
    public string TableName { get; set; }
    public string WhereClause { get; set; }
    public TimeSpan ArchivalInterval { get; set; }
    public ArchivalWindow ArchivalWindow { get; set; }
    public int Priority { get; set; } = 0;
    public DateTimeOffset? LastArchivalDate { get; set; }
}

public class ArchivalWindow
{
    public int StartHour { get; set; }
    public int EndHour { get; set; }
}