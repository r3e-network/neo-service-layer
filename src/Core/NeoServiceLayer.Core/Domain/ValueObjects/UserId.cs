using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// User identifier value object
    /// </summary>
    public class UserId : ValueObject
    {
        /// <summary>
        /// Gets the user ID value
        /// </summary>
        public Guid Value { get; }

        /// <summary>
        /// Initializes a new instance of UserId
        /// </summary>
        /// <param name="value">The user ID value</param>
        private UserId(Guid value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new UserId
        /// </summary>
        /// <param name="value">The user ID value</param>
        /// <returns>A new UserId instance</returns>
        public static UserId Create(Guid value)
        {
            if (value == Guid.Empty)
                throw new DomainValidationException("UserId", "User ID cannot be empty");

            return new UserId(value);
        }

        /// <summary>
        /// Creates a new random UserId
        /// </summary>
        /// <returns>A new UserId instance</returns>
        public static UserId NewId() => new UserId(Guid.NewGuid());

        /// <summary>
        /// Implicit conversion from Guid to UserId
        /// </summary>
        public static implicit operator UserId(Guid value) => Create(value);

        /// <summary>
        /// Implicit conversion from UserId to Guid
        /// </summary>
        public static implicit operator Guid(UserId userId) => userId.Value;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value.ToString();
    }
}