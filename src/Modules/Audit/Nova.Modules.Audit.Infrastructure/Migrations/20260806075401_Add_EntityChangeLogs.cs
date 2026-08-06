using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nova.Modules.Audit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_EntityChangeLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntityChangeLogs",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ChangeType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityChangeLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityPropertyChanges",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityChangeLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PropertyDisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    OriginalValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityPropertyChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityPropertyChanges_EntityChangeLogs_EntityChangeLogId",
                        column: x => x.EntityChangeLogId,
                        principalSchema: "audit",
                        principalTable: "EntityChangeLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntityChangeLogs_CreatedAt",
                schema: "audit",
                table: "EntityChangeLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EntityChangeLogs_EntityId",
                schema: "audit",
                table: "EntityChangeLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityChangeLogs_EntityType",
                schema: "audit",
                table: "EntityChangeLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_EntityChangeLogs_EntityType_EntityId",
                schema: "audit",
                table: "EntityChangeLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityPropertyChanges_EntityChangeLogId",
                schema: "audit",
                table: "EntityPropertyChanges",
                column: "EntityChangeLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityPropertyChanges",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "EntityChangeLogs",
                schema: "audit");
        }
    }
}
