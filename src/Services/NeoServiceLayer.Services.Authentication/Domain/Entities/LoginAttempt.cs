using System;

namespace NeoServiceLayer.Services.Authentication.Domain.Entities
{
    public class LoginAttempt
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public string? DeviceId { get; set; }
        public bool Success { get; set; }
        public DateTime AttemptedAt { get; set; }
        public string? FailureReason { get; set; }
        
        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}