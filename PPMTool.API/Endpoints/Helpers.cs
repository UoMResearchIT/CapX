using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Helpers;
using PPMTool.Enums;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Provides general helper methods for common repeatedable actions in minimal API endpoints.
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Gets the authenticated user from the HttpContext. Should always be present if the authentication middleware is correctly set up.
    /// </summary>
    /// <param name="context">The current HttpContext.</param>
    /// <returns>User entity if there is one in the context</returns>
    internal static User GetCurrentUser(HttpContext context)
    {
        context.Items.TryGetValue("User", out var user);
        var castUser = user as User;
        if (castUser == null)
        {
            throw new InvalidOperationException("User not found in the context!");
        }
        return castUser;
    }

    /// <summary>
    /// Checks if a given user has the Superuser role.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <returns>True if user is a superuser</returns>
    internal static bool IsSuperUser(User? user)
    {
        if (user == null) return false;
        return user.RoleType == RoleType.Superuser;
    }

    /// <summary>
    /// Get a matching person if they exist by name, case insensitive, underscores treated as spaces.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="name">The name of the person to find.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the found Person object, or null if not found.</returns>
    internal static async Task<Person?> FindPersonWithLineManagerByNameAsync(PPMToolContext context, string name)
    {
        return await context.People
                .Include(x => x.LineManager)
                .Include(x => x.WorkloadModelChanges)
                .FirstOrDefaultAsync(x => x.Name.ToLower() == name.Trim().ToLower().Replace("_", " "));
    }

    /// <summary>
    /// Determines if the caller is a superuser, the line manager of the specified person, or the person themselves.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="http">The current HttpContext, used to identify the caller.</param>
    /// <param name="person">The person to check authorisation against.</param>
    /// <returns>True if the caller is a superuser, the person's line manager, or the person themselves; otherwise, false.</returns>
    internal static bool IsSuperUserOrLineManagerOrSelf(PPMToolContext context, HttpContext http, Person person)
    {
        var caller = GetCurrentUser(http);
        var callerPersonId = caller.Person?.PersonId ?? 0;

        // Self?
        var isSelf = callerPersonId != 0 && callerPersonId == person.PersonId;

        // LM?
        var isLineManager = person.LineManager?.PersonId == callerPersonId;

        // SU?
        var isSuper = IsSuperUser(caller);

        return isSelf || isLineManager || isSuper;
    }

    /// <summary>
    /// Try parse a date from a string to a DateTime object.
    /// </summary>
    /// <param name="dateAsString">The string to parse, expected format "yyyy-MM-dd".</param>
    /// <param name="dateAsDateTime">When this method returns, contains the DateTime value equivalent to the date contained in dateAsString, if the conversion succeeded, or the default value of DateTime if the conversion failed.</param>
    /// <returns>True if the string was converted successfully; otherwise, false.</returns>
    internal static bool ParseDateTime(string dateAsString, out DateTime dateAsDateTime)
    {
        if (!DateTime.TryParseExact(dateAsString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateAsDateTime))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Formats a single object value for inclusion in a CSV field.
    /// It handles nulls and wraps strings containing commas in double quotes.
    /// </summary>
    private static string FormatCsvField(object? field)
    {
        if (field == null)
        {
            return "";
        }

        // Adding DateTime converter.
        if (field is DateTime dt)
        {
            return dt.ToString("o");
        }

        // Make sure value is null guarded.
        var value = field.ToString();
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        if (value.Contains(","))
        {
            // Wrap fields with commas in double quotes.
            return $"\"{value}\"";
        }
        return value;
    }

    /// <summary>
    /// Generates a CSV file as a byte array from a list of objects.
    /// </summary>
    /// <typeparam name="T">The type of the objects in the list.</typeparam>
    /// <param name="items">The collection of items to include in the CSV.</param>
    /// <returns>A byte array representing the UTF-8 encoded CSV file.</returns>
    internal static byte[] GenerateCsv<T>(IEnumerable<T> items)
    {
        var csvBuilder = new StringBuilder();
        var properties = typeof(T).GetProperties();

        // Add Header Row.
        csvBuilder.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        // Add Data Rows.
        foreach (var item in items)
        {
            var line = string.Join(",", properties.Select(p => FormatCsvField(p.GetValue(item))));
            csvBuilder.AppendLine(line);
        }

        return Encoding.UTF8.GetBytes(csvBuilder.ToString());
    }

    /// <summary>
    /// Generates WLM analysis data for a collection of people over a specified date range.
    /// This is a helper method specific to the WorkLoadModel API.
    /// </summary>
    /// <param name="context">The database context for data retrieval.</param>
    /// <param name="people">The collection of people for whom to generate the analysis.</param>
    /// <param name="startDate">The start date of the analysis period.</param>
    /// <param name="endDate">The end date of the analysis period.</param>
    /// <param name="compareToWLM">A flag to determine if the data should be the difference against the WLM.</param>
    /// <param name="normalisedByTotalHours">A flag to determine if the data should be normalised as a fraction of total hours.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of DTOs with the structured WLM analysis data.</returns>
    internal static async Task<List<WLMAnalysisPersonDataDTO>> GenerateWlmAnalysisDataAsync(
        PPMToolContext context,
        IEnumerable<Person> people,
        DateTime startDate,
        DateTime endDate,
        bool compareToWLM,
        bool normalisedByTotalHours)
    {
        var results = new List<WLMAnalysisPersonDataDTO>();

        string units = WorkloadModelChartHelper.GetChartYAxisTitle(compareToWLM, normalisedByTotalHours);

        foreach (var person in people)
        {
            var personWeeklyData = new List<WLMWeeklyAnalysisDTO>();

            var allTimesheets = await context.Timesheets
                .Include(t => t.TimesheetEntries).ThenInclude(e => e.InnateCodeTask).ThenInclude(tk => tk.InnateCode)
                .Where(t => t.Owner.PersonId == person.PersonId && t.StartDate >= startDate && t.StartDate <= endDate)
                .ToListAsync();

            var weekStart = startDate;
            while (weekStart <= endDate)
            {
                var wlmDataItem = WorkloadModelChartHelper.GetWorkloadModelChartData(person, weekStart, allTimesheets);

                if (normalisedByTotalHours)
                {
                    wlmDataItem.SwitchNormalisation(true);
                }
                wlmDataItem.UpdateWLMNetValues(normalisedByTotalHours);

                var sourceData = compareToWLM ? wlmDataItem.WLMNetByDuty : wlmDataItem.WeeklyValuesByDuty;
                var dutiesDict = sourceData.ToDictionary(kvp => kvp.Key.GetDescription(), kvp => kvp.Value);

                personWeeklyData.Add(new WLMWeeklyAnalysisDTO(weekStart, units, dutiesDict));
                weekStart = weekStart.AddDays(7);
            }
            results.Add(new WLMAnalysisPersonDataDTO(person.Name, personWeeklyData));
        }
        return results;
    }
}