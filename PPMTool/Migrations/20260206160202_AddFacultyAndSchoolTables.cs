// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddFacultyAndSchoolTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //
            // 1. Create Faculties table
            //
            migrationBuilder.CreateTable(
                name: "Faculties",
                columns: table => new
                {
                    FacultyId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.FacultyId);
                });

            //
            // 2. Create Schools table
            //
            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    SchoolId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FacultyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.SchoolId);
                    table.ForeignKey(
                        name: "FK_Schools_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "FacultyId",
                        onDelete: ReferentialAction.Cascade);
                });

            //
            // 3. Seed Faculties (including None)
            //
            migrationBuilder.InsertData(
                table: "Faculties",
                columns: new[] { "FacultyId", "Name", "Description", "Code", "Order", "IsActive" },
                values: new object[,]
                {
                    { 1, "None", "", "None", 0, true },

                    { 2, "Research IT / Internal", "", "Internal", 1, true },
                    { 3, "Professional Services and Cultural Institutions", "", "PSCI", 2, true },
                    { 4, "Biology, Medicine and Health", "", "FBMH", 3, true },
                    { 5, "Humanities", "", "FHUMS", 4, true },
                    { 6, "Science and Engineering", "", "FSE", 5, true },
                    { 7, "Research Lifecycle Programme", "", "RLP", 6, true },
                    { 8, "Commercial / External", "", "External", 7, true },
                    { 9, "Cross-Faculty Research Institutes", "", "ResInst", 8, true }
                });

            //
            // 4. Seed Schools (including None)
            //
            migrationBuilder.InsertData(
                table: "Schools",
                columns: new[] { "SchoolId", "FacultyId", "Name", "Description", "Code", "Order", "IsActive" },
                values: new object[,]
                {
                    { 1, 1, "None", "", "None", 0, true },
                    { 2, 2, "None", "", "None", 0, true },
                    { 3, 3, "None", "", "None", 0, true },
                    { 4, 7, "None", "", "None", 0, true },
                    { 5, 8, "None", "", "None", 0, true },
                    { 6, 9, "None", "", "None", 0, true },

                    // FSE
                    { 7, 6, "School of Engineering", "", "SE", 1, true },
                    { 8, 6, "School of Natural Sciences", "", "SNS", 2, true },

                    // FBMH
                    { 9, 4, "School of Biological Sciences", "", "SBS", 1, true },
                    { 10, 4, "School of Medical Sciences", "", "SMS", 2, true },
                    { 11, 4, "School of Health Sciences", "", "SHS", 3, true },

                    // FHUMS
                    { 12, 5, "Alliance Manchester Business School", "", "AMBS", 1, true },
                    { 13, 5, "School of Arts, Languages and Cultures", "", "SALC", 2, true },
                    { 14, 5, "School of Environment, Education and Development", "", "SEED", 3, true },
                    { 15, 5, "School of Social Sciences", "", "SSS", 4, true }
                });

            //
            // 5. Set new schools
            //
            migrationBuilder.Sql(@"
                UPDATE Projects
                SET School =
                    CASE Faculty
                        WHEN 0 THEN 1
                        WHEN 1 THEN 2
                        WHEN 2 THEN 3
                        WHEN 6 THEN 4
                        WHEN 7 THEN 5
                        WHEN 8 THEN 6
                        WHEN 3 THEN School + 6
                        WHEN 4 THEN School + 6
                        WHEN 5 THEN School + 6
                        ELSE School
                    END
            ");

            //
            // 6. Set new faculties
            //
            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET Faculty = Faculty + 1;
                "
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Disable FK enforcement OUTSIDE transaction
            migrationBuilder.Sql(
                "PRAGMA foreign_keys = OFF;",
                suppressTransaction: true);

            //
            // 2a. Revert faculties
            //
            migrationBuilder.Sql(@"
                UPDATE Projects
                SET Faculty = Faculty - 1;
            ");

            //
            // 2b. Revert schools
            //
            migrationBuilder.Sql(@"
                UPDATE Projects
                SET School =
                    CASE Faculty
                        WHEN 0 THEN 0
                        WHEN 1 THEN 1
                        WHEN 2 THEN 2
                        WHEN 6 THEN 6
                        WHEN 7 THEN 7
                        WHEN 8 THEN 8
                        WHEN 3 THEN School - 6
                        WHEN 4 THEN School - 6
                        WHEN 5 THEN School - 6
                        ELSE School
                    END
            ");

            // 3. Drop indexes defensively (SQLite-safe)
            migrationBuilder.Sql(
                @"DROP INDEX IF EXISTS ""IX_Projects_FacultyId"";");

            migrationBuilder.Sql(
                @"DROP INDEX IF EXISTS ""IX_Projects_SchoolId"";");

            // 4. Drop dependent tables
            migrationBuilder.DropTable(name: "Schools");
            migrationBuilder.DropTable(name: "Faculties");

            // 5. Re-enable FK enforcement OUTSIDE transaction
            migrationBuilder.Sql(
                "PRAGMA foreign_keys = ON;",
                suppressTransaction: true);
        }
    }
}
