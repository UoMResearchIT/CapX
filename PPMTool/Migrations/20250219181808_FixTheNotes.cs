using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class FixTheNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Copy data from the temporary table to the Notes table
            migrationBuilder.Sql(@"
                DELETE FROM Notes;
                INSERT INTO Notes (NoteId, AuthorUserId, CompletedDate, CreatedDate, DueDate, EditedDate, EditorUserId, HtmlContent, IsFinanceInfo, ProjectId)
                SELECT NoteId, AuthorRoleId, CompletedDate, CreatedDate, DueDate, EditedDate, EditorRoleId, HtmlContent, IsFinanceInfo, ProjectId
                FROM TempNotes;
            ");

            // Drop temporary tables
            migrationBuilder.Sql(@"
                DROP TABLE TempRoles;
                DROP TABLE TempNotes;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Create the TempRoles table from the Users table
            migrationBuilder.Sql(@"
                CREATE TABLE TempRoles AS
                SELECT UserId AS RoleId, RoleType, CASUserName, Name, PersonId, LastLoggedIn, EmailAddress
                FROM Users;
            ");

            // Create the TempNotes table with the original column names
            migrationBuilder.Sql(@"
                CREATE TABLE TempNotes AS
                SELECT NoteId, AuthorUserId AS AuthorRoleId, CompletedDate, CreatedDate, DueDate, EditedDate, EditorUserId AS EditorRoleId, HtmlContent, IsFinanceInfo, ProjectId
                FROM Notes;
            ");
        }
    }
}
