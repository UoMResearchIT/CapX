using PPMTool.Data.Context;

namespace PPMTool.Migrations.Sqlite
{
    /// <summary>
    /// Design time factory for the context for the SQLite provider
    /// </summary>
    public sealed class SqliteDesignTimeFactory : PPMToolContextDesignTimeFactory
    {
        public override PPMToolContext CreateDbContext(string[] args)
        {
            var optionsBuilder = GetOptionsBuilder("sqlite");
            return new PPMToolContext(optionsBuilder.Options);
        }
    }
}
