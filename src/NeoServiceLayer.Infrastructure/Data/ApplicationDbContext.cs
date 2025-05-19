using System;
using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Infrastructure.Data.Entities;

namespace NeoServiceLayer.Infrastructure.Data
{
    /// <summary>
    /// Application database context.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the ApplicationDbContext class.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the API keys.
        /// </summary>
        public DbSet<ApiKeyEntity> ApiKeys { get; set; }

        /// <summary>
        /// Gets or sets the API key roles.
        /// </summary>
        public DbSet<ApiKeyRoleEntity> ApiKeyRoles { get; set; }

        /// <summary>
        /// Gets or sets the API key scopes.
        /// </summary>
        public DbSet<ApiKeyScopeEntity> ApiKeyScopes { get; set; }

        /// <summary>
        /// Configures the model.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure API key entity
            modelBuilder.Entity<ApiKeyEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsRevoked).IsRequired();

                // Configure relationships
                entity.HasMany(e => e.Roles)
                    .WithOne(e => e.ApiKey)
                    .HasForeignKey(e => e.ApiKeyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Scopes)
                    .WithOne(e => e.ApiKey)
                    .HasForeignKey(e => e.ApiKeyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure API key role entity
            modelBuilder.Entity<ApiKeyRoleEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ApiKeyId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);

                // Create a unique index on ApiKeyId and Role
                entity.HasIndex(e => new { e.ApiKeyId, e.Role }).IsUnique();
            });

            // Configure API key scope entity
            modelBuilder.Entity<ApiKeyScopeEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ApiKeyId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Scope).IsRequired().HasMaxLength(50);

                // Create a unique index on ApiKeyId and Scope
                entity.HasIndex(e => new { e.ApiKeyId, e.Scope }).IsUnique();
            });
        }
    }
}
