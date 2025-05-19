using System;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents a user-specified secret that can be accessed by JavaScript functions.
    /// </summary>
    public class UserSecret
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user secret.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the user secret.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the user secret.
        /// This is only stored in the TEE and never exposed outside.
        /// </summary>
        [Required]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the description of the user secret.
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the owner of the user secret.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
