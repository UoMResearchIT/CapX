using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class SwapAuthorAndRoleOnNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create a new table with the same schema as the original table, but without the foreign key constraints
            migrationBuilder.Sql(@"
                CREATE TABLE Notes_New (
                    NoteId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                    AuthorRoleId INTEGER,
                    CompletedDate TEXT,
                    CreatedDate TEXT NOT NULL,
                    DueDate TEXT,
                    EditedDate TEXT NOT NULL,
                    EditorRoleId INTEGER,
                    HtmlContent TEXT NOT NULL,
                    IsFinanceInfo INTEGER NOT NULL,
                    ProjectId INTEGER NOT NULL
                );
            ");

            // Copy the data from the original table to the new table
            migrationBuilder.Sql(@"
                INSERT INTO Notes_New (NoteId, AuthorRoleId, CompletedDate, CreatedDate, DueDate, EditedDate, EditorRoleId, HtmlContent, IsFinanceInfo, ProjectId)
                SELECT NoteId, AuthorPersonId, CompletedDate, CreatedDate, DueDate, EditedDate, EditorPersonId, HtmlContent, IsFinanceInfo, ProjectId
                FROM Notes;
            ");

            // Drop the original table
            migrationBuilder.Sql(@"
                DROP TABLE Notes;
            ");

            // Rename the new table to the original table name
            migrationBuilder.Sql(@"
                ALTER TABLE Notes_New RENAME TO Notes;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Create a new table with the same schema as the original table, but with the foreign key constraints
            migrationBuilder.Sql(@"
                CREATE TABLE Notes_New (
                    NoteId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                    AuthorPersonId INTEGER,
                    CompletedDate TEXT,
                    CreatedDate TEXT NOT NULL,
                    DueDate TEXT,
                    EditedDate TEXT NOT NULL,
                    EditorPersonId INTEGER,
                    HtmlContent TEXT NOT NULL,
                    IsFinanceInfo INTEGER NOT NULL,
                    ProjectId INTEGER NOT NULL,
                    FOREIGN KEY (AuthorPersonId) REFERENCES People (PersonId) ON DELETE CASCADE,
                    FOREIGN KEY (EditorPersonId) REFERENCES People (PersonId)
                );
            ");

            // Copy the data from the original table to the new table
            migrationBuilder.Sql(@"
                INSERT INTO Notes_New (NoteId, AuthorPersonId, CompletedDate, CreatedDate, DueDate, EditedDate, EditorPersonId, HtmlContent, IsFinanceInfo, ProjectId)
                SELECT NoteId, AuthorRoleId, CompletedDate, CreatedDate, DueDate, EditedDate, EditorRoleId, HtmlContent, IsFinanceInfo, ProjectId
                FROM Notes;
            ");

            // Drop the original table
            migrationBuilder.Sql(@"
                DROP TABLE Notes;
            ");

            // Rename the new table to the original table name
            migrationBuilder.Sql(@"
                ALTER TABLE Notes_New RENAME TO Notes;
            ");
        }
    }
}
