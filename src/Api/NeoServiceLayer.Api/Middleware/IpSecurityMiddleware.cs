using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Api.Middleware
{
    /// <summary>
    /// Middleware for IP-based security features including filtering, geolocation, and threat detection
    /// </summary>
    public class IpSecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IpSecurityMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly IpSecurityOptions _options;
        private readonly HashSet<string> _whitelist;
        private readonly HashSet<string> _blacklist;
        private readonly HashSet<string> _trustedProxies;
        private readonly Dictionary<string, List<string>> _countryRestrictions;

        public IpSecurityMiddleware(
            RequestDelegate next,
            ILogger<IpSecurityMiddleware> logger,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            _cache = cache;

            _options = configuration.GetSection("IpSecurity").Get<IpSecurityOptions>() 
                ?? new IpSecurityOptions();

            // Initialize IP lists
            _whitelist = new HashSet<string>(_options.Whitelist ?? Array.Empty<string>());
            _blacklist = new HashSet<string>(_options.Blacklist ?? Array.Empty<string>());
            _trustedProxies = new HashSet<string>(_options.TrustedProxies ?? Array.Empty<string>());

            // Initialize country restrictions
            _countryRestrictions = new Dictionary<string, List<string>>();
            if (_options.CountryRestrictions != null)
            {
                foreach (var restriction in _options.CountryRestrictions)
                {
                    if (!_countryRestrictions.ContainsKey(restriction.Path))
                    {
                        _countryRestrictions[restriction.Path] = new List<string>();
                    }
                    _countryRestrictions[restriction.Path].AddRange(restriction.AllowedCountries);
                }
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_options.Enabled)
            {
                await _next(context);
                return;
            }

            var clientIp = GetTrueClientIp(context);
            var ipInfo = await GetIpInfoAsync(clientIp);

            // Store IP info in context for downstream use
            context.Items["ClientIp"] = clientIp;
            context.Items["IpInfo"] = ipInfo;

            // Check whitelist first
            if (IsWhitelisted(clientIp))
            {
                _logger.LogDebug("IP {ClientIp} is whitelisted", clientIp);
                await _next(context);
                return;
            }

            // Check blacklist
            if (IsBlacklisted(clientIp))
            {
                _logger.LogWarning("Blocked request from blacklisted IP: {ClientIp}", clientIp);
                await HandleBlockedRequestAsync(context, "Access denied from this IP address");
                return;
            }

            // Check dynamic blacklist (from threat detection)
            if (await IsDynamicallyBlacklistedAsync(clientIp))
            {
                _logger.LogWarning("Blocked request from dynamically blacklisted IP: {ClientIp}", clientIp);
                await HandleBlockedRequestAsync(context, "Access temporarily restricted");
                return;
            }

            // Check for known threat IPs
            if (_options.EnableThreatDetection && await IsThreatIpAsync(clientIp))
            {
                _logger.LogWarning("Blocked request from threat IP: {ClientIp}", clientIp);
                await HandleBlockedRequestAsync(context, "Access denied - security threat detected");
                return;
            }

            // Check country restrictions
            if (_options.EnableGeoBlocking && !await IsCountryAllowedAsync(context, ipInfo))
            {
                _logger.LogWarning("Blocked request from restricted country: {ClientIp} ({Country})", 
                    clientIp, ipInfo?.Country);
                await HandleBlockedRequestAsync(context, "Access not available in your region");
                return;
            }

            // Check for suspicious patterns
            if (_options.EnableAnomalyDetection && await IsSuspiciousActivityAsync(context, clientIp))
            {
                _logger.LogWarning("Suspicious activity detected from IP: {ClientIp}", clientIp);
                
                if (_options.BlockSuspiciousActivity)
                {
                    await HandleBlockedRequestAsync(context, "Suspicious activity detected");
                    return;
                }
            }

            // Track IP activity
            await TrackIpActivityAsync(context, clientIp, ipInfo);

            // Add security headers based on IP reputation
            AddIpBasedSecurityHeaders(context, ipInfo);

            await _next(context);
        }

        private string GetTrueClientIp(HttpContext context)
        {
            // Handle various proxy headers
            var headers = new[]
            {
                "CF-Connecting-IP",     // Cloudflare
                "True-Client-IP",       // Akamai
                "X-Real-IP",           // Nginx
                "X-Forwarded-For",     // Standard
                "X-Client-IP"          // Alternative
            };

            foreach (var header in headers)
            {
                var value = context.Request.Headers[header].ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    // X-Forwarded-For can contain multiple IPs
                    if (header == "X-Forwarded-For")
                    {
                        var ips = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(ip => ip.Trim())
                            .ToList();

                        // Get the first non-proxy IP
                        foreach (var ip in ips)
                        {
                            if (!IsPrivateIp(ip) && !_trustedProxies.Contains(ip))
                            {
                                return ip;
                            }
                        }

                        // If all are proxies, return the first one
                        if (ips.Count > 0)
                        {
                            return ips[0];
                        }
                    }
                    else
                    {
                        return value;
                    }
                }
            }

            // Fallback to remote IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool IsPrivateIp(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out var ip))
                return false;

            // Check for private IP ranges
            var bytes = ip.GetAddressBytes();
            
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // 10.0.0.0/8
                if (bytes[0] == 10)
                    return true;

                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    return true;

                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168)
                    return true;

                // 127.0.0.0/8 (loopback)
                if (bytes[0] == 127)
                    return true;
            }

            return false;
        }

        private async Task<IpInfo> GetIpInfoAsync(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "unknown")
                return null;

            // Check cache first
            var cacheKey = $"ipinfo:{ipAddress}";
            if (_cache.TryGetValue<IpInfo>(cacheKey, out var cachedInfo))
            {
                return cachedInfo;
            }

            try
            {
                // In a real implementation, you would call a geolocation API
                // For now, we'll return mock data
                var ipInfo = new IpInfo
                {
                    IpAddress = ipAddress,
                    Country = "US",
                    CountryCode = "US",
                    Region = "California",
                    City = "San Francisco",
                    Latitude = 37.7749,
                    Longitude = -122.4194,
                    Isp = "Example ISP",
                    Organization = "Example Org",
                    IsVpn = false,
                    IsProxy = false,
                    IsTor = false,
                    IsHosting = false,
                    ThreatLevel = ThreatLevel.Low,
                    ReputationScore = 85
                };

                // Cache for 24 hours
                _cache.Set(cacheKey, ipInfo, TimeSpan.FromHours(24));

                return ipInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get IP info for {IpAddress}", ipAddress);
                return null;
            }
        }

        private bool IsWhitelisted(string ipAddress)
        {
            if (_whitelist.Contains(ipAddress))
                return true;

            // Check CIDR ranges
            foreach (var entry in _whitelist)
            {
                if (entry.Contains('/') && IsIpInCidrRange(ipAddress, entry))
                    return true;
            }

            return false;
        }

        private bool IsBlacklisted(string ipAddress)
        {
            if (_blacklist.Contains(ipAddress))
                return true;

            // Check CIDR ranges
            foreach (var entry in _blacklist)
            {
                if (entry.Contains('/') && IsIpInCidrRange(ipAddress, entry))
                    return true;
            }

            return false;
        }

        private bool IsIpInCidrRange(string ipAddress, string cidrRange)
        {
            try
            {
                var parts = cidrRange.Split('/');
                if (parts.Length != 2)
                    return false;

                if (!IPAddress.TryParse(ipAddress, out var ip))
                    return false;

                if (!IPAddress.TryParse(parts[0], out var rangeIp))
                    return false;

                if (!int.TryParse(parts[1], out var maskBits))
                    return false;

                var ipBytes = ip.GetAddressBytes();
                var rangeBytes = rangeIp.GetAddressBytes();

                if (ipBytes.Length != rangeBytes.Length)
                    return false;

                var bytesToCheck = maskBits / 8;
                var bitsToCheck = maskBits % 8;

                for (int i = 0; i < bytesToCheck; i++)
                {
                    if (ipBytes[i] != rangeBytes[i])
                        return false;
                }

                if (bitsToCheck > 0 && bytesToCheck < ipBytes.Length)
                {
                    var mask = (byte)(0xFF << (8 - bitsToCheck));
                    if ((ipBytes[bytesToCheck] & mask) != (rangeBytes[bytesToCheck] & mask))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> IsDynamicallyBlacklistedAsync(string ipAddress)
        {
            var cacheKey = $"dynamic_blacklist:{ipAddress}";
            return _cache.TryGetValue<bool>(cacheKey, out _);
        }

        private async Task<bool> IsThreatIpAsync(string ipAddress)
        {
            // Check against threat intelligence feeds
            // In a real implementation, this would query threat databases
            
            var ipInfo = await GetIpInfoAsync(ipAddress);
            if (ipInfo == null)
                return false;

            // Check various threat indicators
            if (ipInfo.IsTor && _options.BlockTor)
                return true;

            if (ipInfo.IsVpn && _options.BlockVpn)
                return true;

            if (ipInfo.IsProxy && _options.BlockProxy)
                return true;

            if (ipInfo.IsHosting && _options.BlockHostingProviders)
                return true;

            if (ipInfo.ThreatLevel >= ThreatLevel.High)
                return true;

            if (ipInfo.ReputationScore < _options.MinimumReputationScore)
                return true;

            return false;
        }

        private async Task<bool> IsCountryAllowedAsync(HttpContext context, IpInfo ipInfo)
        {
            if (ipInfo == null || string.IsNullOrEmpty(ipInfo.CountryCode))
                return _options.AllowUnknownCountries;

            // Check global country restrictions
            if (_options.BlockedCountries?.Contains(ipInfo.CountryCode) == true)
                return false;

            if (_options.AllowedCountries?.Length > 0 && 
                !_options.AllowedCountries.Contains(ipInfo.CountryCode))
                return false;

            // Check path-specific country restrictions
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            foreach (var restriction in _countryRestrictions)
            {
                if (path.StartsWith(restriction.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return restriction.Value.Contains(ipInfo.CountryCode);
                }
            }

            return true;
        }

        private async Task<bool> IsSuspiciousActivityAsync(HttpContext context, string ipAddress)
        {
            var cacheKey = $"ip_activity:{ipAddress}";
            var activity = _cache.Get<IpActivity>(cacheKey) ?? new IpActivity();

            // Check for various suspicious patterns
            
            // 1. Too many requests in a short time
            if (activity.RequestCount > _options.MaxRequestsPerMinute)
                return true;

            // 2. Too many different user agents
            if (activity.UserAgents.Count > 5)
                return true;

            // 3. Too many authentication failures
            if (activity.FailedAuthAttempts > 3)
                return true;

            // 4. Accessing honeypot URLs
            var honeypotPaths = new[] { "/admin.php", "/wp-admin", "/.env", "/phpmyadmin" };
            if (honeypotPaths.Any(p => context.Request.Path.Value?.Contains(p) == true))
            {
                _logger.LogWarning("Honeypot access detected from IP: {IpAddress}", ipAddress);
                return true;
            }

            // 5. Suspicious user agent
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var suspiciousAgents = new[] { "bot", "crawler", "spider", "scraper", "curl", "wget" };
            if (suspiciousAgents.Any(a => userAgent.Contains(a, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private async Task TrackIpActivityAsync(HttpContext context, string ipAddress, IpInfo ipInfo)
        {
            var cacheKey = $"ip_activity:{ipAddress}";
            var activity = _cache.Get<IpActivity>(cacheKey) ?? new IpActivity
            {
                FirstSeen = DateTime.UtcNow,
                IpAddress = ipAddress
            };

            activity.LastSeen = DateTime.UtcNow;
            activity.RequestCount++;
            activity.UserAgents.Add(context.Request.Headers["User-Agent"].ToString());
            
            if (context.Request.Path.Value?.Contains("/auth/login") == true)
            {
                activity.LoginAttempts++;
            }

            // Track the activity for 1 hour
            _cache.Set(cacheKey, activity, TimeSpan.FromHours(1));

            // Log significant events
            if (activity.RequestCount == 100)
            {
                _logger.LogInformation("IP {IpAddress} has made 100 requests", ipAddress);
            }

            // Update metrics
            var metrics = context.RequestServices.GetService<IMetricsService>();
            if (metrics != null)
            {
                await metrics.RecordIpActivityAsync(ipAddress, ipInfo);
            }
        }

        private void AddIpBasedSecurityHeaders(HttpContext context, IpInfo ipInfo)
        {
            if (ipInfo == null)
                return;

            // Add custom headers based on IP reputation
            context.Response.Headers["X-IP-Country"] = ipInfo.CountryCode;
            context.Response.Headers["X-IP-Reputation"] = ipInfo.ReputationScore.ToString();

            // Stricter security for suspicious IPs
            if (ipInfo.ThreatLevel >= ThreatLevel.Medium || ipInfo.ReputationScore < 50)
            {
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["Cache-Control"] = "no-store";
            }
        }

        private async Task HandleBlockedRequestAsync(HttpContext context, string message)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "access_denied",
                message = message,
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);

            // Log the blocked request
            var clientIp = context.Items["ClientIp"]?.ToString() ?? "unknown";
            _logger.LogWarning("Blocked request from IP {ClientIp}: {Message}", clientIp, message);
        }

        public async Task AddToBlacklistAsync(string ipAddress, TimeSpan duration, string reason)
        {
            var cacheKey = $"dynamic_blacklist:{ipAddress}";
            _cache.Set(cacheKey, true, duration);

            _logger.LogWarning("IP {IpAddress} added to dynamic blacklist for {Duration}: {Reason}",
                ipAddress, duration, reason);
        }

        public async Task RemoveFromBlacklistAsync(string ipAddress)
        {
            var cacheKey = $"dynamic_blacklist:{ipAddress}";
            _cache.Remove(cacheKey);

            _logger.LogInformation("IP {IpAddress} removed from dynamic blacklist", ipAddress);
        }
    }

    /// <summary>
    /// IP security configuration options
    /// </summary>
    public class IpSecurityOptions
    {
        public bool Enabled { get; set; } = true;
        public string[] Whitelist { get; set; } = Array.Empty<string>();
        public string[] Blacklist { get; set; } = Array.Empty<string>();
        public string[] TrustedProxies { get; set; } = Array.Empty<string>();
        
        // Geo-blocking
        public bool EnableGeoBlocking { get; set; } = false;
        public string[] AllowedCountries { get; set; }
        public string[] BlockedCountries { get; set; }
        public bool AllowUnknownCountries { get; set; } = true;
        public CountryRestriction[] CountryRestrictions { get; set; }

        // Threat detection
        public bool EnableThreatDetection { get; set; } = true;
        public bool BlockTor { get; set; } = false;
        public bool BlockVpn { get; set; } = false;
        public bool BlockProxy { get; set; } = false;
        public bool BlockHostingProviders { get; set; } = false;
        public int MinimumReputationScore { get; set; } = 30;

        // Anomaly detection
        public bool EnableAnomalyDetection { get; set; } = true;
        public bool BlockSuspiciousActivity { get; set; } = false;
        public int MaxRequestsPerMinute { get; set; } = 60;
    }

    public class CountryRestriction
    {
        public string Path { get; set; }
        public string[] AllowedCountries { get; set; }
    }

    /// <summary>
    /// IP information model
    /// </summary>
    public class IpInfo
    {
        public string IpAddress { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Isp { get; set; }
        public string Organization { get; set; }
        public bool IsVpn { get; set; }
        public bool IsProxy { get; set; }
        public bool IsTor { get; set; }
        public bool IsHosting { get; set; }
        public ThreatLevel ThreatLevel { get; set; }
        public int ReputationScore { get; set; }
    }

    public enum ThreatLevel
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// IP activity tracking model
    /// </summary>
    public class IpActivity
    {
        public string IpAddress { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public int RequestCount { get; set; }
        public int LoginAttempts { get; set; }
        public int FailedAuthAttempts { get; set; }
        public HashSet<string> UserAgents { get; set; } = new HashSet<string>();
        public HashSet<string> AccessedPaths { get; set; } = new HashSet<string>();
    }

    /// <summary>
    /// Interface for metrics service
    /// </summary>
    public interface IMetricsService
    {
        Task RecordIpActivityAsync(string ipAddress, IpInfo ipInfo);
    }

    /// <summary>
    /// Extension methods for IP security middleware
    /// </summary>
    public static class IpSecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseIpSecurity(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IpSecurityMiddleware>();
        }
    }
}