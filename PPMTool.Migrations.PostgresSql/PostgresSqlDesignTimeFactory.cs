using PPMTool.Data.Context;

namespace PPMTool.Migrations.PostgresSql
{
    /// <summary>
    /// Design time factory for the context for the PGSQL provider
    /// </summary>
    public sealed class PostgresSqlDesignTimeFactory : PPMToolContextDesignTimeFactory
    {
        public override PPMToolContext CreateDbContext(string[] args)
        {
            var optionsBuilder = GetOptionsBuilder("postgresql");
            return new PPMToolContext(optionsBuilder.Options);
        }
    }
}
