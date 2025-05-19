using System;

namespace NeoServiceLayer.Integration.Tests.Models
{
    /// <summary>
    /// Represents the error codes returned by the API.
    /// </summary>
    [Serializable]
    public enum ApiErrorCodes
    {
        /// <summary>
        /// Unknown error.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Validation error.
        /// </summary>
        ValidationError = 1,

        /// <summary>
        /// Resource not found.
        /// </summary>
        ResourceNotFound = 2,

        /// <summary>
        /// Unauthorized access.
        /// </summary>
        Unauthorized = 3,

        /// <summary>
        /// Forbidden access.
        /// </summary>
        Forbidden = 4,

        /// <summary>
        /// Internal server error.
        /// </summary>
        InternalServerError = 5,

        /// <summary>
        /// Service unavailable.
        /// </summary>
        ServiceUnavailable = 6,

        /// <summary>
        /// Bad request.
        /// </summary>
        BadRequest = 7,

        /// <summary>
        /// Conflict.
        /// </summary>
        Conflict = 8,

        /// <summary>
        /// Too many requests.
        /// </summary>
        TooManyRequests = 9,

        /// <summary>
        /// Not implemented.
        /// </summary>
        NotImplemented = 10
    }
}
