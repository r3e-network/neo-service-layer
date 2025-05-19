using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Api.Models
{
    /// <summary>
    /// Request model for creating a user secret.
    /// </summary>
    public class CreateUserSecretRequest
    {
        /// <summary>
        /// Gets or sets the name of the user secret.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the user secret.
        /// </summary>
        [Required]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the description of the user secret.
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }
    }

    /// <summary>
    /// Request model for updating a user secret.
    /// </summary>
    public class UpdateUserSecretRequest
    {
        /// <summary>
        /// Gets or sets the name of the user secret.
        /// </summary>
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the user secret.
        /// If null, the value will not be updated.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the description of the user secret.
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }
    }

    /// <summary>
    /// Response model for a user secret.
    /// </summary>
    public class UserSecretResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user secret.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the user secret.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the user secret.
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
    }

    /// <summary>
    /// Response model for a list of user secrets.
    /// </summary>
    public class UserSecretListResponse
    {
        /// <summary>
        /// Gets or sets the list of user secrets.
        /// </summary>
        public List<UserSecretResponse> Secrets { get; set; }

        /// <summary>
        /// Gets or sets the total count of user secrets.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the current page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; }
    }
}
