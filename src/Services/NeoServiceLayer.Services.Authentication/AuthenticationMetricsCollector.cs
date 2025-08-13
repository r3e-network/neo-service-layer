using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Observability.Metrics;
using NeoServiceLayer.Infrastructure.Observability.Logging;

namespace NeoServiceLayer.Services.Authentication
{
    /// <summary>
    /// Collects and reports authentication-related metrics for monitoring
    /// </summary>
    public interface IAuthenticationMetricsCollector
    {
        void RecordLoginAttempt(bool success, string method, double duration);
        void RecordTokenGeneration(string tokenType, double duration);
        void RecordTokenValidation(string tokenType, bool success, double duration);
        void RecordPasswordReset(bool success, double duration);
        void RecordEmailVerification(bool success, double duration);
        void RecordMfaAttempt(bool success, string method, double duration);
        void RecordAccountLockout(string reason);
        void RecordRateLimitHit(string endpoint, string identifier);
        void RecordSecurityEvent(string eventType, string severity);
        Task<AuthenticationMetrics> GetMetricsAsync();
    }

    public class AuthenticationMetricsCollector : IAuthenticationMetricsCollector
    {
        private readonly ILogger<AuthenticationMetricsCollector> _logger;
        private readonly IMetricsService _metricsService;
        private readonly IStructuredLogger _structuredLogger;
        private readonly Dictionary<string, long> _counters;
        private readonly Dictionary<string, List<double>> _timings;
        private readonly object _lock = new object();

        public AuthenticationMetricsCollector(
            ILogger<AuthenticationMetricsCollector> logger,
            IMetricsService metricsService,
            IStructuredLoggerFactory structuredLoggerFactory)
        {
            _logger = logger;
            _metricsService = metricsService;
            _structuredLogger = structuredLoggerFactory?.CreateLogger("AuthMetrics");
            _counters = new Dictionary<string, long>();
            _timings = new Dictionary<string, List<double>>();
        }

        public void RecordLoginAttempt(bool success, string method, double duration)
        {
            var metricName = success ? "auth.login.success" : "auth.login.failure";
            
            _metricsService?.RecordCounter(metricName, 1, new Dictionary<string, string>
            {
                ["method"] = method
            });
            
            _metricsService?.RecordHistogram("auth.login.duration", duration, new Dictionary<string, string>
            {
                ["method"] = method,
                ["success"] = success.ToString()
            });

            lock (_lock)
            {
                IncrementCounter($"login_{method}_{(success ? "success" : "failure")}");
                AddTiming("login_duration", duration);
            }

            _structuredLogger?.LogMetric("LoginAttempt", new Dictionary<string, object>
            {
                ["Success"] = success,
                ["Method"] = method,
                ["Duration"] = duration,
                ["Timestamp"] = DateTime.UtcNow
            });

            if (!success)
            {
                _logger.LogWarning("Failed login attempt using method {Method}", method);
            }
        }

        public void RecordTokenGeneration(string tokenType, double duration)
        {
            _metricsService?.RecordCounter($"auth.token.{tokenType.ToLower()}.generated", 1);
            _metricsService?.RecordHistogram($"auth.token.{tokenType.ToLower()}.generation.duration", duration);

            lock (_lock)
            {
                IncrementCounter($"token_{tokenType}_generated");
                AddTiming($"token_{tokenType}_generation", duration);
            }

            _structuredLogger?.LogMetric("TokenGeneration", new Dictionary<string, object>
            {
                ["TokenType"] = tokenType,
                ["Duration"] = duration,
                ["Timestamp"] = DateTime.UtcNow
            });
        }

        public void RecordTokenValidation(string tokenType, bool success, double duration)
        {
            var metricName = success 
                ? $"auth.token.{tokenType.ToLower()}.validation.success" 
                : $"auth.token.{tokenType.ToLower()}.validation.failure";
            
            _metricsService?.RecordCounter(metricName, 1);
            _metricsService?.RecordHistogram($"auth.token.{tokenType.ToLower()}.validation.duration", duration);

            lock (_lock)
            {
                IncrementCounter($"token_{tokenType}_validation_{(success ? "success" : "failure")}");
                AddTiming($"token_{tokenType}_validation", duration);
            }

            _structuredLogger?.LogMetric("TokenValidation", new Dictionary<string, object>
            {
                ["TokenType"] = tokenType,
                ["Success"] = success,
                ["Duration"] = duration,
                ["Timestamp"] = DateTime.UtcNow
            });
        }

        public void RecordPasswordReset(bool success, double duration)
        {
            var metricName = success ? "auth.password.reset.success" : "auth.password.reset.failure";
            
            _metricsService?.RecordCounter(metricName, 1);
            _metricsService?.RecordHistogram("auth.password.reset.duration", duration);

            lock (_lock)
            {
                IncrementCounter($"password_reset_{(success ? "success" : "failure")}");
                AddTiming("password_reset_duration", duration);
            }

            _structuredLogger?.LogMetric("PasswordReset", new Dictionary<string, object>
            {
                ["Success"] = success,
                ["Duration"] = duration,
                ["Timestamp"] = DateTime.UtcNow
            });
        }

        public void RecordEmailVerification(bool success, double duration)
        {
            var metricName = success ? "auth.email.verification.success" : "auth.email.verification.failure";
            
            _metricsService?.RecordCounter(metricName, 1);
            _metricsService?.RecordHistogram("auth.email.verification.duration", duration);

            lock (_lock)
            {
                IncrementCounter($"email_verification_{(success ? "success" : "failure")}");
                AddTiming("email_verification_duration", duration);
            }

            _structuredLogger?.LogMetric("EmailVerification", new Dictionary<string, object>
            {
                ["Success"] = success,
                ["Duration"] = duration,
                ["Timestamp"] = DateTime.UtcNow
            });
        }

        public void RecordMfaAttempt(bool success, string method, double duration)
        {
            var metricName = success ? "auth.mfa.success" : "auth.mfa.failure";
            
            _metricsService?.RecordCounter(metricName, 1, new Dictionary<string, string>
            {
                ["method"] = method
            });
            
            _metricsService?.RecordHistogram("auth.mfa.duration", duration, new Dictionary<string, string>
            {
                ["method"] = method,
                ["success"] = success.ToString()
            });

            lock (_lock)
            {
                IncrementCounter($"mfa_{method}_{(success ? "success" : "failure")}");
                AddTiming($"mfa_{method}_duration", duration);
            }

            _structuredLogger?.LogMetric("MfaAttempt", new Dictionary<string, object>
            {
                ["Success"] = success,
                ["Method"] = method,
                ["Duration"] = duration,
                ["Timestamp"] = DateTime.UtcNow
            });
        }

        public void RecordAccountLockout(string reason)
        {
            _metricsService?.RecordCounter("auth.account.lockout", 1, new Dictionary<string, string>
            {
                ["reason"] = reason
            });

            lock (_lock)
            {
                IncrementCounter($"account_lockout_{reason}");
            }

            _structuredLogger?.LogMetric("AccountLockout", new Dictionary<string, object>
            {
                ["Reason"] = reason,
                ["Timestamp"] = DateTime.UtcNow
            });

            _logger.LogWarning("Account locked out for reason: {Reason}", reason);
        }

        public void RecordRateLimitHit(string endpoint, string identifier)
        {
            _metricsService?.RecordCounter("auth.ratelimit.hit", 1, new Dictionary<string, string>
            {
                ["endpoint"] = endpoint,
                ["identifier"] = identifier.Substring(0, Math.Min(8, identifier.Length)) + "..."
            });

            lock (_lock)
            {
                IncrementCounter($"ratelimit_{endpoint}");
            }

            _structuredLogger?.LogMetric("RateLimitHit", new Dictionary<string, object>
            {
                ["Endpoint"] = endpoint,
                ["Identifier"] = identifier.Substring(0, Math.Min(8, identifier.Length)) + "...",
                ["Timestamp"] = DateTime.UtcNow
            });

            _logger.LogWarning("Rate limit hit for {Endpoint} by {Identifier}", endpoint, identifier.Substring(0, Math.Min(8, identifier.Length)) + "...");
        }

        public void RecordSecurityEvent(string eventType, string severity)
        {
            _metricsService?.RecordCounter($"auth.security.{eventType.ToLower()}", 1, new Dictionary<string, string>
            {
                ["severity"] = severity
            });

            lock (_lock)
            {
                IncrementCounter($"security_{eventType}_{severity}");
            }

            _structuredLogger?.LogMetric("SecurityEvent", new Dictionary<string, object>
            {
                ["EventType"] = eventType,
                ["Severity"] = severity,
                ["Timestamp"] = DateTime.UtcNow
            });

            if (severity == "high" || severity == "critical")
            {
                _logger.LogError("High severity security event: {EventType}", eventType);
            }
        }

        public async Task<AuthenticationMetrics> GetMetricsAsync()
        {
            await Task.CompletedTask;

            lock (_lock)
            {
                var metrics = new AuthenticationMetrics
                {
                    Timestamp = DateTime.UtcNow,
                    LoginMetrics = new LoginMetrics
                    {
                        TotalAttempts = GetCounter("login_password_success") + GetCounter("login_password_failure") +
                                       GetCounter("login_oauth_success") + GetCounter("login_oauth_failure"),
                        SuccessfulLogins = GetCounter("login_password_success") + GetCounter("login_oauth_success"),
                        FailedLogins = GetCounter("login_password_failure") + GetCounter("login_oauth_failure"),
                        AverageLoginDuration = GetAverageTiming("login_duration"),
                        LoginsByMethod = new Dictionary<string, long>
                        {
                            ["password"] = GetCounter("login_password_success") + GetCounter("login_password_failure"),
                            ["oauth"] = GetCounter("login_oauth_success") + GetCounter("login_oauth_failure")
                        }
                    },
                    TokenMetrics = new TokenMetrics
                    {
                        AccessTokensGenerated = GetCounter("token_access_generated"),
                        RefreshTokensGenerated = GetCounter("token_refresh_generated"),
                        TokenValidations = GetCounter("token_access_validation_success") + GetCounter("token_access_validation_failure"),
                        TokenValidationFailures = GetCounter("token_access_validation_failure"),
                        AverageGenerationTime = GetAverageTiming("token_access_generation"),
                        AverageValidationTime = GetAverageTiming("token_access_validation")
                    },
                    MfaMetrics = new MfaMetrics
                    {
                        TotalAttempts = GetCounter("mfa_totp_success") + GetCounter("mfa_totp_failure") +
                                       GetCounter("mfa_email_success") + GetCounter("mfa_email_failure") +
                                       GetCounter("mfa_sms_success") + GetCounter("mfa_sms_failure"),
                        SuccessfulVerifications = GetCounter("mfa_totp_success") + GetCounter("mfa_email_success") + GetCounter("mfa_sms_success"),
                        FailedVerifications = GetCounter("mfa_totp_failure") + GetCounter("mfa_email_failure") + GetCounter("mfa_sms_failure"),
                        VerificationsByMethod = new Dictionary<string, long>
                        {
                            ["totp"] = GetCounter("mfa_totp_success") + GetCounter("mfa_totp_failure"),
                            ["email"] = GetCounter("mfa_email_success") + GetCounter("mfa_email_failure"),
                            ["sms"] = GetCounter("mfa_sms_success") + GetCounter("mfa_sms_failure")
                        }
                    },
                    SecurityMetrics = new SecurityMetrics
                    {
                        AccountLockouts = GetCounter("account_lockout_failed_attempts") + 
                                         GetCounter("account_lockout_suspicious_activity") +
                                         GetCounter("account_lockout_admin_action"),
                        RateLimitHits = GetCounter("ratelimit_login") + GetCounter("ratelimit_register") + GetCounter("ratelimit_password_reset"),
                        PasswordResets = GetCounter("password_reset_success") + GetCounter("password_reset_failure"),
                        EmailVerifications = GetCounter("email_verification_success") + GetCounter("email_verification_failure"),
                        SecurityEventsByType = new Dictionary<string, long>
                        {
                            ["suspicious_activity"] = GetCounter("security_suspicious_activity_high"),
                            ["unauthorized_access"] = GetCounter("security_unauthorized_access_high"),
                            ["token_theft"] = GetCounter("security_token_theft_critical")
                        }
                    }
                };

                return metrics;
            }
        }

        private void IncrementCounter(string key)
        {
            if (!_counters.ContainsKey(key))
            {
                _counters[key] = 0;
            }
            _counters[key]++;
        }

        private long GetCounter(string key)
        {
            return _counters.ContainsKey(key) ? _counters[key] : 0;
        }

        private void AddTiming(string key, double value)
        {
            if (!_timings.ContainsKey(key))
            {
                _timings[key] = new List<double>();
            }
            _timings[key].Add(value);
            
            // Keep only last 1000 timings to prevent memory growth
            if (_timings[key].Count > 1000)
            {
                _timings[key].RemoveAt(0);
            }
        }

        private double GetAverageTiming(string key)
        {
            if (!_timings.ContainsKey(key) || _timings[key].Count == 0)
            {
                return 0;
            }
            
            double sum = 0;
            foreach (var timing in _timings[key])
            {
                sum += timing;
            }
            return sum / _timings[key].Count;
        }
    }

    /// <summary>
    /// Authentication metrics data structure
    /// </summary>
    public class AuthenticationMetrics
    {
        public DateTime Timestamp { get; set; }
        public LoginMetrics LoginMetrics { get; set; }
        public TokenMetrics TokenMetrics { get; set; }
        public MfaMetrics MfaMetrics { get; set; }
        public SecurityMetrics SecurityMetrics { get; set; }
    }

    public class LoginMetrics
    {
        public long TotalAttempts { get; set; }
        public long SuccessfulLogins { get; set; }
        public long FailedLogins { get; set; }
        public double AverageLoginDuration { get; set; }
        public Dictionary<string, long> LoginsByMethod { get; set; }
    }

    public class TokenMetrics
    {
        public long AccessTokensGenerated { get; set; }
        public long RefreshTokensGenerated { get; set; }
        public long TokenValidations { get; set; }
        public long TokenValidationFailures { get; set; }
        public double AverageGenerationTime { get; set; }
        public double AverageValidationTime { get; set; }
    }

    public class MfaMetrics
    {
        public long TotalAttempts { get; set; }
        public long SuccessfulVerifications { get; set; }
        public long FailedVerifications { get; set; }
        public Dictionary<string, long> VerificationsByMethod { get; set; }
    }

    public class SecurityMetrics
    {
        public long AccountLockouts { get; set; }
        public long RateLimitHits { get; set; }
        public long PasswordResets { get; set; }
        public long EmailVerifications { get; set; }
        public Dictionary<string, long> SecurityEventsByType { get; set; }
    }
}