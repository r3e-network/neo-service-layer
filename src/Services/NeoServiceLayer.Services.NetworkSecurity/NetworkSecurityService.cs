using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.NetworkSecurity.Models;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.NetworkSecurity;

/// <summary>
/// Service providing secure network communication capabilities for SGX enclaves.
/// </summary>
public partial class NetworkSecurityService : EnclaveBlockchainServiceBase, INetworkSecurityService
{
    private new readonly IEnclaveManager _enclaveManager;
    private readonly IServiceConfiguration _configuration;
    private readonly IPersistentStorageProvider? _persistentStorage;
    private readonly ConcurrentDictionary<string, SecureChannel> _channels = new();
    private readonly ConcurrentDictionary<string, FirewallRule> _firewallRules = new();
    private readonly ConcurrentDictionary<string, NetworkStatistics> _statistics = new();
    private readonly List<SecurityEvent> _securityEvents = new();
    private readonly object _eventLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkSecurityService"/> class.
    /// </summary>
    public NetworkSecurityService(
        IEnclaveManager enclaveManager,
        IServiceConfiguration configuration,
        ILogger<NetworkSecurityService> logger,
        IPersistentStorageProvider? persistentStorage = null)
        : base("NetworkSecurity", "Secure Network Communication Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        _enclaveManager = enclaveManager;
        _configuration = configuration;
        _persistentStorage = persistentStorage;

        // Add capabilities
        AddCapability<INetworkSecurityService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("MaxChannels", _configuration.GetValue("NetworkSecurity:MaxChannels", "100"));
        SetMetadata("SupportedProtocols", "TCP,UDP,HTTPS,WSS");
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            // Load persisted firewall rules
            var rules = await LoadFromPersistentStorageAsync<List<FirewallRule>>("firewall_rules");
            if (rules != null)
            {
                foreach (var rule in rules)
                {
                    _firewallRules[rule.Name] = rule;
                }
            }

            // Initialize default firewall rules
            if (_firewallRules.IsEmpty)
            {
                var defaultRules = new[]
                {
                    new FirewallRule
                    {
                        Name = "allow-enclave-outbound",
                        Action = FirewallAction.Allow,
                        Source = "enclave",
                        Destination = "*",
                        Port = "443",
                        Protocol = "TCP"
                    }
                };

                foreach (var rule in defaultRules)
                {
                    _firewallRules[rule.Name] = rule;
                }

                await SaveToPersistentStorageAsync("firewall_rules", _firewallRules.Values.ToList());
            }

            Logger.LogInformation("Network Security Service initialized with {RuleCount} firewall rules", _firewallRules.Count);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize NetworkSecurity enclave");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<SecureChannelResponse> CreateSecureChannelAsync(CreateChannelRequest request, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            var channelId = $"ch_{Guid.NewGuid():N}";

            // Generate key pair for the channel
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
            var publicKey = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());

            var channel = new SecureChannel
            {
                Id = channelId,
                Name = request.ChannelName,
                TargetEndpoint = request.TargetEndpoint,
                Protocol = request.Protocol,
                PublicKey = publicKey,
                CreatedAt = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddHours(24),
                IsActive = true,
                Statistics = new NetworkStatistics()
            };

            _channels[channelId] = channel;
            _statistics[channelId] = channel.Statistics;

            // Persist channel information
            await SaveToPersistentStorageAsync($"channel_{channelId}", channel);

            // Log security event
            LogSecurityEvent("CHANNEL_CREATED", "system", $"Channel {channelId} created");

            return new SecureChannelResponse
            {
                Success = true,
                ChannelId = channelId,
                Status = "ACTIVE",
                Endpoint = $"https://enclave.local:8443/channel/{channelId}",
                PublicKey = publicKey,
                ValidUntil = channel.ValidUntil
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create secure channel");
            throw new InvalidOperationException("Failed to create secure channel", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<MessageResponse> SendMessageAsync(string channelId, NetworkMessage message, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        if (!_channels.TryGetValue(channelId, out var channel))
        {
            throw new InvalidOperationException($"Channel {channelId} not found");
        }

        if (!channel.IsActive)
        {
            throw new InvalidOperationException($"Channel {channelId} is not active");
        }

        try
        {
            var messageId = $"msg_{Guid.NewGuid():N}";
            var startTime = DateTime.UtcNow;

            // Check firewall rules
            if (!IsAllowedByFirewall("enclave", ExtractHost(channel.TargetEndpoint), GetPort(channel.TargetEndpoint)))
            {
                LogSecurityEvent("MESSAGE_BLOCKED", "enclave", $"Message to {channel.TargetEndpoint} blocked by firewall");
                throw new InvalidOperationException("Connection blocked by firewall");
            }

            // Encrypt the message
            var encryptedPayload = await EncryptMessageAsync(message.Payload, channel.PublicKey);

            // Simulate sending the message (in production, this would use actual network calls)
            await Task.Delay(Random.Shared.Next(50, 150));

            // Update statistics
            var latency = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            channel.Statistics.TotalRequests++;
            channel.Statistics.SuccessfulRequests++;
            channel.Statistics.AverageLatency =
                ((channel.Statistics.AverageLatency * (channel.Statistics.TotalRequests - 1)) + latency) / channel.Statistics.TotalRequests;
            channel.Statistics.BandwidthUsed += Encoding.UTF8.GetByteCount(message.Payload);
            channel.LastActivity = DateTime.UtcNow;
            channel.MessageCount++;

            // Persist updated statistics
            await SaveToPersistentStorageAsync($"channel_{channelId}", channel);

            return new MessageResponse
            {
                Success = true,
                MessageId = messageId,
                Response = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"status\":\"delivered\"}")),
                Latency = latency,
                Encrypted = true
            };
        }
        catch (Exception ex)
        {
            channel.Statistics.FailedRequests++;
            channel.ErrorCount++;
            Logger.LogError(ex, "Failed to send message through channel {ChannelId}", channelId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FirewallConfigurationResult> ConfigureFirewallAsync(FirewallRuleSet rules, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            _firewallRules.Clear();
            var appliedCount = 0;

            foreach (var rule in rules.Rules)
            {
                _firewallRules[rule.Name] = rule;
                appliedCount++;
            }

            // Persist firewall rules
            await SaveToPersistentStorageAsync("firewall_rules", _firewallRules.Values.ToList());

            LogSecurityEvent("FIREWALL_UPDATED", "admin", $"Applied {appliedCount} firewall rules");

            return new FirewallConfigurationResult
            {
                Success = true,
                RulesApplied = appliedCount
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to configure firewall");
            return new FirewallConfigurationResult
            {
                Success = false,
                RulesApplied = 0,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<NetworkMonitoringData> MonitorNetworkAsync(MonitoringRequest request, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        await Task.CompletedTask;

        var stats = new NetworkStatistics();
        foreach (var channelStats in _statistics.Values)
        {
            stats.TotalRequests += channelStats.TotalRequests;
            stats.SuccessfulRequests += channelStats.SuccessfulRequests;
            stats.FailedRequests += channelStats.FailedRequests;
            stats.BandwidthUsed += channelStats.BandwidthUsed;
        }

        if (stats.TotalRequests > 0)
        {
            stats.AverageLatency = _statistics.Values.Average(s => s.AverageLatency);
        }

        List<SecurityEvent> events;
        lock (_eventLock)
        {
            events = _securityEvents
                .Where(e => (!request.StartTime.HasValue || e.Timestamp >= request.StartTime.Value) &&
                           (!request.EndTime.HasValue || e.Timestamp <= request.EndTime.Value))
                .OrderByDescending(e => e.Timestamp)
                .Take(100)
                .ToList();
        }

        return new NetworkMonitoringData
        {
            Statistics = stats,
            ActiveChannels = _channels.Count(c => c.Value.IsActive),
            SecurityEvents = events
        };
    }

    /// <inheritdoc/>
    public async Task<bool> CloseChannelAsync(string channelId, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        if (_channels.TryGetValue(channelId, out var channel))
        {
            channel.IsActive = false;
            channel.ClosedAt = DateTime.UtcNow;

            await SaveToPersistentStorageAsync($"channel_{channelId}", channel);

            LogSecurityEvent("CHANNEL_CLOSED", "system", $"Channel {channelId} closed");

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<ChannelStatus> GetChannelStatusAsync(string channelId, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        await Task.CompletedTask;

        if (!_channels.TryGetValue(channelId, out var channel))
        {
            throw new InvalidOperationException($"Channel {channelId} not found");
        }

        return new ChannelStatus
        {
            ChannelId = channelId,
            IsActive = channel.IsActive,
            CreatedAt = channel.CreatedAt,
            LastActivity = channel.LastActivity,
            MessageCount = channel.MessageCount,
            ErrorCount = channel.ErrorCount
        };
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        try
        {
            // Close all active channels
            foreach (var channel in _channels.Values.Where(c => c.IsActive))
            {
                channel.IsActive = false;
                channel.ClosedAt = DateTime.UtcNow;
                await SaveToPersistentStorageAsync($"channel_{channel.Id}", channel);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping NetworkSecurity service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        return ServiceHealth.Healthy;
    }

    private void ValidateBlockchainType(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }
    }

    private async Task<T?> LoadFromPersistentStorageAsync<T>(string key) where T : class
    {
        if (_persistentStorage == null) return null;

        try
        {
            return await _persistentStorage.RetrieveObjectAsync<T>(key, Logger, $"Loading {key}");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load {Key} from persistent storage", key);
            return null;
        }
    }

    private async Task SaveToPersistentStorageAsync<T>(string key, T obj)
    {
        if (_persistentStorage == null) return;

        try
        {
            await _persistentStorage.StoreObjectAsync(key, obj, new StorageOptions
            {
                Encrypt = true,
                Compress = true
            }, Logger, $"Saving {key}");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to save {Key} to persistent storage", key);
        }
    }

    private bool IsAllowedByFirewall(string source, string destination, int port)
    {
        // Check specific rules first
        foreach (var rule in _firewallRules.Values)
        {
            if (MatchesRule(rule, source, destination, port))
            {
                return rule.Action == FirewallAction.Allow;
            }
        }

        // Default deny
        return false;
    }

    private bool MatchesRule(FirewallRule rule, string source, string destination, int port)
    {
        var sourceMatch = rule.Source == "*" || rule.Source == source;
        var destMatch = rule.Destination == "*" || rule.Destination == destination;
        var portMatch = rule.Port == "*" || rule.Port == port.ToString();

        return sourceMatch && destMatch && portMatch;
    }

    private string ExtractHost(string endpoint)
    {
        try
        {
            var uri = new Uri(endpoint);
            return uri.Host;
        }
        catch
        {
            return endpoint;
        }
    }

    private int GetPort(string endpoint)
    {
        try
        {
            var uri = new Uri(endpoint);
            return uri.Port > 0 ? uri.Port : (uri.Scheme == "https" ? 443 : 80);
        }
        catch
        {
            return 443;
        }
    }

    private async Task<string> EncryptMessageAsync(string payload, string publicKey)
    {
        try
        {
            using var rsa = RSA.Create();

            // Import the public key
            var publicKeyBytes = Convert.FromBase64String(publicKey);
            rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            // Encrypt the payload
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var encryptedBytes = rsa.Encrypt(payloadBytes, RSAEncryptionPadding.OaepSHA256);

            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to encrypt message");
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    private async Task<string> DecryptMessageAsync(string encryptedPayload, string privateKey)
    {
        try
        {
            using var rsa = RSA.Create();

            // Import the private key
            var privateKeyBytes = Convert.FromBase64String(privateKey);
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

            // Decrypt the payload
            var encryptedBytes = Convert.FromBase64String(encryptedPayload);
            var decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to decrypt message");
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }

    private void LogSecurityEvent(string type, string source, string description)
    {
        lock (_eventLock)
        {
            _securityEvents.Add(new SecurityEvent
            {
                Timestamp = DateTime.UtcNow,
                Type = type,
                Source = source,
                Action = description
            });

            // Keep only last 1000 events
            if (_securityEvents.Count > 1000)
            {
                _securityEvents.RemoveRange(0, _securityEvents.Count - 1000);
            }
        }
    }

    private class SecureChannel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TargetEndpoint { get; set; } = string.Empty;
        public NetworkProtocol Protocol { get; set; }
        public string PublicKey { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ValidUntil { get; set; }
        public DateTime LastActivity { get; set; }
        public DateTime? ClosedAt { get; set; }
        public bool IsActive { get; set; }
        public long MessageCount { get; set; }
        public long ErrorCount { get; set; }
        public NetworkStatistics Statistics { get; set; } = new();
    }
}
