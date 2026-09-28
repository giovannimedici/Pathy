using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pathy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeactivatedAtToShortLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deactivated_at",
                table: "short_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_link_audit_logs_short_links_link_id",
                table: "link_audit_logs",
                column: "link_id",
                principalTable: "short_links",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_link_audit_logs_short_links_link_id",
                table: "link_audit_logs");

            migrationBuilder.DropColumn(
                name: "deactivated_at",
                table: "short_links");
        }
    }
}
