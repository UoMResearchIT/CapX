// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Enums;

namespace PPMTool.Tests.API
{
    public abstract class BaseApiTest
    {
        public static string? ManagerApiKey { get; private set; }
        public static string? ManagerName { get; private set; }
        public static string? ManagerReport { get; private set; }
        public static string? DeveloperApiKey { get; private set; }

        /// <summary>
        /// Provides a configured HttpClient for making requests to the API with an API key for a manager.
        /// </summary>
        /// <returns></returns>
        public static HttpClient GetClientAsManager()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(Setup.BaseUrl);
            client.DefaultRequestHeaders.Add("x-api-key", new List<string> { ManagerApiKey! });
            return client;
        }

        /// <summary>
        /// Provides a configured HttpClient for making requests to the API with an API key for a developer.
        /// </summary>
        /// <returns></returns>
        public static HttpClient GetClientAsDeveloper()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(Setup.BaseUrl);
            client.DefaultRequestHeaders.Add("x-api-key", new List<string> { DeveloperApiKey! });
            return client;
        }

        [OneTimeSetUp]
        public virtual void OneTimeSetup()
        {
            SetupForAPI();
        }

        /// <summary>
        /// Setup for API tests. Retrieved the API keys for a manager and developer from the database and sets them for use in tests.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void SetupForAPI()
        {
            // Get the API key to use from the database
            // TODO: This needs to use the DbProvider options in the environment variables instead of hardcoding the path and provider
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

                // Get developer keys
                var developerKey = keys
                    .Where(k => k.Owner.RoleType == RoleType.Developer);

                if (developerKey.Count() == 0)
                {
                    throw new Exception("No valid API keys found for a developer in the database. Please create one for testing.");
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
                        ManagerName = key.Owner!.Person?.Name;
                        ManagerReport = report.Name;
                        return;
                    }
                }

                // If no report found then error
                throw new Exception("No valid API keys found for a manager with a report in the database. Please create one for testing.");
            }
        }

        /// <summary>
        /// Gets the start date for a date range query (one month ago).
        /// </summary>
        protected static string GetStartDate() => DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");

        /// <summary>
        /// Gets the end date for a date range query (today).
        /// </summary>
        protected static string GetEndDate() => DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>
        /// Gets the current year for year-based queries.
        /// </summary>
        protected static int GetCurrentYear() => DateTime.Now.Year;
    }
}
