using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nova.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIdentityUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "Roles");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "Users",
                column: "NormalizedUserName");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "Roles",
                column: "NormalizedName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "Roles");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "Roles",
                column: "NormalizedName",
                unique: true);
        }
    }
}
