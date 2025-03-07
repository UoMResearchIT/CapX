using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class MakePersonRequiredForResource : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_People_PersonId",
                table: "Resources");

            migrationBuilder.AlterColumn<int>(
                name: "PersonId",
                table: "Resources",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_People_PersonId",
                table: "Resources",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_People_PersonId",
                table: "Resources");

            migrationBuilder.AlterColumn<int>(
                name: "PersonId",
                table: "Resources",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_People_PersonId",
                table: "Resources",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }
    }
}
