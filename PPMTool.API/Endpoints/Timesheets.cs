using System.Text;
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetTimesheetEntriesForPersonForDateRange(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        string name,
        string startDate,
        string endDate)
    {
        try
        {
            // Try parse the datetimes
            var success = APIHelper.ParseDateTime(startDate, out DateTime start);
            if (!success)
            {
                logger.LogWarning($"API: GetTimesheetEntriesForPersonForDateRange: Invalid start date {startDate}");
                return Results.BadRequest($"Invalid start date {startDate}. Must be in the format yyyy-MM-dd.");
            }
            success = APIHelper.ParseDateTime(endDate, out DateTime end);
            if (!success)
            {
                logger.LogWarning($"API: GetTimesheetEntriesForPersonForDateRange: Invalid end date {endDate}");
                return Results.BadRequest($"Invalid end date {endDate}. Must be in the format yyyy-MM-dd.");
            }

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
                return Results.Unauthorized();
            }

            // Normalise date range to full days
            // Start inclusive, end inclusive (implemented as end exclusive)
            start = start.Date;
            var endDateExclusive = end.Date.AddDays(1);

            // Query weekly timesheets that overlap the window
            // Read only, include owner and entries with innate info
            var timesheets = await context.Timesheets
                .Include(t => t.Owner)
                .Include(t => t.TimesheetEntries)
                    .ThenInclude(e => e.InnateCodeTask)
                        .ThenInclude(tk => tk.InnateCode)
                .Where(t =>
                    t.OwnerId == person.PersonId &&
                    t.StartDate < endDateExclusive &&
                    t.StartDate.AddDays(7) > start)
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

            logger.LogInformation($"Timesheets: Returned {timesheetsAsDTOs.Count} timesheets for {person.Name}");
            return Results.Json(timesheetsAsDTOs);
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetMyTimesheetEntriesForDateRange(
         PPMToolContext context,
         ILogger logger,
         HttpContext http,
         string startDate,
         string endDate)
    {
        try
        {
            // Get the caller from the request context -- should always be not null here as middleware would have rejected otherwise
            var user = APIHelper.GetCurrentUser(http);

            // Person entity might be null if the user is not linked to a person
            var name = user!.Person?.Name.Replace(' ', '_') ?? "Unknown";
            return await GetTimesheetEntriesForPersonForDateRange(context, logger, http, name, startDate, endDate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MyTimesheets: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Give timesheets for a person across a date range as a downloadable CSV file.
    /// Access: superuser, the person, or their line manager
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileContentResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetTimesheetEntriesForPersonForDateRangeAsCsv(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        string name,
        string startDate,
        string endDate)
    {
        try
        {
            // Try parse the datetimes
            var success = APIHelper.ParseDateTime(startDate, out DateTime start);
            if (!success)
            {
                logger.LogWarning(
                    $"API: GetTimesheetEntriesForPersonForDateRangeAsCsv: Invalid start date {startDate}");
                return Results.BadRequest($"Invalid start date {startDate}. Must be in the format yyyy-MM-dd.");
            }

            success = APIHelper.ParseDateTime(endDate, out DateTime end);
            if (!success)
            {
                logger.LogWarning($"API: GetTimesheetEntriesForPersonForDateRangeAsCsv: Invalid end date {endDate}");
                return Results.BadRequest($"Invalid end date {endDate}. Must be in the format yyyy-MM-dd.");
            }

            // Get the person from the request arguments
            var person = await APIHelper.FindPersonWithLineManagerByNameAsync(context, name);
            if (person == null)
            {
                logger.LogWarning($"API: GetTimesheetEntriesForPersonForDateRangeAsCsv: Person = {name} not found!");
                return Results.NotFound();
            }

            // Authorisation check
            var canAccess = APIHelper.IsSuperUserOrLineManagerOrSelf(context, http, person);
            if (!canAccess)
            {
                logger.LogWarning(
                    $"API: GetTimesheetEntriesForPersonForDateRangeAsCsv: Caller does not have permission to access the data!");
                return Results.Unauthorized();
            }

            // Normalise date range
            start = start.Date;
            var endDateExclusive = end.Date.AddDays(1);

            // Query weekly timesheets
            var timesheets = await context.Timesheets
                .Include(t => t.Owner)
                .Include(t => t.TimesheetEntries)
                .ThenInclude(e => e.InnateCodeTask)
                .ThenInclude(tk => tk.InnateCode)
                .Where(t =>
                    t.OwnerId == person.PersonId &&
                    t.StartDate < endDateExclusive &&
                    t.StartDate.AddDays(7) > start)
                .OrderBy(t => t.StartDate)
                .ToListAsync();
            
            // Use the DTO
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

            // Generate CSV
            var csvBuilder = new StringBuilder();

            // Add Header Row
            csvBuilder.AppendLine(
                "PersonName,WeekStartDate,InnateCode,InnateCodeName,TaskName,Duty,MondayHours,TuesdayHours,WednesdayHours,ThursdayHours,FridayHours,SaturdayHours,SundayHours,WeeklyTotalHours");

            // Add Data Rows
            foreach (var timesheetDto in timesheetsAsDTOs)
            {
                foreach (var entryDto in timesheetDto.Entries)
                {
                    var weeklyTotal = entryDto.MondayHours + entryDto.TuesdayHours + entryDto.WednesdayHours +
                                      entryDto.ThursdayHours + entryDto.FridayHours + entryDto.SaturdayHours + entryDto.SundayHours;

                    csvBuilder.AppendLine(
                        $"{timesheetDto.OwnerName}," +
                        $"{timesheetDto.StartDate:yyyy-MM-dd}," +
                        $"{entryDto.InnateCode}," +
                        $"\"{entryDto.InnateCodeName}\"," +
                        $"\"{entryDto.TaskName}\"," +
                        $"{entryDto.Duty}," +
                        $"{entryDto.MondayHours}," +
                        $"{entryDto.TuesdayHours}," +
                        $"{entryDto.WednesdayHours}," +
                        $"{entryDto.ThursdayHours}," +
                        $"{entryDto.FridayHours}," +
                        $"{entryDto.SaturdayHours}," +
                        $"{entryDto.SundayHours}," +
                        $"{weeklyTotal}"
                    );
                }
            }

            logger.LogInformation($"Timesheets CSV: Generated CSV for {person.Name} from DTOs.");

            // Return the CSV as a file
            var fileName = $"{person.Name.Replace(' ', '_')}_timesheets_{startDate}_to_{endDate}.csv";
            var fileBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());

            return Results.File(fileBytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Timesheets CSV: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}