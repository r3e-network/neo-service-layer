using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Role value object
    /// </summary>
    public class Role : ValueObject
    {
        /// <summary>
        /// Gets the role name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the role description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Initializes a new instance of Role
        /// </summary>
        /// <param name="name">The role name</param>
        /// <param name="description">The role description</param>
        private Role(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Creates a new Role
        /// </summary>
        /// <param name="name">The role name</param>
        /// <param name="description">The role description</param>
        /// <returns>A new Role instance</returns>
        public static Role Create(string name, string description = "")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainValidationException("Role", "Role name cannot be empty");

            name = name.Trim();

            if (name.Length > 100)
                throw new DomainValidationException("Role", "Role name cannot be longer than 100 characters");

            return new Role(name, description?.Trim() ?? string.Empty);
        }

        // Predefined common roles
        public static Role Admin => Create("Admin", "System administrator with full access");
        public static Role User => Create("User", "Regular user with standard access");
        public static Role Moderator => Create("Moderator", "User with moderation privileges");
        public static Role Guest => Create("Guest", "Limited access guest user");

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name.ToUpperInvariant();
        }

        public override string ToString() => Name;
    }
}