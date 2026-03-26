using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PPMTool.Data.Helpers;

namespace PPMTool.Data.Context
{
    public abstract class PPMToolContextDesignTimeFactory : IDesignTimeDbContextFactory<PPMToolContext>
    {
        /// <inheritdoc />
        public abstract PPMToolContext CreateDbContext(string[] args);

        /// <summary>
        /// Creates and configures a new DbContextOptionsBuilder for the PPMToolContext using the specified database
        /// provider.
        /// </summary>
        /// <remarks>This method is intended for use during design-time operations, such as migrations,
        /// where a properly configured options builder is required. The connection string is retrieved from the
        /// application's configuration using the key "PPMToolContextConnection".</remarks>
        /// <param name="dbProvider">The name or invariant identifier of the database provider to use for configuring the options builder.</param>
        /// <returns>A DbContextOptionsBuilder instance configured for the PPMToolContext with the specified database provider
        /// and connection string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the required connection string for PPMToolContext is not set in the configuration.</exception>
        protected DbContextOptionsBuilder<PPMToolContext> GetOptionsBuilder(string dbProvider)
        {
            var configuration = DesignTimeHelper.BuildConfiguration();
            var connectionString = configuration.GetConnectionString("PPMToolContextConnection");

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DesignTimeDbContextFactory: CONNECTION_STRING is not set!");

            Console.WriteLine($"** Using design-time connection string {connectionString}");

            var optionsBuilder = new DbContextOptionsBuilder<PPMToolContext>();
            optionsBuilder.AddDbProvider(connectionString, dbProviderString: dbProvider);

            return optionsBuilder;
        }
    }
}
