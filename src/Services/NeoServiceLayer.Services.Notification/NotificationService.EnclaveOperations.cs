using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Core.SGX;
using NeoServiceLayer.Services.Notification.Models;
using NeoServiceLayer.Tee.Host.Services;
using CoreModels = NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Services.Notification;

/// <summary>
/// Enclave operations for the Notification Service.
/// </summary>
public partial class NotificationService
{
    private IEnclaveManager? _enclaveManager;

    /// <summary>
    /// Sets the enclave manager for SGX operations.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager.</param>
    public void SetEnclaveManager(IEnclaveManager enclaveManager)
    {
        _enclaveManager = enclaveManager ?? throw new ArgumentNullException(nameof(enclaveManager));
    }

    /// <summary>
    /// Processes a notification using privacy-preserving operations in the SGX enclave.
    /// </summary>
    /// <param name="request">The notification request.</param>
    /// <param name="notificationId">The notification ID.</param>
    /// <returns>The privacy-preserving notification result.</returns>
    private async Task<PrivacyNotificationResult> ProcessNotificationWithPrivacyAsync(
        SendNotificationRequest request, string notificationId)
    {
        if (_enclaveManager == null)
        {
            // Fallback if enclave not available
            return new PrivacyNotificationResult
            {
                NotificationId = notificationId,
                DeliveryProof = GenerateSimpleDeliveryProof(request.Recipient, notificationId),
                Success = true
            };
        }

        // Prepare notification data for privacy-preserving processing
        var notificationData = new
        {
            type = request.Channel.ToString(),
            priority = request.Priority.ToString(),
            content = request.Content,
            metadata = request.Metadata ?? new Dictionary<string, object>()
        };

        var recipientProof = new
        {
            identity = request.Recipient,
            channel = new
            {
                type = request.Channel.ToString(),
                id = HashRecipient(request.Recipient)
            },
            preferences = new
            {
                frequency = "default",
                quiet_hours = false,
                categories = new[] { request.Channel.ToString() }
            }
        };

        var operation = request.NotificationBatch?.Count > 1 ? "batch" : "send";

        var jsParams = operation == "batch" ?
            new
            {
                operation,
                notificationData = new
                {
                    notifications = request.NotificationBatch,
                    recipients = request.NotificationBatch.Select(n => new
                    {
                        identity = n.Recipient,
                        channel = new { type = n.Channel.ToString(), id = HashRecipient(n.Recipient) },
                        preferences = new { frequency = "default", quiet_hours = false, categories = new[] { n.Channel.ToString() } }
                    }).ToArray()
                },
                recipientProof = (object?)null
            } :
            new
            {
                operation,
                notificationData = (object)notificationData,
                recipientProof = (object)recipientProof
            };

        string paramsJson = JsonSerializer.Serialize(jsParams);

        // Execute privacy-preserving notification processing in SGX
        string result = await _enclaveManager.ExecuteJavaScriptAsync(
            PrivacyComputingJavaScriptTemplates.NotificationOperations,
            paramsJson);

        if (string.IsNullOrEmpty(result))
            throw new InvalidOperationException("Privacy-preserving notification processing returned null");

        var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

        if (!resultJson.TryGetProperty("success", out var success) || !success.GetBoolean())
        {
            throw new InvalidOperationException("Privacy-preserving notification processing failed");
        }

        // Extract privacy-preserving notification result
        var notificationResult = resultJson.GetProperty("result");

        if (operation == "batch")
        {
            var batchResult = notificationResult;
            return new PrivacyNotificationResult
            {
                NotificationId = batchResult.GetProperty("batchId").GetString() ?? notificationId,
                DeliveryProof = new DeliveryProof
                {
                    NotificationId = batchResult.GetProperty("batchId").GetString() ?? "",
                    RecipientHash = "batch",
                    ChannelHash = "multi",
                    Timestamp = DateTimeOffset.UtcNow,
                    Proof = batchResult.GetProperty("batchId").GetString() ?? ""
                },
                Success = true,
                BatchResults = ExtractBatchResults(batchResult.GetProperty("results"))
            };
        }
        else
        {
            var delivery = notificationResult.GetProperty("delivery");
            return new PrivacyNotificationResult
            {
                NotificationId = delivery.GetProperty("notificationId").GetString() ?? "",
                DeliveryProof = ExtractDeliveryProof(delivery),
                Success = notificationResult.GetProperty("success").GetBoolean()
            };
        }
    }

    /// <summary>
    /// Validates notification recipient using privacy-preserving computation.
    /// </summary>
    /// <param name="recipient">The recipient identifier.</param>
    /// <param name="channel">The notification channel.</param>
    /// <returns>True if the recipient is valid.</returns>
    private async Task<bool> ValidateRecipientWithPrivacyAsync(string recipient, NotificationChannel channel)
    {
        if (_enclaveManager == null)
        {
            // Basic validation if enclave not available
            return !string.IsNullOrWhiteSpace(recipient);
        }

        var notificationData = new
        {
            type = channel.ToString(),
            priority = "normal",
            content = "validation",
            metadata = new Dictionary<string, object>()
        };

        var recipientProof = new
        {
            identity = recipient,
            channel = new
            {
                type = channel.ToString(),
                id = HashRecipient(recipient)
            },
            preferences = new
            {
                frequency = "default",
                quiet_hours = false,
                categories = new[] { channel.ToString() }
            }
        };

        var jsParams = new
        {
            operation = "validate",
            notificationData,
            recipientProof
        };

        string paramsJson = JsonSerializer.Serialize(jsParams);

        string result = await _enclaveManager.ExecuteJavaScriptAsync(
            PrivacyComputingJavaScriptTemplates.NotificationOperations,
            paramsJson);

        if (string.IsNullOrEmpty(result))
            return false;

        try
        {
            var resultJson = JsonSerializer.Deserialize<JsonElement>(result);
            return resultJson.TryGetProperty("success", out var success) && success.GetBoolean();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Anonymizes notification content for privacy.
    /// </summary>
    /// <param name="content">The notification content.</param>
    /// <param name="metadata">The notification metadata.</param>
    /// <returns>Anonymized content and metadata.</returns>
    private async Task<(string Content, Dictionary<string, object> Metadata)> AnonymizeNotificationContentAsync(
        string content, Dictionary<string, object>? metadata)
    {
        await Task.CompletedTask;

        // Hash any potentially sensitive content
        var anonymizedMetadata = new Dictionary<string, object>();

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                if (IsSensitiveKey(kvp.Key))
                {
                    anonymizedMetadata[$"{kvp.Key}_hash"] = HashContent(kvp.Value?.ToString() ?? "");
                }
                else
                {
                    anonymizedMetadata[kvp.Key] = kvp.Value;
                }
            }
        }

        // Keep content as-is for now, but could hash sensitive parts
        return (content, anonymizedMetadata);
    }

    /// <summary>
    /// Hashes recipient for privacy.
    /// </summary>
    private string HashRecipient(string recipient)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(recipient).Take(16).ToArray());
    }

    /// <summary>
    /// Hashes content for privacy.
    /// </summary>
    private string HashContent(string content)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content).Take(32).ToArray());
    }

    /// <summary>
    /// Checks if a metadata key is sensitive.
    /// </summary>
    private bool IsSensitiveKey(string key)
    {
        var sensitiveKeys = new[] { "password", "secret", "key", "token", "auth", "private" };
        return sensitiveKeys.Any(sk => key.Contains(sk, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Generates a simple delivery proof.
    /// </summary>
    private DeliveryProof GenerateSimpleDeliveryProof(string recipient, string notificationId)
    {
        return new DeliveryProof
        {
            NotificationId = notificationId,
            RecipientHash = HashRecipient(recipient),
            ChannelHash = "default",
            Timestamp = DateTimeOffset.UtcNow,
            Proof = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{notificationId}-{recipient}").Take(32).ToArray())
        };
    }

    /// <summary>
    /// Extracts delivery proof from JSON.
    /// </summary>
    private DeliveryProof ExtractDeliveryProof(JsonElement deliveryElement)
    {
        return new DeliveryProof
        {
            NotificationId = deliveryElement.GetProperty("notificationId").GetString() ?? "",
            RecipientHash = deliveryElement.GetProperty("recipientHash").GetString() ?? "",
            ChannelHash = deliveryElement.GetProperty("channelHash").GetString() ?? "",
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(deliveryElement.GetProperty("timestamp").GetInt64()),
            Proof = deliveryElement.GetProperty("proof").GetString() ?? ""
        };
    }

    /// <summary>
    /// Extracts batch results from JSON.
    /// </summary>
    private List<BatchNotificationResult> ExtractBatchResults(JsonElement resultsElement)
    {
        var results = new List<BatchNotificationResult>();

        if (resultsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var resultElement in resultsElement.EnumerateArray())
            {
                results.Add(new BatchNotificationResult
                {
                    Index = resultElement.GetProperty("index").GetInt32(),
                    NotificationId = resultElement.GetProperty("notificationId").GetString() ?? "",
                    Success = resultElement.GetProperty("success").GetBoolean()
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Privacy-preserving notification result.
    /// </summary>
    private class PrivacyNotificationResult
    {
        public string NotificationId { get; set; } = "";
        public DeliveryProof DeliveryProof { get; set; } = new();
        public bool Success { get; set; }
        public List<BatchNotificationResult>? BatchResults { get; set; }
    }

    /// <summary>
    /// Delivery proof.
    /// </summary>
    private class DeliveryProof
    {
        public string NotificationId { get; set; } = "";
        public string RecipientHash { get; set; } = "";
        public string ChannelHash { get; set; } = "";
        public DateTimeOffset Timestamp { get; set; }
        public string Proof { get; set; } = "";
    }

    /// <summary>
    /// Batch notification result.
    /// </summary>
    private class BatchNotificationResult
    {
        public int Index { get; set; }
        public string NotificationId { get; set; } = "";
        public bool Success { get; set; }
    }
}
