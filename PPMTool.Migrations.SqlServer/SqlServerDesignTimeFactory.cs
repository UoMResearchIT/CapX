using PPMTool.Data.Context;

namespace PPMTool.Migrations.SqlServer
{
    /// <summary>
    /// Design time factory for the context for the SQLServer provider
    /// </summary>
    public sealed class SqlServerDesignTimeFactory : PPMToolContextDesignTimeFactory
    {
        public override PPMToolContext CreateDbContext(string[] args)
        {
            var optionsBuilder = GetOptionsBuilder("sqlserver");
            return new PPMToolContext(optionsBuilder.Options);
        }
    }
}
