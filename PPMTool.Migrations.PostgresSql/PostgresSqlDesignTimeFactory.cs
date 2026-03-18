using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Helpers;

namespace PPMTool.Migrations.PostgresSql
{
    /// <summary>
    /// Design time factory for the context for the PGSQL provider
    /// </summary>
    public sealed class PostgresSqlDesignTimeFactory : IDesignTimeDbContextFactory<PPMToolContext>
    {
        public PPMToolContext CreateDbContext(string[] args)
        {
            var configuration = DesignTimeHelper.BuildConfiguration(args);
            var connectionString = configuration.GetConnectionString("PPMToolContextConnection");

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DesignTimeDbContextFactory: CONNECTION_STRING is not set!");

            Console.WriteLine($"** PostgresSQL: Using design-time connection string {connectionString}");

            var optionsBuilder = new DbContextOptionsBuilder<PPMToolContext>();
            optionsBuilder.AddDbProvider(connectionString, dbProviderString: "postgres");

            return new PPMToolContext(optionsBuilder.Options);
        }
    }
}
