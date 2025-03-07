// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace PPMTool.Migrations
{
    public partial class AddSkillTagTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SkillTag_People_PersonId",
                table: "SkillTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SkillTag",
                table: "SkillTag");

            migrationBuilder.RenameTable(
                name: "SkillTag",
                newName: "SkillTags");

            migrationBuilder.RenameIndex(
                name: "IX_SkillTag_PersonId",
                table: "SkillTags",
                newName: "IX_SkillTags_PersonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SkillTags",
                table: "SkillTags",
                column: "SkillTagId");

            migrationBuilder.AddForeignKey(
                name: "FK_SkillTags_People_PersonId",
                table: "SkillTags",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SkillTags_People_PersonId",
                table: "SkillTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SkillTags",
                table: "SkillTags");

            migrationBuilder.RenameTable(
                name: "SkillTags",
                newName: "SkillTag");

            migrationBuilder.RenameIndex(
                name: "IX_SkillTags_PersonId",
                table: "SkillTag",
                newName: "IX_SkillTag_PersonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SkillTag",
                table: "SkillTag",
                column: "SkillTagId");

            migrationBuilder.AddForeignKey(
                name: "FK_SkillTag_People_PersonId",
                table: "SkillTag",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
