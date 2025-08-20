using System;
using NeoServiceLayer.ServiceFramework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


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

        // Constructor overload for domain aggregate compatibility
        public LoginAttempt(
            string ipAddress,
            string userAgent,
            string deviceId,
            string? failureReason = null)
        {
            IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            Success = string.IsNullOrEmpty(failureReason);
            AttemptTime = DateTime.UtcNow;
            FailureReason = failureReason;
        }
    }
}