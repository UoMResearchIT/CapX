using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    FeatureId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    FeatureType = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    MustAlwaysBeEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.FeatureId);
                });

            // Seed features
            migrationBuilder.Sql(@"
                INSERT INTO features (FeatureType, Name, Description, Enabled, MustAlwaysBeEnabled) VALUES
                (
                  0,
                  'People',
                  'This is a mandatory feature that allows administrators to manage a team of people. People are at the centre of the data model so this cannot be disabled.',
                  1,
                  1
                );

                INSERT INTO features (FeatureType, Name, Description) VALUES
                (
                  1,      
                  'Projects & Capacity',
                  'Allows mangers to add projects to the database and assign people to them. It also allows them to visualise the capacity of their team based on the project assignments and workload models of the people.'
                ),
                (
                  2,      
                  'Absences',
                  'Allows mangers to add absences for team members, notifying project managers when their resources may be unavailable.'
                ),
                (
                  3,  
                  'Skills',
                  'Allows administrators to curate a list of skill tags, people to add their skill profiles and tasks within projects to be associated with skill tags.'
                ),
                (
                  4,
                  'Development Journey',
                  'Allows the inclusion of a three-tier competency framework and the ability of staff and managers to use it as a tool to support development.'
                ),
                (
                  5,
                  'API',
                  'Enables the API endpoints. The available endpoints will be determined by the features enabled.'
                ),
                (
                  6,
                  'Timesheets',
                  'Allows people to enter weekly timesheet data against activity and task codes curated by administrators. This also enables workload model analysis to allow people to monitor how they are spending their time against their workload mdoel.'
                ),
                (
                  7,
                  'Project Finance',
                  'Allows managers to keep financial records associated with projects including funding sources and invoices. Allows the cost of projects to calculated.'
                ),
                (
                  8,
                  'Data Dashboard',
                  'Provides a page visible to all system users that summarises key information and allows export of data from the database in structured reports.'
                );
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Features");
        }
    }
}
