using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Helper methods for the Proof of Reserve Service.
/// </summary>
public partial class ProofOfReserveService
{
    /// <summary>
    /// Monitors reserves for all registered assets.
    /// </summary>
    /// <param name="state">Timer state.</param>
    private async void MonitorReserves(object? state)
    {
        try
        {
            var assets = GetActiveAssets();

            foreach (var asset in assets)
            {
                await UpdateReserveStatusAsync(asset.AssetId, Array.Empty<string>(), BlockchainType.NeoN3);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during reserve monitoring");
        }
    }

    /// <summary>
    /// Updates the reserve status for an asset.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="reserveAddresses">The reserve addresses.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    private async Task UpdateReserveStatusAsync(string assetId, string[] reserveAddresses, BlockchainType blockchainType)
    {
        await UpdateReserveStatusWithResilienceAsync(assetId, reserveAddresses, blockchainType);
    }

    /// <summary>
    /// Gets a monitored asset by ID.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <returns>The monitored asset.</returns>
    private MonitoredAsset GetMonitoredAsset(string assetId)
    {
        lock (_assetsLock)
        {
            if (_monitoredAssets.TryGetValue(assetId, out var asset))
            {
                return asset;
            }
        }

        throw new ArgumentException($"Asset {assetId} not found", nameof(assetId));
    }

    /// <summary>
    /// Gets the latest reserve snapshot for an asset.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <returns>The latest snapshot.</returns>
    private ReserveSnapshot? GetLatestSnapshot(string assetId)
    {
        lock (_assetsLock)
        {
            if (_reserveHistory.TryGetValue(assetId, out var history))
            {
                return history.LastOrDefault();
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all active monitored assets.
    /// </summary>
    /// <returns>The active assets.</returns>
    private List<MonitoredAsset> GetActiveAssets()
    {
        lock (_assetsLock)
        {
            return _monitoredAssets.Values.Where(a => a.IsActive).ToList();
        }
    }

    /// <summary>
    /// Calculates the health status based on reserve ratio.
    /// </summary>
    /// <param name="reserveRatio">The reserve ratio.</param>
    /// <param name="minReserveRatio">The minimum reserve ratio.</param>
    /// <returns>The health status.</returns>
    private ReserveHealthStatus CalculateHealthStatus(decimal reserveRatio, decimal minReserveRatio)
    {
        if (reserveRatio >= minReserveRatio * 1.2m) return ReserveHealthStatus.Healthy;
        if (reserveRatio >= minReserveRatio) return ReserveHealthStatus.Warning;
        if (reserveRatio >= minReserveRatio * 0.8m) return ReserveHealthStatus.Critical;
        return ReserveHealthStatus.Undercollateralized;
    }

    /// <summary>
    /// Checks for alert conditions.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="snapshot">The reserve snapshot.</param>
    private async Task CheckAlertsAsync(string assetId, ReserveSnapshot snapshot)
    {
        var relevantAlerts = _alertConfigs.Values.Where(a => a.IsEnabled);

        foreach (var alert in relevantAlerts)
        {
            var shouldAlert = alert.Type switch
            {
                ReserveAlertType.LowReserveRatio => snapshot.ReserveRatio < alert.Threshold,
                ReserveAlertType.HighVolatility => false, // Would need historical data
                ReserveAlertType.AuditOverdue => false, // Would need audit schedule
                ReserveAlertType.ComplianceViolation => snapshot.Health == ReserveHealthStatus.Undercollateralized,
                _ => false
            };

            if (shouldAlert)
            {
                Logger.LogWarning("Alert triggered for asset {AssetId}: {AlertType} - {AlertName}",
                    assetId, alert.Type, alert.AlertName);

                // Create notification
                var notification = new ProofOfReserveNotification
                {
                    AssetId = assetId,
                    AlertType = alert.Type.ToString(),
                    Message = $"Alert: {alert.AlertName} - {alert.Type}",
                    Severity = AlertSeverity.Warning,
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["AlertId"] = alert.AlertId,
                        ["AssetId"] = assetId,
                        ["AlertType"] = alert.Type.ToString(),
                        ["Threshold"] = alert.Threshold,
                        ["CurrentValue"] = snapshot.ReserveRatio
                    }
                };

                // Send notifications to recipients (placeholder - would need recipient list)
                var recipients = new[] { "admin@example.com" }; // Placeholder
                foreach (var recipient in recipients)
                {
                    await SendNotificationAsync(recipient, notification);
                }
            }
        }
    }

    /// <summary>
    /// Sends a notification to a recipient.
    /// </summary>
    /// <param name="recipient">The notification recipient.</param>
    /// <param name="notification">The notification to send.</param>
    private async Task SendNotificationAsync(string recipient, ProofOfReserveNotification notification)
    {
        try
        {
            // Determine notification method based on recipient format
            if (IsEmailAddress(recipient))
            {
                await SendEmailNotificationAsync(recipient, notification);
            }
            else if (IsPhoneNumber(recipient))
            {
                await SendSmsNotificationAsync(recipient, notification);
            }
            else if (IsWebhookUrl(recipient))
            {
                await SendWebhookNotificationAsync(recipient, notification);
            }
            else
            {
                Logger.LogWarning("Unknown recipient format: {Recipient}", recipient);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send notification to {Recipient}", recipient);
        }
    }

    /// <summary>
    /// Sends an email notification.
    /// </summary>
    /// <param name="emailAddress">The email address.</param>
    /// <param name="notification">The notification to send.</param>
    private async Task SendEmailNotificationAsync(string emailAddress, ProofOfReserveNotification notification)
    {
        try
        {
            // In a production environment, this would integrate with an email service
            // such as SendGrid, AWS SES, or Azure Communication Services

            var emailContent = FormatEmailContent(notification);

            // Send email using email service provider
            await SendEmailAsync(emailAddress, emailContent);

            Logger.LogInformation("Email notification sent to {EmailAddress} for asset {AssetId}",
                emailAddress, notification.AssetId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send email notification to {EmailAddress}", emailAddress);
            throw;
        }
    }

    /// <summary>
    /// Sends an SMS notification.
    /// </summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <param name="notification">The notification to send.</param>
    private async Task SendSmsNotificationAsync(string phoneNumber, ProofOfReserveNotification notification)
    {
        try
        {
            // In a production environment, this would integrate with an SMS service
            // such as Twilio, AWS SNS, or Azure Communication Services

            var smsContent = FormatSmsContent(notification);

            // Send SMS using SMS service provider
            await SendSmsAsync(phoneNumber, smsContent);

            Logger.LogInformation("SMS notification sent to {PhoneNumber} for asset {AssetId}",
                phoneNumber, notification.AssetId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send SMS notification to {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    /// <summary>
    /// Sends a webhook notification.
    /// </summary>
    /// <param name="webhookUrl">The webhook URL.</param>
    /// <param name="notification">The notification to send.</param>
    private async Task SendWebhookNotificationAsync(string webhookUrl, ProofOfReserveNotification notification)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var payload = new
            {
                timestamp = notification.Timestamp,
                assetId = notification.AssetId,
                alertType = notification.AlertType,
                message = notification.Message,
                severity = notification.Severity,
                metadata = notification.Metadata
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(webhookUrl, content);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Webhook notification sent to {WebhookUrl} for asset {AssetId}",
                    webhookUrl, notification.AssetId);
            }
            else
            {
                Logger.LogWarning("Webhook notification failed with status {StatusCode} for {WebhookUrl}",
                    response.StatusCode, webhookUrl);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send webhook notification to {WebhookUrl}", webhookUrl);
            throw;
        }
    }

    /// <summary>
    /// Formats notification content for email.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <returns>The formatted email content.</returns>
    private string FormatEmailContent(ProofOfReserveNotification notification)
    {
        return $@"
Subject: Proof of Reserve Alert - {notification.AlertType}

Dear User,

This is an automated notification from the Neo Service Layer Proof of Reserve system.

Asset ID: {notification.AssetId}
Alert Type: {notification.AlertType}
Severity: {notification.Severity}
Timestamp: {notification.Timestamp:yyyy-MM-dd HH:mm:ss UTC}

Message: {notification.Message}

Please review your asset reserves and take appropriate action if necessary.

Best regards,
Neo Service Layer Team
";
    }

    /// <summary>
    /// Formats notification content for SMS.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <returns>The formatted SMS content.</returns>
    private string FormatSmsContent(ProofOfReserveNotification notification)
    {
        return $"Neo PoR Alert: {notification.AlertType} for asset {notification.AssetId}. " +
               $"Severity: {notification.Severity}. Check your dashboard for details.";
    }

    /// <summary>
    /// Checks if a string is a valid email address.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>True if it's an email address.</returns>
    private bool IsEmailAddress(string input)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(input);
            return addr.Address == input;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a string is a valid phone number.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>True if it's a phone number.</returns>
    private bool IsPhoneNumber(string input)
    {
        // Simple phone number validation (starts with + and contains only digits)
        return input.StartsWith("+") && input.Length > 5 &&
               input.Skip(1).All(char.IsDigit);
    }

    /// <summary>
    /// Checks if a string is a valid webhook URL.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>True if it's a webhook URL.</returns>
    private bool IsWebhookUrl(string input)
    {
        return Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Generates audit recommendations based on asset performance.
    /// </summary>
    /// <param name="asset">The monitored asset.</param>
    /// <param name="snapshots">The reserve snapshots.</param>
    /// <returns>Array of recommendations.</returns>
    private string[] GenerateAuditRecommendations(MonitoredAsset asset, ReserveSnapshot[] snapshots)
    {
        var recommendations = new List<string>();

        if (snapshots.Length == 0)
        {
            recommendations.Add("No reserve data available for analysis. Ensure regular reserve updates.");
            return recommendations.ToArray();
        }

        var averageRatio = snapshots.Average(s => s.ReserveRatio);
        var minRatio = snapshots.Min(s => s.ReserveRatio);
        var complianceRate = (decimal)snapshots.Count(s => s.ReserveRatio >= asset.MinReserveRatio) / snapshots.Length;

        // Reserve ratio recommendations
        if (averageRatio < asset.MinReserveRatio)
        {
            recommendations.Add($"Average reserve ratio ({averageRatio:P2}) is below minimum threshold ({asset.MinReserveRatio:P2}). Consider increasing reserves.");
        }

        if (minRatio < asset.MinReserveRatio * 0.8m)
        {
            recommendations.Add($"Minimum reserve ratio ({minRatio:P2}) indicates critical undercollateralization. Implement emergency reserve protocols.");
        }

        // Compliance recommendations
        if (complianceRate < 0.95m)
        {
            recommendations.Add($"Compliance rate ({complianceRate:P1}) is below 95%. Improve reserve management processes.");
        }

        // Volatility recommendations
        var ratioVariance = snapshots.Select(s => s.ReserveRatio).ToArray();
        if (ratioVariance.Length > 1)
        {
            var variance = ratioVariance.Select(r => Math.Pow((double)(r - averageRatio), 2)).Average();
            var standardDeviation = (decimal)Math.Sqrt(variance);

            if (standardDeviation > 0.1m)
            {
                recommendations.Add("High reserve ratio volatility detected. Consider implementing buffer reserves to reduce fluctuations.");
            }
        }

        // Trend recommendations
        if (snapshots.Length >= 5)
        {
            var recentSnapshots = snapshots.TakeLast(5).ToArray();
            var recentAverage = recentSnapshots.Average(s => s.ReserveRatio);
            var olderSnapshots = snapshots.Take(snapshots.Length - 5).ToArray();

            if (olderSnapshots.Length > 0)
            {
                var olderAverage = olderSnapshots.Average(s => s.ReserveRatio);

                if (recentAverage < olderAverage * 0.95m)
                {
                    recommendations.Add("Declining reserve ratio trend detected. Monitor closely and consider proactive reserve increases.");
                }
            }
        }

        // General recommendations
        if (recommendations.Count == 0)
        {
            recommendations.Add("Reserve management appears healthy. Continue current practices and maintain regular monitoring.");
        }

        return recommendations.ToArray();
    }

    /// <summary>
    /// Fetches reserve balances from the blockchain.
    /// </summary>
    /// <param name="reserveAddresses">The reserve addresses.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The reserve balances.</returns>
    private async Task<decimal[]> FetchReserveBalancesAsync(string[] reserveAddresses, BlockchainType blockchainType)
    {
        var balances = new decimal[reserveAddresses.Length];
        var (cachingEnabled, _, _, _) = GetPerformanceSettings();

        for (int i = 0; i < reserveAddresses.Length; i++)
        {
            if (cachingEnabled > 0 && _cacheHelper != null)
            {
                balances[i] = await GetBlockchainBalanceWithCachingAsync(reserveAddresses[i], blockchainType);
            }
            else
            {
                balances[i] = await QueryAddressBalanceAsync(reserveAddresses[i], blockchainType);
            }
        }

        return balances;
    }

    /// <summary>
    /// Queries the balance of a specific address on the blockchain.
    /// </summary>
    /// <param name="address">The address to query.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The address balance.</returns>
    private async Task<decimal> QueryAddressBalanceAsync(string address, BlockchainType blockchainType)
    {
        // Implementation would query the actual blockchain
        // For now, return a placeholder value
        await Task.CompletedTask;
        return 1000000m; // Placeholder balance
    }

    /// <summary>
    /// Sends an email using the configured email service.
    /// </summary>
    /// <param name="emailAddress">The email address.</param>
    /// <param name="content">The email content.</param>
    private async Task SendEmailAsync(string emailAddress, string content)
    {
        // Implementation would use actual email service
        await Task.CompletedTask;
    }

    /// <summary>
    /// Sends an SMS using the configured SMS service.
    /// </summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <param name="content">The SMS content.</param>
    private async Task SendSmsAsync(string phoneNumber, string content)
    {
        // Implementation would use actual SMS service
        await Task.CompletedTask;
    }
}
