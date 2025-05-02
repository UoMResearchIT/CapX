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
            // Read skills from file
            var localFilePath = "./Migrations/Data/SkillsToCull.txt";
            if (!File.Exists(localFilePath)) throw new FileNotFoundException("File not found", localFilePath);
            var skillsToCull = File.ReadAllLines(localFilePath);

            // Remove the skill tag from tasks and people and then delete from table
            foreach (var skill in skillsToCull)
            {
                // Log
                Console.WriteLine($"Removing \"{skill}\"...");

                // Escape apostrophe
                var cleanSkill = skill.Replace("'", "''");

                migrationBuilder.Sql(@$"
                    DELETE FROM OwnedSkills
                    WHERE SkillTagId IN (
                        SELECT SkillTagId
                        FROM SkillTags
                        WHERE LOWER(ControlledName) == LOWER('{cleanSkill}')
                    );

                    DELETE FROM SkillTagSubTask
                    WHERE SkillsRequiredSkillTagId IN (
                        SELECT SkillTagId
                        FROM SkillTags
                        WHERE LOWER(ControlledName) == LOWER('{cleanSkill}')
                    );

                    DELETE FROM SkillTags
                    WHERE LOWER(ControlledName) == LOWER('{cleanSkill}');
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Don't inted to add them back as there will be data loss on associaions
        }
    }
}
