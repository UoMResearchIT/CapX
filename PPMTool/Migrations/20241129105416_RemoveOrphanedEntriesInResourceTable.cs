// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RemoveOrphanedEntriesInResourceTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    DELETE FROM Resources
                    WHERE PersonId IS NULL
                       OR SubTaskId IS NULL
                       OR PersonId NOT IN (SELECT PersonId FROM People)
                       OR SubTaskId NOT IN (SELECT SubTaskId FROM SubTasks);
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
