// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSuperUsersFromFollowingUnownedProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Unsubscribe the super users from all projects they do not own
            migrationBuilder.Sql(@"
                DELETE FROM PersonProject
                WHERE EXISTS (
                    SELECT 1
                    FROM Users AS U
                    JOIN Projects AS P ON P.ProjectId = PersonProject.FollowedProjectsProjectId
                    WHERE U.PersonId = PersonProject.FollowersPersonId
                      AND U.RoleType = 5
                      AND P.ProjectManagerPersonId != PersonProject.FollowersPersonId
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible
        }
    }
}
