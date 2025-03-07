using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AdrianToFollowAllProjects : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    INSERT INTO PersonProject (FollowedProjectsProjectId, FollowersPersonId)
                    SELECT ProjectId, 38
                    FROM Projects
                    WHERE ProjectStatus NOT IN (5, 6, 7, 8)
                    AND NOT EXISTS (
                        SELECT 1 FROM PersonProject 
                        WHERE FollowedProjectsProjectId = Projects.ProjectId 
                        AND FollowersPersonId = 38
                    );
                ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
