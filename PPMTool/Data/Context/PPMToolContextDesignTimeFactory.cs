using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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
            Console.WriteLine("** Building design time context...");
            var configuration = LoadEnvironmentConfiguration();
            var connectionString = configuration.GetConnectionString("PPMToolContextConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DesignTimeDbContextFactory: CONNECTION_STRING environment variable is not set!");
            }

            var optionsBuilder = new DbContextOptionsBuilder<PPMToolContext>();
            optionsBuilder.UseSqlite(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

            return new PPMToolContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Method to build a configuration object injecting the connection string from the environment
        /// </summary>
        /// <returns></returns>
        private IConfiguration LoadEnvironmentConfiguration()
        {
            // Create a new config builder
            var builder = new ConfigurationBuilder()
                        .AddEnvironmentVariables();

            // Configuration overrides from the environment variables
            var overridingValues = new Dictionary<string, string>();
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                overridingValues.Add("ConnectionStrings:PPMToolContextConnection", connectionString);
            }
            builder.AddInMemoryCollection(overridingValues);
            return builder.Build();
        }
    }
}
