using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Infrastructure.Data.Entities;

namespace NeoServiceLayer.Infrastructure.Data
{
    /// <summary>
    /// Database context for the Neo Service Layer.
    /// </summary>
    public class NeoServiceLayerDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the NeoServiceLayerDbContext class.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public NeoServiceLayerDbContext(DbContextOptions<NeoServiceLayerDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the attestation proofs.
        /// </summary>
        public DbSet<AttestationProofEntity> AttestationProofs { get; set; }

        /// <summary>
        /// Gets or sets the TEE accounts.
        /// </summary>
        public DbSet<TeeAccountEntity> TeeAccounts { get; set; }

        /// <summary>
        /// Gets or sets the tasks.
        /// </summary>
        public DbSet<TaskEntity> Tasks { get; set; }

        /// <summary>
        /// Gets or sets the verification results.
        /// </summary>
        public DbSet<VerificationResultEntity> VerificationResults { get; set; }

        /// <summary>
        /// Configures the model that was discovered by convention from the entity types.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure AttestationProofEntity
            modelBuilder.Entity<AttestationProofEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Report).IsRequired();
                entity.Property(e => e.Signature).IsRequired();
                entity.Property(e => e.MrEnclave).IsRequired();
                entity.Property(e => e.MrSigner).IsRequired();
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.SecurityVersion).IsRequired();
                entity.Property(e => e.Attributes).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.ExpiresAt).IsRequired();
                entity.HasIndex(e => e.CreatedAt);
            });

            // Configure TeeAccountEntity
            modelBuilder.Entity<TeeAccountEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.PublicKey).IsRequired();
                entity.Property(e => e.Address).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsExportable).IsRequired();
                entity.HasIndex(e => e.UserId);
            });

            // Configure TaskEntity
            modelBuilder.Entity<TaskEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
            });

            // Configure VerificationResultEntity
            modelBuilder.Entity<VerificationResultEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasIndex(e => e.Status);
            });
        }
    }
}
