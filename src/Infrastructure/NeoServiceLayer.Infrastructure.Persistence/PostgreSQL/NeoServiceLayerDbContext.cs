using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;

/// <summary>
/// Unified PostgreSQL database context for all Neo Service Layer services
/// Provides centralized data persistence for confidential computing and blockchain services
/// </summary>
public class NeoServiceLayerDbContext : DbContext
{
    public NeoServiceLayerDbContext(DbContextOptions<NeoServiceLayerDbContext> options) : base(options) { }

    #region Core Entities
    
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Service> Services { get; set; } = null!;
    public DbSet<ServiceConfiguration> ServiceConfigurations { get; set; } = null!;
    public DbSet<HealthCheckResult> HealthCheckResults { get; set; } = null!;
    
    #endregion

    #region SGX Confidential Storage
    
    public DbSet<SealedDataItem> SealedDataItems { get; set; } = null!;
    public DbSet<EnclaveAttestation> EnclaveAttestations { get; set; } = null!;
    public DbSet<SealingPolicy> SealingPolicies { get; set; } = null!;
    
    #endregion

    #region Authentication & Authorization
    
    public DbSet<AuthenticationSession> AuthenticationSessions { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    
    #endregion

    #region Key Management
    
    public DbSet<CryptographicKey> CryptographicKeys { get; set; } = null!;
    public DbSet<KeyRotationEvent> KeyRotationEvents { get; set; } = null!;
    public DbSet<KeyAccessAudit> KeyAccessAudits { get; set; } = null!;
    
    #endregion

    #region Oracle Services
    
    public DbSet<OracleDataFeed> OracleDataFeeds { get; set; } = null!;
    public DbSet<OracleRequest> OracleRequests { get; set; } = null!;
    public DbSet<OracleResponse> OracleResponses { get; set; } = null!;
    public DbSet<DataSourceAttestation> DataSourceAttestations { get; set; } = null!;
    
    #endregion

    #region Voting & Governance
    
    public DbSet<Proposal> Proposals { get; set; } = null!;
    public DbSet<Vote> Votes { get; set; } = null!;
    public DbSet<VotingPower> VotingPowers { get; set; } = null!;
    
    #endregion

    #region Cross-Chain & Bridge
    
    public DbSet<CrossChainTransaction> CrossChainTransactions { get; set; } = null!;
    public DbSet<BridgeOperation> BridgeOperations { get; set; } = null!;
    public DbSet<ChainState> ChainStates { get; set; } = null!;
    
    #endregion

    #region Monitoring & Analytics
    
    public DbSet<MetricRecord> MetricRecords { get; set; } = null!;
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; } = null!;
    public DbSet<SecurityEvent> SecurityEvents { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    
    #endregion

    #region Event Sourcing
    
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<EventSnapshot> EventSnapshots { get; set; } = null!;
    public DbSet<AggregateRoot> AggregateRoots { get; set; } = null!;
    
    #endregion

    #region Compute Services
    
    public DbSet<ComputationEntity> Computations { get; set; } = null!;
    public DbSet<ComputationStatusEntity> ComputationStatuses { get; set; } = null!;
    public DbSet<ComputationResultEntity> ComputationResults { get; set; } = null!;
    public DbSet<ComputationResourceUsageEntity> ComputationResourceUsages { get; set; } = null!;
    public DbSet<ComputationPermissionEntity> ComputationPermissions { get; set; } = null!;
    
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure schemas for logical separation
        ConfigureSchemas(modelBuilder);
        
        // Configure entity relationships and constraints
        ConfigureEntities(modelBuilder);
        
        // Configure indexes for performance
        ConfigureIndexes(modelBuilder);
        
        // Configure data seeding
        SeedInitialData(modelBuilder);
    }

    private void ConfigureSchemas(ModelBuilder modelBuilder)
    {
        // Core system schema
        modelBuilder.Entity<User>().ToTable("users", "core");
        modelBuilder.Entity<Service>().ToTable("services", "core");
        modelBuilder.Entity<ServiceConfiguration>().ToTable("service_configurations", "core");
        modelBuilder.Entity<HealthCheckResult>().ToTable("health_check_results", "core");

        // SGX confidential computing schema
        modelBuilder.Entity<SealedDataItem>().ToTable("sealed_data_items", "sgx");
        modelBuilder.Entity<EnclaveAttestation>().ToTable("enclave_attestations", "sgx");
        modelBuilder.Entity<SealingPolicy>().ToTable("sealing_policies", "sgx");

        // Authentication schema
        modelBuilder.Entity<AuthenticationSession>().ToTable("authentication_sessions", "auth");
        modelBuilder.Entity<Permission>().ToTable("permissions", "auth");
        modelBuilder.Entity<Role>().ToTable("roles", "auth");
        modelBuilder.Entity<UserRole>().ToTable("user_roles", "auth");
        modelBuilder.Entity<RolePermission>().ToTable("role_permissions", "auth");

        // Key management schema
        modelBuilder.Entity<CryptographicKey>().ToTable("cryptographic_keys", "keymanagement");
        modelBuilder.Entity<KeyRotationEvent>().ToTable("key_rotation_events", "keymanagement");
        modelBuilder.Entity<KeyAccessAudit>().ToTable("key_access_audits", "keymanagement");

        // Oracle services schema
        modelBuilder.Entity<OracleDataFeed>().ToTable("oracle_data_feeds", "oracle");
        modelBuilder.Entity<OracleRequest>().ToTable("oracle_requests", "oracle");
        modelBuilder.Entity<OracleResponse>().ToTable("oracle_responses", "oracle");
        modelBuilder.Entity<DataSourceAttestation>().ToTable("data_source_attestations", "oracle");

        // Voting schema
        modelBuilder.Entity<Proposal>().ToTable("proposals", "voting");
        modelBuilder.Entity<Vote>().ToTable("votes", "voting");
        modelBuilder.Entity<VotingPower>().ToTable("voting_powers", "voting");

        // Cross-chain schema
        modelBuilder.Entity<CrossChainTransaction>().ToTable("cross_chain_transactions", "crosschain");
        modelBuilder.Entity<BridgeOperation>().ToTable("bridge_operations", "crosschain");
        modelBuilder.Entity<ChainState>().ToTable("chain_states", "crosschain");

        // Monitoring schema
        modelBuilder.Entity<MetricRecord>().ToTable("metric_records", "monitoring");
        modelBuilder.Entity<PerformanceMetric>().ToTable("performance_metrics", "monitoring");
        modelBuilder.Entity<SecurityEvent>().ToTable("security_events", "monitoring");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs", "monitoring");

        // Event sourcing schema
        modelBuilder.Entity<Event>().ToTable("events", "eventsourcing");
        modelBuilder.Entity<EventSnapshot>().ToTable("event_snapshots", "eventsourcing");
        modelBuilder.Entity<AggregateRoot>().ToTable("aggregate_roots", "eventsourcing");

        // Compute services schema
        modelBuilder.Entity<ComputationEntity>().ToTable("computations", "compute");
        modelBuilder.Entity<ComputationStatusEntity>().ToTable("computation_statuses", "compute");
        modelBuilder.Entity<ComputationResultEntity>().ToTable("computation_results", "compute");
        modelBuilder.Entity<ComputationResourceUsageEntity>().ToTable("computation_resource_usages", "compute");
        modelBuilder.Entity<ComputationPermissionEntity>().ToTable("computation_permissions", "compute");
    }

    private void ConfigureEntities(ModelBuilder modelBuilder)
    {
        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Service entity
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => new { e.Name, e.Version }).IsUnique();
        });

        // Configure SGX SealedDataItem
        modelBuilder.Entity<SealedDataItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ServiceName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SealedData).IsRequired();
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.ServiceName);
            entity.HasIndex(e => e.ExpiresAt);
        });

        // Configure Authentication Sessions
        modelBuilder.Entity<AuthenticationSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UserId).IsRequired();
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.UserId);
        });

        // Configure Cryptographic Keys
        modelBuilder.Entity<CryptographicKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KeyId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ServiceName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.KeyType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EncryptedKeyMaterial).IsRequired();
            entity.HasIndex(e => e.KeyId).IsUnique();
            entity.HasIndex(e => new { e.ServiceName, e.KeyType });
        });

        // Configure Oracle Data Feeds
        modelBuilder.Entity<OracleDataFeed>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FeedId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DataSource).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.FeedId).IsUnique();
            entity.HasIndex(e => e.LastUpdated);
        });

        // Configure Voting Proposals
        modelBuilder.Entity<Proposal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).IsRequired();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.VotingDeadline);
        });

        // Configure Cross-Chain Transactions
        modelBuilder.Entity<CrossChainTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.SourceChain).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DestinationChain).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.TransactionHash).IsUnique();
            entity.HasIndex(e => new { e.SourceChain, e.DestinationChain });
        });

        // Configure Events for Event Sourcing
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AggregateId).IsRequired();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.EventData).IsRequired();
            entity.HasIndex(e => new { e.AggregateId, e.Version }).IsUnique();
            entity.HasIndex(e => e.Timestamp);
        });
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Performance indexes for frequent queries
        modelBuilder.Entity<SealedDataItem>()
            .HasIndex(e => new { e.ServiceName, e.ExpiresAt })
            .HasDatabaseName("IX_SealedDataItems_Service_Expiry");

        modelBuilder.Entity<SecurityEvent>()
            .HasIndex(e => new { e.Severity, e.Timestamp })
            .HasDatabaseName("IX_SecurityEvents_Severity_Time");

        modelBuilder.Entity<MetricRecord>()
            .HasIndex(e => new { e.ServiceName, e.MetricType, e.Timestamp })
            .HasDatabaseName("IX_MetricRecords_Service_Type_Time");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(e => new { e.ServiceName, e.Action, e.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Service_Action_Time");
    }

    private void SeedInitialData(ModelBuilder modelBuilder)
    {
        // Seed initial services
        modelBuilder.Entity<Service>().HasData(
            new Service
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "Authentication",
                Version = "1.0.0",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new Service
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "EnclaveStorage",
                Version = "2.0.0",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new Service
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Name = "Oracle",
                Version = "1.0.0",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            }
        );

        // Seed initial roles and permissions
        var adminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var userRoleId = Guid.Parse("10000000-0000-0000-0000-000000000002");

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = adminRoleId, Name = "Administrator", Description = "Full system access", CreatedAt = DateTime.UtcNow },
            new Role { Id = userRoleId, Name = "User", Description = "Standard user access", CreatedAt = DateTime.UtcNow }
        );

        modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = Guid.NewGuid(), Name = "storage:read", Description = "Read from confidential storage", CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Name = "storage:write", Description = "Write to confidential storage", CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Name = "storage:delete", Description = "Delete from confidential storage", CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Name = "oracle:read", Description = "Read oracle data", CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Name = "voting:participate", Description = "Participate in voting", CreatedAt = DateTime.UtcNow }
        );
    }
}

/// <summary>
/// Design-time factory for creating DbContext during migrations
/// </summary>
public class NeoServiceLayerDbContextFactory : IDesignTimeDbContextFactory<NeoServiceLayerDbContext>
{
    public NeoServiceLayerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<NeoServiceLayerDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Database=neo_service_layer;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });

        return new NeoServiceLayerDbContext(optionsBuilder.Options);
    }
}