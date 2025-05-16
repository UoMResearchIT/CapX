using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class LinkSkillsTagsToPeople : Migration
    {
        string skillJsonEndpoint = @"https://raw.githubusercontent.com/UoMResearchIT/RSESkillsGraph/refs/heads/master/people.json";

        /// <summary>
        /// Gets the skills grouped by person from the RSE Skills Graph repository
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private Dictionary<string, List<string>> GetSkillsFromSkillGraphRepoByPerson()
        {
            using var client = new HttpClient();
            var response = client.GetAsync(skillJsonEndpoint).Result.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;

            var dictionary = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(content) ?? throw new Exception("Failed to deserialize JSON");

            Dictionary<string, List<string>> interestsByPerson = new Dictionary<string, List<string>>();
            foreach (var person in dictionary)
            {
                person.Value.TryGetValue("interests", out List<string> interests);
                if (interests is not null)
                {
                    interestsByPerson.Add(CheckSwap(person.Key), person.Value["interests"]);
                }
            }

            return interestsByPerson;

        }

        /// <summary>
        /// Some people do not have the same name in CapX as they do in the Skills Graph. Manually corrects the name.
        /// </summary>
        /// <param name="personName"></param>
        /// <returns></returns>
        private string CheckSwap(string personName)
        {
            if (personName == "Anja Le_Blanc") return "Anja Le Blanc";
            if (personName == "Martin Herrerias_Azcue") return "Martin Herrerias Azcue";
            if (personName == "Francisco Herrerias-Azcue") return "Francisco Herrerias Azcue";
            if (personName == "Tony Evans") return "Anthony Evans";
            if (personName == "Jonny Taylor") return "Jonathan Taylor";
            if (personName == "Josh Woodcock") return "Joshua Woodcock";
            return personName;
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var interestsByPerson = GetSkillsFromSkillGraphRepoByPerson();

            foreach (var interest in interestsByPerson)
            {
                Console.WriteLine($"** Adding skills for {interest.Key}");
                string personName = interest.Key;
                var skills = interest.Value;

                migrationBuilder.Sql($@"
                    INSERT INTO PersonSkillTag (PeoplePersonId, SkillTagsSkillTagId)
                    SELECT p.PersonId, s.SkillTagId
                    FROM People p
                    JOIN SkillTags s ON s.ControlledName IN ({string.Join(", ", skills.Select(skill => $"'{skill.Replace("'", "''")}'"))})
                    WHERE p.Name = '{personName.Replace("'", "''")}'
                    AND NOT EXISTS (
                        SELECT 1 FROM PersonSkillTag pst
                        WHERE pst.PeoplePersonId = p.PersonId
                        AND pst.SkillTagsSkillTagId = s.SkillTagId
                    );
                ");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Easiest thing to do is to clear out the linking table
            migrationBuilder.Sql("DELETE FROM PersonSkillTag");
        }
    }
}
