// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class SkillTagPersonManyToMany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SkillTags_People_PersonId",
                table: "SkillTags");

            migrationBuilder.DropIndex(
                name: "IX_SkillTags_PersonId",
                table: "SkillTags");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "SkillTags");

            migrationBuilder.CreateTable(
                name: "PersonSkillTag",
                columns: table => new
                {
                    PeoplePersonId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillTagsSkillTagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonSkillTag", x => new { x.PeoplePersonId, x.SkillTagsSkillTagId });
                    table.ForeignKey(
                        name: "FK_PersonSkillTag_People_PeoplePersonId",
                        column: x => x.PeoplePersonId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonSkillTag_SkillTags_SkillTagsSkillTagId",
                        column: x => x.SkillTagsSkillTagId,
                        principalTable: "SkillTags",
                        principalColumn: "SkillTagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonSkillTag_SkillTagsSkillTagId",
                table: "PersonSkillTag",
                column: "SkillTagsSkillTagId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonSkillTag");

            migrationBuilder.AddColumn<int>(
                name: "PersonId",
                table: "SkillTags",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillTags_PersonId",
                table: "SkillTags",
                column: "PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_SkillTags_People_PersonId",
                table: "SkillTags",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }
    }
}
