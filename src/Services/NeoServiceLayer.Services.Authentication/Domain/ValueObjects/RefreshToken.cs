using System;

namespace NeoServiceLayer.Services.Authentication.Domain.ValueObjects
{
    public class RefreshToken
    {
        public Guid Id { get; }
        public string Token { get; }
        public DateTime IssuedAt { get; }
        public DateTime ExpiresAt { get; }
        public string? DeviceId { get; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsRevoked => RevokedAt.HasValue;
        public bool IsValid => !IsExpired && !IsRevoked;

        public RefreshToken(
            Guid id,
            string token,
            DateTime issuedAt,
            DateTime expiresAt,
            string? deviceId = null)
        {
            Id = id;
            Token = token ?? throw new ArgumentNullException(nameof(token));
            IssuedAt = issuedAt;
            ExpiresAt = expiresAt;
            DeviceId = deviceId;
        }
    }
}