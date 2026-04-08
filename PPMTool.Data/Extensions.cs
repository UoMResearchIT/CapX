using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Data
{
    public static class Extensions
    {
        /// <summary>
        /// Find the sum of a value in a collection, rounded to a specified number of decimal places.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <param name="decimalPlaces"></param>
        /// <returns></returns>
        public static double RoundedSum<TSource>(this IEnumerable<TSource> source,
            Func<TSource, double> selector, int decimalPlaces = 3)
        {
            return Math.Round(source.Sum(selector), decimalPlaces);
        }

        /// <summary>
        /// Method to extract a suitable financial reference from a list of references given a financial year
        /// </summary>
        /// <param name="list"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        /// <exception cref="Exception">If no suitable references can be found</exception>
        public static FinancialReference GetSuitableFinancialReference(this IEnumerable<FinancialReference> list, int year)
        {
            // Try find matching reference
            var match = list.FirstOrDefault(x => x.FinancialYear == year);

            // If not then look for the earliest
            if (match == null)
            {
                match = list.Where(x => x.FinancialYear < year).OrderByDescending(x => x.FinancialYear).FirstOrDefault();
            }

            // If not then use the next latest
            if (match == null)
            {
                match = list.Where(x => x.FinancialYear > year).OrderBy(x => x.FinancialYear).FirstOrDefault();
            }

            // If there are no matches then there are no references so throw exception
            if (match == null)
            {
                throw new Exception("No suitable financial references can be found!");
            }

            return match;
        }

        /// <summary>
        /// Method to extract a suitable financial reference from a list of references given a date
        /// </summary>
        /// <param name="list"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static FinancialReference GetSuitableFinancialReference(this IEnumerable<FinancialReference> list, DateTime date)
        {
            // Get financial year from date
            int year = FinancialReference.GetFinancialYear(date);

            // Call the other method
            return GetSuitableFinancialReference(list, year);
        }

        /// <summary>
        /// Save changes with a retry mechanism for SQLite database when the DB Locked exception is thrown
        /// </summary>
        /// <param name="context"></param>
        /// <param name="maxRetries"></param>
        public static void SaveChangesWithRetry(this PPMToolContext context, int maxRetries = 3)
        {
            int retries = 0;
            while (true)
            {
                try
                {
                    context.SaveChanges();
                    break;
                }
                catch (DbUpdateException ex)
                    when (ex.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 5)
                {
                    if (retries >= maxRetries)
                        throw new Exception("Max retry attempts hit!", ex);
                    retries++;

                    // Wait for 1 second before retrying
                    Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// Extension method to allow merging dictionaries as long as there are no duplicate keys
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <exception cref="InvalidOperationException">If there are duplicate keys</exception>
        public static void AddRange<T, U>(this IDictionary<T, U> target, IDictionary<T, U> source)
        {
            foreach (var kvp in source)
            {
                if (!target.ContainsKey(kvp.Key))
                {
                    target.Add(kvp.Key, kvp.Value);
                }
                else
                {
                    throw new InvalidOperationException($"Key already exists in the dictionary so cannot add {kvp.Key}!");
                }
            }
        }

        /// <summary>
        /// Extension method to use the budget detail to define the amounts and status of a window of time
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="budgetDetail"></param>        
        /// <param name="status"></param>
        /// <param name="amount"></param>
        public static void GetBudgetDetailsForWindow(
            this AssignmentBudgetDetail budgetDetail,
            DateTime start,
            DateTime end,
            out string status,
            out double amount)
        {
            amount = 0d;
            status = BudgetStatus.NotInBudget.GetDescription();

            var durationTask = budgetDetail.Resource.SubTask.DurationDays;
            var lengthOfWindow = end.Subtract(start).TotalDays + 1;
            var daysFunded = budgetDetail.DailyCost == 0 ? 0 : budgetDetail.InBudget / budgetDetail.DailyCost;
            var proportionInBudget = daysFunded == 0 ? 0 : lengthOfWindow / daysFunded;
            if (proportionInBudget > 1)
            {
                proportionInBudget = 1;
            }

            // Only update if the budget line is found and the whole task is
            // not out of budget since all the chunks will be as well
            if (budgetDetail != null && budgetDetail.Status != BudgetStatus.NotInBudget)
            {
                if (budgetDetail.Status == BudgetStatus.FullyInBudget)
                {
                    amount = budgetDetail.InBudget * proportionInBudget;
                    status = budgetDetail.Status.GetDescription();
                }
                else
                {
                    // If chunk includes the funding source expiry date then need to work out proportion
                    if (budgetDetail.Status == BudgetStatus.PartiallyInBudget && budgetDetail.FundingSourceExpired != null)
                    {
                        var expiryDate = budgetDetail.FundingSourceExpired.Value;

                        // If expired before the chunk starts then out of budget
                        if (expiryDate < start)
                        {
                            amount = 0;
                            status = BudgetStatus.NotInBudget.GetDescription();
                        }

                        // If expired after the end date then fully in budget
                        else if (expiryDate > end)
                        {
                            amount = budgetDetail.InBudget * proportionInBudget;
                            status = BudgetStatus.FullyInBudget.GetDescription();
                        }

                        // Expires sometime during the window
                        else
                        {
                            proportionInBudget = daysFunded == 0 ? 0 : (expiryDate.Subtract(start).TotalDays + 1) / daysFunded;
                            if (proportionInBudget > 1)
                            {
                                proportionInBudget = 1;
                                status = BudgetStatus.FullyInBudget.GetDescription();
                            }
                            else
                            {
                                status = BudgetStatus.PartiallyInBudget.GetDescription();
                            }
                            amount = budgetDetail.InBudget * proportionInBudget;

                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Any partially funded resources should have an expiry date!");
                    }
                }
            }
        }

        /// <summary>
        /// Extension method to add the appropriate DB options
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="connectionString"></param>
        /// <param name="dbProvider"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <returns></returns>
        public static DbContextOptionsBuilder AddDbProvider(
            this DbContextOptionsBuilder optionsBuilder,
            string connectionString,
            string dbProvider)
        {
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
                    optionsBuilder.UseNpgsql(connectionString, o => o.MigrationsAssembly("PPMTool.Migrations.PostgreSql"));
                    break;
                default:
                    throw new InvalidOperationException($"DesignTimeDbContextFactory: Unsupported DbProvider '{dbProvider}' specified in environment variable.");
            }
            return optionsBuilder;
        }

        /// <summary>
        /// Cleans a string with some basic formatting, generally
        /// used to compare items to check for duplicates
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string Clean(this string item)
        {
            return item.Trim().ToLower();
        }
    }
}
