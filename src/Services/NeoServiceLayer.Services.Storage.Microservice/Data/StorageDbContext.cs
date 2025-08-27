using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Models;

namespace Neo.Storage.Service.Data;

public class StorageDbContext : DbContext
{
    public StorageDbContext(DbContextOptions<StorageDbContext> options) : base(options)
    {
    }

    public DbSet<StorageObject> StorageObjects { get; set; } = null!;
    public DbSet<StorageBucket> StorageBuckets { get; set; } = null!;
    public DbSet<StorageReplica> StorageReplicas { get; set; } = null!;
    public DbSet<StorageNode> StorageNodes { get; set; } = null!;
    public DbSet<ObjectVersion> ObjectVersions { get; set; } = null!;
    public DbSet<BucketPolicy> BucketPolicies { get; set; } = null!;
    public DbSet<AccessLog> AccessLogs { get; set; } = null!;
    public DbSet<NodeHealth> NodeHealthChecks { get; set; } = null!;
    public DbSet<ReplicationJob> ReplicationJobs { get; set; } = null!;
    public DbSet<StorageTransaction> StorageTransactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // StorageObject configuration
        modelBuilder.Entity<StorageObject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.BucketName).IsRequired().HasMaxLength(63);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Hash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Metadata).HasDefaultValue("{}");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.Key);
            entity.HasIndex(e => e.BucketName);
            entity.HasIndex(e => e.Hash);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StorageClass);
            entity.HasIndex(e => new { e.BucketName, e.Key }).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
        });

        // StorageBucket configuration
        modelBuilder.Entity<StorageBucket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(63);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Region).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Configuration).HasDefaultValue("{}");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Region);
            entity.HasIndex(e => e.Type);
        });

        // StorageReplica configuration
        modelBuilder.Entity<StorageReplica>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PhysicalPath).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.Hash).HasMaxLength(256);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.LastVerified).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.ObjectId);
            entity.HasIndex(e => e.NodeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.ObjectId, e.NodeId }).IsUnique();
            
            entity.HasOne(e => e.Object)
                  .WithMany(e => e.Replicas)
                  .HasForeignKey(e => e.ObjectId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Node)
                  .WithMany(e => e.Replicas)
                  .HasForeignKey(e => e.NodeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // StorageNode configuration
        modelBuilder.Entity<StorageNode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Region).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Zone).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Configuration).HasDefaultValue("{}");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.LastHeartbeat).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Region);
            entity.HasIndex(e => e.Zone);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.LastHeartbeat);
        });

        // ObjectVersion configuration
        modelBuilder.Entity<ObjectVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VersionId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Hash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.ObjectId);
            entity.HasIndex(e => new { e.ObjectId, e.VersionId }).IsUnique();
            entity.HasIndex(e => new { e.ObjectId, e.IsLatest });
            
            entity.HasOne(e => e.Object)
                  .WithMany(e => e.Versions)
                  .HasForeignKey(e => e.ObjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // BucketPolicy configuration
        modelBuilder.Entity<BucketPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Principal).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Effect).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Actions).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.BucketId);
            entity.HasIndex(e => e.Principal);
            
            entity.HasOne(e => e.Bucket)
                  .WithMany(e => e.Policies)
                  .HasForeignKey(e => e.BucketId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AccessLog configuration
        modelBuilder.Entity<AccessLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(20);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45); // IPv6 support
            entity.Property(e => e.UserAgent).HasMaxLength(512);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.ObjectId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Action);
            
            entity.HasOne(e => e.Object)
                  .WithMany(e => e.AccessLogs)
                  .HasForeignKey(e => e.ObjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // NodeHealth configuration
        modelBuilder.Entity<NodeHealth>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HealthData).HasDefaultValue("{}");
            entity.Property(e => e.CheckedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.NodeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CheckedAt);
            
            entity.HasOne(e => e.Node)
                  .WithMany(e => e.HealthChecks)
                  .HasForeignKey(e => e.NodeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ReplicationJob configuration
        modelBuilder.Entity<ReplicationJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.ObjectId);
            entity.HasIndex(e => e.SourceNodeId);
            entity.HasIndex(e => e.TargetNodeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.CreatedAt);
        });

        // StorageTransaction configuration
        modelBuilder.Entity<StorageTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Operations).HasDefaultValue("[]");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Cost).HasPrecision(18, 4);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure relationships
        modelBuilder.Entity<StorageObject>()
            .HasOne<StorageBucket>()
            .WithMany(b => b.Objects)
            .HasForeignKey(o => o.BucketName)
            .HasPrincipalKey(b => b.Name)
            .OnDelete(DeleteBehavior.Cascade);
    }
}