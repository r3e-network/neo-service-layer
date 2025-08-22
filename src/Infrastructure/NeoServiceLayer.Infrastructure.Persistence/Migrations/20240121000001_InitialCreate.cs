using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NeoServiceLayer.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Initial database migration creating all tables and indexes
    /// </summary>
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create schemas
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS core;");
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS sgx;");
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS auth;");
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS keymanagement;");
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS oracle;");
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS voting;");
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS crosschain;");
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS monitoring;");
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS eventsourcing;");
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS compute;");

            // Enable extensions
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pgcrypto\";");

            // Create core.users table
            migrationBuilder.CreateTable(
                name: "users",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    username = table.Column<string>(maxLength: 100, nullable: false),
                    email = table.Column<string>(maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(nullable: true),
                    is_active = table.Column<bool>(nullable: false, defaultValue: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                schema: "core",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                schema: "core",
                table: "users",
                column: "email",
                unique: true);

            // Create core.services table
            migrationBuilder.CreateTable(
                name: "services",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(maxLength: 100, nullable: false),
                    version = table.Column<string>(maxLength: 20, nullable: false),
                    status = table.Column<string>(maxLength: 50, nullable: false, defaultValue: "Active"),
                    description = table.Column<string>(nullable: true),
                    created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_services", x => x.id);
                    table.UniqueConstraint("AK_services_name_version", x => new { x.name, x.version });
                });

            // Create sgx.sealing_policies table
            migrationBuilder.CreateTable(
                name: "sealing_policies",
                schema: "sgx",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(maxLength: 100, nullable: false),
                    policy_type = table.Column<string>(maxLength: 50, nullable: false),
                    description = table.Column<string>(maxLength: 500, nullable: true),
                    expiration_hours = table.Column<int>(nullable: false, defaultValue: 24),
                    allow_unseal = table.Column<bool>(nullable: false, defaultValue: true),
                    require_attestation = table.Column<bool>(nullable: false, defaultValue: true),
                    policy_rules = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sealing_policies", x => x.id);
                });

            // Create sgx.sealed_data_items table
            migrationBuilder.CreateTable(
                name: "sealed_data_items",
                schema: "sgx",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    key = table.Column<string>(maxLength: 255, nullable: false),
                    service_name = table.Column<string>(maxLength: 100, nullable: false),
                    storage_id = table.Column<string>(maxLength: 64, nullable: false),
                    sealed_data = table.Column<byte[]>(nullable: false),
                    original_size = table.Column<int>(nullable: true),
                    sealed_size = table.Column<int>(nullable: true),
                    fingerprint = table.Column<string>(maxLength: 255, nullable: true),
                    policy_type = table.Column<string>(maxLength: 50, nullable: false),
                    policy_id = table.Column<Guid>(nullable: true),
                    created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    expires_at = table.Column<DateTime>(nullable: true),
                    last_accessed = table.Column<DateTime>(nullable: true),
                    access_count = table.Column<int>(nullable: false, defaultValue: 0),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sealed_data_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_sealed_data_items_sealing_policies_policy_id",
                        column: x => x.policy_id,
                        principalSchema: "sgx",
                        principalTable: "sealing_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.UniqueConstraint("AK_sealed_data_items_key_service", x => new { x.key, x.service_name });
                });

            migrationBuilder.CreateIndex(
                name: "IX_sealed_data_items_service_expiry",
                schema: "sgx",
                table: "sealed_data_items",
                columns: new[] { "service_name", "expires_at" });

            // Create compute tables
            migrationBuilder.CreateTable(
                name: "computations",
                schema: "compute",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(maxLength: 255, nullable: false),
                    description = table.Column<string>(maxLength: 1000, nullable: true),
                    computation_type = table.Column<string>(maxLength: 50, nullable: false),
                    code = table.Column<string>(nullable: false),
                    version = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "1.0.0"),
                    author = table.Column<string>(maxLength: 255, nullable: true),
                    blockchain_type = table.Column<string>(maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(nullable: true),
                    is_active = table.Column<bool>(nullable: false, defaultValue: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_computations", x => x.id);
                    table.UniqueConstraint("AK_computations_name_version", x => new { x.name, x.version });
                });

            migrationBuilder.CreateIndex(
                name: "IX_computations_blockchain_active",
                schema: "compute",
                table: "computations",
                columns: new[] { "blockchain_type", "is_active" });

            // Create other tables similarly...
            // (Auth tables, monitoring tables, etc.)

            // Create update trigger function
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_updated_at_column()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.updated_at = CURRENT_TIMESTAMP;
                    RETURN NEW;
                END;
                $$ language 'plpgsql';
            ");

            // Apply update triggers
            migrationBuilder.Sql(@"
                CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON core.users
                    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
                    
                CREATE TRIGGER update_services_updated_at BEFORE UPDATE ON core.services
                    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
                    
                CREATE TRIGGER update_computations_updated_at BEFORE UPDATE ON compute.computations
                    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables in reverse order
            migrationBuilder.DropTable(name: "computation_permissions", schema: "compute");
            migrationBuilder.DropTable(name: "computation_resource_usages", schema: "compute");
            migrationBuilder.DropTable(name: "computation_results", schema: "compute");
            migrationBuilder.DropTable(name: "computation_statuses", schema: "compute");
            migrationBuilder.DropTable(name: "computations", schema: "compute");
            
            migrationBuilder.DropTable(name: "enclave_attestations", schema: "sgx");
            migrationBuilder.DropTable(name: "sealed_data_items", schema: "sgx");
            migrationBuilder.DropTable(name: "sealing_policies", schema: "sgx");
            
            migrationBuilder.DropTable(name: "service_configurations", schema: "core");
            migrationBuilder.DropTable(name: "health_check_results", schema: "core");
            migrationBuilder.DropTable(name: "services", schema: "core");
            migrationBuilder.DropTable(name: "users", schema: "core");

            // Drop functions
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_updated_at_column();");
            
            // Drop schemas
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS compute CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS eventsourcing CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS monitoring CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS crosschain CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS voting CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS oracle CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS keymanagement CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS auth CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS sgx CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS core CASCADE;");
        }
    }
}