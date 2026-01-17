using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NameMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferencesAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InitiatorPrefsCompletedAt",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PartnerPrefsCompletedAt",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SetupStatus",
                table: "Sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EndingSound",
                table: "Names",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Meaning",
                table: "Names",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoundType",
                table: "Names",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyllableCount",
                table: "Names",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NameCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CategoryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NameCategoryMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NameId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameCategoryMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NameCategoryMappings_NameCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "NameCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NameCategoryMappings_Names_NameId",
                        column: x => x.NameId,
                        principalTable: "Names",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_NameCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "NameCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NameCategories_CategoryType",
                table: "NameCategories",
                column: "CategoryType");

            migrationBuilder.CreateIndex(
                name: "IX_NameCategories_Code",
                table: "NameCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NameCategoryMappings_CategoryId",
                table: "NameCategoryMappings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_NameCategoryMappings_NameId_CategoryId",
                table: "NameCategoryMappings",
                columns: new[] { "NameId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_CategoryId",
                table: "UserPreferences",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_SessionId",
                table: "UserPreferences",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId_SessionId_CategoryId",
                table: "UserPreferences",
                columns: new[] { "UserId", "SessionId", "CategoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NameCategoryMappings");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "NameCategories");

            migrationBuilder.DropColumn(
                name: "InitiatorPrefsCompletedAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "PartnerPrefsCompletedAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SetupStatus",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "EndingSound",
                table: "Names");

            migrationBuilder.DropColumn(
                name: "Meaning",
                table: "Names");

            migrationBuilder.DropColumn(
                name: "SoundType",
                table: "Names");

            migrationBuilder.DropColumn(
                name: "SyllableCount",
                table: "Names");
        }
    }
}
