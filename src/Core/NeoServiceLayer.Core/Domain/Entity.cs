using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Base class for domain entities
    /// </summary>
    /// <typeparam name="TId">The type of the entity identifier</typeparam>
    public abstract class Entity<TId> : IEquatable<Entity<TId>>
        where TId : class
    {
        /// <summary>
        /// Gets the entity identifier
        /// </summary>
        public TId Id { get; protected set; }

        /// <summary>
        /// Initializes a new instance of Entity
        /// </summary>
        /// <param name="id">The entity identifier</param>
        protected Entity(TId id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        /// <summary>
        /// Protected constructor for EF and serialization
        /// </summary>
        protected Entity()
        {
            Id = default!;
        }

        /// <summary>
        /// Determines whether the specified entity is equal to this entity
        /// </summary>
        /// <param name="other">The entity to compare with this entity</param>
        /// <returns>True if the entities are equal</returns>
        public bool Equals(Entity<TId>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return GetType() == other.GetType() && Id.Equals(other.Id);
        }

        /// <summary>
        /// Determines whether the specified object is equal to this entity
        /// </summary>
        /// <param name="obj">The object to compare with this entity</param>
        /// <returns>True if the objects are equal</returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as Entity<TId>);
        }

        /// <summary>
        /// Gets the hash code for this entity
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(GetType(), Id);
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="left">Left entity</param>
        /// <param name="right">Right entity</param>
        /// <returns>True if entities are equal</returns>
        public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Inequality operator
        /// </summary>
        /// <param name="left">Left entity</param>
        /// <param name="right">Right entity</param>
        /// <returns>True if entities are not equal</returns>
        public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        {
            return !Equals(left, right);
        }
    }
}