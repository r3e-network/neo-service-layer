using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NeoServiceLayer.Api.Models
{
    /// <summary>
    /// Represents a standardized API response.
    /// </summary>
    /// <typeparam name="T">The type of data in the response.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Gets or sets a value indicating whether the request was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the data returned by the API.
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Gets or sets the error information if the request failed.
        /// </summary>
        public ApiError Error { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the response.
        /// </summary>
        public ApiMetadata Metadata { get; set; }

        /// <summary>
        /// Creates a successful response with the specified data.
        /// </summary>
        /// <param name="data">The data to include in the response.</param>
        /// <returns>A successful API response.</returns>
        public static ApiResponse<T> CreateSuccess(T data)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Error = null,
                Metadata = new ApiMetadata()
            };
        }

        /// <summary>
        /// Creates a successful response with the specified data and metadata.
        /// </summary>
        /// <param name="data">The data to include in the response.</param>
        /// <param name="metadata">The metadata to include in the response.</param>
        /// <returns>A successful API response.</returns>
        public static ApiResponse<T> CreateSuccess(T data, ApiMetadata metadata)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Error = null,
                Metadata = metadata
            };
        }

        /// <summary>
        /// Creates an error response with the specified error code and message.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="message">The error message.</param>
        /// <returns>An error API response.</returns>
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
                },
                Metadata = new ApiMetadata()
            };
        }

        /// <summary>
        /// Creates an error response with the specified error code, message, and details.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="details">The error details.</param>
        /// <returns>An error API response.</returns>
        public static ApiResponse<T> CreateError(string code, string message, object details)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Data = default,
                Error = new ApiError
                {
                    Code = code,
                    Message = message,
                    Details = details
                },
                Metadata = new ApiMetadata()
            };
        }

        /// <summary>
        /// Creates an error response with the specified error code, message, details, and metadata.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="details">The error details.</param>
        /// <param name="metadata">The metadata to include in the response.</param>
        /// <returns>An error API response.</returns>
        public static ApiResponse<T> CreateError(string code, string message, object details, ApiMetadata metadata)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Data = default,
                Error = new ApiError
                {
                    Code = code,
                    Message = message,
                    Details = details
                },
                Metadata = metadata
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

        /// <summary>
        /// Gets or sets the error details.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object Details { get; set; }

        /// <summary>
        /// Implicit conversion from string to ApiError.
        /// </summary>
        /// <param name="message">The error message.</param>
        public static implicit operator ApiError(string message)
        {
            return new ApiError
            {
                Code = ApiErrorCodes.BadRequest,
                Message = message
            };
        }
    }

    /// <summary>
    /// Represents metadata in an API response.
    /// </summary>
    public class ApiMetadata
    {
        /// <summary>
        /// Gets or sets the timestamp of the response.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the request ID.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string RequestId { get; set; }

        /// <summary>
        /// Gets or sets the pagination information.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PaginationMetadata Pagination { get; set; }

        /// <summary>
        /// Gets or sets additional metadata.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object> Additional { get; set; }
    }

    /// <summary>
    /// Represents pagination metadata in an API response.
    /// </summary>
    public class PaginationMetadata
    {
        /// <summary>
        /// Gets or sets the current page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of items.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets a value indicating whether there is a previous page.
        /// </summary>
        public bool HasPrevious => Page > 1;

        /// <summary>
        /// Gets a value indicating whether there is a next page.
        /// </summary>
        public bool HasNext => Page < TotalPages;
    }

    /// <summary>
    /// Defines common API error codes.
    /// </summary>
    public static class ApiErrorCodes
    {
        /// <summary>
        /// Validation error.
        /// </summary>
        public const string ValidationError = "VALIDATION_ERROR";

        /// <summary>
        /// Resource not found.
        /// </summary>
        public const string NotFound = "NOT_FOUND";

        /// <summary>
        /// Resource not found.
        /// </summary>
        public const string ResourceNotFound = "RESOURCE_NOT_FOUND";

        /// <summary>
        /// Internal server error.
        /// </summary>
        public const string InternalServerError = "INTERNAL_SERVER_ERROR";

        /// <summary>
        /// Internal error.
        /// </summary>
        public const string InternalError = "INTERNAL_ERROR";

        /// <summary>
        /// Unauthorized access.
        /// </summary>
        public const string Unauthorized = "UNAUTHORIZED";

        /// <summary>
        /// Authorization error.
        /// </summary>
        public const string AuthorizationError = "AUTHORIZATION_ERROR";

        /// <summary>
        /// Forbidden access.
        /// </summary>
        public const string Forbidden = "FORBIDDEN";

        /// <summary>
        /// Bad request.
        /// </summary>
        public const string BadRequest = "BAD_REQUEST";

        /// <summary>
        /// The request was rate limited.
        /// </summary>
        public const string RateLimited = "RATE_LIMITED";

        /// <summary>
        /// The request timed out.
        /// </summary>
        public const string Timeout = "TIMEOUT";

        /// <summary>
        /// The resource already exists.
        /// </summary>
        public const string Conflict = "CONFLICT";

        /// <summary>
        /// The request entity was too large.
        /// </summary>
        public const string PayloadTooLarge = "PAYLOAD_TOO_LARGE";

        /// <summary>
        /// The API version is not supported.
        /// </summary>
        public const string UnsupportedApiVersion = "UNSUPPORTED_API_VERSION";

        /// <summary>
        /// The requested method is not allowed.
        /// </summary>
        public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";

        /// <summary>
        /// The requested media type is not supported.
        /// </summary>
        public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";

        /// <summary>
        /// The service is unavailable.
        /// </summary>
        public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    }
}
