using System;

namespace NeoServiceLayer.Services.Authentication.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string JwtId { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? DeviceId { get; set; }
        public string? IpAddress { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }
        
        // Navigation property
        public virtual User User { get; set; } = null!;
        
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsValid => !IsExpired && !IsRevoked;
        public bool IsActive => IsValid;
    }
}