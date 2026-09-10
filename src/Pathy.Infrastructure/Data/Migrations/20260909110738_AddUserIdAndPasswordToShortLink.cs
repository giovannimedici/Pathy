using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pathy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdAndPasswordToShortLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "short_links",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "short_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_short_links_user_url",
                table: "short_links",
                columns: new[] { "user_id", "original_url" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_short_links_user_url",
                table: "short_links");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "short_links");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "short_links");
        }
    }
}
