using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddFacultyAndSchoolColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int?>(
                name: "Faculty",
                table: "Projects",
                type: "INTEGER",
                nullable: true,
                defaultValue: null);

            migrationBuilder.AddColumn<int?>(
                name: "School",
                table: "Projects",
                type: "INTEGER",
                nullable: true,
                defaultValue: null);

            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET Faculty = CASE
                        WHEN Portfolio = 1 THEN 0
                        WHEN Portfolio IN (5, 6, 7, 8) THEN NULL
                        WHEN Portfolio = 9 THEN 5
                        ELSE Portfolio
                    END;
                    ALTER TABLE Projects DROP COLUMN Portfolio;
                ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                table: "Projects",
                name: "Portfolio",
                type: "INTEGER",
                nullable: true
                );

            // Not an exact reversal of the Up method
            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET Portfolio = CASE
                        WHEN Faculty = 0 THEN 1
                        WHEN Faculty = NULL THEN 6
                        WHEN Faculty = 5 THEN 9
                        ELSE Faculty
                    END;
                ");

            migrationBuilder.DropColumn(
                name: "School",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Faculty",
                table: "Projects");
        }
    }
}
