using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddRoleIdsToNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Lookup the Role IDs which match the author and editor IDs and update the notes table with the Role IDs for the new author and editor columns
            migrationBuilder.Sql(@"
                UPDATE Notes
                SET AuthorRoleId = (SELECT RoleId FROM Roles WHERE PersonId = TempNotes.AuthorId LIMIT 1),
                    EditorRoleId = (SELECT RoleId FROM Roles WHERE PersonId = TempNotes.EditorId LIMIT 1)
                FROM TempNotes
                WHERE Notes.NoteId = TempNotes.NoteId;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Isn't really a down since columns would have contained rubbish anyway
        }
    }
}
