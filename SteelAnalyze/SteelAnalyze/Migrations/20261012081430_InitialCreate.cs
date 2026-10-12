using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteelAnalyze.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DefectClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    SeverityLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectClasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inspections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", nullable: false),
                    InspectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OperatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    PredictedClassId = table.Column<int>(type: "INTEGER", nullable: false),
                    Similarity = table.Column<double>(type: "REAL", nullable: false),
                    SecondDefectSuspected = table.Column<bool>(type: "INTEGER", nullable: false),
                    Verdict = table.Column<string>(type: "TEXT", nullable: false),
                    FinalVerdict = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReviewComment = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspections_DefectClasses_PredictedClassId",
                        column: x => x.PredictedClassId,
                        principalTable: "DefectClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspections_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspections_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceSamples",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DefectClassId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: false),
                    Embedding = table.Column<byte[]>(type: "BLOB", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AddedByUserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceSamples_DefectClasses_DefectClassId",
                        column: x => x.DefectClassId,
                        principalTable: "DefectClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReferenceSamples_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InspectionNeighbors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InspectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false),
                    DefectClassId = table.Column<int>(type: "INTEGER", nullable: false),
                    Similarity = table.Column<double>(type: "REAL", nullable: false),
                    ReferenceSampleId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionNeighbors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionNeighbors_DefectClasses_DefectClassId",
                        column: x => x.DefectClassId,
                        principalTable: "DefectClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InspectionNeighbors_Inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InspectionNeighbors_ReferenceSamples_ReferenceSampleId",
                        column: x => x.ReferenceSampleId,
                        principalTable: "ReferenceSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefectClasses_Code",
                table: "DefectClasses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InspectionNeighbors_DefectClassId",
                table: "InspectionNeighbors",
                column: "DefectClassId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionNeighbors_InspectionId",
                table: "InspectionNeighbors",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionNeighbors_ReferenceSampleId",
                table: "InspectionNeighbors",
                column: "ReferenceSampleId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_OperatorId",
                table: "Inspections",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_PredictedClassId",
                table: "Inspections",
                column: "PredictedClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_ReviewedByUserId",
                table: "Inspections",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceSamples_AddedByUserId",
                table: "ReferenceSamples",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceSamples_DefectClassId",
                table: "ReferenceSamples",
                column: "DefectClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspectionNeighbors");

            migrationBuilder.DropTable(
                name: "Inspections");

            migrationBuilder.DropTable(
                name: "ReferenceSamples");

            migrationBuilder.DropTable(
                name: "DefectClasses");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
