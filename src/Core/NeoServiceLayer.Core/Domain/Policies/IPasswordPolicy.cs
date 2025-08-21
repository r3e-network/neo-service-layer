using System.Collections.Generic;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Interface for password policy enforcement
    /// </summary>
    public interface IPasswordPolicy
    {
        /// <summary>
        /// Gets the maximum number of failed login attempts before account lockout
        /// </summary>
        int MaxFailedLoginAttempts { get; }

        /// <summary>
        /// Gets the minimum password length
        /// </summary>
        int MinimumLength { get; }

        /// <summary>
        /// Gets whether uppercase letters are required
        /// </summary>
        bool RequireUppercase { get; }

        /// <summary>
        /// Gets whether lowercase letters are required
        /// </summary>
        bool RequireLowercase { get; }

        /// <summary>
        /// Gets whether digits are required
        /// </summary>
        bool RequireDigit { get; }

        /// <summary>
        /// Gets whether special characters are required
        /// </summary>
        bool RequireSpecialCharacter { get; }

        /// <summary>
        /// Gets the forbidden passwords list
        /// </summary>
        IReadOnlyList<string> ForbiddenPasswords { get; }

        /// <summary>
        /// Validates a password against the policy
        /// </summary>
        /// <param name="password">The password to validate</param>
        /// <returns>Validation result</returns>
        PasswordValidationResult ValidatePassword(string password);
    }

    /// <summary>
    /// Result of password validation
    /// </summary>
    public class PasswordValidationResult
    {
        /// <summary>
        /// Gets whether the password is valid
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the validation errors
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Initializes a new instance of PasswordValidationResult
        /// </summary>
        /// <param name="isValid">Whether the password is valid</param>
        /// <param name="errors">Validation errors</param>
        public PasswordValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors ?? new List<string>();
        }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        /// <returns>Successful validation result</returns>
        public static PasswordValidationResult Success() => 
            new PasswordValidationResult(true, new List<string>());

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        /// <param name="errors">Validation errors</param>
        /// <returns>Failed validation result</returns>
        public static PasswordValidationResult Failure(params string[] errors) => 
            new PasswordValidationResult(false, errors);
    }

    /// <summary>
    /// Default enterprise password policy implementation
    /// </summary>
    public class EnterprisePasswordPolicy : IPasswordPolicy
    {
        public int MaxFailedLoginAttempts => 5;
        public int MinimumLength => 12;
        public bool RequireUppercase => true;
        public bool RequireLowercase => true;
        public bool RequireDigit => true;
        public bool RequireSpecialCharacter => true;

        public IReadOnlyList<string> ForbiddenPasswords => new[]
        {
            "password",
            "Password123!",
            "Admin123!",
            "Welcome123!",
            "P@ssw0rd",
            "123456789",
            "qwerty123"
        };

        public PasswordValidationResult ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password cannot be empty");
                return PasswordValidationResult.Failure(errors.ToArray());
            }

            if (password.Length < MinimumLength)
                errors.Add($"Password must be at least {MinimumLength} characters long");

            if (RequireUppercase && !HasUppercase(password))
                errors.Add("Password must contain at least one uppercase letter");

            if (RequireLowercase && !HasLowercase(password))
                errors.Add("Password must contain at least one lowercase letter");

            if (RequireDigit && !HasDigit(password))
                errors.Add("Password must contain at least one digit");

            if (RequireSpecialCharacter && !HasSpecialCharacter(password))
                errors.Add("Password must contain at least one special character");

            if (IsForbiddenPassword(password))
                errors.Add("This password is not allowed");

            if (HasRepeatingCharacters(password))
                errors.Add("Password contains too many repeating characters");

            return errors.Count == 0 
                ? PasswordValidationResult.Success() 
                : PasswordValidationResult.Failure(errors.ToArray());
        }

        private static bool HasUppercase(string password) => 
            password.Any(char.IsUpper);

        private static bool HasLowercase(string password) => 
            password.Any(char.IsLower);

        private static bool HasDigit(string password) => 
            password.Any(char.IsDigit);

        private static bool HasSpecialCharacter(string password) => 
            password.Any(c => !char.IsLetterOrDigit(c));

        private bool IsForbiddenPassword(string password) => 
            ForbiddenPasswords.Any(forbidden => 
                string.Equals(password, forbidden, System.StringComparison.OrdinalIgnoreCase));

        private static bool HasRepeatingCharacters(string password, int maxRepeating = 3)
        {
            for (int i = 0; i < password.Length - maxRepeating; i++)
            {
                var currentChar = password[i];
                var repeatingCount = 1;

                for (int j = i + 1; j < password.Length && password[j] == currentChar; j++)
                {
                    repeatingCount++;
                    if (repeatingCount > maxRepeating)
                        return true;
                }
            }

            return false;
        }
    }
}