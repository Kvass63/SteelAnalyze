using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteelAnalyze.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceModelVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "ReferenceSamples",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "ReferenceSamples");
        }
    }
}
