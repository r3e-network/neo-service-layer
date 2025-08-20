using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Authentication;

namespace NeoServiceLayer.Api.Middleware
{
    /// <summary>
    /// Middleware for comprehensive audit logging of user actions and security events
    /// </summary>
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLoggingMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _enabled;
        private readonly HashSet<string> _sensitiveHeaders;
        private readonly HashSet<string> _excludedPaths;
        private readonly HashSet<string> _auditedOperations;
        private readonly int _maxRequestBodySize;
        private readonly int _maxResponseBodySize;

        public AuditLoggingMiddleware(
            RequestDelegate next,
            ILogger<AuditLoggingMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;

            _enabled = configuration.GetValue<bool>("AuditLogging:Enabled", true);
            _maxRequestBodySize = configuration.GetValue<int>("AuditLogging:MaxRequestBodySize", 4096);
            _maxResponseBodySize = configuration.GetValue<int>("AuditLogging:MaxResponseBodySize", 4096);

            // Headers that should not be logged (contain sensitive data)
            _sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Authorization",
                "X-Api-Key",
                "Cookie",
                "Set-Cookie",
                "X-CSRF-Token"
            };

            // Paths that should not be audited
            _excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "/health",
                "/metrics",
                "/swagger",
                "/api-docs",
                "/favicon.ico"
            };

            // Operations that require detailed auditing
            _auditedOperations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "POST:/api/auth/login",
                "POST:/api/auth/logout",
                "POST:/api/auth/register",
                "POST:/api/auth/change-password",
                "POST:/api/auth/reset-password",
                "POST:/api/auth/mfa/enable",
                "POST:/api/auth/mfa/disable",
                "DELETE:",  // All DELETE operations
                "PUT:",     // All PUT operations
                "PATCH:"    // All PATCH operations
            };

            // Load custom configuration
            var customExcludedPaths = configuration.GetSection("AuditLogging:ExcludedPaths").Get<string[]>();
            if (customExcludedPaths != null)
            {
                foreach (var path in customExcludedPaths)
                {
                    _excludedPaths.Add(path);
                }
            }

            var customAuditedOperations = configuration.GetSection("AuditLogging:AuditedOperations").Get<string[]>();
            if (customAuditedOperations != null)
            {
                foreach (var operation in customAuditedOperations)
                {
                    _auditedOperations.Add(operation);
                }
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_enabled || ShouldSkipAuditing(context))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var auditLog = new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                RequestId = context.Request.Headers["X-Request-Id"].ToString() ?? Guid.NewGuid().ToString()
            };

            try
            {
                // Capture request details
                await CaptureRequestDetailsAsync(context, auditLog);

                // Buffer response body if needed
                var originalBodyStream = context.Response.Body;
                using var responseBody = new MemoryStream();
                
                if (ShouldCaptureResponseBody(context))
                {
                    context.Response.Body = responseBody;
                }

                // Execute the request
                await _next(context);

                // Capture response details
                if (ShouldCaptureResponseBody(context))
                {
                    await CaptureResponseDetailsAsync(context, auditLog, responseBody);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
                else
                {
                    CaptureResponseStatus(context, auditLog);
                }
            }
            catch (Exception ex)
            {
                auditLog.Error = new AuditErrorInfo
                {
                    Type = ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = _configuration.GetValue<bool>("AuditLogging:IncludeStackTrace", false) 
                        ? ex.StackTrace 
                        : null
                };
                
                throw;
            }
            finally
            {
                stopwatch.Stop();
                auditLog.Duration = stopwatch.ElapsedMilliseconds;
                auditLog.CompletedAt = DateTime.UtcNow;

                // Log the audit entry
                await LogAuditEntryAsync(auditLog);

                // Check for security events
                await CheckSecurityEventsAsync(context, auditLog);
            }
        }

        private async Task CaptureRequestDetailsAsync(HttpContext context, AuditLogEntry auditLog)
        {
            var request = context.Request;

            auditLog.Request = new AuditRequestInfo
            {
                Method = request.Method,
                Path = request.Path.Value,
                QueryString = request.QueryString.Value,
                Scheme = request.Scheme,
                Host = request.Host.Value,
                ContentType = request.ContentType,
                ContentLength = request.ContentLength
            };

            // Capture user information
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                auditLog.User = new AuditUserInfo
                {
                    Id = GetUserId(context),
                    Username = context.User.Identity.Name,
                    Roles = context.User.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToArray(),
                    IsAuthenticated = true
                };
            }
            else
            {
                auditLog.User = new AuditUserInfo
                {
                    IsAuthenticated = false
                };
            }

            // Capture client information
            auditLog.Client = new AuditClientInfo
            {
                IpAddress = GetClientIpAddress(context),
                UserAgent = request.Headers["User-Agent"].ToString(),
                Referer = request.Headers["Referer"].ToString(),
                Language = request.Headers["Accept-Language"].ToString()
            };

            // Capture headers (excluding sensitive ones)
            auditLog.Request.Headers = new Dictionary<string, string>();
            foreach (var header in request.Headers)
            {
                if (!_sensitiveHeaders.Contains(header.Key))
                {
                    auditLog.Request.Headers[header.Key] = header.Value.ToString();
                }
            }

            // Capture request body for certain operations
            if (ShouldCaptureRequestBody(context))
            {
                request.EnableBuffering();
                
                if (request.Body.CanSeek)
                {
                    request.Body.Position = 0;
                }

                using var reader = new StreamReader(
                    request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);

                var body = await reader.ReadToEndAsync();
                
                if (!string.IsNullOrEmpty(body) && body.Length <= _maxRequestBodySize)
                {
                    auditLog.Request.Body = SanitizeRequestBody(context, body);
                }
                else if (!string.IsNullOrEmpty(body))
                {
                    auditLog.Request.Body = $"[Body truncated - size: {body.Length} bytes]";
                }

                request.Body.Position = 0;
            }
        }

        private async Task CaptureResponseDetailsAsync(HttpContext context, AuditLogEntry auditLog, MemoryStream responseBody)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

            auditLog.Response = new AuditResponseInfo
            {
                StatusCode = context.Response.StatusCode,
                ContentType = context.Response.ContentType,
                ContentLength = context.Response.ContentLength ?? responseBody.Length
            };

            if (!string.IsNullOrEmpty(responseText) && responseText.Length <= _maxResponseBodySize)
            {
                auditLog.Response.Body = responseText;
            }
            else if (!string.IsNullOrEmpty(responseText))
            {
                auditLog.Response.Body = $"[Body truncated - size: {responseText.Length} bytes]";
            }

            // Capture response headers
            auditLog.Response.Headers = new Dictionary<string, string>();
            foreach (var header in context.Response.Headers)
            {
                if (!_sensitiveHeaders.Contains(header.Key))
                {
                    auditLog.Response.Headers[header.Key] = header.Value.ToString();
                }
            }
        }

        private void CaptureResponseStatus(HttpContext context, AuditLogEntry auditLog)
        {
            auditLog.Response = new AuditResponseInfo
            {
                StatusCode = context.Response.StatusCode,
                ContentType = context.Response.ContentType,
                ContentLength = context.Response.ContentLength
            };
        }

        private async Task LogAuditEntryAsync(AuditLogEntry auditLog)
        {
            // Determine log level based on response status
            var logLevel = auditLog.Response?.StatusCode switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ => LogLevel.Information
            };

            // Log to structured logging
            _logger.Log(logLevel, "Audit: {Method} {Path} - Status: {StatusCode} - User: {UserId} - Duration: {Duration}ms",
                auditLog.Request.Method,
                auditLog.Request.Path,
                auditLog.Response?.StatusCode ?? 0,
                auditLog.User?.Id ?? "anonymous",
                auditLog.Duration);

            // Store detailed audit log
            var auditService = _logger as IServiceProvider;
            if (auditService != null)
            {
                var auditStore = auditService.GetService<IAuditLogStore>();
                if (auditStore != null)
                {
                    await auditStore.StoreAuditLogAsync(auditLog);
                }
            }

            // Write to audit file if configured
            if (_configuration.GetValue<bool>("AuditLogging:WriteToFile", false))
            {
                await WriteAuditLogToFileAsync(auditLog);
            }
        }

        private async Task WriteAuditLogToFileAsync(AuditLogEntry auditLog)
        {
            try
            {
                var logDirectory = _configuration["AuditLogging:LogDirectory"] ?? "logs/audit";
                Directory.CreateDirectory(logDirectory);

                var fileName = $"audit-{DateTime.UtcNow:yyyy-MM-dd}.json";
                var filePath = Path.Combine(logDirectory, fileName);

                var json = JsonSerializer.Serialize(auditLog, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                await File.AppendAllTextAsync(filePath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log to file");
            }
        }

        private async Task CheckSecurityEventsAsync(HttpContext context, AuditLogEntry auditLog)
        {
            var securityEvents = new List<string>();

            // Check for authentication failures
            if (auditLog.Request.Path.Contains("/auth/login") && auditLog.Response?.StatusCode == 401)
            {
                securityEvents.Add("LOGIN_FAILURE");
            }

            // Check for authorization failures
            if (auditLog.Response?.StatusCode == 403)
            {
                securityEvents.Add("AUTHORIZATION_FAILURE");
            }

            // Check for rate limiting
            if (auditLog.Response?.StatusCode == 429)
            {
                securityEvents.Add("RATE_LIMIT_EXCEEDED");
            }

            // Check for suspicious patterns
            if (IsSuspiciousRequest(context, auditLog))
            {
                securityEvents.Add("SUSPICIOUS_REQUEST");
            }

            // Log security events
            if (securityEvents.Any())
            {
                auditLog.SecurityEvents = securityEvents.ToArray();
                
                _logger.LogWarning("Security events detected: {Events} - User: {UserId} - IP: {IpAddress}",
                    string.Join(", ", securityEvents),
                    auditLog.User?.Id ?? "anonymous",
                    auditLog.Client?.IpAddress);

                // Notify security monitoring
                var securityService = context.RequestServices.GetService<ISecurityMonitoringService>();
                if (securityService != null)
                {
                    await securityService.ReportSecurityEventsAsync(auditLog);
                }
            }
        }

        private bool IsSuspiciousRequest(HttpContext context, AuditLogEntry auditLog)
        {
            // Check for SQL injection patterns
            var suspiciousPatterns = new[]
            {
                "' OR '1'='1",
                "DROP TABLE",
                "SELECT * FROM",
                "<script>",
                "javascript:",
                "../../../",
                "cmd.exe",
                "/bin/bash"
            };

            var requestContent = $"{auditLog.Request.Path} {auditLog.Request.QueryString} {auditLog.Request.Body}";
            
            return suspiciousPatterns.Any(pattern => 
                requestContent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private bool ShouldSkipAuditing(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            
            return _excludedPaths.Any(excludedPath => 
                path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
        }

        private bool ShouldCaptureRequestBody(HttpContext context)
        {
            // Don't capture for GET requests
            if (context.Request.Method == "GET")
                return false;

            // Don't capture for large requests
            if (context.Request.ContentLength > _maxRequestBodySize)
                return false;

            // Check if this is an audited operation
            var operation = $"{context.Request.Method}:{context.Request.Path}";
            
            return _auditedOperations.Any(op => 
                operation.StartsWith(op, StringComparison.OrdinalIgnoreCase));
        }

        private bool ShouldCaptureResponseBody(HttpContext context)
        {
            // Only capture for specific status codes or audited operations
            return context.Response.StatusCode >= 400 || 
                   ShouldCaptureRequestBody(context);
        }

        private string SanitizeRequestBody(HttpContext context, string body)
        {
            try
            {
                // Parse and sanitize JSON bodies
                if (context.Request.ContentType?.Contains("application/json") == true)
                {
                    var json = JsonDocument.Parse(body);
                    var sanitized = SanitizeJsonElement(json.RootElement);
                    
                    return JsonSerializer.Serialize(sanitized, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }
            }
            catch
            {
                // If parsing fails, return truncated body
                return body.Length > 500 ? body.Substring(0, 500) + "..." : body;
            }

            return body;
        }

        private object SanitizeJsonElement(JsonElement element)
        {
            var sensitiveFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password",
                "confirmPassword",
                "currentPassword",
                "newPassword",
                "secret",
                "token",
                "apiKey",
                "privateKey",
                "creditCard",
                "ssn",
                "socialSecurityNumber"
            };

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        if (sensitiveFields.Contains(property.Name))
                        {
                            obj[property.Name] = "[REDACTED]";
                        }
                        else
                        {
                            obj[property.Name] = SanitizeJsonElement(property.Value);
                        }
                    }
                    return obj;

                case JsonValueKind.Array:
                    return element.EnumerateArray().Select(SanitizeJsonElement).ToArray();

                default:
                    return element.ToString();
            }
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    return ips[0].Trim();
                }
            }

            var realIp = context.Request.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private string GetUserId(HttpContext context)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirst("sub")
                ?? context.User.FindFirst("user_id");

            return userIdClaim?.Value;
        }
    }

    /// <summary>
    /// Audit log entry model
    /// </summary>
    public class AuditLogEntry
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime CompletedAt { get; set; }
        public long Duration { get; set; }
        public string TraceId { get; set; }
        public string RequestId { get; set; }
        public AuditUserInfo User { get; set; }
        public AuditClientInfo Client { get; set; }
        public AuditRequestInfo Request { get; set; }
        public AuditResponseInfo Response { get; set; }
        public AuditErrorInfo Error { get; set; }
        public string[] SecurityEvents { get; set; }
    }

    public class AuditUserInfo
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string[] Roles { get; set; }
        public bool IsAuthenticated { get; set; }
    }

    public class AuditClientInfo
    {
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Referer { get; set; }
        public string Language { get; set; }
    }

    public class AuditRequestInfo
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string Scheme { get; set; }
        public string Host { get; set; }
        public string ContentType { get; set; }
        public long? ContentLength { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
    }

    public class AuditResponseInfo
    {
        public int StatusCode { get; set; }
        public string ContentType { get; set; }
        public long? ContentLength { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
    }

    public class AuditErrorInfo
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }

    /// <summary>
    /// Interface for audit log storage
    /// </summary>
    public interface IAuditLogStore
    {
        Task StoreAuditLogAsync(AuditLogEntry entry);
        Task<IEnumerable<AuditLogEntry>> GetAuditLogsAsync(DateTime from, DateTime to, string userId = null);
    }

    /// <summary>
    /// Interface for security monitoring
    /// </summary>
    public interface ISecurityMonitoringService
    {
        Task ReportSecurityEventsAsync(AuditLogEntry auditLog);
    }

    /// <summary>
    /// Extension methods for audit logging middleware
    /// </summary>
    public static class AuditLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuditLoggingMiddleware>();
        }
    }
}