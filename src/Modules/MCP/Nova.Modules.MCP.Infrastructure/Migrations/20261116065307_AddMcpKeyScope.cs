using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nova.Modules.MCP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMcpKeyScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllServers",
                schema: "mcp",
                table: "McpKeys",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "McpKeyAccesses",
                schema: "mcp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    McpKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpKeyAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_McpKeyAccesses_McpKeys_McpKeyId",
                        column: x => x.McpKeyId,
                        principalSchema: "mcp",
                        principalTable: "McpKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_McpKeyAccesses_McpKeyId_ServerId_ToolId",
                schema: "mcp",
                table: "McpKeyAccesses",
                columns: new[] { "McpKeyId", "ServerId", "ToolId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "McpKeyAccesses",
                schema: "mcp");

            migrationBuilder.DropColumn(
                name: "AllServers",
                schema: "mcp",
                table: "McpKeys");
        }
    }
}
