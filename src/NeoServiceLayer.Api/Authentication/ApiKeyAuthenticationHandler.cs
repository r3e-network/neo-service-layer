using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoServiceLayer.Core.Interfaces;

namespace NeoServiceLayer.Api.Authentication
{
    /// <summary>
    /// Authentication handler for API key authentication.
    /// </summary>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private const string ApiKeyHeaderName = "X-API-Key";
        private readonly IApiKeyService _apiKeyService;

        /// <summary>
        /// Initializes a new instance of the ApiKeyAuthenticationHandler class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="encoder">The URL encoder.</param>
        /// <param name="clock">The system clock.</param>
        /// <param name="apiKeyService">The API key service.</param>
        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IApiKeyService apiKeyService)
            : base(options, logger, encoder, clock)
        {
            _apiKeyService = apiKeyService ?? throw new ArgumentNullException(nameof(apiKeyService));
        }

        /// <summary>
        /// Handles the authentication.
        /// </summary>
        /// <returns>The authentication result.</returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check if the API key is in the request header
            if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
            {
                return AuthenticateResult.NoResult();
            }

            var apiKey = apiKeyHeaderValues.FirstOrDefault();
            if (string.IsNullOrEmpty(apiKey))
            {
                return AuthenticateResult.NoResult();
            }

            // Validate the API key
            var apiKeyInfo = await _apiKeyService.ValidateApiKeyAsync(apiKey);
            if (apiKeyInfo == null)
            {
                return AuthenticateResult.Fail("Invalid API key");
            }

            // Check if the API key is expired
            if (apiKeyInfo.ExpiresAt.HasValue && apiKeyInfo.ExpiresAt.Value < DateTime.UtcNow)
            {
                return AuthenticateResult.Fail("Expired API key");
            }

            // Create the claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, apiKeyInfo.Id),
                new Claim(ClaimTypes.Name, apiKeyInfo.Name),
                new Claim("api_key_id", apiKeyInfo.Id)
            };

            // Add roles
            foreach (var role in apiKeyInfo.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add scopes
            foreach (var scope in apiKeyInfo.Scopes)
            {
                claims.Add(new Claim("scope", scope));
            }

            // Create the identity and principal
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }

    /// <summary>
    /// Options for API key authentication.
    /// </summary>
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Gets or sets the realm.
        /// </summary>
        public string Realm { get; set; }
    }
}
