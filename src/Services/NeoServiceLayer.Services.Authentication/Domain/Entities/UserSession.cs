using System;

namespace NeoServiceLayer.Services.Authentication.Domain.Entities
{
    public class UserSession
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string SessionToken { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime? LoggedOutAt { get; set; }
        public bool IsActive => !LoggedOutAt.HasValue;
        
        // Navigation property
        public virtual User User { get; set; } = null!;
        
        public TimeSpan Duration => (LoggedOutAt ?? DateTime.UtcNow) - StartedAt;
    }
}