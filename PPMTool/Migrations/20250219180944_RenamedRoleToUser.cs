// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RenamedRoleToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                FROM Roles;
            ");

            // Recreate the Notes table with foreign key constraints to the Users table
            migrationBuilder.Sql(@"
                CREATE TABLE Notes_New (
                    NoteId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                    AuthorUserId INTEGER,
                    CompletedDate TEXT,
                    CreatedDate TEXT NOT NULL,
                    DueDate TEXT,
                    EditedDate TEXT NOT NULL,
                    EditorUserId INTEGER,
                    HtmlContent TEXT NOT NULL,
                    IsFinanceInfo INTEGER NOT NULL,
                    ProjectId INTEGER NOT NULL,
                    FOREIGN KEY (AuthorUserId) REFERENCES Users (UserId) ON DELETE CASCADE,
                    FOREIGN KEY (EditorUserId) REFERENCES Users (UserId)
                );
            ");

            // Copy the notes data from the old notes table to the new table
            migrationBuilder.Sql(@"
                INSERT INTO Notes_New (NoteId, AuthorUserId, CompletedDate, CreatedDate, DueDate, EditedDate, EditorUserId, HtmlContent, IsFinanceInfo, ProjectId)
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

            // Drop the Roles table
            migrationBuilder.DropTable(
                name: "Roles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Create the Roles table
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

            // Copy data from the Users table to the Roles table
            migrationBuilder.Sql(@"
                INSERT INTO Roles (RoleId, RoleType, CASUserName, Name, PersonId, LastLoggedIn, EmailAddress)
                SELECT UserId, RoleType, CASUserName, Name, PersonId, LastLoggedIn, EmailAddress
                FROM Users;
            ");

            // Recreate the Notes table with foreign key constraints to the Roles table
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
                    ProjectId INTEGER NOT NULL,
                    FOREIGN KEY (AuthorRoleId) REFERENCES Roles (RoleId) ON DELETE CASCADE,
                    FOREIGN KEY (EditorRoleId) REFERENCES Roles (RoleId)
                );
            ");

            // Copy the notes data from the old notes table to the new table
            migrationBuilder.Sql(@"
                INSERT INTO Notes_New (NoteId, AuthorRoleId, CompletedDate, CreatedDate, DueDate, EditedDate, EditorRoleId, HtmlContent, IsFinanceInfo, ProjectId)
                SELECT NoteId, AuthorUserId, CompletedDate, CreatedDate, DueDate, EditedDate, EditorUserId, HtmlContent, IsFinanceInfo, ProjectId
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

            // Drop the Users table
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
