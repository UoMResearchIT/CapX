using DotNetExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Read only transfer of weekly timesheets and nested entries.
/// Access: superuser, the person, or their line manager
/// </summary>
public static class Timesheets
{
    /// <summary>
    /// Give timesheets for a person across a date range
    /// Route uses underscore name, same pattern as skills endpoints
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TimesheetsDTO>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetTimesheetEntriesForPersonForDateRange(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        string name,
        DateTime start,
        DateTime end)
    {
        try
        {
            // Get the person from the request arguments
            var person = await APIHelper.FindPersonWithLineManagerByNameAsync(context, name);
            if (person == null)
            {
                logger.LogWarning($"API: GetTimesheetEntriesForPersonForDateRange: Person = {name} not found!");
                return Results.NotFound();
            }

            // Authorisation check
            var canAccess = APIHelper.IsSuperUserOrLineManagerOrSelf(context, http, person);
            if (!canAccess)
            {
                logger.LogWarning($"API: GetTimesheetEntriesForPersonForDateRange: Caller does not have permission to access the data!");
                return Results.Forbid();
            }

            // Normalise date range to full days
            // Start inclusive, end inclusive (implemented as end exclusive)
            var startDate = start.Date;
            var endDateExclusive = end.Date.AddDays(1);

            // Query weekly timesheets that overlap the window
            // Read only, include owner and entries with innate info
            var timesheets = await context.Timesheets
                .AsNoTracking()
                .Include(t => t.Owner)
                .Include(t => t.TimesheetEntries)
                    .ThenInclude(e => e.InnateCodeTask)
                        .ThenInclude(tk => tk.InnateCode)
                .Where(t =>
                    t.OwnerId == person.PersonId &&
                    t.StartDate < endDateExclusive &&
                    t.StartDate.AddDays(7) > startDate)
                .OrderBy(t => t.StartDate)
                .ToListAsync();

            // Map to DTOs
            var timesheetsAsDTOs = timesheets.Select(t => new TimesheetsDTO(
                TimesheetId: t.TimesheetId,
                OwnerId: t.OwnerId,
                OwnerName: t.Owner?.Name ?? "Unknown",
                CreatedDate: t.CreatedDate,
                StartDate: t.StartDate,
                Status: t.Status.GetDescription(),
                DateStatusChanged: t.DateStatusChanged,
                Info: t.Info,
                Entries: t.TimesheetEntries.Select(e => new TimesheetEntryDTO(
                    TimesheetEntryId: e.TimesheetEntryId,
                    InnateCodeTaskId: e.InnateCodeTask?.InnateCodeTaskId ?? 0,
                    InnateCode: e.InnateCodeTask?.InnateCode?.ActivityCode ?? string.Empty,
                    InnateCodeName: e.InnateCodeTask?.InnateCode?.ActivityName ?? string.Empty,
                    TaskName: e.InnateCodeTask?.TaskName ?? string.Empty,
                    Duty: e.InnateCodeTask?.Duty.GetDescription() ?? "None",
                    MondayHours: e.MondayHours,
                    TuesdayHours: e.TuesdayHours,
                    WednesdayHours: e.WednesdayHours,
                    ThursdayHours: e.ThursdayHours,
                    FridayHours: e.FridayHours,
                    SaturdayHours: e.SaturdayHours,
                    SundayHours: e.SundayHours
                )).ToList()
            )).ToList();

            logger.LogInformation($"Timesheets: Returned {timesheets.Count} timesheets for {person.Name}");
            return Results.Json(timesheets);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Timesheets: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Convenience route for the caller's own timesheets
    /// API key used to determine the user name argument
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TimesheetsDTO>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetMyTimesheetEntriesForDateRange(
         PPMToolContext context,
         ILogger logger,
         HttpContext http,
         DateTime start,
         DateTime end)
    {
        try
        {
            // Get the caller from the request context -- should always be not null here as middleware would have rejected otherwise
            var user = APIHelper.GetCurrentUser(http);

            // Person entity might be null if the user is not linked to a person
            var name = user!.Person?.Name.Replace(' ', '_') ?? "Unknown";
            return await GetTimesheetEntriesForPersonForDateRange(context, logger, http, name, start, end);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MyTimesheets: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}