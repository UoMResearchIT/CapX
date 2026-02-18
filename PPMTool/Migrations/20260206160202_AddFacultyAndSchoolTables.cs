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

            // FSE
            { 2, 6, "School of Engineering", "", "SE", 1, true },
            { 3, 6, "School of Natural Sciences", "", "SNS", 2, true },

            // FBMH
            { 4, 4, "School of Biological Sciences", "", "SBS", 1, true },
            { 5, 4, "School of Medical Sciences", "", "SMS", 2, true },
            { 6, 4, "School of Health Sciences", "", "SHS", 3, true },

            // FHUMS
            { 7, 5, "Alliance Manchester Business School", "", "AMBS", 1, true },
            { 8, 5, "School of Arts, Languages and Cultures", "", "SALC", 2, true },
            { 9, 5, "School of Environment, Education and Development", "", "SEED", 3, true },
            { 10, 5, "School of Social Sciences", "", "SSS", 4, true }
                });

            //
            // 5. Add one to the enum values
            //
            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET Faculty = Faculty + 1;
                    UPDATE Projects
                    SET School = School + 1;
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

            // 2. Undo enum shift
            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET Faculty = Faculty - 1;
                    UPDATE Projects
                    SET School = School - 1;
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
