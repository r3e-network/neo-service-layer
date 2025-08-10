using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace NeoServiceLayer.Infrastructure.Security.Authentication
{
    /// <summary>
    /// Interface for JWT token generation and validation.
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Generates a JWT token for the specified user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="username">The username.</param>
        /// <param name="roles">The user roles.</param>
        /// <param name="additionalClaims">Additional claims to include in the token.</param>
        /// <returns>The generated JWT token.</returns>
        string GenerateToken(string userId, string username, IEnumerable<string> roles, Dictionary<string, string>? additionalClaims = null);

        /// <summary>
        /// Generates a service-to-service JWT token.
        /// </summary>
        /// <param name="serviceName">The service name.</param>
        /// <param name="serviceId">The service ID.</param>
        /// <returns>The generated JWT token.</returns>
        string GenerateServiceToken(string serviceName, string serviceId);

        /// <summary>
        /// Validates a JWT token and returns the claims principal.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>The claims principal if valid, null otherwise.</returns>
        ClaimsPrincipal? ValidateToken(string token);

        /// <summary>
        /// Gets the expiration time of a token.
        /// </summary>
        /// <param name="token">The JWT token.</param>
        /// <returns>The expiration time if valid, null otherwise.</returns>
        DateTime? GetTokenExpiration(string token);
    }
}