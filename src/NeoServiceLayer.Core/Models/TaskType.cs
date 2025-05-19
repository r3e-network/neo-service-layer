namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents the type of a task.
    /// </summary>
    public enum TaskType
    {
        /// <summary>
        /// A compute task.
        /// </summary>
        Compute = 0,

        /// <summary>
        /// A data processing task.
        /// </summary>
        DataProcessing = 1,

        /// <summary>
        /// A key management task.
        /// </summary>
        KeyManagement = 2,

        /// <summary>
        /// A randomness generation task.
        /// </summary>
        Randomness = 3,

        /// <summary>
        /// A compliance verification task.
        /// </summary>
        Compliance = 4,

        /// <summary>
        /// An event subscription task.
        /// </summary>
        EventSubscription = 5,

        /// <summary>
        /// A smart contract execution task.
        /// </summary>
        SmartContractExecution = 6,

        /// <summary>
        /// A compute offloading task.
        /// </summary>
        ComputeOffloading = 7
    }
}
