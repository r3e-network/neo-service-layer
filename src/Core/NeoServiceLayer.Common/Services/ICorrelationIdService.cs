namespace NeoServiceLayer.Common.Services;

/// <summary>
/// Service for managing correlation IDs across distributed requests
/// </summary>
public interface ICorrelationIdService
{
    /// <summary>
    /// Gets the current correlation ID for the request
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Sets the correlation ID for the current request
    /// </summary>
    /// <param name="correlationId">The correlation ID to set</param>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Generates a new correlation ID
    /// </summary>
    /// <returns>A new unique correlation ID</returns>
    string GenerateCorrelationId();
}