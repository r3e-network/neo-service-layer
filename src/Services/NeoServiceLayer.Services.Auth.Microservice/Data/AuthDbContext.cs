using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Services.Auth.Microservice.Models;
using System.Security.Cryptography;
using System.Text;

namespace NeoServiceLayer.Services.Auth.Microservice.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }
    public DbSet<MfaSecret> MfaSecrets { get; set; }
    public DbSet<BackupCode> BackupCodes { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("auth");

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Salt).HasMaxLength(255).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // JSON columns for metadata and preferences
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.Property(e => e.Preferences).HasColumnType("jsonb");

            // Soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Row Level Security
            entity.Property(e => e.TenantId).HasDefaultValue(Guid.Empty);
        });

        // User Session Configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.SessionToken).HasMaxLength(255).IsRequired();
            entity.Property(e => e.DeviceId).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.LastActivityAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Refresh Token Configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Token).HasMaxLength(255).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Login Attempt Configuration
        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IpAddress);
            entity.HasIndex(e => e.AttemptedAt);

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.IpAddress).HasMaxLength(45).IsRequired();
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.FailureReason).HasMaxLength(255);
            entity.Property(e => e.AttemptedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // MFA Secret Configuration
        modelBuilder.Entity<MfaSecret>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Secret).HasMaxLength(255).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<User>()
                  .WithOne()
                  .HasForeignKey<MfaSecret>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Backup Code Configuration
        modelBuilder.Entity<BackupCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Code).IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Audit Log Configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Timestamp);

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Resource).HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // JSON column for additional details
            entity.Property(e => e.Details).HasColumnType("jsonb");

            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Add triggers for updated_at columns
        AddUpdatedAtTriggers(modelBuilder);

        // Seed default data
        SeedDefaultData(modelBuilder);
    }

    private static void AddUpdatedAtTriggers(ModelBuilder modelBuilder)
    {
        var sql = @"
            CREATE OR REPLACE FUNCTION update_updated_at_column()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.updated_at = CURRENT_TIMESTAMP;
                RETURN NEW;
            END;
            $$ language 'plpgsql';

            CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON auth.users
            FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ";

        modelBuilder.HasDbFunction(typeof(AuthDbContext).GetMethod(nameof(CreateUpdatedAtTrigger))!)
                    .HasName("update_updated_at_column");
    }

    public static void CreateUpdatedAtTrigger() { }

    private static void SeedDefaultData(ModelBuilder modelBuilder)
    {
        // Seed default admin user (password should be changed on first login)
        var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var salt = GenerateSalt();
        var passwordHash = HashPassword("ChangeMe123!", salt);

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = adminUserId,
            Username = "admin",
            Email = "admin@neo-service-layer.local",
            FirstName = "System",
            LastName = "Administrator",
            PasswordHash = passwordHash,
            Salt = salt,
            EmailVerified = true,
            IsActive = true,
            MfaEnabled = false,
            Roles = new[] { "Admin", "User" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Metadata = "{}",
            Preferences = "{\"theme\":\"dark\",\"language\":\"en\"}"
        });
    }

    private static string GenerateSalt()
    {
        using var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[32];
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    private static string HashPassword(string password, string salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 100000, HashAlgorithmName.SHA256);
        return Convert.ToBase64String(pbkdf2.GetBytes(32));
    }
}