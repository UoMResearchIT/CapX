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
                SET AuthorRoleId = (SELECT RoleId FROM Roles WHERE PersonId = TempKeyNoteInfo.AuthorId LIMIT 1),
                    EditorRoleId = (SELECT RoleId FROM Roles WHERE PersonId = TempKeyNoteInfo.EditorId LIMIT 1)
                FROM TempKeyNoteInfo
                WHERE Notes.NoteId = TempKeyNoteInfo.NoteId;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Notes
                SET AuthorRoleId = AuthorId,
                    EditorRoleId = EditorId
                FROM TempKeyNoteInfo
                WHERE Notes.NoteId = TempKeyNoteInfo.NoteId;
            ");
        }
    }
}
