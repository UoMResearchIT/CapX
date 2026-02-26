using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class RemovedFacultyColumnFromProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Faculties_FacultyId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_FacultyId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FacultyId",
                table: "Projects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FacultyId",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_FacultyId",
                table: "Projects",
                column: "FacultyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Faculties_FacultyId",
                table: "Projects",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "FacultyId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
