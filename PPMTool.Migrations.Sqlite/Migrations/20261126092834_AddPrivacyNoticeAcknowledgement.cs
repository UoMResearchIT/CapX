// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacyNoticeAcknowledgement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyNoticeAcknowledgedAt",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PrivacyNoticeAcknowledgedDate",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrivacyNoticeAcknowledgedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PrivacyNoticeAcknowledgedDate",
                table: "Users");
        }
    }
}
