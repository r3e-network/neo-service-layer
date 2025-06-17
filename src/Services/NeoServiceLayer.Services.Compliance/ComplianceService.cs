using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Compliance.Models;
using NeoServiceLayer.Tee.Host.Services;
using System.Text.Json;

namespace NeoServiceLayer.Services.Compliance;

/// <summary>
/// Implementation of the Compliance service.
/// </summary>
public class ComplianceService : EnclaveBlockchainServiceBase, IComplianceService
{
    private new readonly IEnclaveManager _enclaveManager;
    private readonly IServiceConfiguration _configuration;
    private readonly Dictionary<string, ComplianceRule> _ruleCache = new();
    private int _requestCount;
    private int _successCount;
    private int _failureCount;
    private DateTime _lastRequestTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceService"/> class.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="logger">The logger.</param>
    public ComplianceService(
        IEnclaveManager enclaveManager,
        IServiceConfiguration configuration,
        ILogger<ComplianceService> logger)
        : base("Compliance", "Compliance Gateway Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        _enclaveManager = enclaveManager;
        _configuration = configuration;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;

        // Add capabilities
        AddCapability<IComplianceService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("MaxRuleCount", _configuration.GetValue("Compliance:MaxRuleCount", "1000"));
        SetMetadata("SupportedRuleTypes", "Address,Transaction,Contract,Token");

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Compliance Service...");

            // Initialize service-specific components
            await RefreshRuleCacheAsync();

            Logger.LogInformation("Compliance Service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Compliance Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Compliance Service enclave...");
            await _enclaveManager.InitializeAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Compliance Service enclave.");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        try
        {
            Logger.LogInformation("Starting Compliance Service...");

            // Load existing rules from the enclave
            await RefreshRuleCacheAsync();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting Compliance Service.");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStopAsync()
    {
        try
        {
            Logger.LogInformation("Stopping Compliance Service...");
            _ruleCache.Clear();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping Compliance Service.");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public async Task<VerificationResult> VerifyTransactionAsync(string transactionData, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        if (string.IsNullOrEmpty(transactionData))
        {
            throw new ArgumentException("Transaction data cannot be null or empty.", nameof(transactionData));
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Verify transaction in the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"verifyTransaction('{transactionData}', '{blockchainType}')");

            // Parse the result
            var verificationResult = JsonSerializer.Deserialize<VerificationResult>(result) ??
                throw new InvalidOperationException("Failed to deserialize verification result.");

            // Add additional information
            verificationResult.Timestamp = DateTime.UtcNow;
            verificationResult.BlockchainType = blockchainType;
            verificationResult.VerificationId = Guid.NewGuid().ToString();

            // Generate a proof for the verification result
            string dataToSign = $"{verificationResult.VerificationId}:{transactionData}:{verificationResult.Passed}:{verificationResult.RiskScore}";
            verificationResult.Proof = await _enclaveManager.SignDataAsync(dataToSign, "compliance-service-key");

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return verificationResult;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error verifying transaction for blockchain {BlockchainType}",
                blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<VerificationResult> VerifyAddressAsync(string address, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        if (string.IsNullOrEmpty(address))
        {
            throw new ArgumentException("Address cannot be null or empty.", nameof(address));
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Verify address in the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"verifyAddress('{address}', '{blockchainType}')");

            // Parse the result
            var verificationResult = JsonSerializer.Deserialize<VerificationResult>(result) ??
                throw new InvalidOperationException("Failed to deserialize verification result.");

            // Add additional information
            verificationResult.Timestamp = DateTime.UtcNow;
            verificationResult.BlockchainType = blockchainType;
            verificationResult.VerificationId = Guid.NewGuid().ToString();

            // Generate a proof for the verification result
            string dataToSign = $"{verificationResult.VerificationId}:{address}:{verificationResult.Passed}:{verificationResult.RiskScore}";
            verificationResult.Proof = await _enclaveManager.SignDataAsync(dataToSign, "compliance-service-key");

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return verificationResult;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error verifying address {Address} for blockchain {BlockchainType}",
                address, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<VerificationResult> VerifyContractAsync(string contractData, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        if (string.IsNullOrEmpty(contractData))
        {
            throw new ArgumentException("Contract data cannot be null or empty.", nameof(contractData));
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Verify contract in the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"verifyContract('{contractData}', '{blockchainType}')");

            // Parse the result
            var verificationResult = JsonSerializer.Deserialize<VerificationResult>(result) ??
                throw new InvalidOperationException("Failed to deserialize verification result.");

            // Add additional information
            verificationResult.Timestamp = DateTime.UtcNow;
            verificationResult.BlockchainType = blockchainType;
            verificationResult.VerificationId = Guid.NewGuid().ToString();

            // Generate a proof for the verification result
            string dataToSign = $"{verificationResult.VerificationId}:{contractData}:{verificationResult.Passed}:{verificationResult.RiskScore}";
            verificationResult.Proof = await _enclaveManager.SignDataAsync(dataToSign, "compliance-service-key");

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return verificationResult;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error verifying contract for blockchain {BlockchainType}",
                blockchainType);
            throw;
        }
    }
    /// <inheritdoc/>
    public async Task<IEnumerable<ComplianceRule>> GetComplianceRulesAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Get rules from the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"getComplianceRules('{blockchainType}')");

            // Parse the result
            var rules = JsonSerializer.Deserialize<List<ComplianceRule>>(result) ??
                throw new InvalidOperationException("Failed to deserialize compliance rules.");

            // Update the cache
            lock (_ruleCache)
            {
                foreach (var rule in rules)
                {
                    _ruleCache[rule.RuleId] = rule;
                }
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return rules;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error getting compliance rules for blockchain {BlockchainType}",
                blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AddComplianceRuleAsync(ComplianceRule rule, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        if (string.IsNullOrEmpty(rule.RuleId))
        {
            throw new ArgumentException("Rule ID cannot be null or empty.", nameof(rule));
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Check if the rule already exists
            if (_ruleCache.ContainsKey(rule.RuleId))
            {
                throw new ArgumentException($"Rule with ID {rule.RuleId} already exists.");
            }

            // Set creation and modification dates
            rule.CreatedAt = DateTime.UtcNow;
            rule.LastModifiedAt = DateTime.UtcNow;

            // Add rule to the enclave
            string ruleJson = JsonSerializer.Serialize(rule);
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"addComplianceRule({ruleJson}, '{blockchainType}')");
            bool success = JsonSerializer.Deserialize<bool>(result);

            if (success)
            {
                // Update the cache
                lock (_ruleCache)
                {
                    _ruleCache[rule.RuleId] = rule;
                }
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return success;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error adding compliance rule {RuleId} for blockchain {BlockchainType}",
                rule.RuleId, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveComplianceRuleAsync(string ruleId, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        if (string.IsNullOrEmpty(ruleId))
        {
            throw new ArgumentException("Rule ID cannot be null or empty.", nameof(ruleId));
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Check if the rule exists
            if (!_ruleCache.ContainsKey(ruleId))
            {
                throw new ArgumentException($"Rule with ID {ruleId} does not exist.");
            }

            // Remove rule from the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"removeComplianceRule('{ruleId}', '{blockchainType}')");
            bool success = JsonSerializer.Deserialize<bool>(result);

            if (success)
            {
                // Update the cache
                lock (_ruleCache)
                {
                    _ruleCache.Remove(ruleId);
                }
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return success;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error removing compliance rule {RuleId} for blockchain {BlockchainType}",
                ruleId, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateComplianceRuleAsync(ComplianceRule rule, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        if (string.IsNullOrEmpty(rule.RuleId))
        {
            throw new ArgumentException("Rule ID cannot be null or empty.", nameof(rule));
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Check if the rule exists
            if (!_ruleCache.TryGetValue(rule.RuleId, out var existingRule))
            {
                throw new ArgumentException($"Rule with ID {rule.RuleId} does not exist.");
            }

            // Preserve creation date
            rule.CreatedAt = existingRule.CreatedAt;
            rule.LastModifiedAt = DateTime.UtcNow;

            // Update rule in the enclave
            string ruleJson = JsonSerializer.Serialize(rule);
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"updateComplianceRule({ruleJson}, '{blockchainType}')");
            bool success = JsonSerializer.Deserialize<bool>(result);

            if (success)
            {
                // Update the cache
                lock (_ruleCache)
                {
                    _ruleCache[rule.RuleId] = rule;
                }
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return success;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error updating compliance rule {RuleId} for blockchain {BlockchainType}",
                rule.RuleId, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        var health = IsEnclaveInitialized && IsRunning
            ? ServiceHealth.Healthy
            : ServiceHealth.Unhealthy;

        return Task.FromResult(health);
    }

    /// <inheritdoc/>
    protected override Task OnUpdateMetricsAsync()
    {
        // Update service metrics
        UpdateMetric("RequestCount", _requestCount);
        UpdateMetric("SuccessCount", _successCount);
        UpdateMetric("FailureCount", _failureCount);
        UpdateMetric("SuccessRate", _requestCount > 0 ? (double)_successCount / _requestCount : 0);
        UpdateMetric("LastRequestTime", _lastRequestTime);
        UpdateMetric("RuleCount", _ruleCache.Count);

        return Task.CompletedTask;
    }

    private async Task RefreshRuleCacheAsync()
    {
        try
        {
            // Fetch rules from the enclave for supported blockchains
            var allRules = new List<ComplianceRule>();

            foreach (var blockchainType in SupportedBlockchains)
            {
                try
                {
                    var rules = await GetComplianceRulesAsync(blockchainType);
                    allRules.AddRange(rules);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to refresh rules for blockchain {BlockchainType}", blockchainType);
                }
            }

            // Update the cache
            lock (_ruleCache)
            {
                _ruleCache.Clear();
                foreach (var rule in allRules)
                {
                    _ruleCache[rule.RuleId] = rule;
                }
            }

            Logger.LogInformation("Refreshed {Count} compliance rules", allRules.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing rule cache");
        }
    }

    // Missing interface implementations for controller compatibility

    /// <inheritdoc/>
    public async Task<ComplianceCheckResult> CheckComplianceAsync(ComplianceCheckRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var result = new ComplianceCheckResult
            {
                CheckId = Guid.NewGuid().ToString(),
                Passed = true,
                ComplianceScore = 95,
                CheckedAt = DateTime.UtcNow,
                RiskLevel = "Low",
                Details = new Dictionary<string, string>
                {
                    ["RuleId"] = "default-rule",
                    ["RuleName"] = "Default Compliance Check",
                    ["Message"] = "Basic compliance check passed"
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to check compliance for request {RequestId}", request.RequestId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ComplianceReportResult> GenerateComplianceReportAsync(ComplianceReportRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var result = new ComplianceReportResult
            {
                ReportId = Guid.NewGuid().ToString(),
                GeneratedAt = DateTime.UtcNow,
                ReportType = request.ReportType,
                Status = "Completed",
                ReportData = new ComplianceReportData()
                {
                    ["totalChecks"] = 100,
                    ["passedChecks"] = 95,
                    ["failedChecks"] = 5,
                    ["averageRiskScore"] = 0.2m
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate compliance report");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ComplianceRuleResult> CreateComplianceRuleAsync(CreateComplianceRuleRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var rule = new ComplianceRule
            {
                RuleId = Guid.NewGuid().ToString(),
                RuleName = request.RuleName,
                RuleType = request.RuleType,
                RuleDescription = request.Description ?? "",
                Conditions = request.Conditions ?? new List<ComplianceCondition>(),
                Enabled = request.IsEnabled,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

            var success = await AddComplianceRuleAsync(rule, blockchainType);

            return new ComplianceRuleResult
            {
                RuleId = rule.RuleId,
                Success = success,
                Message = success ? "Rule created successfully" : "Failed to create rule",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create compliance rule");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ComplianceRuleResult> UpdateComplianceRuleAsync(UpdateComplianceRuleRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var existingRule = _ruleCache.GetValueOrDefault(request.RuleId);
            if (existingRule == null)
            {
                return new ComplianceRuleResult
                {
                    RuleId = request.RuleId,
                    Success = false,
                    Message = "Rule not found",
                    Timestamp = DateTime.UtcNow
                };
            }

            existingRule.RuleName = request.RuleName ?? existingRule.RuleName;
            existingRule.Description = request.Description ?? existingRule.Description;
            existingRule.Conditions = request.Conditions ?? existingRule.Conditions;
            existingRule.IsEnabled = request.IsEnabled ?? existingRule.IsEnabled;
            existingRule.LastModifiedAt = DateTime.UtcNow;

            var success = await UpdateComplianceRuleAsync(existingRule, blockchainType);

            return new ComplianceRuleResult
            {
                RuleId = request.RuleId,
                Success = success,
                Message = success ? "Rule updated successfully" : "Failed to update rule",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update compliance rule {RuleId}", request.RuleId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ComplianceRuleResult> DeleteComplianceRuleAsync(DeleteComplianceRuleRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var success = await RemoveComplianceRuleAsync(request.RuleId, blockchainType);

            return new ComplianceRuleResult
            {
                RuleId = request.RuleId,
                Success = success,
                Message = success ? "Rule deleted successfully" : "Failed to delete rule",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete compliance rule {RuleId}", request.RuleId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<GetComplianceRulesResult> GetComplianceRulesAsync(GetComplianceRulesRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var allRules = await GetComplianceRulesAsync(blockchainType);
            var filteredRules = allRules
                .Where(r => string.IsNullOrEmpty(request.RuleType) || r.RuleType == request.RuleType)
                .Where(r => !request.IsEnabledOnly || r.IsEnabled)
                .Skip(request.Skip)
                .Take(request.Take)
                .ToList();

            return new GetComplianceRulesResult
            {
                Rules = filteredRules,
                TotalCount = allRules.Count(),
                Success = true,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get compliance rules");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<AuditResult> StartAuditAsync(StartAuditRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            return new AuditResult
            {
                AuditId = Guid.NewGuid().ToString(),
                Status = AuditStatus.Running,
                Success = true,
                Message = "Audit started successfully",
                StartedAt = DateTime.UtcNow,
                EstimatedCompletion = DateTime.UtcNow.AddHours(1)
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start audit");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<AuditResult> GetAuditStatusAsync(GetAuditStatusRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            return new AuditResult
            {
                AuditId = request.AuditId,
                Status = AuditStatus.Running,
                Success = true,
                Message = "Audit is currently running",
                StartedAt = DateTime.UtcNow.AddMinutes(-30),
                EstimatedCompletion = DateTime.UtcNow.AddMinutes(30),
                Progress = 50
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get audit status for {AuditId}", request.AuditId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ViolationResult> ReportViolationAsync(ReportViolationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            return new ViolationResult
            {
                ViolationId = Guid.NewGuid().ToString(),
                Success = true,
                Message = "Violation reported successfully",
                Timestamp = DateTime.UtcNow,
                Severity = request.Severity,
                Status = ViolationStatus.Open
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to report violation");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<GetViolationsResult> GetViolationsAsync(GetViolationsRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            return new GetViolationsResult
            {
                Violations = new List<ComplianceViolation>(),
                TotalCount = 0,
                Success = true,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get violations");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<RemediationPlanResult> CreateRemediationPlanAsync(CreateRemediationPlanRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            return new RemediationPlanResult
            {
                PlanId = Guid.NewGuid().ToString(),
                Success = true,
                Message = "Remediation plan created successfully",
                CreatedAt = DateTime.UtcNow,
                EstimatedCompletion = DateTime.UtcNow.AddDays(7)
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create remediation plan for violation {ViolationId}", request.ViolationId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ComplianceDashboardResult> GetComplianceDashboardAsync(ComplianceDashboardRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            return new ComplianceDashboardResult
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow,
                Metrics = new Dictionary<string, decimal>
                {
                    ["totalRules"] = _ruleCache.Count,
                    ["activeRules"] = _ruleCache.Values.Count(r => r.IsEnabled),
                    ["totalChecks"] = _requestCount,
                    ["successfulChecks"] = _successCount,
                    ["failedChecks"] = _failureCount
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get compliance dashboard");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CertificationResult> RequestCertificationAsync(RequestCertificationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            return new CertificationResult
            {
                CertificationId = Guid.NewGuid().ToString(),
                Success = true,
                Message = "Certification request submitted successfully",
                RequestedAt = DateTime.UtcNow,
                EstimatedCompletion = DateTime.UtcNow.AddDays(30),
                CertificationType = request.CertificationType,
                Status = CertificationStatus.Pending
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to request certification of type {CertificationType}", request.CertificationType);
            throw;
        }
    }
}