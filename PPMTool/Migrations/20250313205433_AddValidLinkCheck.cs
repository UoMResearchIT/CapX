using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddValidLinkCheck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HasValidWikiLink",
                table: "SkillTags",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Create an HTTP client to check the links
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(3);

                // Load the configuration from appsettings.json
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                // Get the connection string
                var connectionString = configuration.GetConnectionString("PPMToolContextConnection");

                // Get all records from the SkillTags table
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT SkillTagId, ControlledName
                        FROM SkillTags
                    ";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var skillTagId = reader.GetInt32(0);
                            var controlledName = reader.GetString(1);
                            string wikiLink = "zzzzzzzz";

                            // The assembly containing the class
                            Assembly assembly = Assembly.GetExecutingAssembly();

                            // The class name as a string
                            string className = "PPMTool.Data.Entities.SkillTag";

                            // The method name as a string
                            string methodName = "GetWikiLink";

                            // The parameter to pass to the method
                            string parameter = controlledName;

                            // Get the type of the class
                            Type type = assembly.GetType(className);

                            if (type != null)
                            {
                                // Get the method information
                                MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);

                                if (methodInfo != null)
                                {
                                    // Invoke the method with the required parameters
                                    wikiLink = methodInfo.Invoke(null, new object[] { parameter }) as string;
                                }
                                else
                                {
                                    throw new Exception($"Method '{methodName}' not found in class '{className}'.");
                                }
                            }
                            else
                            {
                                throw new Exception($"Class '{className}' not found in assembly.");
                            }

                            try
                            {
                                // Check if the WikiLink is valid and update
                                var response = Task.Run(async () => await httpClient.GetAsync(wikiLink)).Result;

                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    migrationBuilder.Sql($@"
                                        UPDATE SkillTags
                                        SET HasValidWikiLink = 2
                                        WHERE SkillTagId = {skillTagId}
                                    ");
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                                {
                                    migrationBuilder.Sql($@"
                                        UPDATE SkillTags
                                        SET HasValidWikiLink = 1
                                        WHERE SkillTagId = {skillTagId}
                                    ");
                                }

                                // Write the status code and ControlledName to the console
                                Console.WriteLine($"Status Code: {response.StatusCode}, ControlledName: {controlledName}");
                            }
                            catch (Exception ex)
                            {
                                migrationBuilder.Sql($@"
                                    UPDATE SkillTags
                                    SET HasValidWikiLink = 0
                                    WHERE SkillTagId = {skillTagId}
                                ");
                                Console.WriteLine($"Error checking link for {controlledName}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasValidWikiLink",
                table: "SkillTags");
        }
    }
}
