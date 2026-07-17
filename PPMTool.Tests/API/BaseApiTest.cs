// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Enums;

namespace PPMTool.Tests.API
{
    public abstract class BaseApiTest
    {
        protected static string? ManagerApiKey { get; private set; }
        protected static string? ManagerName { get; private set; }
        protected static string? ManagerReport { get; private set; }
        protected static string? DeveloperApiKey { get; private set; }
        protected static string? PersonName { get; private set; }

        /// <summary>
        /// Provides a configured HttpClient for making requests to the API with an API key for a manager.
        /// </summary>
        /// <returns></returns>
        protected static HttpClient GetClientAsManager()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var client = new HttpClient(handler);
            // IMPORTANT: Trailing slash MUST be preserved in BaseAddress to ensure relative paths are appended correctly
            client.BaseAddress = new Uri($"{Setup.BaseUrl}/api/");
            client.DefaultRequestHeaders.Add("x-api-key", ManagerApiKey!);
            return client;
        }

        /// <summary>
        /// Provides a configured HttpClient for making requests to the API with an API key for a developer.
        /// </summary>
        /// <returns></returns>
        protected static HttpClient GetClientAsDeveloper()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var client = new HttpClient(handler);
            client.BaseAddress = new Uri($"{Setup.BaseUrl}/api/");
            client.DefaultRequestHeaders.Add("x-api-key", DeveloperApiKey!);
            return client;
        }

        [OneTimeSetUp]
        protected virtual void OneTimeSetup()
        {
            SetupForAPI();
        }

        /// <summary>
        /// Setup for API tests. Retrieved the API keys for a manager and developer from the database and sets them for use in tests.
        /// </summary>
        /// <exception cref="Exception"></exception>
        protected void SetupForAPI()
        {
            // Build configuration from user secrets and environment variables
            var config = new ConfigurationBuilder()
                .AddUserSecrets<Setup>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Get the connection string and db provider from configuration
            var connectionString = config.GetConnectionString("PPMToolContextConnection");
            var dbProvider = (config.GetValue<string>("DbProvider") ?? "sqlite").ToLower();

            // If no connection string is configured, fall back to local SQLite database
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var dbPath = Path.Combine(AppContext.BaseDirectory, "../../../../PPMTool/PPMTool.db");
                connectionString = $"Data Source={dbPath};Cache=Shared;";
            }

            Debug.WriteLine($"** Using DB Provider: {dbProvider}");
            Debug.WriteLine($"** Using Connection String: {connectionString}");

            var options = new DbContextOptionsBuilder<PPMToolContext>();
            options.AddDbProvider(connectionString, dbProvider);

            using (var context = new PPMToolContext(options.Options))
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
                    .FirstOrDefault(k => k.Owner.RoleType == RoleType.Developer);

                if (developerKey == null)
                {
                    throw new Exception("No valid API keys found for a developer in the database. Please create one for testing.");
                }
                DeveloperApiKey = developerKey.Key;

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

                // Get the name of the first person in the database replacing spaces with underscores so it can be used as a test parameter in API calls
                PersonName = context.People.FirstOrDefault()?.Name?.Replace(" ", "_");

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
