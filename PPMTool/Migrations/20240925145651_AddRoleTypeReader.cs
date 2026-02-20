// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    // original: none = 0,           , developer = 1, manager = 2, superuser = 3
    // modified: none = 0, reader = 1, developer = 2, manager = 3, superuser = 4
    public partial class AddRoleTypeReader : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql("UPDATE Roles SET RoleType = 4 WHERE RoleType = 3");
            migrationBuilder.Sql("UPDATE Roles SET RoleType = 3 WHERE RoleType = 2");
            migrationBuilder.Sql("UPDATE Roles SET RoleType = 2 WHERE RoleType = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql("UPDATE Roles SET RoleType = 0 WHERE RoleType = 1");
            migrationBuilder.Sql("UPDATE Roles SET RoleType = 1 WHERE RoleType = 2");
            migrationBuilder.Sql("UPDATE Roles SET RoleType = 2 WHERE RoleType = 3");
            migrationBuilder.Sql("UPDATE Roles SET RoleType = 3 WHERE RoleType = 4");
        }
    }
}
