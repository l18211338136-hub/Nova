using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nova.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAgentToAuthAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                schema: "identity",
                table: "AuthAuditLogs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserAgent",
                schema: "identity",
                table: "AuthAuditLogs");
        }
    }
}
