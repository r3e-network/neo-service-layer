using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoServiceLayer.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Initial database migration.
    /// </summary>
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttestationProofs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Report = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Signature = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MrEnclave = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MrSigner = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecurityVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Attributes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttestationProofs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeeAccounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    PublicKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsExportable = table.Column<bool>(type: "bit", nullable: false),
                    AttestationProof = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeeAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerificationResults",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VerificationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdentityData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationResults", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttestationProofs_CreatedAt",
                table: "AttestationProofs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status",
                table: "Tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId",
                table: "Tasks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeeAccounts_UserId",
                table: "TeeAccounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationResults_Status",
                table: "VerificationResults",
                column: "Status");
        }

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttestationProofs");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "TeeAccounts");

            migrationBuilder.DropTable(
                name: "VerificationResults");
        }
    }
}
