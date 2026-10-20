using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteelAnalyze.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceFileInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "ReferenceSamples",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "ReferenceSamples",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceSamples_ContentHash",
                table: "ReferenceSamples",
                column: "ContentHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReferenceSamples_ContentHash",
                table: "ReferenceSamples");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "ReferenceSamples");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "ReferenceSamples");
        }
    }
}
