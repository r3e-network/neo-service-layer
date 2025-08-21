using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Password value object with secure hashing
    /// </summary>
    public class Password : ValueObject
    {
        /// <summary>
        /// Gets the hashed password value
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of Password with an already hashed value
        /// </summary>
        /// <param name="hashedValue">The hashed password value</param>
        private Password(string hashedValue)
        {
            Value = hashedValue;
        }

        /// <summary>
        /// Creates a new Password from a plain text password
        /// </summary>
        /// <param name="plainTextPassword">The plain text password</param>
        /// <returns>A new Password instance with hashed value</returns>
        public static Password Create(string plainTextPassword)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword))
                throw new DomainValidationException("Password", "Password cannot be empty");

            var hashedPassword = HashPassword(plainTextPassword);
            return new Password(hashedPassword);
        }

        /// <summary>
        /// Creates a Password from an already hashed value (for loading from database)
        /// </summary>
        /// <param name="hashedValue">The hashed password value</param>
        /// <returns>A new Password instance</returns>
        public static Password FromHash(string hashedValue)
        {
            if (string.IsNullOrWhiteSpace(hashedValue))
                throw new DomainValidationException("Password", "Hashed password cannot be empty");

            return new Password(hashedValue);
        }

        /// <summary>
        /// Verifies a plain text password against this hashed password
        /// </summary>
        /// <param name="plainTextPassword">The plain text password to verify</param>
        /// <returns>True if the password matches</returns>
        public bool Verify(string plainTextPassword)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword))
                return false;

            return BCrypt.Net.BCrypt.Verify(plainTextPassword, Value);
        }

        /// <summary>
        /// Hashes a plain text password (temporary simple implementation)
        /// </summary>
        /// <param name="plainTextPassword">The plain text password</param>
        /// <returns>The hashed password</returns>
        private static string HashPassword(string plainTextPassword)
        {
            // TODO: Replace with proper BCrypt implementation
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainTextPassword));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => "[PROTECTED]";
    }
}