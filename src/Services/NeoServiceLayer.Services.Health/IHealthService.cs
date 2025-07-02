using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Health;

/// <summary>
/// Interface for Health Service operations.
/// </summary>
public interface IHealthService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Gets health information for a specific node.
    /// </summary>
    Task<NodeHealthReport?> GetNodeHealthAsync(string nodeAddress, BlockchainType blockchainType);
    
    /// <summary>
    /// Gets health information for all monitored nodes.
    /// </summary>
    Task<IEnumerable<NodeHealthReport>> GetAllNodesHealthAsync(BlockchainType blockchainType);
    
    /// <summary>
    /// Gets consensus health information for the blockchain.
    /// </summary>
    Task<ConsensusHealthReport> GetConsensusHealthAsync(BlockchainType blockchainType);
    
    /// <summary>
    /// Registers a node for health monitoring.
    /// </summary>
    Task<bool> RegisterNodeForMonitoringAsync(NodeRegistrationRequest request, BlockchainType blockchainType);
    
    /// <summary>
    /// Unregisters a node from health monitoring.
    /// </summary>
    Task<bool> UnregisterNodeAsync(string nodeAddress, BlockchainType blockchainType);
    
    /// <summary>
    /// Gets active health alerts.
    /// </summary>
    Task<IEnumerable<HealthAlert>> GetActiveAlertsAsync(BlockchainType blockchainType);
    
    /// <summary>
    /// Sets health thresholds for a node.
    /// </summary>
    Task<bool> SetHealthThresholdAsync(string nodeAddress, HealthThreshold threshold, BlockchainType blockchainType);
    
    /// <summary>
    /// Gets network-wide health metrics.
    /// </summary>
    Task<HealthMetrics> GetNetworkMetricsAsync(BlockchainType blockchainType);
}