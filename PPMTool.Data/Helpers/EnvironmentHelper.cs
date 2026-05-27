// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace PPMTool.Data.Helpers
{
    /// <summary>
    /// Helper class to manage environment variables for configuration overrides.
    /// </summary>
    public static class EnvironmentHelper
    {
        /// <summary>
        /// Read in the environment variables that are used by the design context factory
        /// </summary>
        /// <param name="overridingValues"></param>
        public static void LoadDesignTimeVariables(Dictionary<string, string> overridingValues)
        {
            // Load design time variables into the environment so they can be read by the design time factory
            ReadValue("CONNECTION_STRING", "ConnectionStrings:PPMToolContextConnection", ref overridingValues);
            ReadValue("DB_PROVIDER", "DbProvider", ref overridingValues);
        }

        /// <summary>
        /// Validate
        /// </summary>
        /// <param name="builder"></param>
        public static void ValidateDesignTimeConfiguration(WebApplicationBuilder builder)
        {
            // Used by EF Core tools at design time with migrations, so we need to validate even at design time
            ValidateValue("CONNECTION_STRING", "ConnectionStrings:PPMToolContextConnection", ref builder, true);
            ValidateValue("DB_PROVIDER", "DbProvider", ref builder, true);
        }

        /// <summary>
        /// Takes an existing dictionary and inserts a key-value pair.
        /// Key is the configuration key and the value is the value of the environment variable.
        /// Does nothing if the value is null or whitespace.
        /// </summary>
        /// <param name="envVar"></param>
        /// <param name="configKey"></param>
        /// <param name="overridingValues"></param>
        public static void ReadValue(string envVar, string configKey, ref Dictionary<string, string> overridingValues)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrWhiteSpace(value))
            {
                overridingValues.Add(configKey, value);
            }
        }

        /// <summary>
        /// Validates that a critical configuration value is present and not null or whitespace.
        /// Will not check at design time by default.
        /// Throws an exception if the value is not valid.
        /// </summary>
        /// <param name="envVar"></param>
        /// <param name="configKey"></param>
        /// <param name="builder"></param>
        /// <param name="checkAtDesignTime"></param>
        /// <param name="justLog">Whether failed validation should only write to log rather than throwing an exception</param>
        /// <param name="logger"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void ValidateValue(
            string envVar,
            string configKey,
            ref WebApplicationBuilder builder,
            bool checkAtDesignTime = false,
            bool justLog = false,
            ILogger? logger = null)
        {
            var isDesignTime = AppDomain.CurrentDomain.FriendlyName == "ef";
            var checkShouldRun = !isDesignTime || (isDesignTime && checkAtDesignTime);
            if (checkShouldRun && string.IsNullOrWhiteSpace(builder.Configuration[configKey]))
            {
                var message = $"{envVar} environment variable is not set!";
                if (justLog)
                {
                    logger?.LogError(message);
                }
                else
                {
                    throw new InvalidOperationException(message);
                }
            }
        }
    }
}
