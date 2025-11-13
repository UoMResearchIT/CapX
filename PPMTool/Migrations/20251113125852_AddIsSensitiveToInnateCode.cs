using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSensitiveToInnateCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSensitive",
                table: "InnateCodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Mark specific codes as sensitive from data file
            var filePath = "./Migrations/Data/SensitiveInnateCodes.txt";
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var activityCode = line.Trim().Replace("'", "''");
                    if (!string.IsNullOrWhiteSpace(activityCode))
                    {
                        var sqlCommand = $@"
                            UPDATE InnateCodes
                            SET IsSensitive = 1
                            WHERE ActivityCode = '{activityCode}';
                        ";
                        migrationBuilder.Sql(sqlCommand);
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSensitive",
                table: "InnateCodes");
        }
    }
}
