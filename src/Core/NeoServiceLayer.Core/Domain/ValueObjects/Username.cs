using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Username value object
    /// </summary>
    public class Username : ValueObject
    {
        private static readonly Regex UsernameRegex = new Regex(
            @"^[a-zA-Z0-9_.-]{3,50}$", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the username value
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of Username
        /// </summary>
        /// <param name="value">The username value</param>
        private Username(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new Username
        /// </summary>
        /// <param name="value">The username value</param>
        /// <returns>A new Username instance</returns>
        public static Username Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainValidationException("Username", "Username cannot be empty");

            value = value.Trim();

            if (value.Length < 3)
                throw new DomainValidationException("Username", "Username must be at least 3 characters long");

            if (value.Length > 50)
                throw new DomainValidationException("Username", "Username cannot be longer than 50 characters");

            if (!UsernameRegex.IsMatch(value))
                throw new DomainValidationException("Username", "Username contains invalid characters. Only letters, numbers, dots, hyphens and underscores are allowed");

            return new Username(value);
        }

        /// <summary>
        /// Implicit conversion from string to Username
        /// </summary>
        public static implicit operator Username(string value) => Create(value);

        /// <summary>
        /// Implicit conversion from Username to string
        /// </summary>
        public static implicit operator string(Username username) => username.Value;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value.ToLowerInvariant();
        }

        public override string ToString() => Value;
    }
}