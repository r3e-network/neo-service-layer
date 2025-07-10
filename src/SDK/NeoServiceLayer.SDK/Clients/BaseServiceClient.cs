using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.SDK.Clients;

/// <summary>
/// Base class for all service clients
/// </summary>
public abstract class BaseServiceClient
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    private readonly JsonSerializerOptions _jsonOptions;

    protected BaseServiceClient(IHttpClientFactory httpClientFactory, string clientName, ILogger logger = null)
    {
        HttpClient = httpClientFactory.CreateClient(clientName);
        Logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    protected async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpClient.GetAsync(endpoint, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response);

            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Logger?.LogError(ex, "HTTP request failed for GET {Endpoint}", endpoint);
            throw new ServiceClientException($"Failed to GET {endpoint}", ex);
        }
        catch (TaskCanceledException ex)
        {
            Logger?.LogError(ex, "Request timeout for GET {Endpoint}", endpoint);
            throw new ServiceClientException($"Request timeout for GET {endpoint}", ex);
        }
    }

    protected async Task<T> PostAsync<T>(string endpoint, object request, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync(endpoint, content, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response);

            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Logger?.LogError(ex, "HTTP request failed for POST {Endpoint}", endpoint);
            throw new ServiceClientException($"Failed to POST {endpoint}", ex);
        }
        catch (TaskCanceledException ex)
        {
            Logger?.LogError(ex, "Request timeout for POST {Endpoint}", endpoint);
            throw new ServiceClientException($"Request timeout for POST {endpoint}", ex);
        }
    }

    protected async Task<T> PutAsync<T>(string endpoint, object request, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PutAsync(endpoint, content, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response);

            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Logger?.LogError(ex, "HTTP request failed for PUT {Endpoint}", endpoint);
            throw new ServiceClientException($"Failed to PUT {endpoint}", ex);
        }
        catch (TaskCanceledException ex)
        {
            Logger?.LogError(ex, "Request timeout for PUT {Endpoint}", endpoint);
            throw new ServiceClientException($"Request timeout for PUT {endpoint}", ex);
        }
    }

    protected async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpClient.DeleteAsync(endpoint, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response);
        }
        catch (HttpRequestException ex)
        {
            Logger?.LogError(ex, "HTTP request failed for DELETE {Endpoint}", endpoint);
            throw new ServiceClientException($"Failed to DELETE {endpoint}", ex);
        }
        catch (TaskCanceledException ex)
        {
            Logger?.LogError(ex, "Request timeout for DELETE {Endpoint}", endpoint);
            throw new ServiceClientException($"Request timeout for DELETE {endpoint}", ex);
        }
    }

    private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Logger?.LogError("HTTP request failed with status {StatusCode}: {Content}",
                response.StatusCode, errorContent);

            throw new ServiceClientException(
                $"Request failed with status {response.StatusCode}: {errorContent}",
                (int)response.StatusCode);
        }
    }
}

/// <summary>
/// Exception thrown by service clients
/// </summary>
public class ServiceClientException : Exception
{
    public int? StatusCode { get; }

    public ServiceClientException(string message) : base(message)
    {
    }

    public ServiceClientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ServiceClientException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Service health report
/// </summary>
public class ServiceHealthReport
{
    public string Status { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public Dictionary<string, ServiceHealthEntry> Entries { get; set; }
}

/// <summary>
/// Service health entry
/// </summary>
public class ServiceHealthEntry
{
    public string Status { get; set; }
    public string Description { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Data { get; set; }
}

/// <summary>
/// Service metrics
/// </summary>
public class ServiceMetrics
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, MetricValue> Metrics { get; set; }
}

/// <summary>
/// Metric value
/// </summary>
public class MetricValue
{
    public double Value { get; set; }
    public string Unit { get; set; }
    public Dictionary<string, string> Labels { get; set; }
}
