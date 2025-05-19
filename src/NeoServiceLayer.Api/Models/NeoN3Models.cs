using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Api.Models
{
    /// <summary>
    /// Request model for contract invocation.
    /// </summary>
    public class ContractInvocationRequest
    {
        /// <summary>
        /// Gets or sets the script hash of the contract.
        /// </summary>
        [Required]
        public string ScriptHash { get; set; }

        /// <summary>
        /// Gets or sets the operation to invoke.
        /// </summary>
        [Required]
        public string Operation { get; set; }

        /// <summary>
        /// Gets or sets the arguments to pass to the operation.
        /// </summary>
        public object[] Args { get; set; }
    }

    /// <summary>
    /// Request model for event subscription.
    /// </summary>
    public class EventSubscriptionRequest
    {
        /// <summary>
        /// Gets or sets the script hash of the contract.
        /// </summary>
        [Required]
        public string ScriptHash { get; set; }

        /// <summary>
        /// Gets or sets the name of the event to subscribe to.
        /// </summary>
        [Required]
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the URL to call when the event is detected.
        /// </summary>
        [Required]
        [Url]
        public string CallbackUrl { get; set; }

        /// <summary>
        /// Gets or sets the block to start listening from.
        /// </summary>
        public uint StartBlock { get; set; }
    }
}
