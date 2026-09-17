using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pathy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "link_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    old_value = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    new_value = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_link_audit_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_link_audit_logs_changed_at",
                table: "link_audit_logs",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "ix_link_audit_logs_link_id",
                table: "link_audit_logs",
                column: "link_id");

            migrationBuilder.CreateIndex(
                name: "ix_link_audit_logs_user_id",
                table: "link_audit_logs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "link_audit_logs");
        }
    }
}
