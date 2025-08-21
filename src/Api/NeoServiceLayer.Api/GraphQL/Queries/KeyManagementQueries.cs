using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.KeyManagement;

namespace NeoServiceLayer.Api.GraphQL.Queries;

/// <summary>
/// GraphQL queries for key management operations.
/// </summary>
[ExtendObjectType(typeof(Query))]
public class KeyManagementQueries
{
    /// <summary>
    /// Gets a specific key by ID.
    /// </summary>
    /// <param name="keyId">The key identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="keyManagementService">The key management service.</param>
    /// <returns>The key metadata if found.</returns>
    [Authorize(Roles = ["Admin", "KeyManager", "User"])]
    [GraphQLDescription("Gets a specific key by ID")]
    public async Task<KeyMetadata?> GetKey(
        string keyId,
        BlockchainType blockchainType,
        [Service] IKeyManagementService keyManagementService)
    {
        var keys = await keyManagementService.GetKeysAsync(
            new GetKeysRequest { BlockchainType = blockchainType },
            CancellationToken.None);
        
        return keys.Items.FirstOrDefault(k => k.KeyId == keyId);
    }

    /// <summary>
    /// Gets all keys with optional filtering.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="keyType">Optional key type filter.</param>
    /// <param name="includeExpired">Whether to include expired keys.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="keyManagementService">The key management service.</param>
    /// <returns>Paginated list of keys.</returns>
    [Authorize(Roles = ["Admin", "KeyManager"])]
    [GraphQLDescription("Gets all keys with optional filtering")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public async Task<IEnumerable<KeyMetadata>> GetKeys(
        BlockchainType blockchainType,
        string? keyType,
        bool includeExpired,
        int page,
        int pageSize,
        [Service] IKeyManagementService keyManagementService)
    {
        var request = new GetKeysRequest
        {
            BlockchainType = blockchainType,
            Page = page,
            PageSize = pageSize
        };

        var result = await keyManagementService.GetKeysAsync(request, CancellationToken.None);
        
        var keys = result.Items.AsEnumerable();
        
        if (!string.IsNullOrEmpty(keyType))
        {
            keys = keys.Where(k => k.KeyType == keyType);
        }
        
        if (!includeExpired)
        {
            keys = keys.Where(k => !k.ExpiresAt.HasValue || k.ExpiresAt.Value > DateTime.UtcNow);
        }
        
        return keys;
    }

    /// <summary>
    /// Gets key statistics.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="keyManagementService">The key management service.</param>
    /// <returns>Key statistics.</returns>
    [Authorize(Roles = ["Admin", "KeyManager"])]
    [GraphQLDescription("Gets key statistics for monitoring")]
    public async Task<KeyStatistics> GetKeyStatistics(
        BlockchainType blockchainType,
        [Service] IKeyManagementService keyManagementService)
    {
        var keys = await keyManagementService.GetKeysAsync(
            new GetKeysRequest { BlockchainType = blockchainType, PageSize = 1000 },
            CancellationToken.None);

        var allKeys = keys.Items.ToList();
        var now = DateTime.UtcNow;

        return new KeyStatistics
        {
            TotalKeys = allKeys.Count,
            ActiveKeys = allKeys.Count(k => !k.ExpiresAt.HasValue || k.ExpiresAt.Value > now),
            ExpiredKeys = allKeys.Count(k => k.ExpiresAt.HasValue && k.ExpiresAt.Value <= now),
            KeysByType = allKeys.GroupBy(k => k.KeyType)
                .ToDictionary(g => g.Key, g => g.Count()),
            KeysExpiringWithin24Hours = allKeys.Count(k => 
                k.ExpiresAt.HasValue && 
                k.ExpiresAt.Value > now && 
                k.ExpiresAt.Value <= now.AddHours(24)),
            LastKeyCreated = allKeys.OrderByDescending(k => k.CreatedAt).FirstOrDefault()?.CreatedAt
        };
    }
}

/// <summary>
/// Key statistics model.
/// </summary>
public class KeyStatistics
{
    /// <summary>
    /// Gets or sets the total number of keys.
    /// </summary>
    public int TotalKeys { get; set; }
    
    /// <summary>
    /// Gets or sets the number of active keys.
    /// </summary>
    public int ActiveKeys { get; set; }
    
    /// <summary>
    /// Gets or sets the number of expired keys.
    /// </summary>
    public int ExpiredKeys { get; set; }
    
    /// <summary>
    /// Gets or sets the key count by type.
    /// </summary>
    public Dictionary<string, int> KeysByType { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the number of keys expiring within 24 hours.
    /// </summary>
    public int KeysExpiringWithin24Hours { get; set; }
    
    /// <summary>
    /// Gets or sets when the last key was created.
    /// </summary>
    public DateTime? LastKeyCreated { get; set; }
}