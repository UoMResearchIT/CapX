// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RemoveTempKeyNoteInfoTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE TempKeyNoteInfo;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Create the temp notes table again which stores PersonIds
            migrationBuilder.Sql(@"
                CREATE TABLE TempKeyNoteInfo (
                    NoteId INTEGER,
                    AuthorId INTEGER,
                    EditorId INTEGER
                );

                INSERT INTO TempKeyNoteInfo (NoteId, AuthorId, EditorId)
                SELECT n.NoteId, r.PersonId AS AuthorId, e.PersonId AS EditorId
                FROM Notes n
                LEFT JOIN Roles r ON n.AuthorRoleId = r.RoleId
                LEFT JOIN Roles e ON n.EditorRoleId = e.RoleId;
            ");

        }
    }
}
