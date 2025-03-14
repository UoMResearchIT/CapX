using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RenamedColumnInOwnedSkillTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OpportunityWanted",
                table: "OwnedSkills",
                newName: "FavouriteSkill");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FavouriteSkill",
                table: "OwnedSkills",
                newName: "OpportunityWanted");
        }
    }
}
