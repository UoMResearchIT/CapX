using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestColumnsToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Projects",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestCompletedDate",
                table: "Projects",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestOwnerId",
                table: "Projects",
                type: "integer",
                nullable: true);

            // Set request owner to project manager for existing rows
            migrationBuilder.Sql(@"
                UPDATE ""Projects""
                SET ""RequestOwnerId"" = ""ProjectManagerPersonId""
                WHERE ""RequestOwnerId"" IS NULL
                  AND ""ProjectManagerPersonId"" IS NOT NULL;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "RequestOwnerId",
                table: "Projects",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_RequestOwnerId",
                table: "Projects",
                column: "RequestOwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_People_RequestOwnerId",
                table: "Projects",
                column: "RequestOwnerId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_People_RequestOwnerId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_RequestOwnerId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RequestCompletedDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RequestOwnerId",
                table: "Projects");
        }
    }
}
