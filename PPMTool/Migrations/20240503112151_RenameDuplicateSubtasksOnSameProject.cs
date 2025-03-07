using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RenameDuplicateSubtasksOnSameProject : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    UPDATE SubTasks
                    SET Name = Name || ' (Additional)'
                    WHERE rowid NOT IN (
                        SELECT MIN(rowid)
                        FROM SubTasks
                        GROUP BY ProjectId, Name
                    )
                    AND (ProjectId, Name) IN (
                        SELECT ProjectId, Name
                        FROM SubTasks
                        GROUP BY ProjectId, Name
                        HAVING COUNT(*) > 1
                    );
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible
        }
    }
}
