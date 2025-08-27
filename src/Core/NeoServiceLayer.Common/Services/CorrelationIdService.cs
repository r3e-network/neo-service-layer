namespace NeoServiceLayer.Common.Services;

/// <summary>
/// Implementation of correlation ID service for distributed tracing
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private static readonly AsyncLocal<string?> _correlationId = new();
    
    /// <summary>
    /// Gets the current correlation ID for the request
    /// </summary>
    public string CorrelationId => _correlationId.Value ?? GenerateCorrelationId();

    /// <summary>
    /// Sets the correlation ID for the current request
    /// </summary>
    /// <param name="correlationId">The correlation ID to set</param>
    public void SetCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));
        }

        _correlationId.Value = correlationId;
    }

    /// <summary>
    /// Generates a new correlation ID
    /// </summary>
    /// <returns>A new unique correlation ID</returns>
    public string GenerateCorrelationId()
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        _correlationId.Value = correlationId;
        return correlationId;
    }
}