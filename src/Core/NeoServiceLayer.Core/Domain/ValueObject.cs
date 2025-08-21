using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Base class for value objects in domain-driven design
    /// </summary>
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        /// <summary>
        /// Gets the components that define equality for this value object
        /// </summary>
        /// <returns>An enumerable of objects that define equality</returns>
        protected abstract IEnumerable<object> GetEqualityComponents();

        /// <summary>
        /// Determines whether this value object is equal to another
        /// </summary>
        /// <param name="other">The other value object to compare</param>
        /// <returns>True if the value objects are equal, false otherwise</returns>
        public bool Equals(ValueObject? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        /// <summary>
        /// Determines whether this value object is equal to another object
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if the objects are equal, false otherwise</returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as ValueObject);
        }

        /// <summary>
        /// Gets the hash code for this value object
        /// </summary>
        /// <returns>A hash code based on the equality components</returns>
        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Aggregate(1, (current, obj) => 
                {
                    unchecked
                    {
                        return current * 23 + (obj?.GetHashCode() ?? 0);
                    }
                });
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        public static bool operator ==(ValueObject? left, ValueObject? right)
        {
            if (left is null && right is null)
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator
        /// </summary>
        public static bool operator !=(ValueObject? left, ValueObject? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns a string representation of this value object
        /// </summary>
        public override string ToString()
        {
            var components = GetEqualityComponents().Select(x => x?.ToString() ?? "null");
            return $"{GetType().Name}({string.Join(", ", components)})";
        }
    }
}