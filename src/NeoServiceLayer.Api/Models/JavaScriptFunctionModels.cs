using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Api.Models
{
    /// <summary>
    /// Request model for creating a JavaScript function.
    /// </summary>
    public class CreateJavaScriptFunctionRequest
    {
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
        public long? GasLimit { get; set; }
    }

    /// <summary>
    /// Request model for updating a JavaScript function.
    /// </summary>
    public class UpdateJavaScriptFunctionRequest
    {
        /// <summary>
        /// Gets or sets the name of the JavaScript function.
        /// </summary>
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
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the list of required secret names that this function needs access to.
        /// </summary>
        public List<string> RequiredSecrets { get; set; }

        /// <summary>
        /// Gets or sets the GAS limit for this function execution.
        /// </summary>
        public long? GasLimit { get; set; }

        /// <summary>
        /// Gets or sets the status of the JavaScript function.
        /// </summary>
        public string Status { get; set; }
    }

    /// <summary>
    /// Response model for a JavaScript function.
    /// </summary>
    public class JavaScriptFunctionResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the JavaScript function.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the JavaScript function.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the JavaScript function.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript code to be executed.
        /// This is only included when explicitly requested.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the list of required secret names that this function needs access to.
        /// </summary>
        public List<string> RequiredSecrets { get; set; }

        /// <summary>
        /// Gets or sets the GAS limit for this function execution.
        /// </summary>
        public long GasLimit { get; set; }

        /// <summary>
        /// Gets or sets the status of the JavaScript function.
        /// </summary>
        public string Status { get; set; }

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
    /// Response model for a list of JavaScript functions.
    /// </summary>
    public class JavaScriptFunctionListResponse
    {
        /// <summary>
        /// Gets or sets the list of JavaScript functions.
        /// </summary>
        public List<JavaScriptFunctionResponse> Functions { get; set; }

        /// <summary>
        /// Gets or sets the total count of JavaScript functions.
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
