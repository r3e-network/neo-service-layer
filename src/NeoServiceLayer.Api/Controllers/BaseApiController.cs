using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api.Models;
using NeoServiceLayer.Core.Exceptions;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Base API controller with common functionality.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        private ILogger<BaseApiController> _logger;

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger<BaseApiController> Logger => _logger ??= HttpContext.RequestServices.GetService<ILogger<BaseApiController>>();

        /// <summary>
        /// Creates a successful response.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="data">The data to include in the response.</param>
        /// <returns>An action result with a successful API response.</returns>
        protected IActionResult Success<T>(T data)
        {
            return Ok(ApiResponse<T>.CreateSuccess(data));
        }

        /// <summary>
        /// Creates a successful response with metadata.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="data">The data to include in the response.</param>
        /// <param name="metadata">The metadata to include in the response.</param>
        /// <returns>An action result with a successful API response.</returns>
        protected IActionResult Success<T>(T data, ApiMetadata metadata)
        {
            return Ok(ApiResponse<T>.CreateSuccess(data, metadata));
        }

        /// <summary>
        /// Creates a successful response with pagination metadata.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="data">The data to include in the response.</param>
        /// <param name="page">The current page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="totalCount">The total number of items.</param>
        /// <returns>An action result with a successful API response.</returns>
        protected IActionResult SuccessWithPagination<T>(IEnumerable<T> data, int page, int pageSize, int totalCount)
        {
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var metadata = new ApiMetadata
            {
                RequestId = HttpContext.TraceIdentifier,
                Pagination = new PaginationMetadata
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                }
            };

            return Ok(ApiResponse<IEnumerable<T>>.CreateSuccess(data, metadata));
        }

        /// <summary>
        /// Creates a created response.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="data">The data to include in the response.</param>
        /// <param name="uri">The URI of the created resource.</param>
        /// <returns>An action result with a successful API response.</returns>
        protected IActionResult Created<T>(T data, string uri)
        {
            return Created(uri, ApiResponse<T>.CreateSuccess(data));
        }

        /// <summary>
        /// Creates a created response.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="data">The data to include in the response.</param>
        /// <param name="uri">The URI of the created resource.</param>
        /// <param name="metadata">The metadata to include in the response.</param>
        /// <returns>An action result with a successful API response.</returns>
        protected IActionResult Created<T>(T data, string uri, ApiMetadata metadata)
        {
            return Created(uri, ApiResponse<T>.CreateSuccess(data, metadata));
        }

        /// <summary>
        /// Creates a no content response.
        /// </summary>
        /// <returns>An action result with a no content response.</returns>
        protected IActionResult NoContentResponse()
        {
            return NoContent();
        }

        /// <summary>
        /// Creates a bad request response.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="message">The error message.</param>
        /// <returns>An action result with an error API response.</returns>
        protected IActionResult BadRequest<T>(string message)
        {
            return BadRequest(ApiResponse<T>.CreateError(ApiErrorCodes.BadRequest, message));
        }

        /// <summary>
        /// Creates a bad request response with details.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="message">The error message.</param>
        /// <param name="details">The error details.</param>
        /// <returns>An action result with an error API response.</returns>
        protected IActionResult BadRequest<T>(string message, object details)
        {
            return BadRequest(ApiResponse<T>.CreateError(ApiErrorCodes.BadRequest, message, details));
        }

        /// <summary>
        /// Creates a validation error response.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <returns>An action result with an error API response.</returns>
        protected IActionResult ValidationError<T>()
        {
            var errors = ModelState
                .Where(e => e.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return BadRequest(ApiResponse<T>.CreateError(ApiErrorCodes.ValidationError, "Validation failed", errors));
        }

        /// <summary>
        /// Creates a not found response.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="message">The error message.</param>
        /// <returns>An action result with an error API response.</returns>
        protected IActionResult NotFound<T>(string message)
        {
            return NotFound(ApiResponse<T>.CreateError(ApiErrorCodes.NotFound, message));
        }

        /// <summary>
        /// Creates a conflict response.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="message">The error message.</param>
        /// <returns>An action result with an error API response.</returns>
        protected IActionResult Conflict<T>(string message)
        {
            return Conflict(ApiResponse<T>.CreateError(ApiErrorCodes.Conflict, message));
        }

        /// <summary>
        /// Creates an unauthorized response.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="message">The error message.</param>
        /// <returns>An action result with an error API response.</returns>
        protected IActionResult Unauthorized<T>(string message)
        {
            return Unauthorized(ApiResponse<T>.CreateError(ApiErrorCodes.Unauthorized, message));
        }

        /// <summary>
        /// Creates a forbidden response.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="message">The error message.</param>
        /// <returns>An action result with an error API response.</returns>
        protected IActionResult Forbidden<T>(string message)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.CreateError(ApiErrorCodes.Forbidden, message));
        }

        /// <summary>
        /// Creates an internal server error response.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="message">The error message.</param>
        /// <returns>An action result with an error API response.</returns>
        protected IActionResult InternalServerError<T>(string message)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<T>.CreateError(ApiErrorCodes.InternalServerError, message));
        }

        /// <summary>
        /// Creates a service unavailable response.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="message">The error message.</param>
        /// <returns>An action result with an error API response.</returns>
        protected IActionResult ServiceUnavailable<T>(string message)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<T>.CreateError(ApiErrorCodes.ServiceUnavailable, message));
        }

        /// <summary>
        /// Creates a too many requests response.
        /// </summary>
        /// <typeparam name="T">The type of data in the response.</typeparam>
        /// <param name="message">The error message.</param>
        /// <param name="retryAfterSeconds">The retry after time in seconds.</param>
        /// <returns>An action result with an error API response.</returns>
        protected IActionResult TooManyRequests<T>(string message, int retryAfterSeconds = 60)
        {
            Response.Headers.Add("Retry-After", retryAfterSeconds.ToString());
            return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse<T>.CreateError(ApiErrorCodes.RateLimited, message));
        }

        /// <summary>
        /// Gets the user ID from the claims.
        /// </summary>
        /// <returns>The user ID.</returns>
        protected string GetUserId()
        {
            return User.Identity.IsAuthenticated
                ? User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                : null;
        }

        /// <summary>
        /// Gets the user name from the claims.
        /// </summary>
        /// <returns>The user name.</returns>
        protected string GetUserName()
        {
            return User.Identity.IsAuthenticated
                ? User.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                : null;
        }

        /// <summary>
        /// Gets the user email from the claims.
        /// </summary>
        /// <returns>The user email.</returns>
        protected string GetUserEmail()
        {
            return User.Identity.IsAuthenticated
                ? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                : null;
        }

        /// <summary>
        /// Gets the user roles from the claims.
        /// </summary>
        /// <returns>The user roles.</returns>
        protected IEnumerable<string> GetUserRoles()
        {
            return User.Identity.IsAuthenticated
                ? User.Claims.Where(c => c.Type == "role").Select(c => c.Value)
                : Enumerable.Empty<string>();
        }

        /// <summary>
        /// Checks if the user has a specific role.
        /// </summary>
        /// <param name="role">The role to check.</param>
        /// <returns>True if the user has the role, false otherwise.</returns>
        protected bool UserHasRole(string role)
        {
            return User.Identity.IsAuthenticated && User.IsInRole(role);
        }
    }
}
