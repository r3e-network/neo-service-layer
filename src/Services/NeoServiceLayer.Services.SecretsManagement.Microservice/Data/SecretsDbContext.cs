using Microsoft.EntityFrameworkCore;
using Neo.SecretsManagement.Service.Models;
using System.Text.Json;

namespace Neo.SecretsManagement.Service.Data;

public class SecretsDbContext : DbContext
{
    public SecretsDbContext(DbContextOptions<SecretsDbContext> options) : base(options)
    {
    }

    public DbSet<Secret> Secrets { get; set; }
    public DbSet<SecretVersion> SecretVersions { get; set; }
    public DbSet<EncryptionKey> EncryptionKeys { get; set; }
    public DbSet<SecretAccess> SecretAccesses { get; set; }
    public DbSet<SecretShare> SecretShares { get; set; }
    public DbSet<SecretPolicy> SecretPolicies { get; set; }
    public DbSet<RotationJob> RotationJobs { get; set; }
    public DbSet<SecretBackup> SecretBackups { get; set; }
    public DbSet<AuditLogEntry> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Secret entity
        modelBuilder.Entity<Secret>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Path).IsUnique();
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => e.ExpiresAt).HasFilter("expires_at IS NOT NULL");

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.EncryptedValue).IsRequired();
            entity.Property(e => e.KeyId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastModifiedBy).IsRequired().HasMaxLength(100);

            // Configure JSON columns for PostgreSQL
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new());

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            entity.HasMany(e => e.Versions)
                .WithOne(e => e.Secret)
                .HasForeignKey(e => e.SecretId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.AccessHistory)
                .WithOne(e => e.Secret)
                .HasForeignKey(e => e.SecretId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Shares)
                .WithOne(e => e.Secret)
                .HasForeignKey(e => e.SecretId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SecretVersion entity
        modelBuilder.Entity<SecretVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SecretId, e.Version }).IsUnique();
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.EncryptedValue).IsRequired();
            entity.Property(e => e.KeyId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ChangeReason).HasMaxLength(500);

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());
        });

        // Configure EncryptionKey entity
        modelBuilder.Entity<EncryptionKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.KeyId).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => new { e.Status, e.ExpiresAt });
            entity.HasIndex(e => e.LastRotatedAt);

            entity.Property(e => e.KeyId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Algorithm).IsRequired().HasMaxLength(50);
            entity.Property(e => e.HsmSlotId).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());
        });

        // Configure SecretAccess entity
        modelBuilder.Entity<SecretAccess>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SecretId);
            entity.HasIndex(e => new { e.UserId, e.AccessedAt });
            entity.HasIndex(e => new { e.ServiceName, e.AccessedAt });
            entity.HasIndex(e => e.AccessedAt);
            entity.HasIndex(e => new { e.Success, e.AccessedAt });

            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ServiceName).HasMaxLength(100);
            entity.Property(e => e.ClientIpAddress).HasMaxLength(45); // IPv6 max length
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

            entity.Property(e => e.Context)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());
        });

        // Configure SecretShare entity
        modelBuilder.Entity<SecretShare>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SecretId);
            entity.HasIndex(e => new { e.SharedWithUserId, e.Status });
            entity.HasIndex(e => new { e.ShareToken }).HasFilter("share_token IS NOT NULL");
            entity.HasIndex(e => new { e.Status, e.ExpiresAt });

            entity.Property(e => e.SharedWithUserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SharedByUserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ShareToken).HasMaxLength(100);

            entity.Property(e => e.Permissions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());
        });

        // Configure SecretPolicy entity
        modelBuilder.Entity<SecretPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.PathPattern);
            entity.HasIndex(e => new { e.IsActive, e.CreatedAt });

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.PathPattern).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);

            entity.Property(e => e.AllowedUsers)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

            entity.Property(e => e.AllowedServices)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

            entity.Property(e => e.AllowedOperations)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

            entity.Property(e => e.Conditions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());
        });

        // Configure RotationJob entity
        modelBuilder.Entity<RotationJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Status, e.ScheduledAt });
            entity.HasIndex(e => e.SecretId);
            entity.HasIndex(e => e.KeyId);

            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

            entity.Property(e => e.Configuration)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());
        });

        // Configure SecretBackup entity
        modelBuilder.Entity<SecretBackup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BackupName).IsUnique();
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => e.ChecksumSha256);

            entity.Property(e => e.BackupName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ChecksumSha256).IsRequired().HasMaxLength(64);
            entity.Property(e => e.StorageLocation).IsRequired().HasMaxLength(500);

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());
        });

        // Configure AuditLogEntry entity
        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
            entity.HasIndex(e => new { e.ServiceName, e.Timestamp });
            entity.HasIndex(e => new { e.Operation, e.Timestamp });
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId });
            entity.HasIndex(e => new { e.Success, e.Timestamp });

            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ServiceName).HasMaxLength(100);
            entity.Property(e => e.Operation).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ResourceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ResourceId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ResourcePath).HasMaxLength(500);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.ClientIpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.Property(e => e.Details)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());
        });

        // Configure table names (lowercase with underscores for PostgreSQL convention)
        modelBuilder.Entity<Secret>().ToTable("secrets");
        modelBuilder.Entity<SecretVersion>().ToTable("secret_versions");
        modelBuilder.Entity<EncryptionKey>().ToTable("encryption_keys");
        modelBuilder.Entity<SecretAccess>().ToTable("secret_accesses");
        modelBuilder.Entity<SecretShare>().ToTable("secret_shares");
        modelBuilder.Entity<SecretPolicy>().ToTable("secret_policies");
        modelBuilder.Entity<RotationJob>().ToTable("rotation_jobs");
        modelBuilder.Entity<SecretBackup>().ToTable("secret_backups");
        modelBuilder.Entity<AuditLogEntry>().ToTable("audit_logs");

        // Configure column names to use snake_case
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ConvertToSnakeCase(property.Name));
            }
        }
    }

    private static string ConvertToSnakeCase(string input)
    {
        return string.Concat(input.Select((x, i) => 
            i > 0 && char.IsUpper(x) ? "_" + x.ToString().ToLower() : x.ToString().ToLower()));
    }
}