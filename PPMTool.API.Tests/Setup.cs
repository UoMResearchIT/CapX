// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Enums;

namespace PPMTool.API.Tests
{
    [SetUpFixture]
    public class Setup
    {
        public static string? ManagerApiKey { get; private set; }
        public static string? ManagerName { get; private set; }
        public static string? ManagerReport { get; private set; }

        [OneTimeSetUp]
        public void SetupForAll()
        {
            // Get the API key to use from the database
            var dbPath = Path.Combine(AppContext.BaseDirectory, "../../../../PPMTool/PPMTool.db");
            var options = new DbContextOptionsBuilder<PPMToolContext>()
                .UseSqlite($"Data Source={dbPath};Cache=Shared;")
                .Options;
            Debug.WriteLine($"** Using DB at: {dbPath}");

            using (var context = new PPMToolContext(options))
            {
                // Get valid API keys with related owner and person data
                var keys = context.ApiKeys
                    .Where(x => x.ExpiresAt > DateTime.Now)
                    .Include(k => k.Owner)
                        .ThenInclude(o => o.Person);

                if (keys.Count() == 0)
                {
                    throw new Exception("No valid API keys found in the database. Please create one for testing.");
                }

                // Get manager keys
                var managerKey = keys
                    .Where(k => k.Owner.RoleType == RoleType.Manager);

                if (managerKey.Count() == 0)
                {
                    throw new Exception("No valid API keys found for a manager in the database. Please create one for testing.");
                }

                // Get the first report of a manager
                foreach (var key in managerKey)
                {
                    var report = context.People
                        .Include(x => x.LineManager)
                        .FirstOrDefault(p => key.Owner.Person != null && p.LineManager != null && p.LineManager.PersonId == key.Owner.Person.PersonId);

                    // If a report is found, check valid
                    if (report != null)
                    {
                        ManagerApiKey = key.Key;
                        ManagerName = key.Owner.Person.Name;
                        ManagerReport = report.Name;
                        return;
                    }
                }

                // If no report found then error
                throw new Exception("No valid API keys found for a manager with a report in the database. Please create one for testing.");
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
        }

        /// <summary>
        /// Provides a configured HttpClient for making requests to the API with an API key for a manager.
        /// </summary>
        /// <returns></returns>
        public static HttpClient GetClientAsManager()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://localhost:6001");
            client.DefaultRequestHeaders.Add("x-api-key", new List<string> { ManagerApiKey! });
            return client;
        }
    }
}