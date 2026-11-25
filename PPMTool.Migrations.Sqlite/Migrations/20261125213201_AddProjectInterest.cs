// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectInterest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectInterest",
                columns: table => new
                {
                    InterestedPeoplePersonId = table.Column<int>(type: "INTEGER", nullable: false),
                    InterestedProjectsProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectInterest", x => new { x.InterestedPeoplePersonId, x.InterestedProjectsProjectId });
                    table.ForeignKey(
                        name: "FK_ProjectInterest_People_InterestedPeoplePersonId",
                        column: x => x.InterestedPeoplePersonId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectInterest_Projects_InterestedProjectsProjectId",
                        column: x => x.InterestedProjectsProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInterest_InterestedProjectsProjectId",
                table: "ProjectInterest",
                column: "InterestedProjectsProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectInterest");
        }
    }
}
