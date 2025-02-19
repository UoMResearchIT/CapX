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
            // Not even going to bother with this!
        }
    }
}
