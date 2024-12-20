using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedInversePropertyToSubTaskProject : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubTasks_Projects_ProjectId",
                table: "SubTasks");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "SubTasks",
                newName: "OwningProjectProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_SubTasks_ProjectId",
                table: "SubTasks",
                newName: "IX_SubTasks_OwningProjectProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubTasks_Projects_OwningProjectProjectId",
                table: "SubTasks",
                column: "OwningProjectProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubTasks_Projects_OwningProjectProjectId",
                table: "SubTasks");

            migrationBuilder.RenameColumn(
                name: "OwningProjectProjectId",
                table: "SubTasks",
                newName: "ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_SubTasks_OwningProjectProjectId",
                table: "SubTasks",
                newName: "IX_SubTasks_ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubTasks_Projects_ProjectId",
                table: "SubTasks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId");
        }
    }
}
