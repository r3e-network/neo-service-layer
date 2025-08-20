namespace NeoServiceLayer.Web.Models;

/// <summary>
/// View model for error pages.
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// Gets or sets the request ID that caused the error.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets a value indicating whether to show the request ID.
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}