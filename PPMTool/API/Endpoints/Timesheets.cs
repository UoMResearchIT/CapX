// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;
using PPMTool.Services;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Transfer of weekly timesheets and nested entries. Read access:
/// superuser, the person, or their line manager. CreateTimesheetEntry and
/// UpdateTimesheetEntry are Superuser-only writes, gated behind
/// SettingType.ImportApiEnabled -- see UoMResearchIT/CapX#1310.
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
        [FromQuery] string name = null,
        [FromQuery] bool? asCsv = null)
    {
        try
        {
            // Try parse the datetimes
            var success = GeneralHelpers.ParseDateTime(startDate, out DateTime start);
            if (!success)
            {
                logger.LogWarning($"API: GetTimesheetEntriesForPersonForDateRange: Invalid start date {startDate}");
                return Results.BadRequest($"Invalid start date {startDate}. Must be in the format yyyy-MM-dd.");
            }
            success = GeneralHelpers.ParseDateTime(endDate, out DateTime end);
            if (!success)
            {
                logger.LogWarning($"API: GetTimesheetEntriesForPersonForDateRange: Invalid end date {endDate}");
                return Results.BadRequest($"Invalid end date {endDate}. Must be in the format yyyy-MM-dd.");
            }

            // If the name is null then assume the caller
            if (name == null)
            {
                var user = GeneralHelpers.GetCurrentUser(http);
                name = user!.Person?.Name.Replace(' ', '_') ?? "Unknown";
            }

            // Get the person from the request arguments
            var person = await GeneralHelpers.FindPersonWithLineManagerByNameAsync(context, name);
            if (person == null)
            {
                logger.LogWarning($"API: GetTimesheetEntriesForPersonForDateRange: Person = {name} not found!");
                return Results.NotFound();
            }

            // Authorisation check
            var canAccess = GeneralHelpers.IsSuperUserOrLineManagerOrSelf(context, http, person);
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
            var query = TimesheetsHelpers.BuildTimesheetQuery(context)
                .Where(t => t.OwnerId == person.PersonId);

            // Enhance query to apply date range filter
            query = TimesheetsHelpers.ApplyDateRangeFilter(query, start, endDateExclusive);

            // Execute the query
            var timesheets = await query
                .OrderBy(t => t.StartDate)
                .ToListAsync();

            // Map to DTOs using shared helper
            var timesheetsAsDTOs = TimesheetsHelpers.MapToTimesheetDTOs(timesheets);

            // Check to see if we need to return a CSV file
            if (asCsv != null && asCsv == true)
            {
                logger.LogInformation($"Timesheets: Generating CSV for {person.Name}.");

                // Flatten the data for CSV
                var csvData = TimesheetsHelpers.MapToCsvRowData(timesheetsAsDTOs);
                var fileBytes = GeneralHelpers.GenerateCsv(csvData);
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
    /// <param name="code">The code to query (required). This is just the code part of the full timesheet activity e.g. if timesheet activity = "S-RESXXX - Long Name", then code = "S-RESXXX".</param>
    /// <param name="taskName">Optional task name to filter by. If null, returns bookings for all tasks for the code.</param>
    /// <param name="startDate">Optional start date in the format yyyy-MM-dd. If omitted, returns all historical data.</param>
    /// <param name="endDate">Optional end date in the format yyyy-MM-dd. If omitted, returns all historical data.</param>
    /// <param name="asCsv">Whether the returned data should be as a CSV download. Default is JSON if not present.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TimesheetsByCodeTaskResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult GetTimesheetBookingsByCodeAndTask(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        [FromQuery] string code,
        [FromQuery] string taskName = null,
        [FromQuery] string startDate = null,
        [FromQuery] string endDate = null,
        [FromQuery] bool? asCsv = null)
    {
        try
        {
            // Authorisation check - superusers and managers only
            var user = GeneralHelpers.GetCurrentUser(http);
            if (!GeneralHelpers.IsSuperUserOrManager(user))
            {
                logger.LogWarning($"API: GetTimesheetBookingsByCodeTask: User does not have permission to access booking data");
                return Results.Unauthorized();
            }

            // Validate required parameters
            if (string.IsNullOrWhiteSpace(code))
            {
                logger.LogWarning($"API: GetTimesheetBookingsByCodeTask: Missing code parameter");
                return Results.BadRequest("Code parameter is required.");
            }

            // Check the code exists in the system
            var innateCode = TimesheetsHelpers.GetInnateCode(context, code);
            if (innateCode == null)
            {
                logger.LogWarning($"API: GetTimesheetBookingsByCodeTask: Code provided is not known in the system");
                return Results.NotFound("Unknown timesheet code.");
            }

            // Parse optional date range
            var (start, endDateExclusive, dateError) = GeneralHelpers.ParseOptionalDateRange(startDate, endDate);
            if (dateError != null)
            {
                logger.LogWarning($"API: GetTimesheetBookingsByCodeTask: {dateError}");
                return Results.BadRequest(dateError);
            }

            // Query timesheets matching the code/task filter
            var query = TimesheetsHelpers.BuildTimesheetQuery(context);

            // Add the date range filter to the query
            query = TimesheetsHelpers.ApplyDateRangeFilter(query, start, endDateExclusive);

            // Filter on the task and code (includes sensitive data filtering)
            var filteredTimesheets = TimesheetsHelpers.GetAllMatchingCodeAndTask(query, code, taskName, innateCode, user);

            // Execute the query
            var orderedAndFilteredTimesheets = filteredTimesheets
                .OrderBy(t => t.StartDate)
                .ThenBy(t => t.Owner!.Name)
                .ToList();

            // Map to grouped bookings format
            var groupedResponse = TimesheetsHelpers.MapToGroupedBookingsDTO(
                orderedAndFilteredTimesheets,
                code,
                code,
                taskName
            );

            // Calculate grand total for logging
            var grandTotal = groupedResponse.Tasks.Sum(t => t.TaskTotal);

            // Check to see if we need to return a CSV file
            if (asCsv != null && asCsv == true)
            {
                logger.LogInformation("Timesheets: Generating CSV for code {Code}, task {TaskName}", code, taskName ?? "ALL");

                // Flatten the grouped data for CSV
                var csvData = TimesheetsHelpers.MapToGroupedCsvRowData(groupedResponse);
                var fileBytes = GeneralHelpers.GenerateCsv(csvData);
                var taskFilter = string.IsNullOrWhiteSpace(taskName) ? "all_tasks" : taskName.Replace(' ', '_');
                var dateFilter = string.IsNullOrWhiteSpace(startDate) && string.IsNullOrWhiteSpace(endDate)
                    ? "all_dates"
                    : $"{startDate ?? "start"}_to_{endDate ?? "end"}";
                var fileName = $"timesheets_{code}_{taskFilter}_{dateFilter}.csv";
                logger.LogInformation("Timesheets: Returned grouped timesheets as CSV. Grand total: {GrandTotal} hours", grandTotal);
                return Results.File(fileBytes, "text/csv", fileName);
            }
            else
            {
                // Return JSON with grouped format
                logger.LogInformation("Timesheets: Returned grouped timesheets for code {Code} as JSON. Grand total: {GrandTotal} hours", code, grandTotal);
                return Results.Json(groupedResponse);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Timesheets: error in GetTimesheetBookingsByCodeTask");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Create or update one week's actual hours for one person on one
    /// project's InnateActivity task (see ImportTimesheetEntryDTO). See
    /// UoMResearchIT/CapX#1310.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ImportTimesheetResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult CreateTimesheetEntry(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportTimesheetEntryDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Timesheets.CreateTimesheetEntry");
            if (!allowed) return gateResult!;

            var person = request.Username ?? $"PersonId {request.PersonId}";

            var errors = importService.ValidateTimesheetEntry(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Timesheets: timesheet validation failed for '{Person}'/{ProjectId}/{Week}: {Errors}", person, request.ProjectId, request.WeekStartDate, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.CreateOrUpdateTimesheetEntry(context, request);
            logger.LogInformation(
                "API: Timesheets: timesheet {TimesheetId} for {Person}, week {Week}, project {ProjectId}: {Hours}h ({Created}) by {User}",
                result.TimesheetId, person, request.WeekStartDate, request.ProjectId, result.TotalHours, result.EntryCreated ? "new entry" : "updated entry", caller!.Name);
            return Results.Created($"/api/timesheets/{result.TimesheetId}", result);
        }
        catch (Exception ex)
        {
            var person = request.Username ?? $"PersonId {request.PersonId}";
            logger.LogError(ex, "API: Timesheets: error creating timesheet entry for '{Person}'/{ProjectId}/{Week}", person, request.ProjectId, request.WeekStartDate);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Correct an existing TimesheetEntry's day hours and/or task.
    /// Identified by TimesheetEntryId (from GET /api/timesheets). See
    /// UpdateTimesheetEntryRequestDTO.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateTimesheetEntryResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult UpdateTimesheetEntry(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] UpdateTimesheetEntryRequestDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Timesheets.UpdateTimesheetEntry");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateTimesheetEntryUpdate(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Timesheets: timesheet entry update validation failed for TimesheetEntryId {TimesheetEntryId}: {Errors}", request.TimesheetEntryId, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.UpdateTimesheetEntry(context, request);
            logger.LogInformation("API: Timesheets: updated TimesheetEntry {TimesheetEntryId}, {Hours}h total, by {User}", result.TimesheetEntryId, result.TotalHours, caller!.Name);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Timesheets: error updating TimesheetEntry {TimesheetEntryId}", request.TimesheetEntryId);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
