using System.Threading.Tasks;
using NeoServiceLayer.Core.Attributes;

namespace NeoServiceLayer.Services.Authentication.Contracts
{
    /// <summary>
    /// Interface for user registration operations
    /// </summary>
    public interface IUserRegistration
    {
        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">Registration request</param>
        /// <returns>Registration result</returns>
        Task<RegistrationResult> RegisterAsync(UserRegistrationRequest request);

        /// <summary>
        /// Validates password strength
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <returns>True if password meets requirements</returns>
        Task<bool> ValidatePasswordStrengthAsync(string password);
    }
}