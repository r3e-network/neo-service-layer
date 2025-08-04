using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace NeoServiceLayer.RPC.Server.Hubs;

/// <summary>
/// SignalR hub for real-time notifications and events.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles client connections.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        
        // Add to general group
        await Groups.AddToGroupAsync(Context.ConnectionId, "all");
        
        // Send welcome message
        await Clients.Caller.SendAsync("Welcome", new
        {
            message = "Connected to Neo Service Layer notification hub",
            connectionId = Context.ConnectionId,
            timestamp = DateTime.UtcNow
        });

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Handles client disconnections.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribes to blockchain events.
    /// </summary>
    public async Task SubscribeToBlockchain(string blockchainType)
    {
        var groupName = $"blockchain:{blockchainType.ToLower()}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("Client {ConnectionId} subscribed to {BlockchainType} events", 
            Context.ConnectionId, blockchainType);
        
        await Clients.Caller.SendAsync("Subscribed", new
        {
            subscription = "blockchain",
            type = blockchainType,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Subscribes to service events.
    /// </summary>
    public async Task SubscribeToService(string serviceName)
    {
        var groupName = $"service:{serviceName.ToLower()}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("Client {ConnectionId} subscribed to {ServiceName} events", 
            Context.ConnectionId, serviceName);
        
        await Clients.Caller.SendAsync("Subscribed", new
        {
            subscription = "service",
            name = serviceName,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Subscribes to contract events.
    /// </summary>
    public async Task SubscribeToContract(string contractAddress, string? eventName = null)
    {
        var groupName = string.IsNullOrEmpty(eventName)
            ? $"contract:{contractAddress.ToLower()}"
            : $"contract:{contractAddress.ToLower()}:{eventName.ToLower()}";
            
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("Client {ConnectionId} subscribed to contract {ContractAddress} events {EventName}", 
            Context.ConnectionId, contractAddress, eventName ?? "all");
        
        await Clients.Caller.SendAsync("Subscribed", new
        {
            subscription = "contract",
            address = contractAddress,
            eventName = eventName,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Unsubscribes from a group.
    /// </summary>
    public async Task Unsubscribe(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("Client {ConnectionId} unsubscribed from {GroupName}", 
            Context.ConnectionId, groupName);
        
        await Clients.Caller.SendAsync("Unsubscribed", new
        {
            group = groupName,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Gets active subscriptions for the client.
    /// </summary>
    public async Task GetSubscriptions()
    {
        // This is a simplified version - in production you'd track subscriptions
        await Clients.Caller.SendAsync("Subscriptions", new
        {
            connectionId = Context.ConnectionId,
            subscriptions = new string[] { "all" }, // Would list actual subscriptions
            timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Service for sending notifications to connected clients.
/// </summary>
public interface INotificationService
{
    Task SendBlockchainEventAsync(string blockchainType, object eventData);
    Task SendServiceEventAsync(string serviceName, object eventData);
    Task SendContractEventAsync(string contractAddress, string eventName, object eventData);
    Task SendGlobalNotificationAsync(object notification);
}

/// <summary>
/// Implementation of the notification service.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendBlockchainEventAsync(string blockchainType, object eventData)
    {
        var groupName = $"blockchain:{blockchainType.ToLower()}";
        await _hubContext.Clients.Group(groupName).SendAsync("BlockchainEvent", new
        {
            type = blockchainType,
            data = eventData,
            timestamp = DateTime.UtcNow
        });
        
        _logger.LogDebug("Sent blockchain event to group {GroupName}", groupName);
    }

    public async Task SendServiceEventAsync(string serviceName, object eventData)
    {
        var groupName = $"service:{serviceName.ToLower()}";
        await _hubContext.Clients.Group(groupName).SendAsync("ServiceEvent", new
        {
            service = serviceName,
            data = eventData,
            timestamp = DateTime.UtcNow
        });
        
        _logger.LogDebug("Sent service event to group {GroupName}", groupName);
    }

    public async Task SendContractEventAsync(string contractAddress, string eventName, object eventData)
    {
        var specificGroup = $"contract:{contractAddress.ToLower()}:{eventName.ToLower()}";
        var generalGroup = $"contract:{contractAddress.ToLower()}";
        
        var notification = new
        {
            contractAddress,
            eventName,
            data = eventData,
            timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group(specificGroup).SendAsync("ContractEvent", notification);
        await _hubContext.Clients.Group(generalGroup).SendAsync("ContractEvent", notification);
        
        _logger.LogDebug("Sent contract event to groups {SpecificGroup} and {GeneralGroup}", 
            specificGroup, generalGroup);
    }

    public async Task SendGlobalNotificationAsync(object notification)
    {
        await _hubContext.Clients.Group("all").SendAsync("GlobalNotification", new
        {
            data = notification,
            timestamp = DateTime.UtcNow
        });
        
        _logger.LogDebug("Sent global notification to all clients");
    }
}