namespace NeoServiceLayer.Core;

// Service Interfaces
public interface ICrossChainService : IEnclaveService, IBlockchainService
{
    Task<string> SendMessageAsync(CrossChainMessageRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain);
    Task<string> TransferTokensAsync(CrossChainTransferRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain);
    Task<string> ExecuteRemoteCallAsync(RemoteCallRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain);
    Task<CrossChainMessageStatus> GetMessageStatusAsync(string messageId, BlockchainType blockchainType);
    Task<IEnumerable<CrossChainMessage>> GetPendingMessagesAsync(BlockchainType destinationChain);
    Task<bool> VerifyMessageAsync(string messageId, string proof, BlockchainType blockchainType);
    Task<CrossChainRoute> GetOptimalRouteAsync(BlockchainType source, BlockchainType destination);
    Task<decimal> EstimateFeesAsync(CrossChainOperation operation, BlockchainType blockchainType);
    Task<IEnumerable<SupportedChain>> GetSupportedChainsAsync();
    Task<bool> RegisterTokenMappingAsync(TokenMapping mapping, BlockchainType blockchainType);
}

public interface IProofOfReserveService : IEnclaveService, IBlockchainService
{
    Task<string> RegisterAssetAsync(AssetRegistrationRequest registration, BlockchainType blockchainType);
    Task<bool> UpdateReserveDataAsync(string assetId, ReserveUpdateRequest data, BlockchainType blockchainType);
    Task<ProofOfReserve> GenerateProofAsync(string assetId, BlockchainType blockchainType);
    Task<bool> VerifyProofAsync(string proofId, BlockchainType blockchainType);
    Task<ReserveStatusInfo> GetReserveStatusAsync(string assetId, BlockchainType blockchainType);
    Task<IEnumerable<MonitoredAsset>> GetRegisteredAssetsAsync(BlockchainType blockchainType);
    Task<ReserveSnapshot[]> GetReserveHistoryAsync(string assetId, DateTime from, DateTime to, BlockchainType blockchainType);
    Task<bool> SetAlertThresholdAsync(string assetId, decimal threshold, BlockchainType blockchainType);
    Task<IEnumerable<ReserveAlert>> GetActiveAlertsAsync(BlockchainType blockchainType);
    Task<AuditReport> GenerateAuditReportAsync(string assetId, DateTime from, DateTime to, BlockchainType blockchainType);
}

public interface IAutomationService : IEnclaveService, IBlockchainService
{
    Task<string> CreateJobAsync(AutomationJobRequest request, BlockchainType blockchainType);
    Task<bool> UpdateJobAsync(string jobId, AutomationJobUpdate update, BlockchainType blockchainType);
    Task<bool> CancelJobAsync(string jobId, BlockchainType blockchainType);
    Task<bool> PauseJobAsync(string jobId, BlockchainType blockchainType);
    Task<bool> ResumeJobAsync(string jobId, BlockchainType blockchainType);
    Task<AutomationJobStatus> GetJobStatusAsync(string jobId, BlockchainType blockchainType);
    Task<IEnumerable<AutomationJob>> GetJobsAsync(string owner, BlockchainType blockchainType);
    Task<IEnumerable<AutomationExecution>> GetExecutionHistoryAsync(string jobId, BlockchainType blockchainType);
}

public interface IHealthService : IEnclaveService, IBlockchainService
{
    Task<NodeHealthReport?> GetNodeHealthAsync(string nodeAddress, BlockchainType blockchainType);
    Task<IEnumerable<NodeHealthReport>> GetAllNodesHealthAsync(BlockchainType blockchainType);
    Task<ConsensusHealthReport> GetConsensusHealthAsync(BlockchainType blockchainType);
    Task<bool> RegisterNodeForMonitoringAsync(NodeRegistrationRequest request, BlockchainType blockchainType);
    Task<bool> UnregisterNodeAsync(string nodeAddress, BlockchainType blockchainType);
    Task<IEnumerable<HealthAlert>> GetActiveAlertsAsync(BlockchainType blockchainType);
    Task<bool> SetHealthThresholdAsync(string nodeAddress, HealthThreshold threshold, BlockchainType blockchainType);
    Task<HealthMetrics> GetNetworkMetricsAsync(BlockchainType blockchainType);
}

public interface IVotingService : IEnclaveService, IBlockchainService
{
    Task<string> CreateVotingStrategyAsync(VotingStrategyRequest request, BlockchainType blockchainType);
    Task<bool> ExecuteVotingAsync(string strategyId, string voterAddress, BlockchainType blockchainType);
    Task<VotingResult> GetVotingResultAsync(string executionId, BlockchainType blockchainType);
    Task<IEnumerable<CandidateInfo>> GetCandidatesAsync(BlockchainType blockchainType);
    Task<IEnumerable<VotingStrategy>> GetVotingStrategiesAsync(string ownerAddress, BlockchainType blockchainType);
    Task<bool> UpdateVotingStrategyAsync(string strategyId, VotingStrategyUpdate update, BlockchainType blockchainType);
    Task<bool> DeleteVotingStrategyAsync(string strategyId, BlockchainType blockchainType);
    Task<VotingRecommendation> GetVotingRecommendationAsync(VotingPreferences preferences, BlockchainType blockchainType);
}
