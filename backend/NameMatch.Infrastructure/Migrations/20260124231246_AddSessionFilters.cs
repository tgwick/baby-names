using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NameMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InitiatorFiltersCompletedAt",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PartnerFiltersCompletedAt",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SessionFilters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinPopularityScore = table.Column<int>(type: "integer", nullable: true),
                    MaxPopularityScore = table.Column<int>(type: "integer", nullable: true),
                    MinSyllables = table.Column<int>(type: "integer", nullable: true),
                    MaxSyllables = table.Column<int>(type: "integer", nullable: true),
                    AllowedEndingSounds = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionFilters_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionFilters_SessionId",
                table: "SessionFilters",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionFilters_UserId_SessionId",
                table: "SessionFilters",
                columns: new[] { "UserId", "SessionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionFilters");

            migrationBuilder.DropColumn(
                name: "InitiatorFiltersCompletedAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "PartnerFiltersCompletedAt",
                table: "Sessions");
        }
    }
}
