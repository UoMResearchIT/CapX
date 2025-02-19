using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class CopyNoteValuesToTempTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Copy the NoteID, AuthorId, and EditorId to a temporary table for each note
            migrationBuilder.Sql(@"
                CREATE TABLE TempNotes (
                    NoteId INTEGER,
                    AuthorId INTEGER,
                    EditorId INTEGER
                );

                INSERT INTO TempNotes (NoteId, AuthorId, EditorId)
                SELECT NoteId, AuthorPersonId, EditorPersonId
                FROM Notes;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE TempNotes
            ");
        }
    }
}
