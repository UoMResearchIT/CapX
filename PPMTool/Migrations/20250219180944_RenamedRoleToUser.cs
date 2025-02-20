using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RenamedRoleToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create a temporary table to store the data from the Roles table
            migrationBuilder.Sql(@"
                CREATE TABLE TempRoles AS
                SELECT * FROM Roles;
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE TempNotes AS
                SELECT * FROM Notes;
            ");

            // Create the Users table
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleType = table.Column<int>(type: "INTEGER", nullable: false),
                    CASUserName = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: true),
                    LastLoggedIn = table.Column<string>(type: "TEXT", nullable: true),
                    EmailAddress = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "PersonId");
                });

            // Copy data from the temporary table to the Users table
            migrationBuilder.Sql(@"
                INSERT INTO Users (UserId, RoleType, CASUserName, Name, PersonId, LastLoggedIn, EmailAddress)
                SELECT RoleId, RoleType, CASUserName, Name, PersonId, LastLoggedIn, EmailAddress
                FROM TempRoles;
            ");

            // Drop foreign keys referencing the Roles table
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Roles_AuthorRoleId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Roles_EditorRoleId",
                table: "Notes");

            // Rename columns in the Notes table
            migrationBuilder.RenameColumn(
                name: "EditorRoleId",
                table: "Notes",
                newName: "EditorUserId");

            migrationBuilder.RenameColumn(
                name: "AuthorRoleId",
                table: "Notes",
                newName: "AuthorUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Notes_EditorRoleId",
                table: "Notes",
                newName: "IX_Notes_EditorUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Notes_AuthorRoleId",
                table: "Notes",
                newName: "IX_Notes_AuthorUserId");

            // Add foreign keys referencing the Users table
            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Users_AuthorUserId",
                table: "Notes",
                column: "AuthorUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Users_EditorUserId",
                table: "Notes",
                column: "EditorUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            // Drop the Roles table
            migrationBuilder.DropTable(
                name: "Roles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Create a temporary table to store the data from the Users table
            migrationBuilder.Sql(@"
                CREATE TABLE TempUsers AS
                SELECT * FROM Users;
            ");

            // Recreate the Roles table
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: true),
                    CASUserName = table.Column<string>(type: "TEXT", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", nullable: true),
                    LastLoggedIn = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    RoleType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                    table.ForeignKey(
                        name: "FK_Roles_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "PersonId");
                });

            // Copy data from the temporary table to the Roles table
            migrationBuilder.Sql(@"
                INSERT INTO Roles (RoleId, RoleType, CASUserName, Name, PersonId, LastLoggedIn, EmailAddress)
                SELECT UserId, RoleType, CASUserName, Name, PersonId, LastLoggedIn, EmailAddress
                FROM TempUsers;
            ");

            // Drop foreign keys referencing the Users table
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Users_AuthorUserId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Users_EditorUserId",
                table: "Notes");

            // Rename columns in the Notes table
            migrationBuilder.RenameColumn(
                name: "EditorUserId",
                table: "Notes",
                newName: "EditorRoleId");

            migrationBuilder.RenameColumn(
                name: "AuthorUserId",
                table: "Notes",
                newName: "AuthorRoleId");

            migrationBuilder.RenameIndex(
                name: "IX_Notes_EditorUserId",
                table: "Notes",
                newName: "IX_Notes_EditorRoleId");

            migrationBuilder.RenameIndex(
                name: "IX_Notes_AuthorUserId",
                table: "Notes",
                newName: "IX_Notes_AuthorRoleId");

            // Add foreign keys referencing the Roles table
            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Roles_AuthorRoleId",
                table: "Notes",
                column: "AuthorRoleId",
                principalTable: "Roles",
                principalColumn: "RoleId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Roles_EditorRoleId",
                table: "Notes",
                column: "EditorRoleId",
                principalTable: "Roles",
                principalColumn: "RoleId");

            // Drop the temporary tables
            migrationBuilder.Sql(@"
                DROP TABLE TempUsers;
                DROP TABLE TempRoles;
                DROP TABLE TempNotes;
            ");
        }
    }
}
