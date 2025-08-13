using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Services.KeyManagement.ReadModels;

namespace NeoServiceLayer.Services.KeyManagement.Queries
{
    public class GetKeyByIdQuery : IQuery<KeyReadModel?>
    {
        public GetKeyByIdQuery(string keyId)
        {
            KeyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
        }

        public string KeyId { get; }
    }

    public class GetKeysByTypeQuery : IQuery<IEnumerable<KeyReadModel>>
    {
        public GetKeysByTypeQuery(string keyType)
        {
            KeyType = keyType ?? throw new ArgumentNullException(nameof(keyType));
        }

        public string KeyType { get; }
    }

    public class GetActiveKeysQuery : IQuery<IEnumerable<KeyReadModel>>
    {
        public GetActiveKeysQuery(int? limit = null)
        {
            Limit = limit;
        }

        public int? Limit { get; }
    }

    public class GetKeysExpiringBeforeQuery : IQuery<IEnumerable<KeyReadModel>>
    {
        public GetKeysExpiringBeforeQuery(DateTime expiryDate)
        {
            ExpiryDate = expiryDate;
        }

        public DateTime ExpiryDate { get; }
    }

    public class GetKeysByUserQuery : IQuery<IEnumerable<KeyReadModel>>
    {
        public GetKeysByUserQuery(string userId)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        }

        public string UserId { get; }
    }

    public class GetKeyUsageStatisticsQuery : IQuery<KeyUsageStatistics>
    {
        public GetKeyUsageStatisticsQuery(string keyId, DateTime? from = null, DateTime? to = null)
        {
            KeyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
            From = from ?? DateTime.UtcNow.AddDays(-30);
            To = to ?? DateTime.UtcNow;
        }

        public string KeyId { get; }
        public DateTime From { get; }
        public DateTime To { get; }
    }

    public class SearchKeysQuery : IQuery<IEnumerable<KeyReadModel>>
    {
        public SearchKeysQuery(
            string? searchTerm = null,
            string? keyType = null,
            string? algorithm = null,
            KeyStatusFilter? status = null,
            int offset = 0,
            int limit = 50)
        {
            SearchTerm = searchTerm;
            KeyType = keyType;
            Algorithm = algorithm;
            Status = status;
            Offset = Math.Max(0, offset);
            Limit = Math.Min(Math.Max(1, limit), 100);
        }

        public string? SearchTerm { get; }
        public string? KeyType { get; }
        public string? Algorithm { get; }
        public KeyStatusFilter? Status { get; }
        public int Offset { get; }
        public int Limit { get; }
    }

    public enum KeyStatusFilter
    {
        All,
        Active,
        Inactive,
        Revoked,
        Expired
    }
}