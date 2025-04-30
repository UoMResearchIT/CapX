using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class CullSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Read from the data file and remove the skills with controlled names that are in the file
            migrationBuilder.Sql(@"
                -- Create a temporary table to store the skills to cull
                CREATE TEMP TABLE SkillsToCull (SkillName TEXT);

                -- Read the SkillsToCull.txt file and insert each line into the temporary table
                INSERT INTO SkillsToCull (SkillName)
                SELECT TRIM(LOWER(value))
                FROM readfile('SkillsToCull.txt');

                -- Iterate over each skill to cull
                DELETE FROM OwnedSkills
                WHERE SkillId IN (
                    SELECT SkillId
                    FROM SkillTags
                    WHERE LOWER(ControlledName) IN (SELECT SkillName FROM SkillsToCull)
                );

                DELETE FROM SkillTagSubTask
                WHERE SkillsRequiredSkillTagId IN (
                    SELECT SkillId
                    FROM SkillTags
                    WHERE LOWER(ControlledName) IN (SELECT SkillName FROM SkillsToCull)
                );

                -- Drop the temporary table
                DROP TABLE SkillsToCull;
                )   
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Don't inted to add them back as there will be data loss on associaions
        }
    }
}
