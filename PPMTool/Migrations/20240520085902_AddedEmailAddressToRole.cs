// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedEmailAddressToRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailAddress",
                table: "Roles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(
                @"
                    UPDATE Roles
                    SET EmailAddress = (
                        SELECT LOWER(REPLACE(Name, ' ', '.')) || '@manchester.ac.uk'
                        FROM People
                        WHERE People.PersonID = Roles.PersonID
                    )
                    WHERE RoleType IN (2, 3);
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailAddress",
                table: "Roles");
        }
    }
}
