using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nova.Framework.MultiTenancy.Migrations
{
    /// <inheritdoc />
    public partial class Initial_MultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "system");

            migrationBuilder.CreateTable(
                name: "TenantInfo",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ConnectionString = table.Column<string>(type: "text", nullable: true),
                    AdminEmail = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ValidUpto = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Issuer = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInfo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInfo_Identifier",
                schema: "system",
                table: "TenantInfo",
                column: "Identifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantInfo",
                schema: "system");
        }
    }
}
