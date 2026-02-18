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
            ReadValue("CONNECTION_STRING", "ConnectionStrings:PPMToolContextConnection", ref overridingValues);

            // Get email settings
            ReadValue("MAIL_SMTP_SERVER", "Email:SmtpServer", ref overridingValues);
            ReadValue("MAIL_FROM_ADDRESS", "Email:From", ref overridingValues);

            // Get CAS settings
            ReadValue("CAS_PROTOCOL", "Authentication:CAS:ProtocolVersion", ref overridingValues);
            ReadValue("CAS_BASE_URL", "Authentication:CAS:ServerUrlBase", ref overridingValues);

            // Generic auth settings
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
