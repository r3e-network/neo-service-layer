using System;
using Microsoft.EntityFrameworkCore;
using AuthModels = NeoServiceLayer.Services.Authentication.Models;
using NeoServiceLayer.Services.Authentication.Domain.Entities;
using NeoServiceLayer.Services.Authentication.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Infrastructure
{
    /// <summary>
    /// Entity Framework DbContext for authentication system
    /// </summary>
    public class AuthenticationDbContext : DbContext
    {
        public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options)
            : base(options)
        {
        }

        // User management
        public DbSet<AuthModels.User> Users { get; set; }
        public DbSet<AuthModels.Role> Roles { get; set; }
        public DbSet<AuthModels.Permission> Permissions { get; set; }
        public DbSet<AuthModels.UserRole> UserRoles { get; set; }
        public DbSet<AuthModels.RolePermission> RolePermissions { get; set; }
        public DbSet<AuthModels.UserPermission> UserPermissions { get; set; }

        // Authentication & Authorization
        public DbSet<AuthModels.RefreshToken> RefreshTokens { get; set; }
        public DbSet<AuthModels.UserSession> UserSessions { get; set; }
        public DbSet<AuthModels.BackupCode> BackupCodes { get; set; }
        public DbSet<AuthModels.BlacklistedToken> BlacklistedTokens { get; set; }

        // Auditing & Security
        public DbSet<AuthModels.AuditLog> AuditLogs { get; set; }
        public DbSet<AuthModels.LoginAttempt> LoginAttempts { get; set; }
        public DbSet<AuthModels.PasswordHistory> PasswordHistories { get; set; }
        public DbSet<AuthModels.RateLimitEntry> RateLimitEntries { get; set; }

        // Email templates
        public DbSet<AuthModels.EmailTemplate> EmailTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity configuration
            modelBuilder.Entity<AuthModels.User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.EmailVerificationToken);
                entity.HasIndex(e => e.PasswordResetToken);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PasswordHash)
                    .IsRequired();

                // Set default values
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.EmailVerified).HasDefaultValue(false);
                entity.Property(e => e.TwoFactorEnabled).HasDefaultValue(false);
                entity.Property(e => e.FailedLoginAttempts).HasDefaultValue(0);
            });

            // Role entity configuration
            modelBuilder.Entity<AuthModels.Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Description)
                    .HasMaxLength(200);

                entity.Property(e => e.IsSystemRole).HasDefaultValue(false);
            });

            // Permission entity configuration
            modelBuilder.Entity<AuthModels.Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.Resource, e.Action }).IsUnique();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasMaxLength(200);

                entity.Property(e => e.Resource)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            // UserRole relationship configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.RoleId);
            });

            // RolePermission relationship configuration
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => new { e.RoleId, e.PermissionId });

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(e => e.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.RoleId);
                entity.HasIndex(e => e.PermissionId);
            });

            // UserPermission relationship configuration
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.PermissionId });

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Permissions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Permission)
                    .WithMany(p => p.UserPermissions)
                    .HasForeignKey(e => e.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.PermissionId);
            });

            // RefreshToken configuration
            modelBuilder.Entity<Domain.Entities.RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.JwtId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserSession configuration
            modelBuilder.Entity<Domain.Entities.UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SessionId).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.LastActivityAt);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Sessions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // BackupCode configuration
            modelBuilder.Entity<BackupCode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.Code }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.BackupCodes)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // BlacklistedToken configuration
            modelBuilder.Entity<BlacklistedToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => e.JwtId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
            });

            // AuditLog configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.EventCategory);
                entity.HasIndex(e => e.CreatedAt);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.EventType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.EventCategory)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            // LoginAttempt configuration
            modelBuilder.Entity<Domain.Entities.LoginAttempt>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.IpAddress);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.AttemptedAt);

                entity.Property(e => e.IpAddress)
                    .IsRequired();
            });

            // PasswordHistory configuration
            modelBuilder.Entity<PasswordHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RateLimitEntry configuration
            modelBuilder.Entity<RateLimitEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.Key, e.Action }).IsUnique();
                entity.HasIndex(e => e.WindowEnd);

                entity.Property(e => e.Key)
                    .IsRequired();

                entity.Property(e => e.Action)
                    .IsRequired();
            });

            // EmailTemplate configuration
            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Subject)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.HtmlBody)
                    .IsRequired();

                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Seed initial data
            SeedInitialData(modelBuilder);
        }

        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed default roles
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var userRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    Description = "System administrator with full access",
                    IsSystemRole = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Role
                {
                    Id = userRoleId,
                    Name = "User",
                    Description = "Standard user with basic access",
                    IsSystemRole = true,
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Seed default permissions
            var permissionIds = new[]
            {
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")
            };

            modelBuilder.Entity<Permission>().HasData(
                new Permission
                {
                    Id = permissionIds[0],
                    Name = "users.read",
                    Description = "Read user information",
                    Resource = "users",
                    Action = "read"
                },
                new Permission
                {
                    Id = permissionIds[1],
                    Name = "users.write",
                    Description = "Create and update users",
                    Resource = "users",
                    Action = "write"
                },
                new Permission
                {
                    Id = permissionIds[2],
                    Name = "users.delete",
                    Description = "Delete users",
                    Resource = "users",
                    Action = "delete"
                },
                new Permission
                {
                    Id = permissionIds[3],
                    Name = "roles.manage",
                    Description = "Manage roles and permissions",
                    Resource = "roles",
                    Action = "manage"
                },
                new Permission
                {
                    Id = permissionIds[4],
                    Name = "system.admin",
                    Description = "Full system administration",
                    Resource = "system",
                    Action = "admin"
                }
            );

            // Assign permissions to roles
            modelBuilder.Entity<RolePermission>().HasData(
                // Admin role gets all permissions
                new RolePermission { RoleId = adminRoleId, PermissionId = permissionIds[0], GrantedAt = DateTime.UtcNow },
                new RolePermission { RoleId = adminRoleId, PermissionId = permissionIds[1], GrantedAt = DateTime.UtcNow },
                new RolePermission { RoleId = adminRoleId, PermissionId = permissionIds[2], GrantedAt = DateTime.UtcNow },
                new RolePermission { RoleId = adminRoleId, PermissionId = permissionIds[3], GrantedAt = DateTime.UtcNow },
                new RolePermission { RoleId = adminRoleId, PermissionId = permissionIds[4], GrantedAt = DateTime.UtcNow },

                // User role gets read permission
                new RolePermission { RoleId = userRoleId, PermissionId = permissionIds[0], GrantedAt = DateTime.UtcNow }
            );

            // Seed email templates
            modelBuilder.Entity<EmailTemplate>().HasData(
                new EmailTemplate
                {
                    Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    Name = "EmailVerification",
                    Subject = "Verify your email address",
                    HtmlBody = @"<html><body>
                        <h2>Email Verification</h2>
                        <p>Hello {{Username}},</p>
                        <p>Please click the link below to verify your email address:</p>
                        <p><a href='{{VerificationLink}}'>Verify Email</a></p>
                        <p>This link will expire in 24 hours.</p>
                        <p>Best regards,<br>The Team</p>
                    </body></html>",
                    TextBody = @"Email Verification

Hello {{Username}},

Please click the link below to verify your email address:
{{VerificationLink}}

This link will expire in 24 hours.

Best regards,
The Team",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("22222222-3333-4444-5555-666666666666"),
                    Name = "PasswordReset",
                    Subject = "Password Reset Request",
                    HtmlBody = @"<html><body>
                        <h2>Password Reset</h2>
                        <p>Hello {{Username}},</p>
                        <p>We received a request to reset your password. Click the link below to reset it:</p>
                        <p><a href='{{ResetLink}}'>Reset Password</a></p>
                        <p>This link will expire in 1 hour.</p>
                        <p>If you didn't request this, please ignore this email.</p>
                        <p>Best regards,<br>The Team</p>
                    </body></html>",
                    TextBody = @"Password Reset

Hello {{Username}},

We received a request to reset your password. Click the link below to reset it:
{{ResetLink}}

This link will expire in 1 hour.

If you didn't request this, please ignore this email.

Best regards,
The Team",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new EmailTemplate
                {
                    Id = Guid.Parse("33333333-4444-5555-6666-777777777777"),
                    Name = "TwoFactorCode",
                    Subject = "Your Two-Factor Authentication Code",
                    HtmlBody = @"<html><body>
                        <h2>Two-Factor Authentication</h2>
                        <p>Hello {{Username}},</p>
                        <p>Your authentication code is: <strong>{{Code}}</strong></p>
                        <p>This code will expire in 5 minutes.</p>
                        <p>If you didn't request this, please contact support immediately.</p>
                        <p>Best regards,<br>The Team</p>
                    </body></html>",
                    TextBody = @"Two-Factor Authentication

Hello {{Username}},

Your authentication code is: {{Code}}

This code will expire in 5 minutes.

If you didn't request this, please contact support immediately.

Best regards,
The Team",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}