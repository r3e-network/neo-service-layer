using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoServiceLayer.Infrastructure.Data.Entities
{
    /// <summary>
    /// Represents an API key entity.
    /// </summary>
    [Table("ApiKeys")]
    public class ApiKeyEntity
    {
        /// <summary>
        /// Gets or sets the ID of the API key.
        /// </summary>
        [Key]
        [Required]
        [MaxLength(50)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the API key.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the hash of the API key.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string KeyHash { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who created the API key.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the API key.
        /// </summary>
        [Required]
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
        [Required]
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Gets or sets the revocation date of the API key.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets the roles associated with the API key.
        /// </summary>
        public virtual ICollection<ApiKeyRoleEntity> Roles { get; set; } = new List<ApiKeyRoleEntity>();

        /// <summary>
        /// Gets or sets the scopes associated with the API key.
        /// </summary>
        public virtual ICollection<ApiKeyScopeEntity> Scopes { get; set; } = new List<ApiKeyScopeEntity>();
    }

    /// <summary>
    /// Represents an API key role entity.
    /// </summary>
    [Table("ApiKeyRoles")]
    public class ApiKeyRoleEntity
    {
        /// <summary>
        /// Gets or sets the ID of the API key role.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the API key.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ApiKeyId { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        [ForeignKey("ApiKeyId")]
        public virtual ApiKeyEntity ApiKey { get; set; }
    }

    /// <summary>
    /// Represents an API key scope entity.
    /// </summary>
    [Table("ApiKeyScopes")]
    public class ApiKeyScopeEntity
    {
        /// <summary>
        /// Gets or sets the ID of the API key scope.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the API key.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ApiKeyId { get; set; }

        /// <summary>
        /// Gets or sets the scope.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Scope { get; set; }

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        [ForeignKey("ApiKeyId")]
        public virtual ApiKeyEntity ApiKey { get; set; }
    }
}
