using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddLineManagerRelationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LineManagerPersonId",
                table: "People",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_LineManagerPersonId",
                table: "People",
                column: "LineManagerPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_People_People_LineManagerPersonId",
                table: "People",
                column: "LineManagerPersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_People_People_LineManagerPersonId",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_LineManagerPersonId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "LineManagerPersonId",
                table: "People");
        }
    }
}
