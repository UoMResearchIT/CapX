using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class CreatedOwnedSkills : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OwnedSkills",
                columns: table => new
                {
                    OwnedSkillId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OwnerPersonId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Proficiency = table.Column<int>(type: "INTEGER", nullable: false),
                    OpportunityWanted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnedSkills", x => x.OwnedSkillId);
                    table.ForeignKey(
                        name: "FK_OwnedSkills_People_OwnerPersonId",
                        column: x => x.OwnerPersonId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OwnedSkills_SkillTags_SkillTagId",
                        column: x => x.SkillTagId,
                        principalTable: "SkillTags",
                        principalColumn: "SkillTagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwnedSkills_OwnerPersonId",
                table: "OwnedSkills",
                column: "OwnerPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnedSkills_SkillTagId",
                table: "OwnedSkills",
                column: "SkillTagId");

            // Migrate data from PersonSkillTag to OwnedSkills
            migrationBuilder.Sql(@"
                INSERT INTO OwnedSkills (OwnerPersonId, SkillTagId)
                SELECT PeoplePersonId, SkillTagsSkillTagId
                FROM PersonSkillTag;
            ");

            // Drop the PersonSkillTag table
            migrationBuilder.DropTable(
                name: "PersonSkillTag");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            // Migrate data from OwnedSkills to PersonSkillTag
            migrationBuilder.Sql(@"
                INSERT INTO PersonSkillTag (PeoplePersonId, SkillTagsSkillTagId)
                SELECT OwnerPersonId, SkillTagId
                FROM OwnedSkills;
            ");

            // Drop the OwnedSkills table
            migrationBuilder.DropTable(
                name: "OwnedSkills");
        }
    }
}
