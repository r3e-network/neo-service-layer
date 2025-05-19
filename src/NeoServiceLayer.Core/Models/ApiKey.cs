using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents an API key.
    /// </summary>
    public class ApiKey
    {
        /// <summary>
        /// Gets or sets the ID of the API key.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the API key.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the API key value.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who created the API key.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the API key.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the API key.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the last used date of the API key.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the API key is revoked.
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Gets or sets the revocation date of the API key.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets the roles associated with the API key.
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the scopes associated with the API key.
        /// </summary>
        public List<string> Scopes { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents API key information.
    /// </summary>
    public class ApiKeyInfo
    {
        /// <summary>
        /// Gets or sets the ID of the API key.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the API key.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who created the API key.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the API key.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the roles associated with the API key.
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the scopes associated with the API key.
        /// </summary>
        public List<string> Scopes { get; set; } = new List<string>();
    }
}
