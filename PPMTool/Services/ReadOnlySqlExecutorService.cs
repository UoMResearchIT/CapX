// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Text.Json;
using Microsoft.Data.Sqlite;
using Npgsql;
using PPMTool.Data;

namespace PPMTool.Services
{
    /// <summary>
    /// A service that executes read-only SQL queries.
    /// </summary>
    public sealed class ReadOnlySqlExecutorService
    {
        private readonly string connectionString;

        /// <summary>
        /// Initialises a new instance of the <see cref="ReadOnlySqlExecutorService"/> class.
        /// Constructs a read-only SQL connection string.
        /// </summary>
        /// <param name="configuration"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public ReadOnlySqlExecutorService(IConfiguration configuration)
        {
            // Get existing DB connection strings and modify them to be read-only if possible.
            connectionString = configuration.GetConnectionString("PPMToolContextConnection");
            var dbProvider = configuration.GetValue<string>("DbProvider").Clean();
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("PPMToolContextConnection is not configured.");
            }

            // Build appropriate read-only connection string based on the database provider.
            switch (dbProvider)
            {
                case "sqlite":
                    connectionString = new SqliteConnectionStringBuilder(connectionString)
                    {
                        Mode = SqliteOpenMode.ReadOnly
                    }.ToString();
                    break;
                case "sqlserver":
                    // TODO: Implement read-only mode for SQL Server.
                    // SQL Server does not have a direct equivalent of SQLite's read-only mode.
                    // You can enforce read-only behavior through user permissions or by using a read-only database user.
                    break;
                case "postgresql":
                    // TODO: Not really sure if this works.
                    var builder = new NpgsqlConnectionStringBuilder(connectionString)
                    {
                        TargetSessionAttributes = TargetSessionAttributes.ReadOnly.ToString()
                    };
                    connectionString = builder.ConnectionString;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported database provider: {dbProvider}");
            }
        }

        /// <summary>
        /// Executes a read-only SQL query and returns the results as a JSON string.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> ExecuteAsync(
            string sql,
            CancellationToken cancellationToken = default)
        {
            // Validate the SQL query to ensure it is read-only and does not contain prohibited keywords.
            ValidateSql(sql);

            // Construct and open a connection to the DB
            await using var connection =
                new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Create a command to execute the SQL query
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 15;

            // Execute the command and read the results
            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            // Read the results into a list of dictionaries, where each dictionary represents a row
            var rows = new List<Dictionary<string, object>>();
            const int maximumRows = 1000;

            while (await reader.ReadAsync(cancellationToken))
            {
                // If the number of rows exceeds the maximum allowed, stop reading further
                if (rows.Count >= maximumRows)
                {
                    break;
                }

                var row = new Dictionary<string, object>(
                    StringComparer.OrdinalIgnoreCase);

                // Populate the dictionary with column names and their corresponding values
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    // Use GetValue to retrieve the value of the column, handling DBNull values appropriately
                    row[reader.GetName(index)] =
                        await reader.IsDBNullAsync(index, cancellationToken)
                            ? null
                            : reader.GetValue(index);
                }

                rows.Add(row);
            }

            // Return the results as a JSON string, including metadata about the number of rows returned and whether the results were truncated
            return JsonSerializer.Serialize(new
            {
                returnedRowCount = rows.Count,
                truncated = rows.Count >= maximumRows,
                rows
            });
        }

        /// <summary>
        /// Validates the provided SQL query to ensure it is read-only and does not contain prohibited keywords or multiple statements.
        /// </summary>
        /// <param name="sql"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private static void ValidateSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new InvalidOperationException("SQL cannot be empty.");
            }

            var trimmed = sql.Trim();

            // Ensure the SQL query starts with either SELECT or WITH, indicating a read-only operation.
            if (!trimmed.StartsWith(
                    "SELECT",
                    StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith(
                    "WITH",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only SELECT and WITH queries are permitted.");
            }

            // Check for prohibited SQL keywords that could modify the database or its structure.
            var prohibitedTerms = new[]
            {
            "INSERT",
            "UPDATE",
            "DELETE",
            "REPLACE",
            "DROP",
            "ALTER",
            "CREATE",
            "ATTACH",
            "DETACH",
            "VACUUM",
            "PRAGMA",
            "REINDEX",
            "ANALYZE"
        };

            foreach (var term in prohibitedTerms)
            {
                if (ContainsSqlKeyword(trimmed, term))
                {
                    throw new InvalidOperationException(
                        $"SQL keyword '{term}' is not permitted.");
                }
            }

            // Ensure that the SQL query does not contain multiple statements by checking for additional semicolons after trimming any trailing semicolon.
            var statementWithoutTrailingSemicolon =
                trimmed.TrimEnd().TrimEnd(';');

            if (statementWithoutTrailingSemicolon.Contains(';'))
            {
                throw new InvalidOperationException(
                    "Only one SQL statement is permitted.");
            }
        }

        /// <summary>
        /// Checks if the provided SQL query contains a specific keyword, ensuring that the keyword is treated as a whole word and not part of another word.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        private static bool ContainsSqlKeyword(string sql, string keyword)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(
                sql,
                $@"\b{System.Text.RegularExpressions.Regex.Escape(keyword)}\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        }
    }
}
