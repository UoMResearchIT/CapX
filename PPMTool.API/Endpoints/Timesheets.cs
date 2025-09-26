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
    /// Get timesheets for a person across a date range.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="http"></param>
    /// <param name="startDate">The start date of the query window in the format yyyy-MM-dd</param>
    /// <param name="endDate">The end date of the query window in the format yyyy-MM-dd</param>
    /// <param name="name">The name of the person with spaces replaced with underscores. If not present defaults to the API key owner.</param>
    /// <param name="asCsv">Whether the retruned data should be as a CSV download. Default is JSON if not present.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TimesheetsDTO>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetTimesheetEntriesForPersonForDateRange(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        [FromQuery] string? name = null,
        [FromQuery] bool? asCsv = null)
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

            // If the name is null then assume the caller
            if (name == null)
            {
                var user = APIHelper.GetCurrentUser(http);
                name = user!.Person?.Name.Replace(' ', '_') ?? "Unknown";
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

            // Check to see if we need to return a CSV file
            if (asCsv != null && asCsv == true)
            {
                logger.LogInformation($"Timesheets: Generating CSV for {person.Name}.");

                // Flatten the data for a simple CSV structure
                var csvData = timesheetsAsDTOs.SelectMany(timesheetDto =>

                    // Map to an anonymous DTO for the CSV file
                    timesheetDto.Entries.Select(entryDto => new
                    {
                        PersonName = timesheetDto.OwnerName,
                        TimesheetWeekStart = timesheetDto.StartDate.ToString("yyyy-MM-dd"),
                        TimesheetStatus = timesheetDto.Status,
                        TimesheetInfo = timesheetDto.Info,
                        entryDto.InnateCode,
                        entryDto.InnateCodeName,
                        entryDto.TaskName,
                        entryDto.Duty,
                        entryDto.MondayHours,
                        entryDto.TuesdayHours,
                        entryDto.WednesdayHours,
                        entryDto.ThursdayHours,
                        entryDto.FridayHours,
                        entryDto.SaturdayHours,
                        entryDto.SundayHours,
                        TotalHoursForWeek = entryDto.MondayHours + entryDto.TuesdayHours + entryDto.WednesdayHours +
                            entryDto.ThursdayHours + entryDto.FridayHours + entryDto.SaturdayHours + entryDto.SundayHours
                    }));

                var fileBytes = APIHelper.GenerateCsv(csvData);
                var fileName = $"{person.Name.Replace(' ', '_')}_timesheets_{startDate}_to_{endDate}.csv";
                logger.LogInformation($"Timesheets: Returned {timesheetsAsDTOs.Count} timesheets for {person.Name} as CSV.");
                return Results.File(fileBytes, "text/csv", fileName);
            }
            else
            {
                // Default to JSON
                logger.LogInformation($"Timesheets: Returned {timesheetsAsDTOs.Count} timesheets for {person.Name} as JSON.");
                return Results.Json(timesheetsAsDTOs);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Timesheets: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}