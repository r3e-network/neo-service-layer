using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Core entities
    public DbSet<ServiceConfigurationEntity> ServiceConfigurations { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ServiceHealthCheck> ServiceHealthChecks { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<RateLimitRule> RateLimitRules { get; set; }

    // Service-specific entities
    public DbSet<StoredDocument> StoredDocuments { get; set; }
    public DbSet<KeyVaultEntry> KeyVaultEntries { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }
    public DbSet<WorkflowExecution> WorkflowExecutions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Add indexes for performance
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditLog_Timestamp");

        modelBuilder.Entity<ServiceHealthCheck>()
            .HasIndex(s => new { s.ServiceName, s.CheckTime })
            .HasDatabaseName("IX_ServiceHealthCheck_ServiceName_CheckTime");

        modelBuilder.Entity<ApiKey>()
            .HasIndex(a => a.Key)
            .HasDatabaseName("IX_ApiKey_Key")
            .IsUnique();

        modelBuilder.Entity<StoredDocument>()
            .HasIndex(s => s.DocumentId)
            .HasDatabaseName("IX_StoredDocument_DocumentId")
            .IsUnique();

        // Configure soft delete
        modelBuilder.Entity<StoredDocument>()
            .HasQueryFilter(d => !d.IsDeleted);

        modelBuilder.Entity<KeyVaultEntry>()
            .HasQueryFilter(k => !k.IsDeleted);

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default rate limit rules
        modelBuilder.Entity<RateLimitRule>().HasData(
            new RateLimitRule
            {
                Id = Guid.NewGuid(),
                Name = "DefaultApiLimit",
                Endpoint = "*",
                PermitLimit = 100,
                WindowMinutes = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new RateLimitRule
            {
                Id = Guid.NewGuid(),
                Name = "KeyManagementLimit",
                Endpoint = "/api/keymanagement/*",
                PermitLimit = 20,
                WindowMinutes = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        // Seed default notification templates
        modelBuilder.Entity<NotificationTemplate>().HasData(
            new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Name = "WelcomeEmail",
                Subject = "Welcome to Neo Service Layer",
                Body = "Welcome {{UserName}}! Your account has been created successfully.",
                Type = "Email",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Name = "SecurityAlert",
                Subject = "Security Alert",
                Body = "A security event has been detected: {{EventType}} at {{Timestamp}}",
                Type = "Email",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }

            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}

// Design-time factory for migrations
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Build configuration
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

// Base entity class
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

// Entity models
public class ServiceConfiguration : BaseEntity
{
    public string ServiceName { get; set; }
    public string ConfigurationKey { get; set; }
    public string ConfigurationValue { get; set; }
    public bool IsEncrypted { get; set; }
}

public class AuditLog : BaseEntity
{
    public string UserId { get; set; }
    public string Action { get; set; }
    public string Entity { get; set; }
    public string EntityId { get; set; }
    public string OldValues { get; set; }
    public string NewValues { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; }
}

public class ServiceHealthCheck : BaseEntity
{
    public string ServiceName { get; set; }
    public string Status { get; set; }
    public string Details { get; set; }
    public DateTime CheckTime { get; set; }
    public int ResponseTimeMs { get; set; }
}

public class ApiKey : BaseEntity
{
    public string Name { get; set; }
    public string Key { get; set; }
    public string Description { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string[] Scopes { get; set; }
    public int RequestCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class RateLimitRule : BaseEntity
{
    public string Name { get; set; }
    public string Endpoint { get; set; }
    public int PermitLimit { get; set; }
    public int WindowMinutes { get; set; }
    public bool IsActive { get; set; }
}

public class StoredDocument : BaseEntity
{
    public string DocumentId { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public string StoragePath { get; set; }
    public string ChecksumSha256 { get; set; }
    public bool IsEncrypted { get; set; }
    public string EncryptionKeyId { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

public class KeyVaultEntry : BaseEntity
{
    public string KeyId { get; set; }
    public string KeyType { get; set; }
    public string Algorithm { get; set; }
    public int KeySize { get; set; }
    public string PublicKey { get; set; }
    public string EncryptedPrivateKey { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, string> Tags { get; set; }
}

public class NotificationTemplate : BaseEntity
{
    public string Name { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public string Type { get; set; } // Email, SMS, Push
    public bool IsActive { get; set; }
    public Dictionary<string, string> Variables { get; set; }
}

public class NotificationLog : BaseEntity
{
    public string RecipientId { get; set; }
    public string RecipientAddress { get; set; }
    public string Type { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public string Status { get; set; } // Pending, Sent, Failed
    public int RetryCount { get; set; }
    public DateTime? SentAt { get; set; }
    public string ErrorMessage { get; set; }
}

public class WorkflowDefinition : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string WorkflowJson { get; set; }
    public bool IsActive { get; set; }
    public string Version { get; set; }
}

public class WorkflowExecution : BaseEntity
{
    public Guid WorkflowDefinitionId { get; set; }
    public WorkflowDefinition WorkflowDefinition { get; set; }
    public string Status { get; set; } // Running, Completed, Failed
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string InputData { get; set; }
    public string OutputData { get; set; }
    public string ErrorMessage { get; set; }
    public Dictionary<string, object> ExecutionContext { get; set; }
}
