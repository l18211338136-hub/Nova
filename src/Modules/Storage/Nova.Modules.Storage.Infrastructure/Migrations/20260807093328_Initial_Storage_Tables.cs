using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nova.Modules.Storage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Storage_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "storage");

            migrationBuilder.CreateTable(
                name: "Attachments",
                schema: "storage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TargetId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AttachmentType = table.Column<int>(type: "integer", nullable: false),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileObjects",
                schema: "storage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FileKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    BucketName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    AccessMode = table.Column<int>(type: "integer", nullable: false),
                    AccessUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
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
                    table.PrimaryKey("PK_FileObjects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_AttachmentType",
                schema: "storage",
                table: "Attachments",
                column: "AttachmentType");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_FileId",
                schema: "storage",
                table: "Attachments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TargetType_TargetId",
                schema: "storage",
                table: "Attachments",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_FileObjects_CreatedAt",
                schema: "storage",
                table: "FileObjects",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FileObjects_FileHash",
                schema: "storage",
                table: "FileObjects",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_FileObjects_FileKey",
                schema: "storage",
                table: "FileObjects",
                column: "FileKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "FileObjects",
                schema: "storage");
        }
    }
}
