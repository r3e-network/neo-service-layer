using System.ComponentModel.DataAnnotations;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Integration.Tests.Models
{
    /// <summary>
    /// Represents a request to create a new account.
    /// </summary>
    public class CreateAccountRequest
    {
        /// <summary>
        /// Gets or sets the type of the account.
        /// </summary>
        [Required]
        public AccountType Type { get; set; }

        /// <summary>
        /// Gets or sets the name of the account.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether the account is exportable.
        /// </summary>
        public bool IsExportable { get; set; } = false;
    }
}
