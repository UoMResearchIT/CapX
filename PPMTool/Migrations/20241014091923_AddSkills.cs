using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.Json;
using PPMTool.Data.Context;
using System.Text.RegularExpressions;

#nullable disable

namespace PPMTool.Migrations
{
    

    public partial class AddSkills : Migration
    {

        string LocalFilePath = "./ToInsertSkills.txt";
        string SkillJsonEndpoint = @"https://raw.githubusercontent.com/UoMResearchIT/RSESkillsGraph/refs/heads/master/people.json";
        
        protected  bool IsDuplicatedSkill(string toInsertSkill, IEnumerable<string> existingSkills)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 .#]");
            foreach (var existingSkill in existingSkills)
            {
                var cleanedToInsertSkill = rgx.Replace(toInsertSkill.Trim().ToLower(), "");
                var cleanedExistingSkill = rgx.Replace(existingSkill.Trim().ToLower(), "");

                if (cleanedExistingSkill.Contains(cleanedToInsertSkill)) return true;
            }
            return false;
        }

        /// <summary>
        /// Get the unique skills from the <see cref="SkillJsonEndpoint"/>, , save it to a <see cref="LocalFilePath"/>, update file if it exists, and return it as a HashSet<string>
        /// </summary>
        /// <exception cref="Exception">Fail to deserialise the Json from github</exception>
        /// <exception cref="HttpRequestException">Fail to get the json document on github</exception>
        protected HashSet<string> GetSkillsFromSkillGraphRepo()
        {
            using var client = new HttpClient();

            var respone = client.GetAsync(SkillJsonEndpoint).Result.EnsureSuccessStatusCode();
            var content = respone.Content.ReadAsStringAsync().Result;
            
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(content) ?? throw new Exception("Failed to deserialize JSON");
            
            HashSet<string> toInsertSkills = new();
            foreach (var person in dictionary)
            {
                person.Value.TryGetValue("interests", out List<string> interests);
                if (interests is not null)
                {
                    toInsertSkills.UnionWith(interests);
                }
            }


            // Retrive the existing Skills Tag
            var context = new PPMToolContext();
            var ts = new Services.TagService();
            IEnumerable<string> existingSkills = ts.GetAll(context).Select(skillTag => skillTag.Name);

            // Remove duplicated skills if already exists in the database
            foreach (string skill in toInsertSkills)
            {
                if (IsDuplicatedSkill(skill, existingSkills))
                {                
                    Console.WriteLine($"Duplicated skill: {skill}");
                    toInsertSkills.Remove(skill);
                }
            }


            // Write the skills to a .txt file
            if (!File.Exists(LocalFilePath))
            {
                // file does not exist: write skills to a .txt file
                File.WriteAllLines(LocalFilePath, toInsertSkills);
                Console.WriteLine($"toInsertSkills have been written to {LocalFilePath}");
            }
            else
            {
                // file exists: compare its content, warn and update if different
                var existingContent = File.ReadAllLines(LocalFilePath);
                var newContent = toInsertSkills.ToArray();

                if (!existingContent.SequenceEqual(newContent)) // only update if the content is different
                {
                    File.WriteAllLines(LocalFilePath, toInsertSkills);
                    Console.WriteLine("Warning: File content has been updated.");
                }
            }

            return toInsertSkills;

        }

        /// <summary>
        /// read the skills from <see cref="LocalFilePath"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        IEnumerable<string> ReadSkillFromFile()
        {
            if (!File.Exists(LocalFilePath)) throw new FileNotFoundException("File not found", LocalFilePath);
            return File.ReadAllLines(LocalFilePath);
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var toInsetSkills = GetSkillsFromSkillGraphRepo();
            foreach (var skill in toInsetSkills)
            {
                migrationBuilder.InsertData(
                    table: "SkillTags",
                    columns: new[] { "Name" },
                    values: new object[] { skill }
                );
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var toRemoveSkills = ReadSkillFromFile();
            foreach (var skill in toRemoveSkills)
            {
                migrationBuilder.DeleteData(
                    table: "SkillTags",
                    keyColumn: "Name",
                    keyValue: skill
                );
            }
        }
    }
}
