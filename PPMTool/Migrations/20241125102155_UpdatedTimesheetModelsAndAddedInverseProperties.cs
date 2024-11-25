using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class UpdatedTimesheetModelsAndAddedInverseProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_People_ChangedByPersonId",
                table: "Timesheets");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_People_PersonId",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "MinHours",
                table: "Timesheets");

            migrationBuilder.RenameColumn(
                name: "PersonId",
                table: "Timesheets",
                newName: "OwnerPersonId");

            migrationBuilder.RenameColumn(
                name: "DateChanged",
                table: "Timesheets",
                newName: "DateStatusChanged");

            migrationBuilder.RenameColumn(
                name: "CreateDate",
                table: "Timesheets",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "ChangedByPersonId",
                table: "Timesheets",
                newName: "StatusChangedByPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_Timesheets_PersonId",
                table: "Timesheets",
                newName: "IX_Timesheets_OwnerPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_Timesheets_ChangedByPersonId",
                table: "Timesheets",
                newName: "IX_Timesheets_StatusChangedByPersonId");

            migrationBuilder.RenameColumn(
                name: "TimesheetActivityId",
                table: "TimesheetActivities",
                newName: "TimesheetEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_People_OwnerPersonId",
                table: "Timesheets",
                column: "OwnerPersonId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_People_StatusChangedByPersonId",
                table: "Timesheets",
                column: "StatusChangedByPersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_People_OwnerPersonId",
                table: "Timesheets");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_People_StatusChangedByPersonId",
                table: "Timesheets");

            migrationBuilder.RenameColumn(
                name: "StatusChangedByPersonId",
                table: "Timesheets",
                newName: "ChangedByPersonId");

            migrationBuilder.RenameColumn(
                name: "OwnerPersonId",
                table: "Timesheets",
                newName: "PersonId");

            migrationBuilder.RenameColumn(
                name: "DateStatusChanged",
                table: "Timesheets",
                newName: "DateChanged");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Timesheets",
                newName: "CreateDate");

            migrationBuilder.RenameIndex(
                name: "IX_Timesheets_StatusChangedByPersonId",
                table: "Timesheets",
                newName: "IX_Timesheets_ChangedByPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_Timesheets_OwnerPersonId",
                table: "Timesheets",
                newName: "IX_Timesheets_PersonId");

            migrationBuilder.RenameColumn(
                name: "TimesheetEntryId",
                table: "TimesheetActivities",
                newName: "TimesheetActivityId");

            migrationBuilder.AddColumn<int>(
                name: "MinHours",
                table: "Timesheets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_People_ChangedByPersonId",
                table: "Timesheets",
                column: "ChangedByPersonId",
                principalTable: "People",
                principalColumn: "PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_People_PersonId",
                table: "Timesheets",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
