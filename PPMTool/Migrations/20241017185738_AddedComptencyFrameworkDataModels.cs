// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedComptencyFrameworkDataModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Competency",
                columns: table => new
                {
                    CompetencyId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Objective = table.Column<string>(type: "TEXT", nullable: false),
                    Grade = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<string>(type: "TEXT", nullable: false),
                    RevisionDate = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competency", x => x.CompetencyId);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyAssessment",
                columns: table => new
                {
                    CompetencyAssessmentId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Evidence = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreated = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CompetencyRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    AssociatedCompetencyCompetencyId = table.Column<int>(type: "INTEGER", nullable: true),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencyAssessment", x => x.CompetencyAssessmentId);
                    table.ForeignKey(
                        name: "FK_CompetencyAssessment_Competency_AssociatedCompetencyCompetencyId",
                        column: x => x.AssociatedCompetencyCompetencyId,
                        principalTable: "Competency",
                        principalColumn: "CompetencyId");
                    table.ForeignKey(
                        name: "FK_CompetencyAssessment_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "PersonId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyAssessment_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessment",
                column: "AssociatedCompetencyCompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyAssessment_PersonId",
                table: "CompetencyAssessment",
                column: "PersonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetencyAssessment");

            migrationBuilder.DropTable(
                name: "Competency");
        }
    }
}
