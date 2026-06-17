// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Helpers
{
    /// <summary>
    /// Helper class to manage environment variables for configuration overrides.
    /// </summary>
    internal static class EnvironmentHelper
    {
        /// <summary>
        /// Reads a value from the specified environment variable or configuration key and updates the overriding values
        /// dictionary accordingly.
        /// </summary>
        /// <param name="envVariableName">The name of the environment variable to read. If the environment variable is set, its value will be used to
        /// override the configuration value.</param>
        /// <param name="configKey">The configuration key to use if the environment variable is not set. Used as a fallback to retrieve the
        /// value.</param>
        /// <param name="overridingValues">A reference to the dictionary containing overriding values. This dictionary will be updated with the value
        /// read from the environment variable or configuration key.</param>
        internal static void ReadValue(string envVariableName, string configKey, ref Dictionary<string, string> overridingValues)
            => Data.Helpers.EnvironmentHelper.ReadValue(envVariableName, configKey, ref overridingValues);

        /// <summary>
        /// Validates the value of a specified environment variable and configuration key within the context of a web
        /// application builder.
        /// </summary>
        /// <param name="envVariableName">The name of the environment variable to validate. Cannot be null or empty.</param>
        /// <param name="configKey">The configuration key associated with the environment variable. Cannot be null or empty.</param>
        /// <param name="builder">A reference to the WebApplicationBuilder instance whose configuration will be validated and potentially updated.</param>
        /// <param name="checkAtDesignTime">true to perform validation at design time; otherwise, false. Defaults to false.</param>
        /// <param name="justLog">true to log validation results without throwing exceptions; otherwise, false. Defaults to false.</param>
        /// <param name="logger">An optional logger used to record validation messages. Can be null if logging is not required.</param>
        internal static void ValidateValue(string envVariableName, string configKey, ref WebApplicationBuilder builder, bool checkAtDesignTime = false, bool justLog = false, ILogger logger = null)
            => Data.Helpers.EnvironmentHelper.ValidateValue(envVariableName, configKey, ref builder, checkAtDesignTime, justLog, logger);

        /// <summary>
        /// Loads design-time environment variables, applying any specified overriding values.
        /// </summary>
        /// <remarks>This method is intended for use during design-time scenarios, such as within
        /// development tools or build processes, to ensure environment variables are set appropriately. Existing
        /// environment variables may be overwritten by the provided values.</remarks>
        /// <param name="overridingValues">A dictionary containing environment variable names and their corresponding values to override the defaults.
        /// Keys represent variable names; values represent the values to set.</param>
        internal static void LoadDesignTimeVariables(Dictionary<string, string> overridingValues)
            => Data.Helpers.EnvironmentHelper.LoadDesignTimeVariables(overridingValues);

        /// <summary>
        /// Validates the design-time configuration for the specified web application builder.
        /// </summary>
        /// <remarks>This method is intended for use during design-time scenarios to ensure that the
        /// application's configuration is valid before runtime. It may throw exceptions if the configuration is
        /// invalid.</remarks>
        /// <param name="builder">The web application builder whose design-time configuration is to be validated. Cannot be null.</param>
        internal static void ValidateDesignTimeConfiguration(WebApplicationBuilder builder)
            => Data.Helpers.EnvironmentHelper.ValidateDesignTimeConfiguration(builder);

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

            // Data protection
            ReadValue("DP_KEY_PATH", "DataProtection:KeyPath", ref overridingValues);

            // Set seed dummy data flag if environment variable is set to true (case insensitive)
            var seedDummyData = Environment.GetEnvironmentVariable("SEED_DUMMY_DATA");
            if (seedDummyData?.ToLowerInvariant() == true.ToString().ToLowerInvariant())
            {
                overridingValues.Add("DeveloperSettings:SeedDummyData", true.ToString().ToLowerInvariant());
            }

            // Read the design time variables
            LoadDesignTimeVariables(overridingValues);

            // Add the overriding values to the configuration
            builder.Configuration.AddInMemoryCollection(overridingValues);
        }

        /// <summary>
        /// Method to validate critical configuration settings are present.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="builder"></param>
        /// <param name="authenticationType"></param>
        internal static void ValidateConfiguration(ILogger logger, WebApplicationBuilder builder, string authenticationType)
        {
            // Validation of values that are used at runtime only
            ValidateValue("API_KEY_SECRET", "Jwt:SecretKey", ref builder);
            ValidateValue("LEAVEBOOKINGS_CONNECTION_STRING", "ConnectionStrings:LeaveBookingsDatabase", ref builder);
            ValidateValue("SUPERUSER_NAME", "DeveloperSettings:DefaultSuperUserName", ref builder);
            ValidateValue("SUPERUSER_USERNAME", "DeveloperSettings:DefaultSuperUserUserName", ref builder);
            ValidateValue("SUPERUSER_EMAIL", "DeveloperSettings:DefaultSuperUserEmail", ref builder);

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
            // Validate the desgin time values only
            ValidateDesignTimeConfiguration(builder);
        }
    }
}
