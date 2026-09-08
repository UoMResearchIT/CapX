// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations.Sqlite.Migrations
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
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestCompletedDate",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestOwnerId",
                table: "Projects",
                type: "INTEGER",
                nullable: true);

            // Set the request owner to the project manager for existing projects
            migrationBuilder.Sql(@"
                UPDATE Projects
                SET RequestOwnerId = ProjectManagerPersonId
                WHERE RequestOwnerId IS NULL
                  AND ProjectManagerPersonId IS NOT NULL;
                ");

            migrationBuilder.AlterColumn<int>(
                name: "RequestOwnerId",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
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
