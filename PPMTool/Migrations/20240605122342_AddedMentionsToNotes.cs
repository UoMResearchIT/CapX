// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedMentionsToNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ScrumProjectLink",
                table: "Projects",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "NoteId",
                table: "People",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_NoteId",
                table: "People",
                column: "NoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_People_Notes_NoteId",
                table: "People",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "NoteId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_People_Notes_NoteId",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_NoteId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "NoteId",
                table: "People");

            migrationBuilder.AlterColumn<string>(
                name: "ScrumProjectLink",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
