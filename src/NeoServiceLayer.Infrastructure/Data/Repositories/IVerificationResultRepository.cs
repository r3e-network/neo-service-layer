using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Infrastructure.Data.Entities;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Interface for the verification result repository.
    /// </summary>
    public interface IVerificationResultRepository : IRepository<VerificationResultEntity, string>
    {
        /// <summary>
        /// Gets a verification result by its ID.
        /// </summary>
        /// <param name="verificationId">The verification ID.</param>
        /// <returns>The verification result, or null if not found.</returns>
        Task<Shared.Models.VerificationResult> GetVerificationResultByIdAsync(string verificationId);

        /// <summary>
        /// Gets all verification results by status.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns>The verification results with the specified status.</returns>
        Task<IEnumerable<Shared.Models.VerificationResult>> GetByStatusAsync(string status);

        /// <summary>
        /// Adds a verification result.
        /// </summary>
        /// <param name="verificationResult">The verification result to add.</param>
        /// <param name="verificationType">The verification type.</param>
        /// <param name="identityData">The encrypted identity data.</param>
        /// <returns>The added verification result.</returns>
        Task<Shared.Models.VerificationResult> AddVerificationResultAsync(Shared.Models.VerificationResult verificationResult, string verificationType, string identityData);

        /// <summary>
        /// Updates a verification result.
        /// </summary>
        /// <param name="verificationResult">The verification result to update.</param>
        /// <returns>The updated verification result.</returns>
        Task<Shared.Models.VerificationResult> UpdateVerificationResultAsync(Shared.Models.VerificationResult verificationResult);
    }
}
