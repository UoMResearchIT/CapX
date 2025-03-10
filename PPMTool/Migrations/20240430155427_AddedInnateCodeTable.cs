using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedInnateCodeTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InnateActivity",
                table: "Projects");

            migrationBuilder.AddColumn<int>(
                name: "InnateActivityInnateCodeId",
                table: "Projects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InnateCodes",
                columns: table => new
                {
                    InnateCodeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityCode = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnateCodes", x => x.InnateCodeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_InnateActivityInnateCodeId",
                table: "Projects",
                column: "InnateActivityInnateCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_InnateCodes_InnateActivityInnateCodeId",
                table: "Projects",
                column: "InnateActivityInnateCodeId",
                principalTable: "InnateCodes",
                principalColumn: "InnateCodeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_InnateCodes_InnateActivityInnateCodeId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "InnateCodes");

            migrationBuilder.DropIndex(
                name: "IX_Projects_InnateActivityInnateCodeId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "InnateActivityInnateCodeId",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "InnateActivity",
                table: "Projects",
                type: "TEXT",
                nullable: true);
        }
    }
}
