// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RemoveNonManagerFollowersFromDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    DELETE FROM PersonProject
                    WHERE FollowersPersonId IN (
                        SELECT PersonId FROM Roles
                        WHERE RoleType NOT IN (2, 3)
                    );
                ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
