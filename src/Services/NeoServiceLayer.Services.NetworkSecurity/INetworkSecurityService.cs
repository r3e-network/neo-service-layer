using System.Threading.Tasks;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.NetworkSecurity.Models;

namespace NeoServiceLayer.Services.NetworkSecurity;

/// <summary>
/// Interface for network security service providing secure network communication capabilities.
/// </summary>
public interface INetworkSecurityService : IService
{
    /// <summary>
    /// Creates a new secure communication channel.
    /// </summary>
    /// <param name="request">The channel creation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The created channel information.</returns>
    Task<SecureChannelResponse> CreateSecureChannelAsync(CreateChannelRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Sends an encrypted message through a secure channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The message response.</returns>
    Task<MessageResponse> SendMessageAsync(string channelId, NetworkMessage message, BlockchainType blockchainType);

    /// <summary>
    /// Configures firewall rules for the enclave.
    /// </summary>
    /// <param name="rules">The firewall rule set.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The configuration result.</returns>
    Task<FirewallConfigurationResult> ConfigureFirewallAsync(FirewallRuleSet rules, BlockchainType blockchainType);

    /// <summary>
    /// Monitors network traffic and security events.
    /// </summary>
    /// <param name="request">The monitoring request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The network monitoring data.</returns>
    Task<NetworkMonitoringData> MonitorNetworkAsync(MonitoringRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Closes a secure channel.
    /// </summary>
    /// <param name="channelId">The channel ID to close.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The close result.</returns>
    Task<bool> CloseChannelAsync(string channelId, BlockchainType blockchainType);

    /// <summary>
    /// Gets the status of a secure channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The channel status.</returns>
    Task<ChannelStatus> GetChannelStatusAsync(string channelId, BlockchainType blockchainType);
}