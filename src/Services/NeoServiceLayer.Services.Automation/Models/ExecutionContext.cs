using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.Automation.Models
{
    /// <summary>
    /// Context for executing an automation.
    /// </summary>
    public class ExecutionContext
    {
        /// <summary>
        /// Gets or sets the user ID initiating the execution.
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the execution parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        /// <summary>
        /// Gets or sets whether this is a test execution.
        /// </summary>
        public bool IsTest { get; set; }
        
        /// <summary>
        /// Gets or sets the execution timeout in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;
        
        /// <summary>
        /// Gets or sets additional metadata for the execution.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
