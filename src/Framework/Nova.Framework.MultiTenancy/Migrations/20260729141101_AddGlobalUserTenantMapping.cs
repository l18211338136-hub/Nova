using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nova.Framework.MultiTenancy.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalUserTenantMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalUserTenantMappings",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Account = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalUserTenantMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlobalUserTenantMappings_Account",
                schema: "system",
                table: "GlobalUserTenantMappings",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalUserTenantMappings_Account_TenantId",
                schema: "system",
                table: "GlobalUserTenantMappings",
                columns: new[] { "Account", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalUserTenantMappings",
                schema: "system");
        }
    }
}
