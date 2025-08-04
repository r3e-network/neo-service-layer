using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NeoServiceLayer.Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Service Configurations table
        migrationBuilder.CreateTable(
            name: "ServiceConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ServiceName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                Configuration = table.Column<string>(type: "jsonb", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ServiceConfigurations", x => x.Id);
                table.UniqueConstraint("AK_ServiceConfigurations_ServiceName", x => x.ServiceName);
            });

        // API Keys table
        migrationBuilder.CreateTable(
            name: "ApiKeys",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                Key = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                Permissions = table.Column<string>(type: "jsonb", nullable: true),
                RateLimitPerMinute = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiKeys", x => x.Id);
            });

        // Audit Logs table
        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                Action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                EntityType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                EntityId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                OldValues = table.Column<string>(type: "jsonb", nullable: true),
                NewValues = table.Column<string>(type: "jsonb", nullable: true),
                IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true),
                UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
            });

        // Storage Items table
        migrationBuilder.CreateTable(
            name: "StorageItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                Value = table.Column<byte[]>(type: "bytea", nullable: true),
                Metadata = table.Column<string>(type: "jsonb", nullable: true),
                IsEncrypted = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StorageItems", x => x.Id);
                table.UniqueConstraint("AK_StorageItems_Key", x => x.Key);
            });

        // Key Metadata table
        migrationBuilder.CreateTable(
            name: "KeyMetadata",
            columns: table => new
            {
                KeyId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                PublicKeyHex = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                KeyType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                KeyUsage = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                Algorithm = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                Metadata = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KeyMetadata", x => x.KeyId);
            });

        // Oracle Data table
        migrationBuilder.CreateTable(
            name: "OracleData",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RequestId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                DataSourceId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                Data = table.Column<string>(type: "jsonb", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                BlockNumber = table.Column<long>(type: "bigint", nullable: true),
                TransactionHash = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OracleData", x => x.Id);
            });

        // Notification Queue table
        migrationBuilder.CreateTable(
            name: "NotificationQueue",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                Recipient = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                Subject = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                Content = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Error = table.Column<string>(type: "text", nullable: true),
                Metadata = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationQueue", x => x.Id);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_ApiKeys_Key",
            table: "ApiKeys",
            column: "Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_Timestamp",
            table: "AuditLogs",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_UserId",
            table: "AuditLogs",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_StorageItems_ExpiresAt",
            table: "StorageItems",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_OracleData_RequestId",
            table: "OracleData",
            column: "RequestId");

        migrationBuilder.CreateIndex(
            name: "IX_OracleData_Timestamp",
            table: "OracleData",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationQueue_Status_ScheduledFor",
            table: "NotificationQueue",
            columns: new[] { "Status", "ScheduledFor" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ServiceConfigurations");
        migrationBuilder.DropTable(name: "ApiKeys");
        migrationBuilder.DropTable(name: "AuditLogs");
        migrationBuilder.DropTable(name: "StorageItems");
        migrationBuilder.DropTable(name: "KeyMetadata");
        migrationBuilder.DropTable(name: "OracleData");
        migrationBuilder.DropTable(name: "NotificationQueue");
    }
}
