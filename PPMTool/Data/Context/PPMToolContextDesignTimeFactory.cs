using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PPMTool.Data.Helpers;

namespace PPMTool.Data.Context
{
    /// <summary>
    /// EF Core will discover this class at design time and use it when running migrations etc.
    /// This is needed since the connection strings are not in appsettings anymore and need to be read in from an environment variable.
    /// Although this happens via Program.cs when running the web app, EF Core does not run these files so we need to
    /// inject the environment variables via a custom factory for the design time context.
    /// </summary>
    public class PPMToolContextDesignTimeFactory : IDesignTimeDbContextFactory<PPMToolContext>
    {
        /// <summary>
        /// Implementation of the interface
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If env variable not set</exception>
        public PPMToolContext CreateDbContext(string[] args)
        {
            var configuration = BuildConfiguration(args);
            var connectionString = configuration.GetConnectionString("PPMToolContextConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DesignTimeDbContextFactory: CONNECTION_STRING environment variable is not set!");
            }
            else
            {
                Console.WriteLine($"** Using design-time connection string {connectionString}");
            }

            var optionsBuilder = new DbContextOptionsBuilder<PPMToolContext>();
            var dbProvider = configuration.GetValue<string>("DbProvider");
            Console.WriteLine($"** Using design-time DB provider {dbProvider}");
            switch (dbProvider)
            {
                case "sqlite":
                    optionsBuilder.UseSqlite(connectionString);
                    break;
                case "sqlserver":
                    optionsBuilder.UseSqlServer(connectionString);
                    break;
                case "postgresql":
                    optionsBuilder.UseNpgsql(connectionString);
                    break;
                default:
                    throw new InvalidOperationException($"DesignTimeDbContextFactory: Unsupported DbProvider '{dbProvider}' specified in environment variable.");
            }

            return new PPMToolContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Method to build a configuration object injecting the connection string from the environment
        /// </summary>
        /// <returns></returns>
        private IConfiguration BuildConfiguration(string[] args)
        {
            // Create a new config builder
            var builder = new ConfigurationBuilder();

            // Add environment variables and user secrets to the configuration
            builder.AddEnvironmentVariables();
            builder.AddUserSecrets<PPMToolContextDesignTimeFactory>();

            // Load in the variables
            var overridingValues = new Dictionary<string, string>();
            EnvironmentHelper.LoadDesignTimeVariables(overridingValues);
            builder.AddInMemoryCollection(overridingValues);

            return builder.Build();
        }
    }
}
