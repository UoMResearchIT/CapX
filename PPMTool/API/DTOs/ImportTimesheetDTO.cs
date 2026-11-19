// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// Request body for POST /api/timesheets/add. One person's hours for one
    /// week on one task of a Project's timesheet code. If the person has no
    /// timesheet for that week, one is created, already Approved. If they
    /// have one, the hours go into it and its status is left alone, unless
    /// ApproveExistingTimesheet is set. Only Approved timesheets appear in a
    /// Project's Actuals Report. Sending the same person, week and task again
    /// replaces that entry's hours. Name the person by exactly one of
    /// Username or PersonId.
    /// </summary>
    /// <param name="Username">Username of a User with a linked Person</param>
    /// <param name="PersonId">Id of a Person, with or without a User</param>
    /// <param name="ProjectId">The Project's database id, not its RTP. The Project must have a timesheet code.</param>
    /// <param name="WeekStartDate">A Monday, with no time of day</param>
    /// <param name="TaskName">One of the tasks on the Project's timesheet code</param>
    /// <param name="MondayHours"></param>
    /// <param name="TuesdayHours"></param>
    /// <param name="WednesdayHours"></param>
    /// <param name="ThursdayHours"></param>
    /// <param name="FridayHours"></param>
    /// <param name="SaturdayHours"></param>
    /// <param name="SundayHours"></param>
    /// <param name="ApproveExistingTimesheet">Dangerous; defaults to false. When true and the person already has a timesheet for the week that isn't Approved (a draft, submitted or rejected), it is marked Approved, with the caller recorded as approver. Approval covers the whole week: any hours the person entered themselves, on any project, are approved too, without their submitting them or their line manager seeing them.</param>
    public sealed record ImportTimesheetEntryDTO(
        string? Username,
        int? PersonId,
        int ProjectId,
        DateTime WeekStartDate,
        string TaskName,
        double MondayHours,
        double TuesdayHours,
        double WednesdayHours,
        double ThursdayHours,
        double FridayHours,
        double SaturdayHours,
        double SundayHours,
        bool ApproveExistingTimesheet = false
    );

    /// <summary>Response for a written timesheet entry.</summary>
    /// <param name="TimesheetId"></param>
    /// <param name="TimesheetCreated">False if the person already had a timesheet for that week</param>
    /// <param name="EntryCreated">False if an existing entry for the task was updated</param>
    /// <param name="TotalHours">The entry's hours for the week</param>
    /// <param name="TimesheetStatus">The timesheet's status after the write, as a <see cref="PPMTool.Data.Enums.TimesheetStatus"/> name. Anything but "Approved" means these hours aren't in the Actuals Report yet.</param>
    public sealed record ImportTimesheetResponseDTO(
        int TimesheetId,
        bool TimesheetCreated,
        bool EntryCreated,
        double TotalHours,
        string? TimesheetStatus = null
    );

    /// <summary>
    /// Request body for PUT /api/timesheets/update. Identifies the entry by
    /// TimesheetEntryId and changes the fields supplied, so a single day can
    /// be corrected, or the entry moved to another task, without resending
    /// the week.
    /// </summary>
    /// <param name="TimesheetEntryId">From GET /api/timesheets/getEntries</param>
    /// <param name="NewTaskName">Another task on the same timesheet code, which has no entry on this timesheet yet</param>
    /// <param name="MondayHours"></param>
    /// <param name="TuesdayHours"></param>
    /// <param name="WednesdayHours"></param>
    /// <param name="ThursdayHours"></param>
    /// <param name="FridayHours"></param>
    /// <param name="SaturdayHours"></param>
    /// <param name="SundayHours"></param>
    public sealed record UpdateTimesheetEntryRequestDTO(
        int TimesheetEntryId,
        string? NewTaskName,
        double? MondayHours,
        double? TuesdayHours,
        double? WednesdayHours,
        double? ThursdayHours,
        double? FridayHours,
        double? SaturdayHours,
        double? SundayHours
    );

    /// <summary>Response for an updated timesheet entry.</summary>
    /// <param name="TimesheetEntryId"></param>
    /// <param name="TotalHours">The entry's hours for the week, after the update</param>
    public sealed record UpdateTimesheetEntryResponseDTO(
        int TimesheetEntryId,
        double TotalHours
    );
}
