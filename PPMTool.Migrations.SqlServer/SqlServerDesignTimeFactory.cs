using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Helpers;

namespace PPMTool.Migrations.SqlServer
{
    /// <summary>
    /// Design time factory for the context for the SQLServer provider
    /// </summary>
    public sealed class SqlServerDesignTimeFactory : IDesignTimeDbContextFactory<PPMToolContext>
    {
        public PPMToolContext CreateDbContext(string[] args)
        {
            var configuration = DesignTimeHelper.BuildConfiguration(args);
            var connectionString = configuration.GetConnectionString("PPMToolContextConnection");

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DesignTimeDbContextFactory: CONNECTION_STRING is not set!");

            Console.WriteLine($"** SQLite: Using design-time connection string {connectionString}");

            var optionsBuilder = new DbContextOptionsBuilder<PPMToolContext>();
            optionsBuilder.AddDbProvider(connectionString, dbProviderString: "sqlserver");

            return new PPMToolContext(optionsBuilder.Options);
        }
    }
}
