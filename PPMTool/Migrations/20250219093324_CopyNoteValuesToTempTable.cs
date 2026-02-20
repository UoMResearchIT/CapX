// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Copy the values from the key info table back to the notes table then drop the key info table
            migrationBuilder.Sql(@"
                UPDATE Notes
                SET AuthorPersonId = AuthorId,
                    EditorPersonId = EditorId
                FROM TempKeyNoteInfo
                WHERE Notes.NoteId = TempKeyNoteInfo.NoteId;
            ");

            migrationBuilder.Sql(@"
                DROP TABLE TempKeyNoteInfo;
            ");
        }
    }
}
