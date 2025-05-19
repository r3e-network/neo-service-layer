using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Interfaces
{
    /// <summary>
    /// Interface for SGX specific operations.
    /// This interface extends ITeeInterface to provide SGX specific functionality.
    /// </summary>
    public interface ISgxEnclaveInterface : ITeeInterface
    {
        /// <summary>
        /// Gets the extended product ID of the enclave.
        /// </summary>
        byte[] ExtendedProductId { get; }

        /// <summary>
        /// Gets the security version number of the enclave.
        /// </summary>
        ushort SecurityVersionNumber { get; }

        /// <summary>
        /// Gets the attributes of the enclave.
        /// </summary>
        ulong Attributes { get; }

        /// <summary>
        /// Gets the extended attributes of the enclave.
        /// </summary>
        ulong ExtendedAttributes { get; }

        /// <summary>
        /// Gets the enclave report for local attestation.
        /// </summary>
        /// <param name="targetInfo">The target info for the report.</param>
        /// <param name="reportData">The report data to include in the report.</param>
        /// <returns>The enclave report.</returns>
        byte[] GetEnclaveReport(byte[] targetInfo, byte[] reportData);

        /// <summary>
        /// Gets the quote for remote attestation.
        /// </summary>
        /// <param name="reportData">The report data to include in the quote.</param>
        /// <param name="quoteType">The quote type.</param>
        /// <param name="spid">The service provider ID.</param>
        /// <param name="sigRL">The signature revocation list.</param>
        /// <returns>The quote.</returns>
        byte[] GetQuote(byte[] reportData, int quoteType, byte[] spid, byte[] sigRL);

        /// <summary>
        /// Verifies a quote.
        /// </summary>
        /// <param name="quote">The quote to verify.</param>
        /// <param name="expectedMrEnclave">The expected MRENCLAVE value, or null to skip this check.</param>
        /// <param name="expectedMrSigner">The expected MRSIGNER value, or null to skip this check.</param>
        /// <returns>True if the quote is valid, false otherwise.</returns>
        bool VerifyQuote(byte[] quote, byte[] expectedMrEnclave, byte[] expectedMrSigner);

        /// <summary>
        /// Gets the launch token for the enclave.
        /// </summary>
        /// <param name="mrEnclave">The MRENCLAVE value.</param>
        /// <param name="mrSigner">The MRSIGNER value.</param>
        /// <param name="attributes">The enclave attributes.</param>
        /// <returns>The launch token.</returns>
        byte[] GetLaunchToken(byte[] mrEnclave, byte[] mrSigner, ulong attributes);
    }
}
