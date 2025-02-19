using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class SwapAuthorAndRoleOnNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_People_AuthorPersonId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_People_EditorPersonId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_AuthorPersonId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "AuthorPersonId",
                table: "Notes");

            migrationBuilder.RenameColumn(
                name: "EditorPersonId",
                table: "Notes",
                newName: "EditorRoleId");

            migrationBuilder.RenameIndex(
                name: "IX_Notes_EditorPersonId",
                table: "Notes",
                newName: "IX_Notes_EditorRoleId");

            migrationBuilder.AddColumn<int>(
                name: "AuthorRoleId",
                table: "Notes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notes_AuthorRoleId",
                table: "Notes",
                column: "AuthorRoleId");

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
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Roles_AuthorRoleId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Roles_EditorRoleId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_AuthorRoleId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "AuthorRoleId",
                table: "Notes");

            migrationBuilder.RenameColumn(
                name: "EditorRoleId",
                table: "Notes",
                newName: "EditorPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_Notes_EditorRoleId",
                table: "Notes",
                newName: "IX_Notes_EditorPersonId");

            migrationBuilder.AddColumn<int>(
                name: "AuthorPersonId",
                table: "Notes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Notes_AuthorPersonId",
                table: "Notes",
                column: "AuthorPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_People_AuthorPersonId",
                table: "Notes",
                column: "AuthorPersonId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_People_EditorPersonId",
                table: "Notes",
                column: "EditorPersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }
    }
}
