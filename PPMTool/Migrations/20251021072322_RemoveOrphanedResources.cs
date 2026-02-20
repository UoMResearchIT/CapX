// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrphanedResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM resources
                WHERE SubtaskId IN (
                    SELECT s.SubtaskId
                    FROM subtasks AS s
                    JOIN projects AS p ON s.OwningProjectProjectId = p.ProjectId
                    WHERE p.ProjectStatus > 7
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible migration - no action taken
        }
    }
}
