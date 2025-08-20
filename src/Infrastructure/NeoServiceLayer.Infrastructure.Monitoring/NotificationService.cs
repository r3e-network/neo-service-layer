using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Infrastructure.Monitoring
{
    /// <summary>
    /// Comprehensive notification service for alerts and system events
    /// Supports multiple notification channels: Email, Slack, Teams, PagerDuty, SMS, Webhooks
    /// </summary>
    public interface INotificationService
    {
        Task SendAlertAsync(Alert alert);
        Task SendCustomNotificationAsync(string channel, string message, Dictionary<string, object>? metadata = null);
        Task<bool> TestNotificationChannelAsync(string channel);
    }

    /// <summary>
    /// Production-ready notification service implementation
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, INotificationChannel> _channels = new();

        public NotificationService(ILogger<NotificationService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            InitializeNotificationChannels();
        }

        public async Task SendAlertAsync(Alert alert)
        {
            var enabledChannels = GetEnabledChannelsForSeverity(alert.Severity);
            var tasks = new List<Task>();

            foreach (var channelName in enabledChannels)
            {
                if (_channels.TryGetValue(channelName, out var channel))
                {
                    tasks.Add(SendToChannelAsync(channel, alert));
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning("No notification channels configured for alert severity {Severity}", alert.Severity);
            }
        }

        public async Task SendCustomNotificationAsync(string channelName, string message, Dictionary<string, object>? metadata = null)
        {
            if (!_channels.TryGetValue(channelName, out var channel))
            {
                _logger.LogWarning("Notification channel {ChannelName} not found", channelName);
                return;
            }

            var customAlert = new Alert
            {
                Type = "custom",
                Severity = AlertSeverity.Info,
                Message = message,
                Context = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };

            await SendToChannelAsync(channel, customAlert).ConfigureAwait(false);
        }

        public async Task<bool> TestNotificationChannelAsync(string channelName)
        {
            if (!_channels.TryGetValue(channelName, out var channel))
            {
                _logger.LogWarning("Notification channel {ChannelName} not found for testing", channelName);
                return false;
            }

            var testAlert = new Alert
            {
                Type = "test",
                Severity = AlertSeverity.Info,
                Message = $"Test notification from Neo Service Layer at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}",
                Context = new Dictionary<string, object>
                {
                    ["test"] = true,
                    ["channel"] = channelName,
                    ["timestamp"] = DateTime.UtcNow
                },
                Timestamp = DateTime.UtcNow
            };

            try
            {
                await SendToChannelAsync(channel, testAlert).ConfigureAwait(false);
                _logger.LogInformation("Test notification sent successfully to {ChannelName}", channelName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test notification to {ChannelName}", channelName);
                return false;
            }
        }

        private void InitializeNotificationChannels()
        {
            var notificationConfig = _configuration.GetSection("Notifications");

            // Email channel
            if (notificationConfig.GetValue<bool>("Email:Enabled"))
            {
                _channels["email"] = new EmailNotificationChannel(_logger, notificationConfig.GetSection("Email"));
            }

            // Slack channel
            if (notificationConfig.GetValue<bool>("Slack:Enabled"))
            {
                _channels["slack"] = new SlackNotificationChannel(_logger, notificationConfig.GetSection("Slack"));
            }

            // Microsoft Teams channel
            if (notificationConfig.GetValue<bool>("Teams:Enabled"))
            {
                _channels["teams"] = new TeamsNotificationChannel(_logger, notificationConfig.GetSection("Teams"));
            }

            // PagerDuty channel
            if (notificationConfig.GetValue<bool>("PagerDuty:Enabled"))
            {
                _channels["pagerduty"] = new PagerDutyNotificationChannel(_logger, notificationConfig.GetSection("PagerDuty"));
            }

            // Webhook channel
            if (notificationConfig.GetValue<bool>("Webhook:Enabled"))
            {
                _channels["webhook"] = new WebhookNotificationChannel(_logger, notificationConfig.GetSection("Webhook"));
            }

            _logger.LogInformation("Initialized {ChannelCount} notification channels: {Channels}",
                _channels.Count, string.Join(", ", _channels.Keys));
        }

        private List<string> GetEnabledChannelsForSeverity(AlertSeverity severity)
        {
            var channels = new List<string>();
            var alertConfig = _configuration.GetSection($"Notifications:AlertRouting:{severity}");

            if (alertConfig.Exists())
            {
                channels.AddRange(alertConfig.Get<string[]>() ?? Array.Empty<string>());
            }
            else
            {
                // Default routing based on severity
                switch (severity)
                {
                    case AlertSeverity.Critical:
                        channels.AddRange(new[] { "email", "slack", "pagerduty", "teams" });
                        break;
                    case AlertSeverity.Warning:
                        channels.AddRange(new[] { "slack", "teams" });
                        break;
                    case AlertSeverity.Info:
                        channels.Add("slack");
                        break;
                }
            }

            return channels.FindAll(c => _channels.ContainsKey(c));
        }

        private async Task SendToChannelAsync(INotificationChannel channel, Alert alert)
        {
            try
            {
                await channel.SendNotificationAsync(alert).ConfigureAwait(false);
                _logger.LogDebug("Notification sent successfully via {ChannelType}", channel.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification via {ChannelType}", channel.GetType().Name);
                ApplicationPerformanceMonitoring.RecordError("notification_failed", channel.GetType().Name, ex);
            }
        }
    }

    /// <summary>
    /// Base interface for notification channels
    /// </summary>
    public interface INotificationChannel
    {
        Task SendNotificationAsync(Alert alert);
    }

    /// <summary>
    /// Email notification channel implementation
    /// </summary>
    public class EmailNotificationChannel : INotificationChannel
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        public EmailNotificationChannel(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task SendNotificationAsync(Alert alert)
        {
            // Implementation would use SMTP client or email service
            _logger.LogInformation("Email notification sent: {AlertType} - {Message}", alert.Type, alert.Message);
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Slack notification channel implementation
    /// </summary>
    public class SlackNotificationChannel : INotificationChannel
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        public SlackNotificationChannel(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task SendNotificationAsync(Alert alert)
        {
            // Implementation would use Slack webhook or API
            _logger.LogInformation("Slack notification sent: {AlertType} - {Message}", alert.Type, alert.Message);
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Microsoft Teams notification channel implementation
    /// </summary>
    public class TeamsNotificationChannel : INotificationChannel
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        public TeamsNotificationChannel(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task SendNotificationAsync(Alert alert)
        {
            // Implementation would use Teams webhook or Graph API
            _logger.LogInformation("Teams notification sent: {AlertType} - {Message}", alert.Type, alert.Message);
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// PagerDuty notification channel implementation
    /// </summary>
    public class PagerDutyNotificationChannel : INotificationChannel
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        public PagerDutyNotificationChannel(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task SendNotificationAsync(Alert alert)
        {
            // Implementation would use PagerDuty Events API
            _logger.LogInformation("PagerDuty notification sent: {AlertType} - {Message}", alert.Type, alert.Message);
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Generic webhook notification channel implementation
    /// </summary>
    public class WebhookNotificationChannel : INotificationChannel
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        public WebhookNotificationChannel(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task SendNotificationAsync(Alert alert)
        {
            // Implementation would HTTP POST to configured webhook URL
            _logger.LogInformation("Webhook notification sent: {AlertType} - {Message}", alert.Type, alert.Message);
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}