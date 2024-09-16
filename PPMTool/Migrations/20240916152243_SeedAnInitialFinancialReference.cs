using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class SeedAnInitialFinancialReference : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    INSERT INTO FinancialReferences (FinancialYear, Grade41Costs, Grade51Costs, Grade55Costs, Grade65Costs, Grade71Costs, Grade75Costs, RecoveryTarget) VALUES (2023, 33333.55, 38011.97, 43172.16, 50935.80, 57458.16, 64797.29, 1118849.0);
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
