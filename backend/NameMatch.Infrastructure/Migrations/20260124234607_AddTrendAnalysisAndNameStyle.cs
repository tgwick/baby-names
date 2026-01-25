using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NameMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrendAnalysisAndNameStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NameStyle",
                table: "SessionFilters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DecadesPresent",
                table: "Names",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeakDecade",
                table: "Names",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "StabilityScore",
                table: "Names",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "TrendScore",
                table: "Names",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameStyle",
                table: "SessionFilters");

            migrationBuilder.DropColumn(
                name: "DecadesPresent",
                table: "Names");

            migrationBuilder.DropColumn(
                name: "PeakDecade",
                table: "Names");

            migrationBuilder.DropColumn(
                name: "StabilityScore",
                table: "Names");

            migrationBuilder.DropColumn(
                name: "TrendScore",
                table: "Names");
        }
    }
}
