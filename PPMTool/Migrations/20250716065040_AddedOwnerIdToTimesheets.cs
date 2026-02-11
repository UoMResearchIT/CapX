// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddedOwnerIdToTimesheets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_People_OwnerPersonId",
                table: "Timesheets");

            migrationBuilder.RenameColumn(
                name: "OwnerPersonId",
                table: "Timesheets",
                newName: "OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Timesheets_OwnerPersonId",
                table: "Timesheets",
                newName: "IX_Timesheets_OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_People_OwnerId",
                table: "Timesheets",
                column: "OwnerId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_People_OwnerId",
                table: "Timesheets");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Timesheets",
                newName: "OwnerPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_Timesheets_OwnerId",
                table: "Timesheets",
                newName: "IX_Timesheets_OwnerPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_People_OwnerPersonId",
                table: "Timesheets",
                column: "OwnerPersonId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
