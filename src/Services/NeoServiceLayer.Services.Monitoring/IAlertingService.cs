using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Services.Monitoring.Models;

namespace NeoServiceLayer.Services.Monitoring
{
    /// <summary>
    /// Interface for alerting and notification service.
    /// </summary>
    public interface IAlertingService
    {
        /// <summary>
        /// Sends an alert.
        /// </summary>
        Task SendAlertAsync(Alert alert);

        /// <summary>
        /// Sends a critical alert.
        /// </summary>
        Task SendCriticalAlertAsync(string title, string message, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Sends a warning alert.
        /// </summary>
        Task SendWarningAlertAsync(string title, string message, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Sends an info alert.
        /// </summary>
        Task SendInfoAlertAsync(string title, string message, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Gets active alerts.
        /// </summary>
        Task<List<Alert>> GetActiveAlertsAsync();

        /// <summary>
        /// Acknowledges an alert.
        /// </summary>
        Task AcknowledgeAlertAsync(string alertId);

        /// <summary>
        /// Resolves an alert.
        /// </summary>
        Task ResolveAlertAsync(string alertId, string resolution);

        /// <summary>
        /// Clears all alerts.
        /// </summary>
        Task ClearAlertsAsync();
    }

    /// <summary>
    /// Represents an alert.
    /// </summary>
    public class Alert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public AlertStatus Status { get; set; } = AlertStatus.Active;
        public bool IsActive => Status == AlertStatus.Active;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string? AcknowledgedBy { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? ResolvedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? Resolution { get; set; }
    }

    /// <summary>
    /// Alert severity levels.
    /// </summary>
    public enum AlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Alert status.
    /// </summary>
    public enum AlertStatus
    {
        Active,
        Acknowledged,
        Resolved,
        Expired
    }
}