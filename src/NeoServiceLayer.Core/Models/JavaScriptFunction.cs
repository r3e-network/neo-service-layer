using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents a JavaScript function that can be executed in the TEE.
    /// </summary>
    public class JavaScriptFunction
    {
        /// <summary>
        /// Gets or sets the unique identifier for the JavaScript function.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the JavaScript function.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the JavaScript function.
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript code to be executed.
        /// </summary>
        [Required]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the list of required secret names that this function needs access to.
        /// </summary>
        public List<string> RequiredSecrets { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the GAS limit for this function execution.
        /// </summary>
        public long GasLimit { get; set; } = 1000000;

        /// <summary>
        /// Gets or sets the status of the JavaScript function.
        /// </summary>
        public JavaScriptFunctionStatus Status { get; set; } = JavaScriptFunctionStatus.Active;

        /// <summary>
        /// Gets or sets the owner of the JavaScript function.
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

    /// <summary>
    /// Represents the status of a JavaScript function.
    /// </summary>
    public enum JavaScriptFunctionStatus
    {
        /// <summary>
        /// The JavaScript function is active and can be executed.
        /// </summary>
        Active,

        /// <summary>
        /// The JavaScript function is inactive and cannot be executed.
        /// </summary>
        Inactive,

        /// <summary>
        /// The JavaScript function is being reviewed.
        /// </summary>
        UnderReview,

        /// <summary>
        /// The JavaScript function has been rejected.
        /// </summary>
        Rejected
    }
}
