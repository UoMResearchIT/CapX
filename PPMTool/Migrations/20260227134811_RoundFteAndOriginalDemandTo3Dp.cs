// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class RoundFteAndOriginalDemandTo3Dp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""SubTasks""
                SET ""OriginalDemand"" = ROUND(""OriginalDemand"", 3)
                WHERE ""OriginalDemand"" != ROUND(""OriginalDemand"", 3);
            ");

            migrationBuilder.Sql(@"
                UPDATE ""SubTasks""
                SET ""Demand"" = ROUND(""Demand"", 3)
                WHERE ""Demand"" != ROUND(""Demand"", 3);
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Resources""
                SET ""AssignmentFTE"" = ROUND(""AssignmentFTE"", 3)
                WHERE ""AssignmentFTE"" != ROUND(""AssignmentFTE"", 3);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty:
            // rounding is a lossy operation and should not be reversed.
        }
    }
}
