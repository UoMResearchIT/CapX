using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.Services;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using TSDTO = PPMTool.API.DTOs.Timesheets;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Read only transfer of weekly timesheets and nested entries.
/// Access: superuser, the person, or their line manager
/// No calculations, no updates
/// </summary>
public static class Timesheets
{
    /// <summary>
    /// Give timesheets for a person across a date range
    /// Route uses underscore name, same pattern as skills endpoints
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TSDTO.TimesheetsDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetTimesheetEntriesForPersonForDateRange(
        PPMToolContext db,
        ILogger logger,
        HttpContext http,
        APIAuthService authService,
        string name,
        DateTime start,
        DateTime end)
    {
        try
        {
            // Make this cleverer??

            // Check API Key.
            if (!http.Request.Headers.TryGetValue("x-api-key", out var apiKey))
                return Results.Unauthorized();

            // Make this cleverer??

            // Resolve caller from API key
            var caller = authService.GetUserIfApiKeyActive(db, apiKey);
            if (caller == null)
                return Results.Unauthorized();

            // Need to check the database. Do we have _? I assume we do. Erdem_Atbas? Taken from PersonSkillsDTO.cs
            var person = await db.People
                .Include(p => p.LineManager)
                .FirstOrDefaultAsync(p => p.Name.ToLower() == name.Trim().ToLower().Replace("_", " "));

            // Not found
            if (person == null)
                return Results.NotFound();
            // Authorisation checks
            var callerPersonId = caller.Person?.PersonId ?? 0;
            // Self?
            var isSelf = callerPersonId != 0 && callerPersonId == person.PersonId;
            //LM?
            var isLineManager = person.LineManager?.PersonId == callerPersonId;
            // SU?
            var isSuper = IsSuperUser(caller);

            // If none of the above, forbid
            if (!(isSuper || isSelf || isLineManager))
                return Results.Forbid();

            // Normalise date range to full days
            // Start inclusive, end inclusive (implemented as end exclusive)
            var startDate = start.Date;
            var endExclusive = end.Date.AddDays(1);

            // Query weekly timesheets that overlap the window
            // Read only, include owner and entries with innate info
            var timesheets = await db.Timesheets
                .AsNoTracking()
                .Include(t => t.Owner)
                .Include(t => t.TimesheetEntries)
                    .ThenInclude(e => e.InnateCodeTask)
                        .ThenInclude(tk => tk.InnateCode)
                .Where(t =>
                    t.OwnerId == person.PersonId &&
                    t.StartDate < endExclusive &&
                    t.StartDate.AddDays(7) > startDate)
                .OrderBy(t => t.StartDate)
                .ToListAsync();

            // Map to DTOs only
            var payload = timesheets.Select(t => new TSDTO.TimesheetsDTO(
                TimesheetId: t.TimesheetId,
                OwnerId: t.OwnerId,
                OwnerName: t.Owner?.Name ?? "Unknown",
                CreatedDate: t.CreatedDate,
                StartDate: t.StartDate,
                Status: t.Status,
                DateStatusChanged: t.DateStatusChanged,
                Info: t.Info,
                Entries: t.TimesheetEntries.Select(e => new TSDTO.TimesheetEntryDTO(
                    TimesheetEntryId: e.TimesheetEntryId,
                    InnateCodeTaskId: e.InnateCodeTask?.InnateCodeTaskId ?? 0,
                    InnateCode: e.InnateCodeTask?.InnateCode?.ActivityCode ?? string.Empty,
                    InnateCodeName: e.InnateCodeTask?.InnateCode?.ActivityName ?? string.Empty,
                    TaskName: e.InnateCodeTask?.TaskName ?? string.Empty,
                    Duty: e.InnateCodeTask?.Duty ?? Duty.Other,
                    MondayHours: e.MondayHours,
                    TuesdayHours: e.TuesdayHours,
                    WednesdayHours: e.WednesdayHours,
                    ThursdayHours: e.ThursdayHours,
                    FridayHours: e.FridayHours,
                    SaturdayHours: e.SaturdayHours,
                    SundayHours: e.SundayHours
                )).ToList()
            )).ToList();

            logger.LogInformation("Timesheets: Returned {Count} timesheets for {Person}", payload.Count, person.Name);
            return Results.Json(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Timesheets: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Convenience route for the caller's own timesheets
    /// Uses the caller's Person name to generate the route parameter
    /// </summary>
    public static async Task<IResult> GetMyTimesheetEntriesForDateRange(
        PPMToolContext db,
        ILogger logger,
        HttpContext http,
        APIAuthService authService,
        DateTime start,
        DateTime end)
    {
        if (!http.Request.Headers.TryGetValue("x-api-key", out var apiKey))
            return Results.Unauthorized();

        var caller = authService.GetUserIfApiKeyActive(db, apiKey);
        if (caller == null || caller.Person == null)
            return Results.Unauthorized();

        var underscored = caller.Person.Name.Replace(' ', '_');
        return await GetTimesheetEntriesForPersonForDateRange(db, logger, http, authService, underscored, start, end);
    }

    private static bool IsSuperUser(User user) => user.RoleType == RoleType.Superuser;
}