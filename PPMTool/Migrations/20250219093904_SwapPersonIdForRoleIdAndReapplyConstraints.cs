// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class SwapPersonIdForRoleIdAndReapplyConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Lookup the Role IDs which match the author and editor IDs and update the notes table with
            // the Role IDs for the new author and editor columns
            migrationBuilder.Sql(@"
                UPDATE Notes
                SET AuthorRoleId = (SELECT RoleId FROM Roles WHERE PersonId = TempKeyNoteInfo.AuthorId LIMIT 1),
                    EditorRoleId = (SELECT RoleId FROM Roles WHERE PersonId = TempKeyNoteInfo.EditorId LIMIT 1)
                FROM TempKeyNoteInfo
                WHERE Notes.NoteId = TempKeyNoteInfo.NoteId;
            ");

            // Add foreign key constraints to the new Role ID columns
            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Roles_AuthorRoleId",
                table: "Notes",
                column: "AuthorRoleId",
                principalTable: "Roles",
                principalColumn: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Roles_EditorRoleId",
                table: "Notes",
                column: "EditorRoleId",
                principalTable: "Roles",
                principalColumn: "RoleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
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

            // Swap the RoleIds back to PersonIds
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
