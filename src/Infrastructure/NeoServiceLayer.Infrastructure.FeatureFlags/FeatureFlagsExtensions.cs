using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.FeatureFlags;

public static class FeatureFlagsExtensions
{
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FeatureFlagOptions>(configuration.GetSection("FeatureFlags"));

        // Register feature flag providers
        services.AddSingleton<IFeatureFlagProvider, ConfigurationFeatureFlagProvider>();
        services.AddSingleton<IFeatureFlagProvider, DatabaseFeatureFlagProvider>();
        services.AddSingleton<IFeatureFlagProvider, RemoteFeatureFlagProvider>();

        // Register the main feature flag service
        services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

        // Register evaluation strategies
        services.AddSingleton<IFeatureEvaluationStrategy, PercentageRolloutStrategy>();
        services.AddSingleton<IFeatureEvaluationStrategy, UserTargetingStrategy>();
        services.AddSingleton<IFeatureEvaluationStrategy, GroupTargetingStrategy>();
        services.AddSingleton<IFeatureEvaluationStrategy, TimeWindowStrategy>();
        services.AddSingleton<IFeatureEvaluationStrategy, EnvironmentStrategy>();

        // Register context providers
        services.AddScoped<IFeatureContext, HttpFeatureContext>();

        // Add hosted service for feature flag updates
        services.AddHostedService<FeatureFlagUpdateService>();

        return services;
    }

    public static IApplicationBuilder UseFeatureFlags(this IApplicationBuilder app)
    {
        app.UseMiddleware<FeatureFlagMiddleware>();
        return app;
    }
}

// Feature flag service interface
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string featureName, IFeatureContext context = null);
    Task<T> GetVariationAsync<T>(string featureName, T defaultValue, IFeatureContext context = null);
    Task<FeatureFlag> GetFeatureFlagAsync(string featureName);
    Task<IEnumerable<FeatureFlag>> GetAllFeatureFlagsAsync();
    Task UpdateFeatureFlagAsync(FeatureFlag featureFlag);
    Task<Dictionary<string, object>> GetFeatureFlagMetricsAsync(string featureName);
}

// Feature flag service implementation
public class FeatureFlagService : IFeatureFlagService
{
    private readonly IEnumerable<IFeatureFlagProvider> _providers;
    private readonly IEnumerable<IFeatureEvaluationStrategy> _strategies;
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly FeatureFlagOptions _options;
    private readonly Dictionary<string, FeatureFlag> _cache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public FeatureFlagService(
        IEnumerable<IFeatureFlagProvider> providers,
        IEnumerable<IFeatureEvaluationStrategy> strategies,
        ILogger<FeatureFlagService> logger,
        IOptions<FeatureFlagOptions> options)
    {
        _providers = providers.OrderBy(p => p.Priority);
        _strategies = strategies;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<bool> IsEnabledAsync(string featureName, IFeatureContext context = null)
    {
        try
        {
            var featureFlag = await GetFeatureFlagWithCacheAsync(featureName);
            
            if (featureFlag == null)
            {
                _logger.LogWarning("Feature flag {FeatureName} not found", featureName);
                return _options.DefaultValue;
            }

            if (!featureFlag.Enabled)
            {
                return false;
            }

            // Evaluate rules
            foreach (var rule in featureFlag.Rules.OrderBy(r => r.Priority))
            {
                var strategy = _strategies.FirstOrDefault(s => s.CanHandle(rule.Type));
                if (strategy != null)
                {
                    var result = await strategy.EvaluateAsync(rule, context);
                    if (result.HasValue)
                    {
                        LogEvaluation(featureName, rule.Type, result.Value);
                        return result.Value;
                    }
                }
            }

            // Return default value if no rules match
            return featureFlag.DefaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating feature flag {FeatureName}", featureName);
            return _options.DefaultValue;
        }
    }

    public async Task<T> GetVariationAsync<T>(string featureName, T defaultValue, IFeatureContext context = null)
    {
        try
        {
            var featureFlag = await GetFeatureFlagWithCacheAsync(featureName);
            
            if (featureFlag?.Variations == null || !featureFlag.Variations.Any())
            {
                return defaultValue;
            }

            // Determine which variation to use based on rules
            foreach (var rule in featureFlag.Rules.OrderBy(r => r.Priority))
            {
                var strategy = _strategies.FirstOrDefault(s => s.CanHandle(rule.Type));
                if (strategy != null)
                {
                    var result = await strategy.EvaluateAsync(rule, context);
                    if (result.HasValue && result.Value)
                    {
                        var variationKey = rule.VariationKey ?? featureFlag.DefaultVariation;
                        if (featureFlag.Variations.TryGetValue(variationKey, out var variation))
                        {
                            return JsonSerializer.Deserialize<T>(variation.ToString());
                        }
                    }
                }
            }

            // Return default variation
            if (featureFlag.DefaultVariation != null && 
                featureFlag.Variations.TryGetValue(featureFlag.DefaultVariation, out var defaultVariation))
            {
                return JsonSerializer.Deserialize<T>(defaultVariation.ToString());
            }

            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting variation for feature flag {FeatureName}", featureName);
            return defaultValue;
        }
    }

    public async Task<FeatureFlag> GetFeatureFlagAsync(string featureName)
    {
        foreach (var provider in _providers)
        {
            var flag = await provider.GetFeatureFlagAsync(featureName);
            if (flag != null)
            {
                return flag;
            }
        }

        return null;
    }

    public async Task<IEnumerable<FeatureFlag>> GetAllFeatureFlagsAsync()
    {
        var allFlags = new Dictionary<string, FeatureFlag>();

        foreach (var provider in _providers.Reverse())
        {
            var flags = await provider.GetAllFeatureFlagsAsync();
            foreach (var flag in flags)
            {
                allFlags[flag.Name] = flag;
            }
        }

        return allFlags.Values;
    }

    public async Task UpdateFeatureFlagAsync(FeatureFlag featureFlag)
    {
        // Update in the primary provider (usually database)
        var primaryProvider = _providers.FirstOrDefault(p => p.CanUpdate);
        if (primaryProvider != null)
        {
            await primaryProvider.UpdateFeatureFlagAsync(featureFlag);
        }

        // Invalidate cache
        await _cacheLock.WaitAsync();
        try
        {
            _cache.Remove(featureFlag.Name);
        }
        finally
        {
            _cacheLock.Release();
        }

        _logger.LogInformation("Feature flag {FeatureName} updated", featureFlag.Name);
    }

    public async Task<Dictionary<string, object>> GetFeatureFlagMetricsAsync(string featureName)
    {
        // This would typically integrate with a metrics system
        return new Dictionary<string, object>
        {
            ["evaluations"] = 0,
            ["trueEvaluations"] = 0,
            ["falseEvaluations"] = 0,
            ["errors"] = 0,
            ["lastEvaluated"] = DateTimeOffset.UtcNow
        };
    }

    private async Task<FeatureFlag> GetFeatureFlagWithCacheAsync(string featureName)
    {
        await _cacheLock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(featureName, out var cachedFlag))
            {
                return cachedFlag;
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        var flag = await GetFeatureFlagAsync(featureName);
        
        if (flag != null)
        {
            await _cacheLock.WaitAsync();
            try
            {
                _cache[featureName] = flag;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        return flag;
    }

    private void LogEvaluation(string featureName, string strategyType, bool result)
    {
        _logger.LogDebug("Feature flag {FeatureName} evaluated by {Strategy}: {Result}", 
            featureName, strategyType, result);
    }
}

// Feature flag providers
public interface IFeatureFlagProvider
{
    int Priority { get; }
    bool CanUpdate { get; }
    Task<FeatureFlag> GetFeatureFlagAsync(string featureName);
    Task<IEnumerable<FeatureFlag>> GetAllFeatureFlagsAsync();
    Task UpdateFeatureFlagAsync(FeatureFlag featureFlag);
}

// Configuration-based provider
public class ConfigurationFeatureFlagProvider : IFeatureFlagProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationFeatureFlagProvider> _logger;

    public int Priority => 1;
    public bool CanUpdate => false;

    public ConfigurationFeatureFlagProvider(IConfiguration configuration, ILogger<ConfigurationFeatureFlagProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<FeatureFlag> GetFeatureFlagAsync(string featureName)
    {
        var section = _configuration.GetSection($"FeatureFlags:Flags:{featureName}");
        if (!section.Exists())
        {
            return Task.FromResult<FeatureFlag>(null);
        }

        var flag = section.Get<FeatureFlag>();
        flag.Name = featureName;
        
        return Task.FromResult(flag);
    }

    public Task<IEnumerable<FeatureFlag>> GetAllFeatureFlagsAsync()
    {
        var flags = new List<FeatureFlag>();
        var flagsSection = _configuration.GetSection("FeatureFlags:Flags");
        
        foreach (var child in flagsSection.GetChildren())
        {
            var flag = child.Get<FeatureFlag>();
            flag.Name = child.Key;
            flags.Add(flag);
        }

        return Task.FromResult(flags.AsEnumerable());
    }

    public Task UpdateFeatureFlagAsync(FeatureFlag featureFlag)
    {
        throw new NotSupportedException("Configuration provider does not support updates");
    }
}

// Database provider
public class DatabaseFeatureFlagProvider : IFeatureFlagProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseFeatureFlagProvider> _logger;

    public int Priority => 2;
    public bool CanUpdate => true;

    public DatabaseFeatureFlagProvider(IServiceProvider serviceProvider, ILogger<DatabaseFeatureFlagProvider> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<FeatureFlag> GetFeatureFlagAsync(string featureName)
    {
        // Implementation would query database
        return null;
    }

    public async Task<IEnumerable<FeatureFlag>> GetAllFeatureFlagsAsync()
    {
        // Implementation would query database
        return Enumerable.Empty<FeatureFlag>();
    }

    public async Task UpdateFeatureFlagAsync(FeatureFlag featureFlag)
    {
        // Implementation would update database
    }
}

// Remote provider (e.g., LaunchDarkly, Split.io)
public class RemoteFeatureFlagProvider : IFeatureFlagProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RemoteFeatureFlagProvider> _logger;
    private readonly FeatureFlagOptions _options;

    public int Priority => 3;
    public bool CanUpdate => false;

    public RemoteFeatureFlagProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<RemoteFeatureFlagProvider> logger,
        IOptions<FeatureFlagOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("FeatureFlags");
        _logger = logger;
        _options = options.Value;
    }

    public async Task<FeatureFlag> GetFeatureFlagAsync(string featureName)
    {
        // Implementation would call remote service
        return null;
    }

    public async Task<IEnumerable<FeatureFlag>> GetAllFeatureFlagsAsync()
    {
        // Implementation would call remote service
        return Enumerable.Empty<FeatureFlag>();
    }

    public Task UpdateFeatureFlagAsync(FeatureFlag featureFlag)
    {
        throw new NotSupportedException("Remote provider does not support direct updates");
    }
}

// Evaluation strategies
public interface IFeatureEvaluationStrategy
{
    bool CanHandle(string ruleType);
    Task<bool?> EvaluateAsync(FeatureRule rule, IFeatureContext context);
}

// Percentage rollout strategy
public class PercentageRolloutStrategy : IFeatureEvaluationStrategy
{
    public bool CanHandle(string ruleType) => ruleType == "percentage";

    public Task<bool?> EvaluateAsync(FeatureRule rule, IFeatureContext context)
    {
        if (!rule.Parameters.TryGetValue("percentage", out var percentageObj) || 
            !int.TryParse(percentageObj.ToString(), out var percentage))
        {
            return Task.FromResult<bool?>(null);
        }

        var userId = context?.UserId ?? "anonymous";
        var hash = userId.GetHashCode();
        var bucket = Math.Abs(hash) % 100;

        return Task.FromResult<bool?>(bucket < percentage);
    }
}

// User targeting strategy
public class UserTargetingStrategy : IFeatureEvaluationStrategy
{
    public bool CanHandle(string ruleType) => ruleType == "user";

    public Task<bool?> EvaluateAsync(FeatureRule rule, IFeatureContext context)
    {
        if (context?.UserId == null || 
            !rule.Parameters.TryGetValue("users", out var usersObj))
        {
            return Task.FromResult<bool?>(null);
        }

        var users = JsonSerializer.Deserialize<List<string>>(usersObj.ToString());
        return Task.FromResult<bool?>(users?.Contains(context.UserId) ?? false);
    }
}

// Group targeting strategy
public class GroupTargetingStrategy : IFeatureEvaluationStrategy
{
    public bool CanHandle(string ruleType) => ruleType == "group";

    public Task<bool?> EvaluateAsync(FeatureRule rule, IFeatureContext context)
    {
        if (context?.Groups == null || 
            !rule.Parameters.TryGetValue("groups", out var groupsObj))
        {
            return Task.FromResult<bool?>(null);
        }

        var targetGroups = JsonSerializer.Deserialize<List<string>>(groupsObj.ToString());
        return Task.FromResult<bool?>(context.Groups.Any(g => targetGroups.Contains(g)));
    }
}

// Time window strategy
public class TimeWindowStrategy : IFeatureEvaluationStrategy
{
    public bool CanHandle(string ruleType) => ruleType == "timeWindow";

    public Task<bool?> EvaluateAsync(FeatureRule rule, IFeatureContext context)
    {
        var now = DateTimeOffset.UtcNow;

        if (rule.Parameters.TryGetValue("startTime", out var startObj) &&
            DateTimeOffset.TryParse(startObj.ToString(), out var startTime) &&
            now < startTime)
        {
            return Task.FromResult<bool?>(false);
        }

        if (rule.Parameters.TryGetValue("endTime", out var endObj) &&
            DateTimeOffset.TryParse(endObj.ToString(), out var endTime) &&
            now > endTime)
        {
            return Task.FromResult<bool?>(false);
        }

        return Task.FromResult<bool?>(true);
    }
}

// Environment strategy
public class EnvironmentStrategy : IFeatureEvaluationStrategy
{
    private readonly IHostEnvironment _environment;

    public EnvironmentStrategy(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool CanHandle(string ruleType) => ruleType == "environment";

    public Task<bool?> EvaluateAsync(FeatureRule rule, IFeatureContext context)
    {
        if (!rule.Parameters.TryGetValue("environments", out var envObj))
        {
            return Task.FromResult<bool?>(null);
        }

        var environments = JsonSerializer.Deserialize<List<string>>(envObj.ToString());
        return Task.FromResult<bool?>(environments?.Contains(_environment.EnvironmentName) ?? false);
    }
}

// Feature context
public interface IFeatureContext
{
    string UserId { get; }
    List<string> Groups { get; }
    Dictionary<string, object> Properties { get; }
}

public class HttpFeatureContext : IFeatureContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpFeatureContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId => _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    
    public List<string> Groups => _httpContextAccessor.HttpContext?.User?.Claims
        .Where(c => c.Type == "groups")
        .Select(c => c.Value)
        .ToList() ?? new List<string>();

    public Dictionary<string, object> Properties => new()
    {
        ["IpAddress"] = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
        ["UserAgent"] = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString(),
        ["Path"] = _httpContextAccessor.HttpContext?.Request?.Path.Value
    };
}

// Feature flag middleware
public class FeatureFlagMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FeatureFlagMiddleware> _logger;

    public FeatureFlagMiddleware(RequestDelegate next, ILogger<FeatureFlagMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IFeatureFlagService featureFlagService, IFeatureContext featureContext)
    {
        // Add feature flag evaluation results to HttpContext
        context.Items["FeatureFlags"] = new FeatureFlagContext(featureFlagService, featureContext);

        await _next(context);
    }
}

// Feature flag context for HttpContext
public class FeatureFlagContext
{
    private readonly IFeatureFlagService _service;
    private readonly IFeatureContext _context;
    private readonly Dictionary<string, bool> _cache = new();

    public FeatureFlagContext(IFeatureFlagService service, IFeatureContext context)
    {
        _service = service;
        _context = context;
    }

    public async Task<bool> IsEnabledAsync(string featureName)
    {
        if (_cache.TryGetValue(featureName, out var cachedValue))
        {
            return cachedValue;
        }

        var result = await _service.IsEnabledAsync(featureName, _context);
        _cache[featureName] = result;
        return result;
    }
}

// Feature flag attribute for action-level control
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class FeatureFlagAttribute : ActionFilterAttribute
{
    private readonly string _featureName;
    private readonly bool _requireEnabled;

    public FeatureFlagAttribute(string featureName, bool requireEnabled = true)
    {
        _featureName = featureName;
        _requireEnabled = requireEnabled;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var featureFlagService = context.HttpContext.RequestServices.GetRequiredService<IFeatureFlagService>();
        var featureContext = context.HttpContext.RequestServices.GetRequiredService<IFeatureContext>();

        var isEnabled = await featureFlagService.IsEnabledAsync(_featureName, featureContext);

        if (isEnabled != _requireEnabled)
        {
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }
}

// Background service for feature flag updates
public class FeatureFlagUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FeatureFlagUpdateService> _logger;
    private readonly FeatureFlagOptions _options;

    public FeatureFlagUpdateService(
        IServiceProvider serviceProvider,
        ILogger<FeatureFlagUpdateService> logger,
        IOptions<FeatureFlagOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableAutoUpdate)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateFeatureFlagsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_options.UpdateIntervalSeconds), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feature flags");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task UpdateFeatureFlagsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var featureFlagService = scope.ServiceProvider.GetRequiredService<IFeatureFlagService>();

        _logger.LogDebug("Updating feature flags from remote sources");

        // This would typically sync with remote feature flag services
        // For now, we just log that we're checking
    }
}

// Models
public class FeatureFlag
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Enabled { get; set; }
    public bool DefaultValue { get; set; }
    public List<FeatureRule> Rules { get; set; } = new();
    public Dictionary<string, object> Variations { get; set; }
    public string DefaultVariation { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class FeatureRule
{
    public string Type { get; set; }
    public int Priority { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string VariationKey { get; set; }
}

// Configuration
public class FeatureFlagOptions
{
    public bool DefaultValue { get; set; } = false;
    public bool EnableAutoUpdate { get; set; } = true;
    public int UpdateIntervalSeconds { get; set; } = 300;
    public string RemoteServiceUrl { get; set; }
    public string RemoteServiceApiKey { get; set; }
    public List<string> EnabledProviders { get; set; } = new() { "Configuration", "Database" };
}