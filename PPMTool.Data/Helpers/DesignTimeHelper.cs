// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.Extensions.Configuration;
using PPMTool.Data.Context;

namespace PPMTool.Data.Helpers
{
    public static class DesignTimeHelper
    {
        /// <summary>
        /// Method to build a configuration object injecting the connection string from the environment
        /// </summary>
        /// <returns></returns>
        public static IConfiguration BuildConfiguration()
        {
            // Create a new config builder
            var builder = new ConfigurationBuilder();

            // Add environment variables and user secrets to the configuration
            builder.AddEnvironmentVariables();
            builder.AddUserSecrets<PPMToolContext>();

            // Load in the variables
            var overridingValues = new Dictionary<string, string>();
            EnvironmentHelper.LoadDesignTimeVariables(overridingValues);
            builder.AddInMemoryCollection(overridingValues!);

            return builder.Build();
        }
    }
}
