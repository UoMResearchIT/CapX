using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedFinancialReferenceEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialReferences",
                columns: table => new
                {
                    FinancialReferenceId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FinancialYear = table.Column<int>(type: "INTEGER", nullable: false),
                    Grade41Costs = table.Column<float>(type: "REAL", nullable: false),
                    Grade55Costs = table.Column<float>(type: "REAL", nullable: false),
                    Grade65Costs = table.Column<float>(type: "REAL", nullable: false),
                    Grade71Costs = table.Column<float>(type: "REAL", nullable: false),
                    Grade75Costs = table.Column<float>(type: "REAL", nullable: false),
                    RecoveryTarget = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialReferences", x => x.FinancialReferenceId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialReferences");
        }
    }
}
