using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RemoveTempNotesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE TempNotes;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Can't restore this table really
        }
    }
}
