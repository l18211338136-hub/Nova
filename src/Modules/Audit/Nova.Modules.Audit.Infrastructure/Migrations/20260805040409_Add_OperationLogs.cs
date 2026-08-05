using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nova.Modules.Audit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_OperationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "OperationLogs",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TraceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    HttpMethod = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    RequestPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ActionName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RequestPayload = table.Column<string>(type: "text", nullable: true),
                    ResponsePayload = table.Column<string>(type: "text", nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    ElapsedMs = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    IsSlowRequest = table.Column<bool>(type: "boolean", nullable: true),
                    HasSanitizedData = table.Column<bool>(type: "boolean", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExceptionStackTrace = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SanitizationDetails",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LogId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MaskedRule = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SanitizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanitizationDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SanitizationDetails_OperationLogs_LogId",
                        column: x => x.LogId,
                        principalSchema: "audit",
                        principalTable: "OperationLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_CreatedAt",
                schema: "audit",
                table: "OperationLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_TraceId",
                schema: "audit",
                table: "OperationLogs",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_UserId",
                schema: "audit",
                table: "OperationLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SanitizationDetails_LogId",
                schema: "audit",
                table: "SanitizationDetails",
                column: "LogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SanitizationDetails",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "OperationLogs",
                schema: "audit");
        }
    }
}
