using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteelAnalyze.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionNeighborVotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NeighborVotes",
                table: "Inspections",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Для уже сохранённых записей журнала голоса считаются по их ближайшим эталонам
            migrationBuilder.Sql(
                """
                UPDATE Inspections
                SET NeighborVotes = (
                    SELECT COUNT(*) FROM InspectionNeighbors n
                    WHERE n.InspectionId = Inspections.Id AND n.DefectClassId = Inspections.PredictedClassId)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeighborVotes",
                table: "Inspections");
        }
    }
}
