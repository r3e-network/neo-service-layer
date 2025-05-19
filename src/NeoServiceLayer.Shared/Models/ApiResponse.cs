using System.Text.Json.Serialization;

namespace NeoServiceLayer.Shared.Models
{
    /// <summary>
    /// Represents a standard API response.
    /// </summary>
    /// <typeparam name="T">The type of the data in the response.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Gets or sets whether the request was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the data in the response.
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Gets or sets the error in the response.
        /// </summary>
        public ApiError Error { get; set; }

        /// <summary>
        /// Creates a successful response with the specified data.
        /// </summary>
        /// <param name="data">The data to include in the response.</param>
        /// <returns>A successful response with the specified data.</returns>
        public static ApiResponse<T> CreateSuccess(T data)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Error = null
            };
        }

        /// <summary>
        /// Creates a failed response with the specified error.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="message">The error message.</param>
        /// <returns>A failed response with the specified error.</returns>
        public static ApiResponse<T> CreateError(string code, string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Data = default,
                Error = new ApiError
                {
                    Code = code,
                    Message = message
                }
            };
        }
    }

    /// <summary>
    /// Represents an error in an API response.
    /// </summary>
    public class ApiError
    {
        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Predefined error codes for API responses.
    /// </summary>
    public static class ApiErrorCodes
    {
        /// <summary>
        /// Authentication error.
        /// </summary>
        public const string AuthenticationError = "authentication_error";

        /// <summary>
        /// Authorization error.
        /// </summary>
        public const string AuthorizationError = "authorization_error";

        /// <summary>
        /// Validation error.
        /// </summary>
        public const string ValidationError = "validation_error";

        /// <summary>
        /// Resource not found error.
        /// </summary>
        public const string ResourceNotFound = "resource_not_found";

        /// <summary>
        /// Rate limit exceeded error.
        /// </summary>
        public const string RateLimitExceeded = "rate_limit_exceeded";

        /// <summary>
        /// Internal error.
        /// </summary>
        public const string InternalError = "internal_error";

        /// <summary>
        /// TEE error.
        /// </summary>
        public const string TeeError = "tee_error";

        /// <summary>
        /// Attestation error.
        /// </summary>
        public const string AttestationError = "attestation_error";
    }
}
