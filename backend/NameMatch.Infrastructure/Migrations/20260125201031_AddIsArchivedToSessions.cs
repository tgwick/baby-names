using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NameMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchivedToSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_InitiatorId_IsArchived",
                table: "Sessions",
                columns: new[] { "InitiatorId", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_PartnerId_IsArchived",
                table: "Sessions",
                columns: new[] { "PartnerId", "IsArchived" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_InitiatorId_IsArchived",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_PartnerId_IsArchived",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Sessions");
        }
    }
}
