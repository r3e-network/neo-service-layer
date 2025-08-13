using System;

namespace NeoServiceLayer.Services.Authentication.Domain.ValueObjects
{
    public class UserSession
    {
        public Guid Id { get; }
        public string IpAddress { get; }
        public string UserAgent { get; }
        public string? DeviceId { get; }
        public DateTime StartedAt { get; }
        public DateTime? LoggedOutAt { get; set; }
        public DateTime LastActivityAt { get; set; }

        public bool IsActive => !LoggedOutAt.HasValue;
        public TimeSpan Duration => (LoggedOutAt ?? DateTime.UtcNow) - StartedAt;

        public UserSession(
            Guid id,
            string ipAddress,
            string userAgent,
            string? deviceId,
            DateTime startedAt)
        {
            Id = id;
            IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            UserAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));
            DeviceId = deviceId;
            StartedAt = startedAt;
            LastActivityAt = startedAt;
        }
    }
}