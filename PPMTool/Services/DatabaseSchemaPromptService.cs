// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PPMTool.Data.Context;

namespace PPMTool.Services
{
    /// <summary>
    /// A service that generates a prompt describing the database schema for use with AI agents.
    /// </summary>
    public sealed class DatabaseSchemaPromptService
    {
        private readonly IDbContextFactory<PPMToolContext> contextFactory;
        private readonly IConfiguration configuration;

        /// <summary>
        /// Cached prompt string to avoid rebuilding the prompt on every request.
        /// </summary>
        private string cachedPrompt;
        private readonly SemaphoreSlim lockFlag = new(1, 1);

        public DatabaseSchemaPromptService(
            IDbContextFactory<PPMToolContext> contextFactory,
            IConfiguration configuration)
        {
            this.contextFactory = contextFactory;
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets the prompt describing the database schema.
        /// If the prompt has already been generated, it returns the cached version.
        /// Otherwise, it builds the prompt from the EF Core model.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> GetPromptAsync(CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(cachedPrompt))
            {
                return cachedPrompt;
            }

            // Join the queue to build the prompt
            await lockFlag.WaitAsync(cancellationToken);

            try
            {
                // Check again if the prompt has been built while waiting for the lock
                if (!string.IsNullOrWhiteSpace(cachedPrompt))
                {
                    return cachedPrompt;
                }

                using var context = contextFactory.CreateDbContext();

                cachedPrompt = BuildPrompt(context.Model);

                return cachedPrompt;
            }
            finally
            {
                lockFlag.Release();
            }
        }

        /// <summary>
        /// Builds a prompt string that describes the database schema based on the provided EF Core model.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private string BuildPrompt(IModel model)
        {
            var provider =
                configuration
                    .GetValue<string>("DbProvider")
                    ?.Trim()
                    ?? "unknown";

            var sb = new StringBuilder();

            sb.AppendLine("You are the CapX Data Agent.");
            sb.AppendLine();

            sb.AppendLine($"DATABASE PROVIDER: {provider}");
            sb.AppendLine();

            switch (provider.ToLowerInvariant())
            {
                case "sqlite":
                    sb.AppendLine("Generate SQLite-compatible SQL.");
                    break;

                case "sqlserver":
                    sb.AppendLine("Generate SQL Server compatible SQL.");
                    break;

                case "postgresql":
                    sb.AppendLine("Generate PostgreSQL compatible SQL.");
                    break;
            }

            sb.AppendLine();
            sb.AppendLine("""
                PURPOSE
                =======

                You answer questions about data stored within CapX.

                Only use tables, columns, enum values and relationships
                documented in this prompt.

                RULES
                =====

                1. Never invent database values.

                2. Never invent table names, column names,
                   relationships or enum values.

                3. Use only information documented in this prompt.

                4. If information cannot be determined from the
                   documented schema, clearly state this.

                5. Prefer aggregate queries (COUNT, SUM, AVG,
                   MIN, MAX) over returning large raw datasets.

                6. When filtering enum values, use the integer
                   values documented in the ENUMERATIONS section.

                7. Use SQL syntax compatible with the configured
                   database provider.

                8. Consider nullable columns when writing joins
                   and filters.

                9. When answering analytical questions, explain
                   the assumptions used.

                10. Do not assume a relationship exists unless it
                    is explicitly listed in the RELATIONSHIPS
                    section.

                ENTITY NOTES
                ============

                Projects
                ---------
                Represents work requested from the Research
                Software Engineering team.

                SubTasks
                --------
                Represents planned project work packages.

                Resources
                ---------
                Represents allocation of people to SubTasks.

                FundingSources
                --------------
                Represents sources of project funding and budget.

                Payments
                --------
                Represents payments received or allocated to
                projects and funding sources.

                Invoices
                --------
                Represents invoices raised against projects.

                Timesheets
                ----------
                Represents actual effort recorded by staff.

                DATABASE SCHEMA
                ===============
            """);

            // List all tables, columns, and relationships in the database schema
            foreach (var entityType in model.GetEntityTypes()
                         .Where(e => !e.IsOwned())
                         .OrderBy(e => e.GetTableName()))
            {
                var tableName = entityType.GetTableName();

                if (string.IsNullOrWhiteSpace(tableName))
                {
                    continue;
                }

                // Skip the EF Core migrations history table, as it is not relevant to the schema description.
                if (tableName == "__EFMigrationsHistory")
                {
                    continue;
                }

                // Add a blank line before each table for better readability
                sb.AppendLine();

                if (IsPureJoinTable(entityType))
                {
                    var tempFk = entityType.GetForeignKeys().ToList();

                    sb.AppendLine(
                        $"MANY_TO_MANY: " +
                        $"{tempFk[0].PrincipalEntityType.GetTableName()} <-> " +
                        $"{tempFk[1].PrincipalEntityType.GetTableName()}");

                    sb.AppendLine($"JOIN_TABLE: {tableName}");
                }

                // Add the table name to the prompt
                sb.AppendLine($"TABLE: {tableName}");

                // Add the table's schema if it exists
                var primaryKey = entityType.FindPrimaryKey();

                foreach (var property in entityType.GetProperties().OrderBy(p => p.Name))
                {
                    var line = new StringBuilder();

                    line.Append("  ");
                    line.Append(property.Name);
                    line.Append(" : ");
                    line.Append(GetTypeDescription(property));

                    if (primaryKey?.Properties.Contains(property) == true)
                    {
                        line.Append(" [PRIMARY KEY]");
                    }

                    line.Append(
                        property.IsNullable
                        ? " [NULLABLE]"
                        : " [REQUIRED]"
                    );

                    sb.AppendLine(line.ToString());
                }

                var foreignKeys = entityType.GetForeignKeys().ToList();

                if (foreignKeys.Count != 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("  RELATIONSHIPS:");

                    // List all foreign key relationships for the table, showing the columns involved and the referenced table and columns
                    foreach (var fk in foreignKeys)
                    {
                        var principalColumns =
                            fk.PrincipalKey.Properties
                                .Select(p => p.Name);

                        sb.AppendLine(
                            $"    {string.Join(", ", fk.Properties.Select(p => p.Name))}" +
                            $" -> {fk.PrincipalEntityType.GetTableName()}." +
                            $"{string.Join(", ", principalColumns)}");
                    }

                    sb.AppendLine();
                    sb.AppendLine("  CARDINALITY:");

                    // List the cardinality of the relationships for the table, showing the referenced table and the type of relationship (1:N)
                    foreach (var fk in foreignKeys)
                    {
                        sb.AppendLine(
                            $"    {fk.PrincipalEntityType.GetTableName()} 1:N {tableName}");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine();

            // List all enumerations used in the database schema
            sb.AppendLine("ENUMERATIONS");
            sb.AppendLine("============");

            // Get all distinct enum types used in the entity properties
            var enums = model.GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Select(p => Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType)
                .Where(t => t.IsEnum)
                .Distinct()
                .OrderBy(t => t.Name);

            foreach (var enumType in enums)
            {
                sb.AppendLine();
                sb.AppendLine(enumType.Name);

                // List all values of the enum type with their corresponding integer values
                foreach (var value in Enum.GetValues(enumType))
                {
                    sb.AppendLine($"  {(int)value} = {value}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a string description of the property type for use in the prompt.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static string GetTypeDescription(IProperty property)
        {
            var clrType =
                Nullable.GetUnderlyingType(property.ClrType)
                ?? property.ClrType;

            if (clrType.IsEnum)
            {
                var underlyingType = Enum.GetUnderlyingType(clrType);
                return $"ENUM({clrType.Name}) STORED_AS_{underlyingType.Name.ToUpperInvariant()}";
            }

            if (clrType == typeof(string))
            {
                return "STRING";
            }

            if (clrType == typeof(int))
            {
                return "INT";
            }

            if (clrType == typeof(long))
            {
                return "LONG";
            }

            if (clrType == typeof(decimal))
            {
                return "DECIMAL";
            }

            if (clrType == typeof(double))
            {
                return "DOUBLE";
            }

            if (clrType == typeof(float))
            {
                return "FLOAT";
            }

            if (clrType == typeof(bool))
            {
                return "BOOLEAN";
            }

            if (clrType == typeof(DateTime))
            {
                return "DATETIME";
            }

            if (clrType == typeof(Guid))
            {
                return "GUID";
            }

            return clrType.Name.ToUpperInvariant();
        }

        /// <summary>
        /// Determines if the given entity type represents a pure join table.
        /// Characterised by having exactly two foreign keys and at most two properties (the foreign keys themselves).
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        private static bool IsPureJoinTable(IEntityType entityType)
        {
            var foreignKeys = entityType.GetForeignKeys().Count();

            return foreignKeys == 2 &&
                   entityType.GetProperties().Count() <= 2;
        }
    }
}
