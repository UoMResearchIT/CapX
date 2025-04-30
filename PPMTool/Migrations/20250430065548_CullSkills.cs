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
                migrationBuilder.Sql(@$"
                    DELETE FROM OwnedSkills
                    WHERE SkillId IN (
                        SELECT SkillId
                        FROM SkillTags
                        WHERE LOWER(ControlledName) == LOWER({skill})
                    );

                    DELETE FROM SkillTagSubTask
                    WHERE SkillsRequiredSkillTagId IN (
                        SELECT SkillId
                        FROM SkillTags
                        WHERE LOWER(ControlledName) == LOWER({skill})
                    );

                    DELETE FROM SkillTags
                    WHERE LOWER(ControlledName) == LOWER({skill});
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
