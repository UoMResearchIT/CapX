// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

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

            // Get the API key secret
            ReadValue("API_KEY_SECRET", "Jwt:SecretKey", ref overridingValues);

            // Get Sentry DSN
            ReadValue("SENTRY_DSN", "Sentry:Dsn", ref overridingValues);

            // Get superuser details
            ReadValue("SUPERUSER_NAME", "DeveloperSettings:DefaultSuperUserName", ref overridingValues);
            ReadValue("SUPERUSER_USERNAME", "DeveloperSettings:DefaultSuperUserUserName", ref overridingValues);
            ReadValue("SUPERUSER_EMAIL", "DeveloperSettings:DefaultSuperUserEmail", ref overridingValues);

            // Connection string for EF Core tools at design time and runtime
            ReadValue("CONNECTION_STRING", "ConnectionStrings:PPMToolContextConnection", ref overridingValues);
            ReadValue("LEAVEBOOKINGS_CONNECTION_STRING", "ConnectionStrings:LeaveBookingsDatabase", ref overridingValues);

            // Get email settings
            ReadValue("MAIL_SMTP_SERVER", "Email:SmtpServer", ref overridingValues);
            ReadValue("MAIL_FROM_ADDRESS", "Email:From", ref overridingValues);

            // Get CAS settings
            ReadValue("CAS_PROTOCOL", "Authentication:CAS:ProtocolVersion", ref overridingValues);
            ReadValue("CAS_BASE_URL", "Authentication:CAS:ServerUrlBase", ref overridingValues);

            // Get Azure AD (Entra) setttings
            ReadValue("ENTRA_INSTANCE", "Authentication:AzureAd:Instance", ref overridingValues);
            ReadValue("ENTRA_DOMAIN", "Authentication:AzureAd:Domain", ref overridingValues);
            ReadValue("ENTRA_TENANT_ID", "Authentication:AzureAd:TenantId", ref overridingValues);
            ReadValue("ENTRA_CLIENT_ID", "Authentication:AzureAd:ClientId", ref overridingValues);
            ReadValue("ENTRA_CLIENT_SECRET", "Authentication:AzureAd:ClientSecret", ref overridingValues);
            ReadValue("ENTRA_CALLBACK_PATH", "Authentication:AzureAd:CallbackPath", ref overridingValues);

            // Generic auth settings
            ReadValue("AUTH_TYPE", "Authentication:Type", ref overridingValues);
            ReadValue("AUTH_HOST_URL", "Authentication:HostUrl", ref overridingValues);

            // Set seed dummy data flag if environment variable is set to true (case insensitive)
            var seedDummyData = Environment.GetEnvironmentVariable("SEED_DUMMY_DATA");
            if (seedDummyData?.ToLowerInvariant() == true.ToString().ToLowerInvariant())
            {
                overridingValues.Add("DeveloperSettings:SeedDummyData", true.ToString().ToLowerInvariant());
            }

            // Add the overriding values to the configuration
            builder.Configuration.AddInMemoryCollection(overridingValues);
        }

        /// <summary>
        /// Method to validate critical configuration settings are present.
        /// </summary>
        /// <param name="builder"></param>
        internal static void ValidateConfiguration(ILogger logger, WebApplicationBuilder builder, string authenticationType)
        {
            // Validation of values that are used at runtime only
            ValidateValue("API_KEY_SECRET", "Jwt:SecretKey", ref builder);
            ValidateValue("LEAVEBOOKINGS_CONNECTION_STRING", "ConnectionStrings:LeaveBookingsDatabase", ref builder);

#if RELEASE
            ValidateValue("SENTRY_DSN", "Sentry:Dsn", ref builder, justLog: true, logger: logger);
            ValidateValue("MAIL_SMTP_SERVER", "Email:SmtpServer", ref builder, justLog: true, logger: logger);
            ValidateValue("MAIL_FROM_ADDRESS", "Email:From", ref builder, justLog: true, logger: logger);
            ValidateValue("AUTH_TYPE", "Authentication:Type", ref builder);

            if (authenticationType == "CAS")
            {
                ValidateValue("CAS_PROTOCOL", "Authentication:CAS:ProtocolVersion", ref builder);
                ValidateValue("CAS_BASE_URL", "Authentication:CAS:ServerUrlBase", ref builder);
            }
            else if (authenticationType == "AzureAd")
            {
                ValidateValue("ENTRA_INSTANCE", "Authentication:AzureAd:Instance", ref builder);
                ValidateValue("ENTRA_DOMAIN", "Authentication:AzureAd:Domain", ref builder);
                ValidateValue("ENTRA_TENANT_ID", "Authentication:AzureAd:TenantId", ref builder);
                ValidateValue("ENTRA_CLIENT_ID", "Authentication:AzureAd:ClientId", ref builder);
                ValidateValue("ENTRA_CALLBACK_PATH", "Authentication:AzureAd:CallbackPath", ref builder);
            }
            ValidateValue("AUTH_HOST_URL", "Authentication:HostUrl", ref builder);
#endif

            // Used by EF Core tools at design time with migrations, so we need to validate even at design time
            ValidateValue("CONNECTION_STRING", "ConnectionStrings:PPMToolContextConnection", ref builder, true);
            ValidateValue("SUPERUSER_NAME", "DeveloperSettings:DefaultSuperUserName", ref builder, true);
            ValidateValue("SUPERUSER_USERNAME", "DeveloperSettings:DefaultSuperUserUserName", ref builder, true);
            ValidateValue("SUPERUSER_EMAIL", "DeveloperSettings:DefaultSuperUserEmail", ref builder, true);
        }

        /// <summary>
        /// Takes an existing dictionary and inserts a key-value pair.
        /// Key is the configuration key and the value is the value of the environment variable.
        /// Does nothing if the value is null or whitespace.
        /// </summary>
        /// <param name="envVar"></param>
        /// <param name="configKey"></param>
        /// <param name="overridingValues"></param>
        private static void ReadValue(string envVar, string configKey, ref Dictionary<string, string> overridingValues)
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
        /// <exception cref="InvalidOperationException"></exception>
        private static void ValidateValue(string envVar, string configKey, ref WebApplicationBuilder builder, bool checkAtDesignTime = false, bool justLog = false, ILogger logger = null)
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
