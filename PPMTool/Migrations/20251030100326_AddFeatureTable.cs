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
                    MustAlwaysBeEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.FeatureId);
                });

            // Seed features
            migrationBuilder.Sql(@"
                INSERT INTO features (Name, Description, Enabled, MustAlwaysBeEnabled) VALUES
                (
                  'Projects, Capacity & People',
                  'The core feature of the tool is provide visualisation of project load against availability of resources. As such, this is a mandatory feature the allows administrators to manage a team of people and a portfolio of projects a how that relates to the capacity of the team.',
                  1,
                  1
                );

                INSERT INTO features (Name, Description) VALUES
                (
                  'Absences',
                  'Allows mangers to add absences for team members, notifying project managers when their resources may be unavailable.'
                ),
                (
                  'Skills',
                  'Allows administrators to curate a list of skill tags, people to add their skill profiles and tasks within projects to be associated with skill tags.'
                ),
                (
                  'Development Journey',
                  'Allows the inclusion of a three-tier competency framework and the ability of staff and managers to use it as a tool to support development.'
                ),
                (
                  'API',
                  'Enables the API endpoints. The available endpoints will be determined by the features enabled.'
                ),
                (
                  'Timesheets',
                  'Allows people to enter weekly timesheet data against activity and task codes curated by administrators. This also enables workload model analysis to allow people to monitor how they are spending their time against their workload mdoel.'
                ),
                (
                  'Project Finance',
                  'Allows managers to keep financial records associated with projects including funding sources and invoices. Allows the cost of projects to calculated.'
                ),
                (
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
