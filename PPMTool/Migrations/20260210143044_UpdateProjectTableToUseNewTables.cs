using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProjectTableToUseNewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //
            // 1. Rename old string columns to new FK columns
            //
            migrationBuilder.RenameColumn(
                name: "School",
                table: "Projects",
                newName: "SchoolId");

            migrationBuilder.RenameColumn(
                name: "Faculty",
                table: "Projects",
                newName: "FacultyId");

            //
            // 2. Add foreign keys
            //
            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Faculties_FacultyId",
                table: "Projects",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "FacultyId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Schools_SchoolId",
                table: "Projects",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "SchoolId",
                onDelete: ReferentialAction.Cascade);

            //
            // 3. Create indexes
            //
            migrationBuilder.CreateIndex(
                name: "IX_Projects_FacultyId",
                table: "Projects",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_SchoolId",
                table: "Projects",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_FacultyId",
                table: "Schools",
                column: "FacultyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Disable foreign keys OUTSIDE transaction
            migrationBuilder.Sql(
                "PRAGMA foreign_keys = OFF;",
                suppressTransaction: true);

            // 2. Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Projects_FacultyId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_SchoolId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Schools_FacultyId",
                table: "Schools");

            // 3. Rename columns
            migrationBuilder.RenameColumn(
                name: "SchoolId",
                table: "Projects",
                newName: "School");

            migrationBuilder.RenameColumn(
                name: "FacultyId",
                table: "Projects",
                newName: "Faculty");

            // 4. Re-enable foreign keys OUTSIDE transaction
            migrationBuilder.Sql(
                "PRAGMA foreign_keys = ON;",
                suppressTransaction: true);
        }
    }
}
