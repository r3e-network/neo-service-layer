using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Email address value object
    /// </summary>
    public class EmailAddress : ValueObject
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the email address value
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the domain part of the email
        /// </summary>
        public string Domain => Value.Substring(Value.IndexOf('@') + 1);

        /// <summary>
        /// Gets the local part of the email (before @)
        /// </summary>
        public string LocalPart => Value.Substring(0, Value.IndexOf('@'));

        /// <summary>
        /// Initializes a new instance of EmailAddress
        /// </summary>
        /// <param name="value">The email address value</param>
        private EmailAddress(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EmailAddress
        /// </summary>
        /// <param name="value">The email address value</param>
        /// <returns>A new EmailAddress instance</returns>
        public static EmailAddress Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainValidationException("EmailAddress", "Email address cannot be empty");

            value = value.Trim().ToLowerInvariant();

            if (value.Length > 320) // RFC 5321 limit
                throw new DomainValidationException("EmailAddress", "Email address cannot be longer than 320 characters");

            if (!EmailRegex.IsMatch(value))
                throw new DomainValidationException("EmailAddress", "Invalid email address format");

            return new EmailAddress(value);
        }

        /// <summary>
        /// Implicit conversion from string to EmailAddress
        /// </summary>
        public static implicit operator EmailAddress(string value) => Create(value);

        /// <summary>
        /// Implicit conversion from EmailAddress to string
        /// </summary>
        public static implicit operator string(EmailAddress email) => email.Value;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;
    }
}