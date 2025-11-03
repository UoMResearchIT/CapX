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
            var query = context.Timesheets
                .Include(t => t.Owner)
                .Include(t => t.TimesheetEntries)
                    .ThenInclude(e => e.InnateCodeTask)
                        .ThenInclude(tk => tk.InnateCode)
                .Where(t => t.OwnerId == person.PersonId);

            query = Helpers.ApplyDateRangeFilter(query, start, endDateExclusive);

            var timesheets = await query
                .OrderBy(t => t.StartDate)
                .ToListAsync();

            // Map to DTOs using shared helper
            var timesheetsAsDTOs = MapToTimesheetDTOs(timesheets);

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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TimesheetsByCodeTaskResponseDTO))]
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

            // Parse optional date range
            var (start, endDateExclusive, dateError) = Helpers.ParseOptionalDateRange(startDate, endDate);
            if (dateError != null)
            {
                logger.LogWarning($"API: GetTimesheetBookingsByCodeTask: {dateError}");
                return Results.BadRequest(dateError);
            }

            // Query timesheets matching the code/task filter
            var query = BuildTimesheetQueryWithCodeTaskFilter(context, code, taskName);
            query = Helpers.ApplyDateRangeFilter(query, start, endDateExclusive);

            var timesheets = await query
                .OrderBy(t => t.StartDate)
                .ThenBy(t => t.Owner!.Name)
                .ToListAsync();

            // Map to DTOs using the same logic as GetTimesheetEntriesForPersonForDateRange
            var timesheetsAsDTOs = MapToTimesheetDTOs(timesheets);

            // Filter entries within each timesheet to only include matching code/task
            var filteredTimesheets = FilterTimesheetEntriesByCodeTask(timesheetsAsDTOs, code, taskName);

            // Calculate aggregated summary by person for capacity analysis
            var summary = CalculatePersonHoursSummary(filteredTimesheets);
            var grandTotal = summary.Sum(s => s.TotalHours);

            // Check to see if we need to return a CSV file
            if (asCsv != null && asCsv == true)
            {
                logger.LogInformation($"Timesheets: Generating CSV for code {code}, task {taskName ?? "ALL"}.");

                var csvData = filteredTimesheets.SelectMany(ts =>
                    ts.Entries.Select(e => new TimesheetCSVDTO(
                        ts.OwnerName,
                        ts.StartDate.ToString("yyyy-MM-dd"),
                        ts.Status,
                        ts.Info ?? "",
                        e.InnateCode,
                        e.InnateCodeName,
                        e.TaskName,
                        e.Duty,
                        e.MondayHours,
                        e.TuesdayHours,
                        e.WednesdayHours,
                        e.ThursdayHours,
                        e.FridayHours,
                        e.SaturdayHours,
                        e.SundayHours,
                        e.MondayHours + e.TuesdayHours + e.WednesdayHours +
                            e.ThursdayHours + e.FridayHours + e.SaturdayHours + e.SundayHours
                    ))
                );

                var fileBytes = Helpers.GenerateCsv(csvData);
                var taskFilter = string.IsNullOrWhiteSpace(taskName) ? "all_tasks" : taskName.Replace(' ', '_');
                var dateFilter = string.IsNullOrWhiteSpace(startDate) && string.IsNullOrWhiteSpace(endDate)
                    ? "all_dates"
                    : $"{startDate ?? "start"}_to_{endDate ?? "end"}";
                var fileName = $"timesheets_{code}_{taskFilter}_{dateFilter}.csv";
                logger.LogInformation($"Timesheets: Returned {filteredTimesheets.Count} timesheets as CSV. Grand total: {grandTotal} hours.");
                return Results.File(fileBytes, "text/csv", fileName);
            }
            else
            {
                // Default to JSON with timesheets and aggregated summary
                var response = new TimesheetsByCodeTaskResponseDTO(
                    Timesheets: filteredTimesheets,
                    Summary: summary,
                    GrandTotalHours: grandTotal
                );

                logger.LogInformation($"Timesheets: Returned {filteredTimesheets.Count} timesheets for code {code} as JSON. Grand total: {grandTotal} hours.");
                return Results.Json(response);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Timesheets: error in GetTimesheetBookingsByCodeTask");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    #region Internal Methods

    /// <summary>
    /// Build a timesheet query filtered by code and optional task name.
    /// NOTE: This query returns ALL timesheet statuses (New, Submitted, Rejected, Approved).
    /// To filter for approved timesheets only, uncomment the line: t.Status == Enums.TimesheetStatus.Approved &&
    /// </summary>
    internal static IQueryable<Data.Entities.Timesheet> BuildTimesheetQueryWithCodeTaskFilter(
        PPMToolContext context, string code, string? taskName)
    {
        return context.Timesheets
            .Include(t => t.Owner)
            .Include(t => t.TimesheetEntries)
                .ThenInclude(e => e.InnateCodeTask)
                    .ThenInclude(tk => tk!.InnateCode)
            .Where(t =>
                // t.Status == Enums.TimesheetStatus.Approved &&
                t.TimesheetEntries.Any(e =>
                    e.InnateCodeTask != null &&
                    e.InnateCodeTask.InnateCode != null &&
                    e.InnateCodeTask.InnateCode.ActivityCode == code &&
                    (string.IsNullOrWhiteSpace(taskName) || e.InnateCodeTask.TaskName == taskName)
                ));
    }

    /// <summary>
    /// Map timesheet entities to DTOs. Reuses the same mapping logic across all timesheet endpoints.
    /// </summary>
    internal static List<TimesheetsDTO> MapToTimesheetDTOs(List<Data.Entities.Timesheet> timesheets)
    {
        return timesheets.Select(t => new TimesheetsDTO(
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
    }

    /// <summary>
    /// Filter timesheet entries to only include those matching the code/task criteria.
    /// Returns new TimesheetsDTO instances with filtered entries.
    /// </summary>
    internal static List<TimesheetsDTO> FilterTimesheetEntriesByCodeTask(
        List<TimesheetsDTO> timesheets, string code, string? taskName)
    {
        return timesheets
            .Select(ts => new TimesheetsDTO(
                ts.TimesheetId,
                ts.OwnerId,
                ts.OwnerName,
                ts.CreatedDate,
                ts.StartDate,
                ts.Status,
                ts.DateStatusChanged,
                ts.Info,
                ts.Entries.Where(e =>
                    e.InnateCode == code &&
                    (string.IsNullOrWhiteSpace(taskName) || e.TaskName == taskName)
                ).ToList()
            ))
            .Where(ts => ts.Entries.Count > 0) // Only include timesheets with matching entries
            .ToList();
    }

    /// <summary>
    /// Calculate aggregated hours summary by person across all timesheets.
    /// </summary>
    internal static List<PersonHoursSummaryDTO> CalculatePersonHoursSummary(List<TimesheetsDTO> timesheets)
    {
        return timesheets
            .GroupBy(ts => ts.OwnerName)
            .Select(g => new PersonHoursSummaryDTO(
                PersonName: g.Key,
                TotalHours: g.SelectMany(ts => ts.Entries)
                    .Sum(e => e.MondayHours + e.TuesdayHours + e.WednesdayHours +
                              e.ThursdayHours + e.FridayHours + e.SaturdayHours + e.SundayHours)
            ))
            .OrderByDescending(s => s.TotalHours)
            .ToList();
    }

    #endregion
}