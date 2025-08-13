using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Statistics.Models;

namespace NeoServiceLayer.Services.Statistics;

/// <summary>
/// Interface for the statistics service that collects and provides metrics for the Neo Service Layer.
/// </summary>
public interface IStatisticsService : IService
{
    /// <summary>
    /// Gets the overall system statistics.
    /// </summary>
    /// <returns>The system statistics.</returns>
    Task<SystemStatistics> GetSystemStatisticsAsync();

    /// <summary>
    /// Gets statistics for a specific service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The service statistics.</returns>
    Task<ServiceStatistics> GetServiceStatisticsAsync(string serviceName);

    /// <summary>
    /// Gets statistics for all services.
    /// </summary>
    /// <returns>Dictionary of service statistics.</returns>
    Task<Dictionary<string, ServiceStatistics>> GetAllServiceStatisticsAsync();

    /// <summary>
    /// Gets blockchain-specific statistics.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The blockchain statistics.</returns>
    Task<BlockchainStatistics> GetBlockchainStatisticsAsync(BlockchainType blockchainType);

    /// <summary>
    /// Gets performance metrics for a time range.
    /// </summary>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <returns>Performance metrics.</returns>
    Task<PerformanceMetrics> GetPerformanceMetricsAsync(DateTime startTime, DateTime endTime);

    /// <summary>
    /// Records a service operation.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="duration">Operation duration in milliseconds.</param>
    Task RecordOperationAsync(string serviceName, string operation, bool success, long duration);

    /// <summary>
    /// Records a blockchain transaction.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="transactionType">The transaction type.</param>
    /// <param name="success">Whether the transaction succeeded.</param>
    Task RecordTransactionAsync(BlockchainType blockchainType, string transactionType, bool success);

    /// <summary>
    /// Gets real-time statistics stream.
    /// </summary>
    /// <returns>Observable stream of statistics updates.</returns>
    IAsyncEnumerable<StatisticsUpdate> GetRealTimeStatisticsAsync();

    /// <summary>
    /// Exports statistics for a given time range.
    /// </summary>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <param name="format">Export format (json, csv, prometheus).</param>
    /// <returns>Exported data.</returns>
    Task<byte[]> ExportStatisticsAsync(DateTime startTime, DateTime endTime, string format = "json");
}
