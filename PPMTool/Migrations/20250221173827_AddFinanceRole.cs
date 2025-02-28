using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddFinanceRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Users
                SET RoleType = 5
                WHERE RoleType = 4;
            ");

            migrationBuilder.Sql(@"
                UPDATE Users
                SET RoleType = 4
                WHERE RoleType = 3;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Users
                SET RoleType = 0
                WHERE RoleType = 3;
            ");

            migrationBuilder.Sql(@"
                UPDATE Users
                SET RoleType = 3
                WHERE RoleType = 4;
            ");

            migrationBuilder.Sql(@"
                UPDATE Users
                SET RoleType = 4
                WHERE RoleType = 5;
            ");
        }
    }
}
