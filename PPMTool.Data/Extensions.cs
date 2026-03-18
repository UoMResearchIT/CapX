using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace PPMTool.Data
{
    public static class Extensions
    {
        /// <summary>
        /// Extension method to add the appropriate DB options
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="connectionString"></param>
        /// <param name="configuration"></param>
        /// <param name="dbProviderString"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <returns></returns>
        public static DbContextOptionsBuilder AddDbProvider(
            this DbContextOptionsBuilder optionsBuilder,
            string connectionString,
            IConfiguration? configuration = null,
            string? dbProviderString = null)
        {
            if (configuration is null && string.IsNullOrWhiteSpace(dbProviderString))
            {
                throw new InvalidOperationException("Configuration and DB provider string cannot both be null here!");
            }

            var dbProvider = configuration?.GetValue<string>("DbProvider");
            Console.WriteLine($"** Using DB provider {dbProvider}");
            switch (dbProvider)
            {
                case "sqlite":
                    optionsBuilder.UseSqlite(connectionString, o => o.MigrationsAssembly("PPMTool.Migrations.Sqlite").UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
                    break;
                case "sqlserver":
                    optionsBuilder.UseSqlServer(connectionString, o => o.MigrationsAssembly("PPMTool.Migrations.SqlServer"));
                    break;
                case "postgresql":
                    optionsBuilder.UseNpgsql(connectionString, o => o.MigrationsAssembly("PPMTool.Migrations.PostgresSql"));
                    break;
                default:
                    throw new InvalidOperationException($"DesignTimeDbContextFactory: Unsupported DbProvider '{dbProvider}' specified in environment variable.");
            }
            return optionsBuilder;
        }
    }
}
