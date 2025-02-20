using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RemoveTempNotesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE TempKeyNoteInfo;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Create the temp notes table again
            migrationBuilder.Sql(@"
                CREATE TABLE TempKeyNoteInfo (
                    NoteId INTEGER,
                    AuthorId INTEGER,
                    EditorId INTEGER
                );

                INSERT INTO TempKeyNoteInfo (NoteId, AuthorId, EditorId)
                SELECT NoteId, AuthorPersonId, EditorPersonId
                FROM Notes;
            ");

        }
    }
}
