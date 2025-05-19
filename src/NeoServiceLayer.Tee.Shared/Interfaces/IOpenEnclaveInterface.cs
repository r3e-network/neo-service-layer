using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Interfaces
{
    /// <summary>
    /// Compatibility interface for Open Enclave specific operations.
    /// DEPRECATED: Use IOcclumInterface directly instead.
    /// This interface is maintained for backward compatibility and will be removed in a future version.
    /// </summary>
    [Obsolete("This interface is deprecated. Use IOcclumInterface instead.")]
    public interface IOpenEnclaveInterface : IOcclumInterface
    {
        /// <summary>
        /// Gets the Open Enclave version.
        /// DEPRECATED: Use GetOcclumVersion() instead.
        /// </summary>
        /// <returns>The Open Enclave version.</returns>
        [Obsolete("This method is deprecated. Use GetOcclumVersion() instead.")]
        string GetOpenEnclaveVersion();
    }
}
