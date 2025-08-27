using Microsoft.EntityFrameworkCore;
using Neo.Compute.Service.Models;

namespace Neo.Compute.Service.Data;

public class ComputeDbContext : DbContext
{
    public ComputeDbContext(DbContextOptions<ComputeDbContext> options) : base(options)
    {
    }

    public DbSet<ComputeJob> ComputeJobs { get; set; } = null!;
    public DbSet<SgxEnclave> SgxEnclaves { get; set; } = null!;
    public DbSet<AttestationResult> AttestationResults { get; set; } = null!;
    public DbSet<ComputeJobLog> ComputeJobLogs { get; set; } = null!;
    public DbSet<ResourceAllocation> ResourceAllocations { get; set; } = null!;
    public DbSet<SecureComputationSession> SecureComputationSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ComputeJob configuration
        modelBuilder.Entity<ComputeJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.JobType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Algorithm).IsRequired().HasMaxLength(100);
            entity.Property(e => e.InputDataHash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.OutputDataHash).HasMaxLength(256);
            entity.Property(e => e.ResultHash).HasMaxLength(256);
            entity.Property(e => e.Configuration).HasDefaultValue("{}");
            entity.Property(e => e.Metadata).HasDefaultValue("{}");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.EnclaveId);
            
            entity.HasOne(e => e.Enclave)
                  .WithMany(e => e.Jobs)
                  .HasForeignKey(e => e.EnclaveId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // SgxEnclave configuration
        modelBuilder.Entity<SgxEnclave>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EnclaveHash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(20);
            entity.Property(e => e.NodeName).HasMaxLength(100);
            entity.Property(e => e.PodName).HasMaxLength(100);
            entity.Property(e => e.Configuration).HasDefaultValue("{}");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.NodeName);
            entity.HasIndex(e => e.EnclaveHash);
            entity.HasIndex(e => e.LastHeartbeat);
        });

        // AttestationResult configuration
        modelBuilder.Entity<AttestationResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quote).IsRequired();
            entity.Property(e => e.Certificate).IsRequired();
            entity.Property(e => e.TcbLevel).HasDefaultValue("UpToDate");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.EnclaveId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);
            
            entity.HasOne(e => e.Enclave)
                  .WithMany(e => e.AttestationResults)
                  .HasForeignKey(e => e.EnclaveId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ComputeJobLog configuration
        modelBuilder.Entity<ComputeJobLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.Timestamp);
            
            entity.HasOne(e => e.Job)
                  .WithMany(e => e.Logs)
                  .HasForeignKey(e => e.JobId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ResourceAllocation configuration
        modelBuilder.Entity<ResourceAllocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResourceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.AllocatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Cost).HasPrecision(18, 4);
            
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.ResourceType);
            entity.HasIndex(e => e.AllocatedAt);
            
            entity.HasOne(e => e.Job)
                  .WithMany()
                  .HasForeignKey(e => e.JobId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SecureComputationSession configuration
        modelBuilder.Entity<SecureComputationSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(256);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.LastActivityAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.EnclaveId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExpiresAt);
            
            entity.HasOne(e => e.Enclave)
                  .WithMany()
                  .HasForeignKey(e => e.EnclaveId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}