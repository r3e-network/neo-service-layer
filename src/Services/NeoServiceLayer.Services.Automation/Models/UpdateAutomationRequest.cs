using System;

namespace NeoServiceLayer.Services.Automation.Models
{
    /// <summary>
    /// Request model for updating an automation.
    /// </summary>
    public class UpdateAutomationRequest
    {
        /// <summary>
        /// Gets or sets the automation ID to update.
        /// </summary>
        public string AutomationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the updated name of the automation.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the updated description of the automation.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether the automation is active.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Gets or sets the updated trigger configuration (JSON serialized).
        /// </summary>
        public string? TriggerConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the updated action configuration (JSON serialized).
        /// </summary>
        public string? ActionConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the updated expiration date of the automation.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
}