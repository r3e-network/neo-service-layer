using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Gateway
{
    /// <summary>
    /// API Gateway service for Neo Service Layer
    /// Provides request routing, load balancing, authentication, and API management
    /// </summary>
    public interface IApiGatewayService
    {
        /// <summary>
        /// Registers a service endpoint with the gateway
        /// </summary>
        /// <param name="serviceEndpoint">Service endpoint configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service registration result</returns>
        Task<ServiceRegistrationResult> RegisterServiceAsync(
            ServiceEndpoint serviceEndpoint,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Unregisters a service endpoint from the gateway
        /// </summary>
        /// <param name="serviceName">Service name</param>
        /// <param name="version">Service version (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service unregistration result</returns>
        Task<ServiceRegistrationResult> UnregisterServiceAsync(
            string serviceName,
            string? version = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an API route configuration
        /// </summary>
        /// <param name="routeConfig">Route configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Route creation result</returns>
        Task<RouteResult> CreateRouteAsync(
            RouteConfiguration routeConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing API route
        /// </summary>
        /// <param name="routeId">Route identifier</param>
        /// <param name="routeConfig">Updated route configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Route update result</returns>
        Task<RouteResult> UpdateRouteAsync(
            string routeId,
            RouteConfiguration routeConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an API route
        /// </summary>
        /// <param name="routeId">Route identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Route deletion result</returns>
        Task<RouteResult> DeleteRouteAsync(
            string routeId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all registered routes
        /// </summary>
        /// <param name="filters">Optional filters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of routes</returns>
        Task<RouteListResult> GetRoutesAsync(
            RouteFilters? filters = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes an incoming API request through the gateway
        /// </summary>
        /// <param name="request">API request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>API response</returns>
        Task<ApiResponse> ProcessRequestAsync(
            ApiRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an API key for client authentication
        /// </summary>
        /// <param name="apiKeyRequest">API key creation request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>API key creation result</returns>
        Task<ApiKeyResult> CreateApiKeyAsync(
            ApiKeyRequest apiKeyRequest,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes an API key
        /// </summary>
        /// <param name="apiKeyId">API key identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>API key revocation result</returns>
        Task<ApiKeyResult> RevokeApiKeyAsync(
            string apiKeyId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists API keys with their metadata
        /// </summary>
        /// <param name="filters">Optional filters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of API keys</returns>
        Task<ApiKeyListResult> ListApiKeysAsync(
            ApiKeyFilters? filters = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a rate limiting policy
        /// </summary>
        /// <param name="rateLimitPolicy">Rate limiting policy</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rate limit policy creation result</returns>
        Task<RateLimitResult> CreateRateLimitPolicyAsync(
            RateLimitPolicy rateLimitPolicy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a rate limiting policy
        /// </summary>
        /// <param name="policyId">Policy identifier</param>
        /// <param name="rateLimitPolicy">Updated policy</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rate limit policy update result</returns>
        Task<RateLimitResult> UpdateRateLimitPolicyAsync(
            string policyId,
            RateLimitPolicy rateLimitPolicy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a rate limiting policy
        /// </summary>
        /// <param name="policyId">Policy identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rate limit policy deletion result</returns>
        Task<RateLimitResult> DeleteRateLimitPolicyAsync(
            string policyId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures load balancing for a service
        /// </summary>
        /// <param name="serviceName">Service name</param>
        /// <param name="loadBalancingConfig">Load balancing configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Load balancing configuration result</returns>
        Task<LoadBalancingResult> ConfigureLoadBalancingAsync(
            string serviceName,
            LoadBalancingConfiguration loadBalancingConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets service health and status information
        /// </summary>
        /// <param name="serviceName">Service name (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service health information</returns>
        Task<ServiceHealthResult> GetServiceHealthAsync(
            string? serviceName = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures request transformation rules
        /// </summary>
        /// <param name="transformationConfig">Transformation configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Transformation configuration result</returns>
        Task<TransformationResult> ConfigureRequestTransformationAsync(
            RequestTransformationConfiguration transformationConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures response transformation rules
        /// </summary>
        /// <param name="transformationConfig">Transformation configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Transformation configuration result</returns>
        Task<TransformationResult> ConfigureResponseTransformationAsync(
            ResponseTransformationConfiguration transformationConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets API gateway analytics and metrics
        /// </summary>
        /// <param name="analyticsQuery">Analytics query parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Analytics results</returns>
        Task<GatewayAnalytics> GetAnalyticsAsync(
            AnalyticsQuery analyticsQuery,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures API versioning strategy
        /// </summary>
        /// <param name="versioningConfig">Versioning configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Versioning configuration result</returns>
        Task<VersioningResult> ConfigureVersioningAsync(
            ApiVersioningConfiguration versioningConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures CORS (Cross-Origin Resource Sharing) policies
        /// </summary>
        /// <param name="corsConfig">CORS configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>CORS configuration result</returns>
        Task<CorsResult> ConfigureCorsAsync(
            CorsConfiguration corsConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets gateway service statistics and health
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Gateway health information</returns>
        Task<GatewayHealth> GetHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports gateway configuration
        /// </summary>
        /// <param name="exportOptions">Export options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Configuration export result</returns>
        Task<ConfigurationExportResult> ExportConfigurationAsync(
            ConfigurationExportOptions exportOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Imports gateway configuration
        /// </summary>
        /// <param name="importOptions">Import options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Configuration import result</returns>
        Task<ConfigurationImportResult> ImportConfigurationAsync(
            ConfigurationImportOptions importOptions,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Service endpoint configuration
    /// </summary>
    public class ServiceEndpoint
    {
        /// <summary>
        /// Service name
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Service version
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Base URL of the service
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Service instances/endpoints
        /// </summary>
        public List<ServiceInstance> Instances { get; set; } = new();

        /// <summary>
        /// Health check configuration
        /// </summary>
        public HealthCheckConfiguration? HealthCheck { get; set; }

        /// <summary>
        /// Service metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Service tags for categorization
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Authentication requirements
        /// </summary>
        public AuthenticationConfiguration? Authentication { get; set; }

        /// <summary>
        /// Default rate limiting for this service
        /// </summary>
        public RateLimitConfiguration? DefaultRateLimit { get; set; }

        /// <summary>
        /// Circuit breaker configuration
        /// </summary>
        public CircuitBreakerConfiguration? CircuitBreaker { get; set; }
    }

    /// <summary>
    /// Service instance configuration
    /// </summary>
    public class ServiceInstance
    {
        /// <summary>
        /// Instance identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Instance URL
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Instance weight for load balancing
        /// </summary>
        public int Weight { get; set; } = 100;

        /// <summary>
        /// Whether the instance is healthy
        /// </summary>
        public bool IsHealthy { get; set; } = true;

        /// <summary>
        /// Instance metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Route configuration
    /// </summary>
    public class RouteConfiguration
    {
        /// <summary>
        /// Route identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Route name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Route pattern (e.g., "/api/v1/users/{id}")
        /// </summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// HTTP methods allowed for this route
        /// </summary>
        public List<string> Methods { get; set; } = new();

        /// <summary>
        /// Target service name
        /// </summary>
        public string TargetService { get; set; } = string.Empty;

        /// <summary>
        /// Target service version (optional)
        /// </summary>
        public string? TargetVersion { get; set; }

        /// <summary>
        /// Route priority (higher number = higher priority)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Route timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Authentication requirements for this route
        /// </summary>
        public AuthenticationConfiguration? Authentication { get; set; }

        /// <summary>
        /// Rate limiting configuration for this route
        /// </summary>
        public RateLimitConfiguration? RateLimit { get; set; }

        /// <summary>
        /// Request transformation rules
        /// </summary>
        public List<TransformationRule> RequestTransformations { get; set; } = new();

        /// <summary>
        /// Response transformation rules
        /// </summary>
        public List<TransformationRule> ResponseTransformations { get; set; } = new();

        /// <summary>
        /// Middleware pipeline for this route
        /// </summary>
        public List<MiddlewareConfiguration> Middleware { get; set; } = new();

        /// <summary>
        /// Route metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Whether the route is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// API request representation
    /// </summary>
    public class ApiRequest
    {
        /// <summary>
        /// Request identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// HTTP method
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Request path
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Query parameters
        /// </summary>
        public Dictionary<string, string[]> Query { get; set; } = new();

        /// <summary>
        /// Request headers
        /// </summary>
        public Dictionary<string, string[]> Headers { get; set; } = new();

        /// <summary>
        /// Request body
        /// </summary>
        public byte[]? Body { get; set; }

        /// <summary>
        /// Content type
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Client IP address
        /// </summary>
        public string? ClientIP { get; set; }

        /// <summary>
        /// User agent
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Authentication context
        /// </summary>
        public AuthenticationContext? Authentication { get; set; }

        /// <summary>
        /// Request timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Request metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// API response representation
    /// </summary>
    public class ApiResponse
    {
        /// <summary>
        /// Response status code
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Response headers
        /// </summary>
        public Dictionary<string, string[]> Headers { get; set; } = new();

        /// <summary>
        /// Response body
        /// </summary>
        public byte[]? Body { get; set; }

        /// <summary>
        /// Content type
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Response timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Processing time
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// Target service that processed the request
        /// </summary>
        public string? TargetService { get; set; }

        /// <summary>
        /// Response metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Whether the response was cached
        /// </summary>
        public bool IsCached { get; set; }

        /// <summary>
        /// Error information (if any)
        /// </summary>
        public ApiError? Error { get; set; }
    }

    /// <summary>
    /// API error information
    /// </summary>
    public class ApiError
    {
        /// <summary>
        /// Error code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Detailed error description
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Error timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Error metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// API key creation request
    /// </summary>
    public class ApiKeyRequest
    {
        /// <summary>
        /// API key name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// API key description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// API key scopes/permissions
        /// </summary>
        public List<string> Scopes { get; set; } = new();

        /// <summary>
        /// Expiration date (optional)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Rate limiting configuration for this API key
        /// </summary>
        public RateLimitConfiguration? RateLimit { get; set; }

        /// <summary>
        /// Allowed IP addresses (optional)
        /// </summary>
        public List<string> AllowedIPs { get; set; } = new();

        /// <summary>
        /// API key metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Rate limiting policy
    /// </summary>
    public class RateLimitPolicy
    {
        /// <summary>
        /// Policy identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Policy name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Rate limiting configuration
        /// </summary>
        public RateLimitConfiguration Configuration { get; set; } = new();

        /// <summary>
        /// Policy scope (global, service, route, user, etc.)
        /// </summary>
        public RateLimitScope Scope { get; set; }

        /// <summary>
        /// Target for the policy (service name, route pattern, etc.)
        /// </summary>
        public string? Target { get; set; }

        /// <summary>
        /// Whether the policy is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Policy priority (higher number = higher priority)
        /// </summary>
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public class RateLimitConfiguration
    {
        /// <summary>
        /// Maximum number of requests
        /// </summary>
        public int MaxRequests { get; set; }

        /// <summary>
        /// Time window for the rate limit
        /// </summary>
        public TimeSpan TimeWindow { get; set; }

        /// <summary>
        /// Rate limiting strategy
        /// </summary>
        public RateLimitStrategy Strategy { get; set; } = RateLimitStrategy.TokenBucket;

        /// <summary>
        /// Burst allowance
        /// </summary>
        public int? BurstAllowance { get; set; }

        /// <summary>
        /// Rate limiting key (IP, user, API key, etc.)
        /// </summary>
        public string LimitingKey { get; set; } = "IP";

        /// <summary>
        /// Custom headers to include in rate limit responses
        /// </summary>
        public Dictionary<string, string> ResponseHeaders { get; set; } = new();
    }

    /// <summary>
    /// Load balancing configuration
    /// </summary>
    public class LoadBalancingConfiguration
    {
        /// <summary>
        /// Load balancing strategy
        /// </summary>
        public LoadBalancingStrategy Strategy { get; set; } = LoadBalancingStrategy.RoundRobin;

        /// <summary>
        /// Health check configuration
        /// </summary>
        public HealthCheckConfiguration HealthCheck { get; set; } = new();

        /// <summary>
        /// Sticky sessions configuration
        /// </summary>
        public StickySessionConfiguration? StickySession { get; set; }

        /// <summary>
        /// Failover configuration
        /// </summary>
        public FailoverConfiguration Failover { get; set; } = new();

        /// <summary>
        /// Load balancing weights (for weighted strategies)
        /// </summary>
        public Dictionary<string, int> Weights { get; set; } = new();
    }

    /// <summary>
    /// Authentication configuration
    /// </summary>
    public class AuthenticationConfiguration
    {
        /// <summary>
        /// Authentication type
        /// </summary>
        public AuthenticationType Type { get; set; }

        /// <summary>
        /// Whether authentication is required
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// Authentication configuration parameters
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();

        /// <summary>
        /// JWT configuration (if using JWT)
        /// </summary>
        public JwtConfiguration? JwtConfiguration { get; set; }

        /// <summary>
        /// OAuth configuration (if using OAuth)
        /// </summary>
        public OAuthConfiguration? OAuthConfiguration { get; set; }

        /// <summary>
        /// API key header name (if using API key auth)
        /// </summary>
        public string? ApiKeyHeader { get; set; }
    }

    /// <summary>
    /// Various enums for API gateway configuration
    /// </summary>
    public enum RateLimitScope
    {
        Global,
        Service,
        Route,
        User,
        ApiKey,
        IP
    }

    public enum RateLimitStrategy
    {
        TokenBucket,
        FixedWindow,
        SlidingWindow,
        LeakyBucket
    }

    public enum LoadBalancingStrategy
    {
        RoundRobin,
        WeightedRoundRobin,
        LeastConnections,
        WeightedLeastConnections,
        Random,
        IPHash,
        HealthiestFirst
    }

    public enum AuthenticationType
    {
        None,
        ApiKey,
        JWT,
        OAuth,
        Basic,
        Custom
    }

    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy,
        Unknown
    }

    /// <summary>
    /// Supporting configuration classes
    /// </summary>
    public class HealthCheckConfiguration
    {
        /// <summary>
        /// Health check URL path
        /// </summary>
        public string Path { get; set; } = "/health";

        /// <summary>
        /// Health check interval
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Health check timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Number of consecutive failures before marking unhealthy
        /// </summary>
        public int FailureThreshold { get; set; } = 3;

        /// <summary>
        /// Number of consecutive successes before marking healthy
        /// </summary>
        public int SuccessThreshold { get; set; } = 2;

        /// <summary>
        /// Expected HTTP status codes for healthy response
        /// </summary>
        public List<int> ExpectedStatusCodes { get; set; } = new() { 200 };
    }

    public class CircuitBreakerConfiguration
    {
        /// <summary>
        /// Failure threshold percentage
        /// </summary>
        public double FailureThreshold { get; set; } = 50.0;

        /// <summary>
        /// Minimum request count before circuit can open
        /// </summary>
        public int MinimumThroughput { get; set; } = 10;

        /// <summary>
        /// Time to wait before trying to close circuit
        /// </summary>
        public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Time window for calculating failure rate
        /// </summary>
        public TimeSpan RollingWindow { get; set; } = TimeSpan.FromMinutes(1);
    }

    public class StickySessionConfiguration
    {
        /// <summary>
        /// Whether sticky sessions are enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Cookie name for session affinity
        /// </summary>
        public string CookieName { get; set; } = "GATEWAY_SESSION";

        /// <summary>
        /// Session timeout
        /// </summary>
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(1);
    }

    public class FailoverConfiguration
    {
        /// <summary>
        /// Whether automatic failover is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum retry attempts
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Retry delay
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }

    public class AuthenticationContext
    {
        /// <summary>
        /// Authenticated user ID
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Authentication type used
        /// </summary>
        public AuthenticationType Type { get; set; }

        /// <summary>
        /// API key ID (if using API key auth)
        /// </summary>
        public string? ApiKeyId { get; set; }

        /// <summary>
        /// JWT claims (if using JWT auth)
        /// </summary>
        public Dictionary<string, object> Claims { get; set; } = new();

        /// <summary>
        /// User scopes/permissions
        /// </summary>
        public List<string> Scopes { get; set; } = new();

        /// <summary>
        /// Authentication metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class JwtConfiguration
    {
        /// <summary>
        /// JWT signing key
        /// </summary>
        public string SigningKey { get; set; } = string.Empty;

        /// <summary>
        /// JWT issuer
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// JWT audience
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// JWT expiration time
        /// </summary>
        public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Whether to validate issuer
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// Whether to validate audience
        /// </summary>
        public bool ValidateAudience { get; set; } = true;
    }

    public class OAuthConfiguration
    {
        /// <summary>
        /// OAuth provider
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Client ID
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Client secret
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// OAuth scopes
        /// </summary>
        public List<string> Scopes { get; set; } = new();

        /// <summary>
        /// Token introspection endpoint
        /// </summary>
        public string? IntrospectionEndpoint { get; set; }
    }

    public class TransformationRule
    {
        /// <summary>
        /// Rule type (header, query, body, etc.)
        /// </summary>
        public TransformationType Type { get; set; }

        /// <summary>
        /// Source field/path
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Target field/path
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// Transformation action
        /// </summary>
        public TransformationAction Action { get; set; }

        /// <summary>
        /// Transformation value (for set/replace actions)
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Condition for applying transformation
        /// </summary>
        public string? Condition { get; set; }
    }

    public class MiddlewareConfiguration
    {
        /// <summary>
        /// Middleware name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Middleware order/priority
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Middleware configuration
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();

        /// <summary>
        /// Whether the middleware is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    public enum TransformationType
    {
        Header,
        Query,
        Body,
        Path,
        Method
    }

    public enum TransformationAction
    {
        Add,
        Set,
        Remove,
        Replace,
        Append
    }

    /// <summary>
    /// Result classes for API gateway operations
    /// </summary>
    public class ServiceRegistrationResult
    {
        public bool Success { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RouteResult
    {
        public bool Success { get; set; }
        public string RouteId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RouteListResult
    {
        public bool Success { get; set; }
        public List<RouteConfiguration> Routes { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class ApiKeyResult
    {
        public bool Success { get; set; }
        public string ApiKeyId { get; set; } = string.Empty;
        public string? ApiKey { get; set; } // Only returned on creation
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ApiKeyListResult
    {
        public bool Success { get; set; }
        public List<ApiKeyInfo> ApiKeys { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class ApiKeyInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Scopes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastUsed { get; set; }
        public long UsageCount { get; set; }
    }

    public class RateLimitResult
    {
        public bool Success { get; set; }
        public string PolicyId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class LoadBalancingResult
    {
        public bool Success { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public DateTime ConfiguredAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ServiceHealthResult
    {
        public bool Success { get; set; }
        public List<ServiceHealthInfo> Services { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class ServiceHealthInfo
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public List<InstanceHealthInfo> Instances { get; set; } = new();
        public DateTime LastChecked { get; set; }
    }

    public class InstanceHealthInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public DateTime LastChecked { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class TransformationResult
    {
        public bool Success { get; set; }
        public string ConfigurationId { get; set; } = string.Empty;
        public DateTime ConfiguredAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class GatewayAnalytics
    {
        public long TotalRequests { get; set; }
        public double AverageResponseTime { get; set; }
        public Dictionary<int, long> StatusCodeCounts { get; set; } = new();
        public Dictionary<string, long> ServiceRequestCounts { get; set; } = new();
        public Dictionary<string, long> RouteRequestCounts { get; set; } = new();
        public List<TopClient> TopClients { get; set; } = new();
        public List<SlowRequest> SlowestRequests { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class TopClient
    {
        public string ClientId { get; set; } = string.Empty;
        public long RequestCount { get; set; }
        public double AverageResponseTime { get; set; }
    }

    public class SlowRequest
    {
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public TimeSpan ResponseTime { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class GatewayHealth
    {
        public HealthStatus Status { get; set; }
        public int TotalServices { get; set; }
        public int HealthyServices { get; set; }
        public int TotalRoutes { get; set; }
        public int ActiveRoutes { get; set; }
        public long TotalRequestsPerSecond { get; set; }
        public double AverageResponseTime { get; set; }
        public double ErrorRate { get; set; }
        public TimeSpan Uptime { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public List<string> Issues { get; set; } = new();
        public Dictionary<string, object> Details { get; set; } = new();
    }

    // Additional supporting classes
    public class RouteFilters
    {
        public string? ServiceName { get; set; }
        public string? Pattern { get; set; }
        public bool? IsEnabled { get; set; }
        public List<string>? Methods { get; set; }
    }

    public class ApiKeyFilters
    {
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? ExpiringBefore { get; set; }
        public List<string>? Scopes { get; set; }
    }

    public class AnalyticsQuery
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> Metrics { get; set; } = new();
        public Dictionary<string, string> Filters { get; set; } = new();
        public string? GroupBy { get; set; }
    }

    public class RequestTransformationConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<TransformationRule> Rules { get; set; } = new();
        public string? Target { get; set; } // Service, route, or global
        public bool IsEnabled { get; set; } = true;
    }

    public class ResponseTransformationConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<TransformationRule> Rules { get; set; } = new();
        public string? Target { get; set; } // Service, route, or global
        public bool IsEnabled { get; set; } = true;
    }

    public class ApiVersioningConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public VersioningStrategy Strategy { get; set; } = VersioningStrategy.Header;
        public string VersionHeader { get; set; } = "X-API-Version";
        public string QueryParameter { get; set; } = "version";
        public string PathPrefix { get; set; } = "v";
        public string DefaultVersion { get; set; } = "1.0";
        public List<ApiVersion> SupportedVersions { get; set; } = new();
    }

    public class ApiVersion
    {
        public string Version { get; set; } = string.Empty;
        public bool IsSupported { get; set; } = true;
        public bool IsDeprecated { get; set; } = false;
        public DateTime? SunsetDate { get; set; }
        public string? MigrationInfo { get; set; }
    }

    public class CorsConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public List<string> AllowedOrigins { get; set; } = new();
        public List<string> AllowedMethods { get; set; } = new();
        public List<string> AllowedHeaders { get; set; } = new();
        public List<string> ExposedHeaders { get; set; } = new();
        public bool AllowCredentials { get; set; } = false;
        public int MaxAge { get; set; } = 86400; // 24 hours
        public string? Target { get; set; } // Service, route, or global
        public bool IsEnabled { get; set; } = true;
    }

    public enum VersioningStrategy
    {
        Header,
        QueryParameter,
        Path,
        Accept,
        Custom
    }

    public class VersioningResult
    {
        public bool Success { get; set; }
        public string ConfigurationId { get; set; } = string.Empty;
        public DateTime ConfiguredAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CorsResult
    {
        public bool Success { get; set; }
        public string ConfigurationId { get; set; } = string.Empty;
        public DateTime ConfiguredAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ConfigurationExportOptions
    {
        public ExportFormat Format { get; set; } = ExportFormat.Json;
        public List<string> IncludeTypes { get; set; } = new(); // routes, services, policies, etc.
        public bool IncludeSecrets { get; set; } = false;
        public Dictionary<string, string> Filters { get; set; } = new();
    }

    public class ConfigurationImportOptions
    {
        public string Configuration { get; set; } = string.Empty;
        public ExportFormat Format { get; set; } = ExportFormat.Json;
        public bool OverwriteExisting { get; set; } = false;
        public bool ValidateOnly { get; set; } = false;
        public Dictionary<string, string> Mappings { get; set; } = new();
    }

    public class ConfigurationExportResult
    {
        public bool Success { get; set; }
        public string Configuration { get; set; } = string.Empty;
        public ExportFormat Format { get; set; }
        public DateTime ExportedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ConfigurationImportResult
    {
        public bool Success { get; set; }
        public int ImportedItems { get; set; }
        public int SkippedItems { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime ImportedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public enum ExportFormat
    {
        Json,
        Yaml,
        Xml
    }
}