using System;

namespace NeoServiceLayer.Services.Authentication.Domain.Entities
{
    public class BlacklistedToken
    {
        public Guid Id { get; set; }
        public string TokenId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public string JwtId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public DateTime BlacklistedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? BlacklistedBy { get; set; }
    }
}