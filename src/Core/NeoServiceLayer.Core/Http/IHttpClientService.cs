using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Http;

/// <summary>
/// Abstraction for HTTP client operations to enable testing and dependency injection.
/// </summary>
public interface IHttpClientService
{
    /// <summary>
    /// Sends a GET request to the specified URI.
    /// </summary>
    /// <param name="requestUri">The URI to send the request to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a GET request to the specified URI.
    /// </summary>
    /// <param name="requestUri">The URI to send the request to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request to the specified URI.
    /// </summary>
    /// <param name="requestUri">The URI to send the request to.</param>
    /// <param name="content">The HTTP content to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request to the specified URI.
    /// </summary>
    /// <param name="requestUri">The URI to send the request to.</param>
    /// <param name="content">The HTTP content to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets the timeout for HTTP requests.
    /// </summary>
    TimeSpan Timeout { get; set; }
}
