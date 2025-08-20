using System;

namespace NeoServiceLayer.Services.Authentication.Domain.Entities
{
    public class RateLimitEntry
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public DateTime FirstAttemptAt { get; set; }
        public DateTime LastAttemptAt { get; set; }
        public DateTime WindowEnd { get; set; }
        public DateTime? BlockedUntil { get; set; }
        public bool IsBlocked => BlockedUntil.HasValue && BlockedUntil.Value > DateTime.UtcNow;
    }
}