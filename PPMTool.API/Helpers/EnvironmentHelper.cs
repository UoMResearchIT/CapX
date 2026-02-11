// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

namespace PPMTool.API.Helpers
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

            // Get connection string from the environment
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                overridingValues.Add("ConnectionStrings:PPMToolContextConnection", connectionString);
            }

            // Leave bookings connection string override
            var leaveBookingsConnectionString = Environment.GetEnvironmentVariable("LEAVEBOOKINGS_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(leaveBookingsConnectionString))
            {
                overridingValues.Add("ConnectionStrings:LeaveBookingsDatabase", leaveBookingsConnectionString);
            }

            // Add the overriding values to the configuration
            builder.Configuration.AddInMemoryCollection(overridingValues!);
        }

        /// <summary>
        /// Method to validate critical configuration settings are present.
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="InvalidOperationException"></exception>
        internal static void ValidateConfiguration(WebApplicationBuilder builder)
        {
            var isDesignTime = AppDomain.CurrentDomain.FriendlyName == "ef";
            if (string.IsNullOrWhiteSpace(builder.Configuration["ConnectionStrings:PPMToolContextConnection"]))
            {
                throw new InvalidOperationException("CONNECTION_STRING environment variable is not set!");
            }

            if (string.IsNullOrWhiteSpace(builder.Configuration["ConnectionStrings:LeaveBookingsDatabase"]))
            {
                throw new InvalidOperationException("LEAVEBOOKINGS_CONNECTION_STRING environment variable is not set!");
            }
        }
    }
}
