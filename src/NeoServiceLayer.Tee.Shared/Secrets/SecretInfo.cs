using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Shared.Secrets
{
    /// <summary>
    /// Information about a secret.
    /// </summary>
    public class SecretInfo
    {
        /// <summary>
        /// Gets or sets the ID of the secret.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the secret.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the secret.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who owns the secret.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the description of the secret.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the expiration timestamp.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the last access timestamp.
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }

        /// <summary>
        /// Gets or sets the number of times the secret has been accessed.
        /// </summary>
        public int AccessCount { get; set; }

        /// <summary>
        /// Gets or sets the version of the secret.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the tags associated with the secret.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the metadata associated with the secret.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the type of the secret.
        /// </summary>
        public SecretType Type { get; set; } = SecretType.String;

        /// <summary>
        /// Gets or sets the status of the secret.
        /// </summary>
        public SecretStatus Status { get; set; } = SecretStatus.Active;

        /// <summary>
        /// Initializes a new instance of the SecretInfo class.
        /// </summary>
        public SecretInfo()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Version = 1;
        }

        /// <summary>
        /// Initializes a new instance of the SecretInfo class.
        /// </summary>
        /// <param name="userId">The ID of the user who owns the secret.</param>
        /// <param name="name">The name of the secret.</param>
        /// <param name="value">The value of the secret.</param>
        public SecretInfo(string userId, string name, string value)
        {
            Id = Guid.NewGuid().ToString();
            UserId = userId;
            Name = name;
            Value = value;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Version = 1;
        }

        /// <summary>
        /// Initializes a new instance of the SecretInfo class.
        /// </summary>
        /// <param name="userId">The ID of the user who owns the secret.</param>
        /// <param name="name">The name of the secret.</param>
        /// <param name="value">The value of the secret.</param>
        /// <param name="description">The description of the secret.</param>
        /// <param name="expiresAt">The expiration timestamp.</param>
        /// <param name="type">The type of the secret.</param>
        public SecretInfo(string userId, string name, string value, string description, DateTime? expiresAt, SecretType type)
        {
            Id = Guid.NewGuid().ToString();
            UserId = userId;
            Name = name;
            Value = value;
            Description = description;
            ExpiresAt = expiresAt;
            Type = type;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Version = 1;
        }

        /// <summary>
        /// Creates a copy of the secret info without the value.
        /// </summary>
        /// <returns>A copy of the secret info without the value.</returns>
        public SecretInfo WithoutValue()
        {
            return new SecretInfo
            {
                Id = Id,
                Name = Name,
                UserId = UserId,
                Description = Description,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                ExpiresAt = ExpiresAt,
                LastAccessedAt = LastAccessedAt,
                AccessCount = AccessCount,
                Version = Version,
                Tags = new List<string>(Tags),
                Metadata = new Dictionary<string, string>(Metadata),
                Type = Type,
                Status = Status
            };
        }

        /// <summary>
        /// Updates the access information.
        /// </summary>
        public void UpdateAccessInfo()
        {
            LastAccessedAt = DateTime.UtcNow;
            AccessCount++;
        }

        /// <summary>
        /// Updates the secret value.
        /// </summary>
        /// <param name="value">The new value of the secret.</param>
        public void UpdateValue(string value)
        {
            Value = value;
            UpdatedAt = DateTime.UtcNow;
            Version++;
        }

        /// <summary>
        /// Checks if the secret has expired.
        /// </summary>
        /// <returns>True if the secret has expired, false otherwise.</returns>
        public bool HasExpired()
        {
            return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Types of secrets.
    /// </summary>
    public enum SecretType
    {
        /// <summary>
        /// String secret.
        /// </summary>
        String,

        /// <summary>
        /// Binary secret.
        /// </summary>
        Binary,

        /// <summary>
        /// JSON secret.
        /// </summary>
        Json,

        /// <summary>
        /// Certificate secret.
        /// </summary>
        Certificate,

        /// <summary>
        /// Key secret.
        /// </summary>
        Key,

        /// <summary>
        /// Password secret.
        /// </summary>
        Password,

        /// <summary>
        /// API key secret.
        /// </summary>
        ApiKey,

        /// <summary>
        /// OAuth token secret.
        /// </summary>
        OAuthToken
    }

    /// <summary>
    /// Status of a secret.
    /// </summary>
    public enum SecretStatus
    {
        /// <summary>
        /// The secret is active.
        /// </summary>
        Active,

        /// <summary>
        /// The secret is disabled.
        /// </summary>
        Disabled,

        /// <summary>
        /// The secret is expired.
        /// </summary>
        Expired,

        /// <summary>
        /// The secret is deleted.
        /// </summary>
        Deleted,

        /// <summary>
        /// The secret is pending rotation.
        /// </summary>
        PendingRotation,

        /// <summary>
        /// The secret is being rotated.
        /// </summary>
        Rotating
    }
}
