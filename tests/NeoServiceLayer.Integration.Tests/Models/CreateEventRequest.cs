using System.ComponentModel.DataAnnotations;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Integration.Tests.Models
{
    /// <summary>
    /// Represents a request to create a new event.
    /// </summary>
    public class CreateEventRequest
    {
        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        [Required]
        public EventType Type { get; set; }

        /// <summary>
        /// Gets or sets the source of the event.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the data associated with the event.
        /// </summary>
        [Required]
        public string Data { get; set; }
    }
}
