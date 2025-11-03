using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Read only transfer of weekly timesheets and nested entries.
/// Access: superuser, the person, or their line manager.
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
    /// <param name="asCsv">Whether the returned data should be as a CSV download. Default is JSON if not present.</param>
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
            var success = Helpers.ParseDateTime(startDate, out DateTime start);
            if (!success)
            {
                logger.LogWarning($"API: GetTimesheetEntriesForPersonForDateRange: Invalid start date {startDate}");
                return Results.BadRequest($"Invalid start date {startDate}. Must be in the format yyyy-MM-dd.");
            }
            success = Helpers.ParseDateTime(endDate, out DateTime end);
            if (!success)
            {
                logger.LogWarning($"API: GetTimesheetEntriesForPersonForDateRange: Invalid end date {endDate}");
                return Results.BadRequest($"Invalid end date {endDate}. Must be in the format yyyy-MM-dd.");
            }

            // If the name is null then assume the caller
            if (name == null)
            {
                var user = Helpers.GetCurrentUser(http);
                name = user!.Person?.Name.Replace(' ', '_') ?? "Unknown";
            }

            // Get the person from the request arguments
            var person = await Helpers.FindPersonWithLineManagerByNameAsync(context, name);
            if (person == null)
            {
                logger.LogWarning($"API: GetTimesheetEntriesForPersonForDateRange: Person = {name} not found!");
                return Results.NotFound();
            }

            // Authorisation check
            var canAccess = Helpers.IsSuperUserOrLineManagerOrSelf(context, http, person);
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

                    // Map to a DTO for the CSV file
                    timesheetDto.Entries.Select(entryDto =>
                        new TimesheetCSVDTO(
                            timesheetDto.OwnerName,
                            timesheetDto.StartDate.ToString("yyyy-MM-dd"),
                            timesheetDto.Status,
                            timesheetDto.Info ?? "",
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
                            entryDto.MondayHours + entryDto.TuesdayHours + entryDto.WednesdayHours +
                                entryDto.ThursdayHours + entryDto.FridayHours + entryDto.SaturdayHours + entryDto.SundayHours
                        )
                    )
                );

                var fileBytes = Helpers.GenerateCsv(csvData);
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

    /// <summary>
    /// Get timesheet bookings for a specific activity code and task combination.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="http"></param>
    /// <param name="code">The InnateCode ActivityCode to query (required). If Activity Code = "S-RESXXX - XXX", then InnateCode = "S-RESXXX".</param>
    /// <param name="taskName">Optional task name to filter by. If null, returns all tasks for the code. This corresponds to the WLM Duty and Task field.</param>
    /// <param name="startDate">Optional start date in the format yyyy-MM-dd. If omitted, returns all historical data.</param>
    /// <param name="endDate">Optional end date in the format yyyy-MM-dd. If omitted, returns all historical data.</param>
    /// <param name="asCsv">Whether the returned data should be as a CSV download. Default is JSON if not present.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TimesheetBookingsByCodeTaskDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetTimesheetBookingsByCodeTask(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        [FromQuery] string code,
        [FromQuery] string? taskName = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] bool? asCsv = null)
    {
        try
        {
            // Authorization check - superuser only
            var user = Helpers.GetCurrentUser(http);
            if (!Helpers.IsSuperUser(user))
            {
                logger.LogWarning($"API: GetTimesheetBookingsByCodeTask: Non-superuser attempted to access booking data");
                return Results.Unauthorized();
            }

            // Validate required parameters
            if (string.IsNullOrWhiteSpace(code))
            {
                logger.LogWarning($"API: GetTimesheetBookingsByCodeTask: Missing code parameter");
                return Results.BadRequest("Code parameter is required.");
            }

            // Parse optional date parameters
            DateTime? start = null;
            DateTime? endDateExclusive = null;

            if (!string.IsNullOrWhiteSpace(startDate))
            {
                var success = Helpers.ParseDateTime(startDate, out DateTime parsedStart);
                if (!success)
                {
                    logger.LogWarning($"API: GetTimesheetBookingsByCodeTask: Invalid start date {startDate}");
                    return Results.BadRequest($"Invalid start date {startDate}. Must be in the format yyyy-MM-dd.");
                }
                start = parsedStart.Date;
            }

            if (!string.IsNullOrWhiteSpace(endDate))
            {
                var success = Helpers.ParseDateTime(endDate, out DateTime parsedEnd);
                if (!success)
                {
                    logger.LogWarning($"API: GetTimesheetBookingsByCodeTask: Invalid end date {endDate}");
                    return Results.BadRequest($"Invalid end date {endDate}. Must be in the format yyyy-MM-dd.");
                }
                endDateExclusive = parsedEnd.Date.AddDays(1);
            }

            // Query weekly timesheets that contain entries matching the code/task
            // NOTE: This query returns ALL timesheet statuses (New, Submitted, Rejected, Approved).
            // To filter for approved timesheets only, add the following to the Where clause:
            // && t.Status == TimesheetStatus.Approved
            var query = context.Timesheets
                .Include(t => t.Owner)
                .Include(t => t.TimesheetEntries)
                    .ThenInclude(e => e.InnateCodeTask)
                        .ThenInclude(tk => tk!.InnateCode)
                .Where(t =>
                    t.TimesheetEntries.Any(e =>
                        e.InnateCodeTask != null &&
                        e.InnateCodeTask.InnateCode != null &&
                        e.InnateCodeTask.InnateCode.ActivityCode == code &&
                        (string.IsNullOrWhiteSpace(taskName) || e.InnateCodeTask.TaskName == taskName)
                    ));

            // Apply optional date filtering
            if (start.HasValue && endDateExclusive.HasValue)
            {
                var startValue = start.Value;
                var endValue = endDateExclusive.Value;
                query = query.Where(t => t.StartDate < endValue && t.StartDate.AddDays(7) > startValue);
            }
            else if (start.HasValue)
            {
                var startValue = start.Value;
                query = query.Where(t => t.StartDate >= startValue);
            }
            else if (endDateExclusive.HasValue)
            {
                var endValue = endDateExclusive.Value;
                query = query.Where(t => t.StartDate < endValue);
            }

            var timesheets = await query
                .OrderBy(t => t.StartDate)
                .ThenBy(t => t.Owner!.Name)
                .ToListAsync();

            // Flatten to individual booking entries
            var bookingEntries = new List<TimesheetBookingEntryDTO>();

            foreach (var timesheet in timesheets)
            {
                // Filter entries to only those matching the code/task criteria
                var matchingEntries = timesheet.TimesheetEntries
                    .Where(e =>
                        e.InnateCodeTask != null &&
                        e.InnateCodeTask.InnateCode != null &&
                        e.InnateCodeTask.InnateCode.ActivityCode == code &&
                        (string.IsNullOrWhiteSpace(taskName) || e.InnateCodeTask.TaskName == taskName))
                    .ToList();

                foreach (var entry in matchingEntries)
                {
                    var totalHours = entry.MondayHours + entry.TuesdayHours + entry.WednesdayHours +
                                   entry.ThursdayHours + entry.FridayHours + entry.SaturdayHours + entry.SundayHours;

                    bookingEntries.Add(new TimesheetBookingEntryDTO(
                        PersonName: timesheet.Owner?.Name ?? "Unknown",
                        WeekStartDate: timesheet.StartDate,
                        TimesheetStatus: timesheet.Status.GetDescription(),
                        InnateCode: entry.InnateCodeTask?.InnateCode?.ActivityCode ?? string.Empty,
                        InnateCodeName: entry.InnateCodeTask?.InnateCode?.ActivityName ?? string.Empty,
                        TaskName: entry.InnateCodeTask?.TaskName ?? string.Empty,
                        Duty: entry.InnateCodeTask?.Duty.GetDescription() ?? "None",
                        MondayHours: entry.MondayHours,
                        TuesdayHours: entry.TuesdayHours,
                        WednesdayHours: entry.WednesdayHours,
                        ThursdayHours: entry.ThursdayHours,
                        FridayHours: entry.FridayHours,
                        SaturdayHours: entry.SaturdayHours,
                        SundayHours: entry.SundayHours,
                        TotalHours: totalHours
                    ));
                }
            }

            // Calculate aggregated summary by person
            var summary = bookingEntries
                .GroupBy(e => e.PersonName)
                .Select(g => new PersonBookingSummaryDTO(
                    PersonName: g.Key,
                    TotalHours: g.Sum(e => e.TotalHours)
                ))
                .OrderByDescending(s => s.TotalHours)
                .ToList();

            var grandTotal = summary.Sum(s => s.TotalHours);

            // Check to see if we need to return a CSV file
            if (asCsv != null && asCsv == true)
            {
                logger.LogInformation($"Timesheets: Generating CSV for code {code}, task {taskName ?? "ALL"}.");

                var fileBytes = Helpers.GenerateCsv(bookingEntries);
                var taskFilter = string.IsNullOrWhiteSpace(taskName) ? "all_tasks" : taskName.Replace(' ', '_');
                var dateFilter = string.IsNullOrWhiteSpace(startDate) && string.IsNullOrWhiteSpace(endDate)
                    ? "all_dates"
                    : $"{startDate ?? "start"}_to_{endDate ?? "end"}";
                var fileName = $"bookings_{code}_{taskFilter}_{dateFilter}.csv";
                logger.LogInformation($"Timesheets: Returned {bookingEntries.Count} booking entries as CSV.");
                return Results.File(fileBytes, "text/csv", fileName);
            }
            else
            {
                // Default to JSON with both detailed entries and summary
                // Pass nullable dates - null indicates no date filtering was applied
                var response = new TimesheetBookingsByCodeTaskDTO(
                    Code: code,
                    TaskName: taskName,
                    StartDate: start,
                    EndDate: endDateExclusive?.AddDays(-1),
                    Entries: bookingEntries,
                    Summary: summary,
                    GrandTotalHours: grandTotal
                );

                logger.LogInformation($"Timesheets: Returned {bookingEntries.Count} booking entries for code {code} as JSON. Grand total: {grandTotal} hours.");
                return Results.Json(response);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Timesheets: error in GetTimesheetBookingsByCodeTask");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}