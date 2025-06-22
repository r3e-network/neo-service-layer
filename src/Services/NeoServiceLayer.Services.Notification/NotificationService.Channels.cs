using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Notification.Models;

namespace NeoServiceLayer.Services.Notification;

/// <summary>
/// Channel management operations for the Notification Service.
/// </summary>
public partial class NotificationService
{
    /// <inheritdoc/>
    public async Task<ChannelRegistrationResult> RegisterChannelAsync(RegisterChannelRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            await Task.Delay(1); // Simulate async channel registration
            var channelId = Guid.NewGuid().ToString();

            var channelInfo = new ChannelInfo
            {
                ChannelId = channelId,
                ChannelType = (Models.NotificationChannel)request.ChannelType,
                ChannelName = request.ChannelName,
                IsEnabled = request.IsEnabled,
                Description = request.Description,
                Configuration = new Dictionary<string, object>(request.Configuration),
                SupportedFeatures = GetSupportedFeatures(request.ChannelType),
                Metadata = new Dictionary<string, object>(request.Metadata)
                {
                    ["registered_at"] = DateTime.UtcNow
                }
            };

            lock (_cacheLock)
            {
                _registeredChannels[channelId] = channelInfo;
            }

            Logger.LogInformation("Registered channel {ChannelName} ({ChannelType}) with ID {ChannelId}",
                request.ChannelName, request.ChannelType, channelId);

            return new ChannelRegistrationResult
            {
                ChannelId = channelId,
                Success = true,
                RegisteredAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["channel_name"] = request.ChannelName,
                    ["channel_type"] = request.ChannelType.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to register channel {ChannelName}", request.ChannelName);

            return new ChannelRegistrationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                RegisteredAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Validates the recipient format for the specified channel.
    /// </summary>
    /// <param name="channel">The notification channel.</param>
    /// <param name="recipient">The recipient address.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private static bool ValidateRecipient(NotificationChannel channel, string recipient)
    {
        return channel switch
        {
            NotificationChannel.Email => IsValidEmail(recipient),
            NotificationChannel.SMS => IsValidPhoneNumber(recipient),
            NotificationChannel.Webhook => IsValidUrl(recipient),
            NotificationChannel.Slack => !string.IsNullOrWhiteSpace(recipient),
            NotificationChannel.Discord => !string.IsNullOrWhiteSpace(recipient),
            NotificationChannel.Telegram => !string.IsNullOrWhiteSpace(recipient),
            NotificationChannel.Push => !string.IsNullOrWhiteSpace(recipient),
            NotificationChannel.InApp => !string.IsNullOrWhiteSpace(recipient),
            _ => false
        };
    }

    /// <summary>
    /// Validates email format.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>True if valid email format.</returns>
    private static bool IsValidEmail(string email)
    {
        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        return emailRegex.IsMatch(email);
    }

    /// <summary>
    /// Validates phone number format.
    /// </summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <returns>True if valid phone number format.</returns>
    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        var phoneRegex = new Regex(@"^\+?[1-9]\d{1,14}$");
        return phoneRegex.IsMatch(phoneNumber.Replace(" ", "").Replace("-", ""));
    }

    /// <summary>
    /// Validates URL format.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <returns>True if valid URL format.</returns>
    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Gets supported features for a channel type.
    /// </summary>
    /// <param name="channelType">The channel type.</param>
    /// <returns>Array of supported features.</returns>
    private static string[] GetSupportedFeatures(NotificationChannel channelType)
    {
        return channelType switch
        {
            NotificationChannel.Email => new[] { "attachments", "html", "templates", "scheduling" },
            NotificationChannel.SMS => new[] { "templates", "scheduling" },
            NotificationChannel.Push => new[] { "rich_content", "actions", "scheduling" },
            NotificationChannel.Webhook => new[] { "json_payload", "retries", "authentication" },
            NotificationChannel.Slack => new[] { "rich_content", "mentions", "channels" },
            NotificationChannel.Discord => new[] { "embeds", "mentions", "channels" },
            NotificationChannel.Telegram => new[] { "markdown", "inline_keyboards", "channels" },
            NotificationChannel.InApp => new[] { "real_time", "persistence", "actions" },
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Initializes default notification channels.
    /// </summary>
    private void InitializeDefaultChannels()
    {
        var defaultChannels = new[]
        {
            (NotificationChannel.Email, "Default Email", "Default email notification channel"),
            (NotificationChannel.SMS, "Default SMS", "Default SMS notification channel"),
            (NotificationChannel.Push, "Default Push", "Default push notification channel"),
            (NotificationChannel.Webhook, "Default Webhook", "Default webhook notification channel"),
            (NotificationChannel.Slack, "Default Slack", "Default Slack notification channel"),
            (NotificationChannel.Discord, "Default Discord", "Default Discord notification channel"),
            (NotificationChannel.Telegram, "Default Telegram", "Default Telegram notification channel"),
            (NotificationChannel.InApp, "Default In-App", "Default in-app notification channel")
        };

        foreach (var (channelType, name, description) in defaultChannels)
        {
            var channelId = $"default_{channelType.ToString().ToLowerInvariant()}";
            var channelInfo = new ChannelInfo
            {
                ChannelId = channelId,
                ChannelType = (Models.NotificationChannel)channelType,
                ChannelName = name,
                IsEnabled = true,
                Description = description,
                SupportedFeatures = GetSupportedFeatures(channelType),
                Configuration = new Dictionary<string, object>
                {
                    ["is_default"] = true,
                    ["created_at"] = DateTime.UtcNow
                },
                Metadata = new Dictionary<string, object>
                {
                    ["default_channel"] = true
                }
            };

            _registeredChannels[channelId] = channelInfo;
        }

        Logger.LogInformation("Initialized {ChannelCount} default notification channels", defaultChannels.Length);
    }
}
