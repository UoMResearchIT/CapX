// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.API.Helpers;

/// <summary>
/// Provides general helper methods for common repeatedable actions in minimal API endpoints.
/// </summary>
public static class GeneralHelpers
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
    internal static bool IsSuperUser(User user)
    {
        if (user == null) return false;
        return user.RoleType == RoleType.Superuser;
    }

    /// <summary>
    /// Checks to see if the user is a superuser or manager.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    internal static bool IsSuperUserOrManager(User user)
    {
        if (user == null) return false;
        return user.RoleType == RoleType.Superuser || user.RoleType == RoleType.Manager;
    }

    /// <summary>
    /// Get a matching person if they exist by name, case insensitive, underscores treated as spaces.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="name">The name of the person to find.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the found Person object, or null if not found.</returns>
    internal static async Task<Person> FindPersonWithLineManagerByNameAsync(PPMToolContext context, string name)
    {
        return await context.People
                .Include(x => x.LineManager)
                .Include(x => x.WorkloadModelChanges)
                .FirstOrDefaultAsync(x => x.Name.Trim().ToLower() == name.Trim().ToLower().Replace("_", " "));
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
    /// Whether the caller is a manager or super-user or matches the person ID given.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="http"></param>
    /// <param name="personId"></param>
    /// <returns></returns>
    internal static bool IsSuperUserOrManagerOrSelf(PPMToolContext context, HttpContext http, int personId)
    {
        var caller = GetCurrentUser(http);
        var callerPersonId = caller.Person?.PersonId ?? 0;

        // Self?
        var isSelf = callerPersonId != 0 && callerPersonId == personId;

        // Manager or SU?
        var isSUorMan = IsSuperUserOrManager(caller);

        return isSelf || isSUorMan;
    }

    /// <summary>
    /// Determines if a caller can view sensitive timesheet data for a specific person.
    /// Sensitive data can only be viewed by superusers, the person themselves, or their direct line manager.
    /// </summary>
    /// <param name="caller">The user making the request.</param>
    /// <param name="timesheetOwner">The person who owns the timesheet being checked.</param>
    /// <returns>True if the caller is authorized to view the sensitive data; otherwise, false.</returns>
    internal static bool CanViewSensitiveData(User caller, Person timesheetOwner)
    {
        if (caller == null) return false;

        var callerPersonId = caller!.Person?.PersonId ?? 0;

        // Superusers can see everything
        if (IsSuperUser(caller)) return true;

        // Can see own data
        if (callerPersonId != 0 && callerPersonId == timesheetOwner.PersonId) return true;

        // Direct line manager can see direct report's data
        if (timesheetOwner.LineManager?.PersonId == callerPersonId) return true;

        return false;
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
    /// Parse optional date range parameters. Returns nullable DateTimes and error message if parsing fails.
    /// </summary>
    /// <param name="startDate">Optional start date string in format yyyy-MM-dd.</param>
    /// <param name="endDate">Optional end date string in format yyyy-MM-dd.</param>
    /// <returns>Tuple containing nullable start date, nullable end date (exclusive), and error message if parsing fails.</returns>
    internal static (DateTime? start, DateTime? endExclusive, string error) ParseOptionalDateRange(string startDate, string endDate)
    {
        DateTime? start = null;
        DateTime? endExclusive = null;

        if (!string.IsNullOrWhiteSpace(startDate))
        {
            if (!ParseDateTime(startDate, out DateTime parsedStart))
            {
                return (null, null, $"Invalid start date {startDate}. Must be in the format yyyy-MM-dd.");
            }
            start = parsedStart.Date;
        }

        if (!string.IsNullOrWhiteSpace(endDate))
        {
            if (!ParseDateTime(endDate, out DateTime parsedEnd))
            {
                return (null, null, $"Invalid end date {endDate}. Must be in the format yyyy-MM-dd.");
            }
            endExclusive = parsedEnd.Date.AddDays(1);
        }

        return (start, endExclusive, null);
    }

    /// <summary>
    /// Formats a single object value for inclusion in a CSV field.
    /// It handles nulls and applies RFC 4180 escaping for commas, quotes, and line terminators.
    /// </summary>
    private static string FormatCsvField(object field)
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

        // Escape double quotes anywhere in the string by doubling them
        var escapedValue = value.Replace("\"", "\"\"");

        // If the original value contains a comma, double quote, or line break, enclose the whole string in double quotes
        if (value.IndexOfAny([',', '\"', '\r', '\n']) >= 0)
        {
            return $"\"{escapedValue}\"";
        }

        return escapedValue;
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
}