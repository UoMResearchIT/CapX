// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddSkillsFromSkillsGraphDB : Migration
    {
        private string localFilePath = "./Migrations/Data/SkillsFromSkillsGraph-migration.txt";
        string SkillJsonEndpoint = @"https://raw.githubusercontent.com/UoMResearchIT/RSESkillsGraph/refs/heads/master/people.json";

        /// <summary>
        /// Get the unique skills from the <see cref="SkillJsonEndpoint"/>, save it to a <see cref="LocalFilePath"/>, update file if it exists, and return it as a HashSet<string>
        /// </summary>
        /// <exception cref="Exception">Fail to deserialise the Json from github</exception>
        /// <exception cref="HttpRequestException">Fail to get the json document on github</exception>
        private HashSet<string> GetSkillsFromSkillGraphRepo()
        {
            using var client = new HttpClient();

            var response = client.GetAsync(SkillJsonEndpoint).Result.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;

            var dictionary = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(content) ?? throw new Exception("Failed to deserialize JSON");

            HashSet<string> skillsToInsert = new();
            foreach (var person in dictionary)
            {
                person.Value.TryGetValue("interests", out List<string> interests);
                if (interests is not null)
                {
                    skillsToInsert.UnionWith(interests);
                }
            }

            // Overwrite local file if it exists
            File.WriteAllLines(localFilePath, skillsToInsert, Encoding.UTF8);
            Console.WriteLine($"{skillsToInsert.Count} skills from the Skills Graph have been written to {localFilePath}");

            return skillsToInsert;

        }

        /// <summary>
        /// Read the skills from <see cref="LocalFilePath"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private IEnumerable<string> ReadSkillsFromFile()
        {
            if (!File.Exists(localFilePath)) throw new FileNotFoundException("File not found", localFilePath);
            return File.ReadAllLines(localFilePath);
        }

        /// <summary>
        /// Tries to get a nicer-looking display name from the "wikipedia controlled vocab" name
        /// </summary>
        /// <param name="skill"></param>
        /// <returns></returns>
        private string GetFriendlyName(string skill)
        {
            // Regular expression to match parentheses and their content at the end of the string
            string pattern = @"\s*\([^)]*\)\s*$";
            var cleanedSkill = skill;
            while (Regex.IsMatch(cleanedSkill, pattern))
            {
                cleanedSkill = Regex.Replace(cleanedSkill, pattern, "").Trim();
            }

            if (cleanedSkill != skill)
            {
                Console.WriteLine($"** Changed {skill} to {cleanedSkill}");
            }

            return cleanedSkill;
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Read the skills from the Skills Graph DB
            var skillsToInsert = GetSkillsFromSkillGraphRepo();

            // Add them to the SkillTags table
            foreach (var skill in skillsToInsert)
            {
                var escapedSkill = skill.Replace("'", "''");
                var friendlyName = GetFriendlyName(escapedSkill);

                // Use a raw SQL query to insert the skill if it doesn't already exist
                migrationBuilder.Sql($@"
                    INSERT INTO SkillTags (Name, ControlledName)
                    SELECT '{friendlyName}', '{escapedSkill}'
                    WHERE NOT EXISTS (
                        SELECT 1 FROM SkillTags WHERE Name = '{friendlyName}'
                    );
                ");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Read the skills from the local file which was written during the Up method
            var toRemoveSkills = ReadSkillsFromFile();

            // Remove those skills from the table -- not a lot we can do about duplicates of existing data!
            foreach (var skill in toRemoveSkills)
            {
                migrationBuilder.DeleteData(
                    table: "SkillTags",
                    keyColumn: "ControlledName",
                    keyValue: skill.Replace("'", "''")
                );
            }
        }
    }
}
