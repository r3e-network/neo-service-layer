using System;

namespace NeoServiceLayer.Services.Authentication.Domain.ValueObjects
{
    public class LoginAttempt
    {
        public string IpAddress { get; }
        public bool Success { get; }
        public DateTime AttemptTime { get; }
        public string? FailureReason { get; }

        public LoginAttempt(
            string ipAddress,
            bool success,
            DateTime attemptTime,
            string? failureReason = null)
        {
            IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            Success = success;
            AttemptTime = attemptTime;
            FailureReason = failureReason;
        }
    }
}