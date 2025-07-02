using NeoServiceLayer.Core;
using NeoServiceLayer.Services.ProofOfReserve.Models;

namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Interface for Proof of Reserve Service operations.
/// </summary>
public interface IProofOfReserveService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Registers an asset for proof of reserve monitoring.
    /// </summary>
    Task<string> RegisterAssetAsync(AssetRegistrationRequest registration, BlockchainType blockchainType);
    
    /// <summary>
    /// Updates reserve data for a registered asset.
    /// </summary>
    Task<bool> UpdateReserveDataAsync(string assetId, ReserveUpdateRequest data, BlockchainType blockchainType);
    
    /// <summary>
    /// Generates a proof of reserve for an asset.
    /// </summary>
    Task<Models.ProofOfReserve> GenerateProofAsync(string assetId, BlockchainType blockchainType);
    
    /// <summary>
    /// Verifies a proof of reserve.
    /// </summary>
    Task<bool> VerifyProofAsync(string proofId, BlockchainType blockchainType);
    
    /// <summary>
    /// Gets the reserve status for an asset.
    /// </summary>
    Task<ReserveStatusInfo> GetReserveStatusAsync(string assetId, BlockchainType blockchainType);
    
    /// <summary>
    /// Gets the health status of reserves for an asset.
    /// </summary>
    Task<ReserveHealthStatus> GetReserveHealthAsync(string assetId, BlockchainType blockchainType);
    
    /// <summary>
    /// Gets all registered assets for monitoring.
    /// </summary>
    Task<IEnumerable<Models.MonitoredAsset>> GetRegisteredAssetsAsync(BlockchainType blockchainType);
    
    /// <summary>
    /// Gets historical reserve snapshots for an asset.
    /// </summary>
    Task<Models.ReserveSnapshot[]> GetReserveHistoryAsync(string assetId, DateTime from, DateTime to, BlockchainType blockchainType);
    
    /// <summary>
    /// Subscribes to reserve update notifications.
    /// </summary>
    Task<string> SubscribeToReserveUpdatesAsync(string assetId, string callbackUrl, BlockchainType blockchainType);
    
    /// <summary>
    /// Unsubscribes from reserve update notifications.
    /// </summary>
    Task<bool> UnsubscribeFromReserveUpdatesAsync(string subscriptionId, BlockchainType blockchainType);
    
    /// <summary>
    /// Sets an alert threshold for reserve monitoring.
    /// </summary>
    Task<bool> SetAlertThresholdAsync(string assetId, decimal threshold, BlockchainType blockchainType);
    
    /// <summary>
    /// Sets up an alert configuration for reserve monitoring.
    /// </summary>
    Task<string> SetupAlertAsync(string assetId, Models.ReserveAlertConfig alertConfig, BlockchainType blockchainType);
    
    /// <summary>
    /// Gets active alerts for reserve monitoring.
    /// </summary>
    Task<IEnumerable<ReserveAlert>> GetActiveAlertsAsync(BlockchainType blockchainType);
    
    /// <summary>
    /// Generates an audit report for reserve activity.
    /// </summary>
    Task<AuditReport> GenerateAuditReportAsync(string assetId, DateTime from, DateTime to, BlockchainType blockchainType);
}