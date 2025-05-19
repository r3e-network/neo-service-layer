namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Represents the result of a call to an enclave function.
    /// </summary>
    public class EnclaveCallResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the call was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the data returned by the function.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets the error message if the call was not successful.
        /// </summary>
        public string Error { get; set; }
    }
}
