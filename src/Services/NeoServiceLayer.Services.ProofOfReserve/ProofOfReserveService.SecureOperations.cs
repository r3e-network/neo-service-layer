using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Secure operations for the Proof of Reserve Service with comprehensive security validation.
/// </summary>
public partial class ProofOfReserveService
{
    /// <summary>
    /// Registers an asset with comprehensive security validation.
    /// </summary>
    /// <param name="request">The asset registration request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="securityContext">The security context.</param>
    /// <returns>The asset ID.</returns>
    public async Task<SecureOperationResult<string>> RegisterAssetSecureAsync(
        AssetRegistrationRequest request, 
        BlockchainType blockchainType, 
        SecurityContext securityContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(securityContext);

        try
        {
            // Validate security context
            var securityValidation = await ValidateSecurityContextAsync(securityContext, "RegisterAsset");
            if (!securityValidation.IsValid)
            {
                return SecureOperationResult<string>.CreateFailure(
                    securityValidation.ErrorMessage ?? "Security validation failed",
                    securityValidation.ErrorCode ?? "SECURITY_ERROR");
            }

            // Validate input safety
            if (!ValidateInputSafety(request.AssetSymbol, nameof(request.AssetSymbol)) ||
                !ValidateInputSafety(request.AssetName, nameof(request.AssetName)) ||
                !ValidateInputSafety(request.Owner, nameof(request.Owner)))
            {
                return SecureOperationResult<string>.CreateFailure(
                    "Invalid input parameters detected",
                    "INVALID_INPUT");
            }

            // Sanitize inputs
            request.AssetSymbol = SanitizeInput(request.AssetSymbol);
            request.AssetName = SanitizeInput(request.AssetName);
            request.Owner = SanitizeInput(request.Owner);

            // Check blockchain support
            if (!SupportsBlockchain(blockchainType))
            {
                return SecureOperationResult<string>.CreateFailure(
                    $"Blockchain {blockchainType} is not supported",
                    "UNSUPPORTED_BLOCKCHAIN");
            }

            // Execute the secure operation
            var assetId = await RegisterAssetWithResilienceAsync(request, blockchainType);

            Logger.LogInformation("Asset {AssetId} registered securely by client {ClientId}", 
                assetId, securityContext.ClientId);

            return SecureOperationResult<string>.CreateSuccess(assetId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in secure asset registration for client {ClientId}", securityContext.ClientId);
            return SecureOperationResult<string>.CreateFailure(
                "Internal error during asset registration",
                "INTERNAL_ERROR");
        }
    }

    /// <summary>
    /// Generates a proof with comprehensive security validation.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="securityContext">The security context.</param>
    /// <returns>The proof of reserve.</returns>
    public async Task<SecureOperationResult<Core.ProofOfReserve>> GenerateProofSecureAsync(
        string assetId, 
        BlockchainType blockchainType, 
        SecurityContext securityContext)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);
        ArgumentNullException.ThrowIfNull(securityContext);

        try
        {
            // Validate security context
            var securityValidation = await ValidateSecurityContextAsync(securityContext, "GenerateProof");
            if (!securityValidation.IsValid)
            {
                return SecureOperationResult<Core.ProofOfReserve>.CreateFailure(
                    securityValidation.ErrorMessage ?? "Security validation failed",
                    securityValidation.ErrorCode ?? "SECURITY_ERROR");
            }

            // Validate input safety
            if (!ValidateInputSafety(assetId, nameof(assetId)))
            {
                return SecureOperationResult<Core.ProofOfReserve>.CreateFailure(
                    "Invalid asset ID parameter",
                    "INVALID_INPUT");
            }

            // Sanitize input
            assetId = SanitizeInput(assetId);

            // Check blockchain support
            if (!SupportsBlockchain(blockchainType))
            {
                return SecureOperationResult<Core.ProofOfReserve>.CreateFailure(
                    $"Blockchain {blockchainType} is not supported",
                    "UNSUPPORTED_BLOCKCHAIN");
            }

            // Execute the secure operation
            var proof = await GenerateProofWithResilienceAsync(assetId, blockchainType);

            Logger.LogInformation("Proof {ProofId} generated securely for asset {AssetId} by client {ClientId}", 
                proof.ProofId, assetId, securityContext.ClientId);

            return SecureOperationResult<Core.ProofOfReserve>.CreateSuccess(proof);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in secure proof generation for asset {AssetId} by client {ClientId}", 
                assetId, securityContext.ClientId);
            return SecureOperationResult<Core.ProofOfReserve>.CreateFailure(
                "Internal error during proof generation",
                "INTERNAL_ERROR");
        }
    }

    /// <summary>
    /// Updates reserve data with comprehensive security validation.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="reserveData">The reserve update request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="securityContext">The security context.</param>
    /// <returns>True if update was successful.</returns>
    public async Task<SecureOperationResult<bool>> UpdateReserveDataSecureAsync(
        string assetId, 
        ReserveUpdateRequest reserveData, 
        BlockchainType blockchainType, 
        SecurityContext securityContext)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);
        ArgumentNullException.ThrowIfNull(reserveData);
        ArgumentNullException.ThrowIfNull(securityContext);

        try
        {
            // Validate security context
            var securityValidation = await ValidateSecurityContextAsync(securityContext, "UpdateReserveData");
            if (!securityValidation.IsValid)
            {
                return SecureOperationResult<bool>.CreateFailure(
                    securityValidation.ErrorMessage ?? "Security validation failed",
                    securityValidation.ErrorCode ?? "SECURITY_ERROR");
            }

            // Validate input safety
            if (!ValidateInputSafety(assetId, nameof(assetId)) ||
                !ValidateInputSafety(reserveData.AuditSource, nameof(reserveData.AuditSource)))
            {
                return SecureOperationResult<bool>.CreateFailure(
                    "Invalid input parameters detected",
                    "INVALID_INPUT");
            }

            // Validate reserve addresses
            foreach (var address in reserveData.ReserveAddresses)
            {
                if (!ValidateInputSafety(address, "ReserveAddress"))
                {
                    return SecureOperationResult<bool>.CreateFailure(
                        "Invalid reserve address detected",
                        "INVALID_ADDRESS");
                }
            }

            // Sanitize inputs
            assetId = SanitizeInput(assetId);
            reserveData.AuditSource = SanitizeInput(reserveData.AuditSource);

            // Check blockchain support
            if (!SupportsBlockchain(blockchainType))
            {
                return SecureOperationResult<bool>.CreateFailure(
                    $"Blockchain {blockchainType} is not supported",
                    "UNSUPPORTED_BLOCKCHAIN");
            }

            // Execute the secure operation
            var success = await UpdateReserveDataWithResilienceAsync(assetId, reserveData, blockchainType);

            Logger.LogInformation("Reserve data updated securely for asset {AssetId} by client {ClientId}: {Success}", 
                assetId, securityContext.ClientId, success);

            return SecureOperationResult<bool>.CreateSuccess(success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in secure reserve data update for asset {AssetId} by client {ClientId}", 
                assetId, securityContext.ClientId);
            return SecureOperationResult<bool>.CreateFailure(
                "Internal error during reserve data update",
                "INTERNAL_ERROR");
        }
    }

    /// <summary>
    /// Gets reserve status with security validation.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="securityContext">The security context.</param>
    /// <returns>The reserve status info.</returns>
    public async Task<SecureOperationResult<ReserveStatusInfo>> GetReserveStatusSecureAsync(
        string assetId, 
        BlockchainType blockchainType, 
        SecurityContext securityContext)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);
        ArgumentNullException.ThrowIfNull(securityContext);

        try
        {
            // Validate security context
            var securityValidation = await ValidateSecurityContextAsync(securityContext, "GetReserveStatus");
            if (!securityValidation.IsValid)
            {
                return SecureOperationResult<ReserveStatusInfo>.CreateFailure(
                    securityValidation.ErrorMessage ?? "Security validation failed",
                    securityValidation.ErrorCode ?? "SECURITY_ERROR");
            }

            // Validate input safety
            if (!ValidateInputSafety(assetId, nameof(assetId)))
            {
                return SecureOperationResult<ReserveStatusInfo>.CreateFailure(
                    "Invalid asset ID parameter",
                    "INVALID_INPUT");
            }

            // Sanitize input
            assetId = SanitizeInput(assetId);

            // Check blockchain support
            if (!SupportsBlockchain(blockchainType))
            {
                return SecureOperationResult<ReserveStatusInfo>.CreateFailure(
                    $"Blockchain {blockchainType} is not supported",
                    "UNSUPPORTED_BLOCKCHAIN");
            }

            // Execute the secure operation
            var statusInfo = await GetReserveStatusWithCachingAsync(assetId, blockchainType);

            Logger.LogDebug("Reserve status retrieved securely for asset {AssetId} by client {ClientId}", 
                assetId, securityContext.ClientId);

            return SecureOperationResult<ReserveStatusInfo>.CreateSuccess(statusInfo);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Asset {AssetId} not found for client {ClientId}", assetId, securityContext.ClientId);
            return SecureOperationResult<ReserveStatusInfo>.CreateFailure(
                "Asset not found",
                "ASSET_NOT_FOUND");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting reserve status for asset {AssetId} by client {ClientId}", 
                assetId, securityContext.ClientId);
            return SecureOperationResult<ReserveStatusInfo>.CreateFailure(
                "Internal error retrieving reserve status",
                "INTERNAL_ERROR");
        }
    }

    /// <summary>
    /// Generates an audit report with security validation.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="from">The start date.</param>
    /// <param name="to">The end date.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="securityContext">The security context.</param>
    /// <returns>The audit report.</returns>
    public async Task<SecureOperationResult<AuditReport>> GenerateAuditReportSecureAsync(
        string assetId, 
        DateTime from, 
        DateTime to, 
        BlockchainType blockchainType, 
        SecurityContext securityContext)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);
        ArgumentNullException.ThrowIfNull(securityContext);

        try
        {
            // Validate security context
            var securityValidation = await ValidateSecurityContextAsync(securityContext, "GenerateAuditReport");
            if (!securityValidation.IsValid)
            {
                return SecureOperationResult<AuditReport>.CreateFailure(
                    securityValidation.ErrorMessage ?? "Security validation failed",
                    securityValidation.ErrorCode ?? "SECURITY_ERROR");
            }

            // Validate input safety
            if (!ValidateInputSafety(assetId, nameof(assetId)))
            {
                return SecureOperationResult<AuditReport>.CreateFailure(
                    "Invalid asset ID parameter",
                    "INVALID_INPUT");
            }

            // Validate date range
            if (from > to)
            {
                return SecureOperationResult<AuditReport>.CreateFailure(
                    "Invalid date range: start date must be before end date",
                    "INVALID_DATE_RANGE");
            }

            // Limit audit report time range (max 1 year)
            if ((to - from).TotalDays > 365)
            {
                return SecureOperationResult<AuditReport>.CreateFailure(
                    "Audit report time range cannot exceed 365 days",
                    "DATE_RANGE_TOO_LARGE");
            }

            // Sanitize input
            assetId = SanitizeInput(assetId);

            // Check blockchain support
            if (!SupportsBlockchain(blockchainType))
            {
                return SecureOperationResult<AuditReport>.CreateFailure(
                    $"Blockchain {blockchainType} is not supported",
                    "UNSUPPORTED_BLOCKCHAIN");
            }

            // Execute the secure operation
            var auditReport = await GenerateAuditReportWithCachingAsync(assetId, from, to, blockchainType);

            Logger.LogInformation("Audit report {ReportId} generated securely for asset {AssetId} by client {ClientId}", 
                auditReport.ReportId, assetId, securityContext.ClientId);

            return SecureOperationResult<AuditReport>.CreateSuccess(auditReport);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Asset {AssetId} not found for audit report by client {ClientId}", 
                assetId, securityContext.ClientId);
            return SecureOperationResult<AuditReport>.CreateFailure(
                "Asset not found",
                "ASSET_NOT_FOUND");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating audit report for asset {AssetId} by client {ClientId}", 
                assetId, securityContext.ClientId);
            return SecureOperationResult<AuditReport>.CreateFailure(
                "Internal error generating audit report",
                "INTERNAL_ERROR");
        }
    }

    /// <summary>
    /// Sets an alert threshold with security validation.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="threshold">The alert threshold.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="securityContext">The security context.</param>
    /// <returns>True if the threshold was set successfully.</returns>
    public async Task<SecureOperationResult<bool>> SetAlertThresholdSecureAsync(
        string assetId, 
        decimal threshold, 
        BlockchainType blockchainType, 
        SecurityContext securityContext)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);
        ArgumentNullException.ThrowIfNull(securityContext);

        try
        {
            // Validate security context
            var securityValidation = await ValidateSecurityContextAsync(securityContext, "SetAlertThreshold");
            if (!securityValidation.IsValid)
            {
                return SecureOperationResult<bool>.CreateFailure(
                    securityValidation.ErrorMessage ?? "Security validation failed",
                    securityValidation.ErrorCode ?? "SECURITY_ERROR");
            }

            // Validate input safety
            if (!ValidateInputSafety(assetId, nameof(assetId)))
            {
                return SecureOperationResult<bool>.CreateFailure(
                    "Invalid asset ID parameter",
                    "INVALID_INPUT");
            }

            // Validate threshold range
            if (threshold < 0 || threshold > 10)
            {
                return SecureOperationResult<bool>.CreateFailure(
                    "Alert threshold must be between 0 and 10",
                    "INVALID_THRESHOLD");
            }

            // Sanitize input
            assetId = SanitizeInput(assetId);

            // Check blockchain support
            if (!SupportsBlockchain(blockchainType))
            {
                return SecureOperationResult<bool>.CreateFailure(
                    $"Blockchain {blockchainType} is not supported",
                    "UNSUPPORTED_BLOCKCHAIN");
            }

            // Execute the secure operation
            var success = await SetAlertThresholdAsync(assetId, threshold, blockchainType);

            Logger.LogInformation("Alert threshold {Threshold} set securely for asset {AssetId} by client {ClientId}", 
                threshold, assetId, securityContext.ClientId);

            return SecureOperationResult<bool>.CreateSuccess(success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error setting alert threshold for asset {AssetId} by client {ClientId}", 
                assetId, securityContext.ClientId);
            return SecureOperationResult<bool>.CreateFailure(
                "Internal error setting alert threshold",
                "INTERNAL_ERROR");
        }
    }

    /// <summary>
    /// Gets active alerts with security validation.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="securityContext">The security context.</param>
    /// <returns>The active alerts.</returns>
    public async Task<SecureOperationResult<IEnumerable<ReserveAlert>>> GetActiveAlertsSecureAsync(
        BlockchainType blockchainType, 
        SecurityContext securityContext)
    {
        ArgumentNullException.ThrowIfNull(securityContext);

        try
        {
            // Validate security context
            var securityValidation = await ValidateSecurityContextAsync(securityContext, "GetActiveAlerts");
            if (!securityValidation.IsValid)
            {
                return SecureOperationResult<IEnumerable<ReserveAlert>>.CreateFailure(
                    securityValidation.ErrorMessage ?? "Security validation failed",
                    securityValidation.ErrorCode ?? "SECURITY_ERROR");
            }

            // Check blockchain support
            if (!SupportsBlockchain(blockchainType))
            {
                return SecureOperationResult<IEnumerable<ReserveAlert>>.CreateFailure(
                    $"Blockchain {blockchainType} is not supported",
                    "UNSUPPORTED_BLOCKCHAIN");
            }

            // Execute the secure operation
            var alerts = await GetActiveAlertsWithCachingAsync(blockchainType);

            Logger.LogDebug("Active alerts retrieved securely for {Blockchain} by client {ClientId}", 
                blockchainType, securityContext.ClientId);

            return SecureOperationResult<IEnumerable<ReserveAlert>>.CreateSuccess(alerts);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting active alerts for {Blockchain} by client {ClientId}", 
                blockchainType, securityContext.ClientId);
            return SecureOperationResult<IEnumerable<ReserveAlert>>.CreateFailure(
                "Internal error retrieving active alerts",
                "INTERNAL_ERROR");
        }
    }
}

/// <summary>
/// Result wrapper for secure operations.
/// </summary>
/// <typeparam name="T">The result type.</typeparam>
public class SecureOperationResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static SecureOperationResult<T> CreateSuccess(T data)
    {
        return new SecureOperationResult<T>
        {
            Success = true,
            Data = data
        };
    }

    public static SecureOperationResult<T> CreateFailure(string errorMessage, string? errorCode = null)
    {
        return new SecureOperationResult<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}