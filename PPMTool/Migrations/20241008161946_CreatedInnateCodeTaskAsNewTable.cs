// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class CreatedInnateCodeTaskAsNewTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duty",
                table: "InnateCodes");

            migrationBuilder.DropColumn(
                name: "TaskName",
                table: "InnateCodes");

            migrationBuilder.CreateTable(
                name: "InnateCodeTask",
                columns: table => new
                {
                    InnateCodeTaskId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskName = table.Column<string>(type: "TEXT", nullable: false),
                    Duty = table.Column<int>(type: "INTEGER", nullable: false),
                    InnateCodeId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnateCodeTask", x => x.InnateCodeTaskId);
                    table.ForeignKey(
                        name: "FK_InnateCodeTask_InnateCodes_InnateCodeId",
                        column: x => x.InnateCodeId,
                        principalTable: "InnateCodes",
                        principalColumn: "InnateCodeId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InnateCodeTask_InnateCodeId",
                table: "InnateCodeTask",
                column: "InnateCodeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InnateCodeTask");

            migrationBuilder.AddColumn<int>(
                name: "Duty",
                table: "InnateCodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TaskName",
                table: "InnateCodes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
