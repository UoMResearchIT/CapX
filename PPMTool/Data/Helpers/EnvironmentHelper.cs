// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

namespace PPMTool.Data.Helpers
{
    /// <summary>
    /// Helper class to manage environment variables for configuration overrides.
    /// </summary>
    internal static class EnvironmentHelper
    {
        /// <summary>
        /// Method to load environment variables and override configuration settings.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        internal static void LoadEnvironmentVariables(WebApplicationBuilder builder)
        {
            // Add environment variables to the configuration
            builder.Configuration.AddEnvironmentVariables();
            var overridingValues = new Dictionary<string, string>();

            // Get the API key from the environment
            var apiKeySecret = Environment.GetEnvironmentVariable("API_KEY_SECRET");
            if (!string.IsNullOrEmpty(apiKeySecret))
            {
                overridingValues.Add("Jwt:SecretKey", apiKeySecret);
            }

            // Get Sentry DSN from the environment
            var sentryDsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
            if (!string.IsNullOrEmpty(sentryDsn))
            {
                overridingValues.Add("Sentry:Dsn", sentryDsn);
            }

            // Seed dummy data if environment variable is set to true (case insensitive)
            var seedDummyData = Environment.GetEnvironmentVariable("SEED_DUMMY_DATA");
            if (seedDummyData?.ToLowerInvariant() == true.ToString().ToLowerInvariant())
            {
                overridingValues.Add("DeveloperSettings:SeedDummyData", true.ToString().ToLowerInvariant());
            }

            // Get superuser name from the environment
            var suName = Environment.GetEnvironmentVariable("SUPERUSER_NAME");
            if (!string.IsNullOrWhiteSpace(suName))
            {
                overridingValues.Add("DeveloperSettings:DefaultSuperUserName", suName);
            }

            // Get superuser username from the environment
            var suUserName = Environment.GetEnvironmentVariable("SUPERUSER_USERNAME");
            if (!string.IsNullOrWhiteSpace(suUserName))
            {
                overridingValues.Add("DeveloperSettings:DefaultSuperUserUserName", suUserName);
            }

            // Get superuser email from the environment
            var suEmail = Environment.GetEnvironmentVariable("SUPERUSER_EMAIL");
            if (!string.IsNullOrWhiteSpace(suEmail))
            {
                overridingValues.Add("DeveloperSettings:DefaultSuperUserEmail", suEmail);
            }

            // Get connection string from the environment
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                overridingValues.Add("ConnectionStrings:PPMToolContextConnection", connectionString);
            }

            // Add the overriding values to the configuration
            builder.Configuration.AddInMemoryCollection(overridingValues);
        }

        /// <summary>
        /// Method to validate critical configuration settings are present.
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="InvalidOperationException"></exception>
        internal static void ValidateConfiguration(WebApplicationBuilder builder)
        {
            var isDesignTime = AppDomain.CurrentDomain.FriendlyName == "ef";
            if (!isDesignTime && string.IsNullOrWhiteSpace(builder.Configuration["Jwt:SecretKey"]))
            {
                throw new InvalidOperationException("API_KEY_SECRET environment variable is not set!");
            }
            if (!isDesignTime && builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(builder.Configuration["Sentry:Dsn"]))
            {
                throw new InvalidOperationException("SENTRY_DSN environment variable is not set!");
            }
            if (string.IsNullOrWhiteSpace(builder.Configuration["ConnectionStrings:PPMToolContextConnection"]))
            {
                throw new InvalidOperationException("CONNECTION_STRING environment variable is not set!");
            }
        }
    }
}
