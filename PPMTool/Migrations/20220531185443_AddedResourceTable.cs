// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace PPMTool.Migrations
{
    public partial class AddedResourceTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resource_People_PersonId",
                table: "Resource");

            migrationBuilder.DropForeignKey(
                name: "FK_Resource_SubTasks_SubTaskId",
                table: "Resource");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Resource",
                table: "Resource");

            migrationBuilder.RenameTable(
                name: "Resource",
                newName: "Resources");

            migrationBuilder.RenameIndex(
                name: "IX_Resource_SubTaskId",
                table: "Resources",
                newName: "IX_Resources_SubTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_Resource_PersonId",
                table: "Resources",
                newName: "IX_Resources_PersonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Resources",
                table: "Resources",
                column: "ResourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_People_PersonId",
                table: "Resources",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_SubTasks_SubTaskId",
                table: "Resources",
                column: "SubTaskId",
                principalTable: "SubTasks",
                principalColumn: "SubTaskId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_People_PersonId",
                table: "Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_SubTasks_SubTaskId",
                table: "Resources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Resources",
                table: "Resources");

            migrationBuilder.RenameTable(
                name: "Resources",
                newName: "Resource");

            migrationBuilder.RenameIndex(
                name: "IX_Resources_SubTaskId",
                table: "Resource",
                newName: "IX_Resource_SubTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_Resources_PersonId",
                table: "Resource",
                newName: "IX_Resource_PersonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Resource",
                table: "Resource",
                column: "ResourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resource_People_PersonId",
                table: "Resource",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Resource_SubTasks_SubTaskId",
                table: "Resource",
                column: "SubTaskId",
                principalTable: "SubTasks",
                principalColumn: "SubTaskId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
