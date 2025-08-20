using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;


namespace NeoServiceLayer.Core.Http;

/// <summary>
/// Production implementation of HTTP client service using HttpClient.
/// </summary>
public class HttpClientService : IHttpClientService, IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the HttpClientService class.
    /// </summary>
    public HttpClientService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Initializes a new instance of the HttpClientService class with a specific HttpClient.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use.</param>
    public HttpClientService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc/>
    public TimeSpan Timeout
    {
        get
        {
            ThrowIfDisposed();
            return _httpClient.Timeout;
        }
        set
        {
            ThrowIfDisposed();
            _httpClient.Timeout = value;
        }
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await _httpClient.GetAsync(requestUri, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await _httpClient.GetAsync(requestUri, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await _httpClient.PostAsync(requestUri, content, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await _httpClient.PostAsync(requestUri, content, cancellationToken);
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HttpClientService));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the HTTP client service.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
