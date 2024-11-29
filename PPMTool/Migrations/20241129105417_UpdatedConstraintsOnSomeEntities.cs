using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class UpdatedConstraintsOnSomeEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InnateCodeTasks_InnateCodes_InnateCodeId",
                table: "InnateCodeTasks");

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

            migrationBuilder.AlterColumn<int>(
                name: "InnateCodeId",
                table: "InnateCodeTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InnateCodeTasks_InnateCodes_InnateCodeId",
                table: "InnateCodeTasks",
                column: "InnateCodeId",
                principalTable: "InnateCodes",
                principalColumn: "InnateCodeId",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_InnateCodeTasks_InnateCodes_InnateCodeId",
                table: "InnateCodeTasks");

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

            migrationBuilder.AlterColumn<int>(
                name: "InnateCodeId",
                table: "InnateCodeTasks",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_InnateCodeTasks_InnateCodes_InnateCodeId",
                table: "InnateCodeTasks",
                column: "InnateCodeId",
                principalTable: "InnateCodes",
                principalColumn: "InnateCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_People_PersonId",
                table: "Resources",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }
    }
}
