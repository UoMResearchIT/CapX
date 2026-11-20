// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A single row from a weekly timesheet, holding hours for each day.
    /// </summary>
    /// <param name="TimesheetEntryId"></param>
    /// <param name="InnateCodeTaskId"></param>
    /// <param name="InnateCode"></param>
    /// <param name="InnateCodeName"></param>
    /// <param name="TaskName"></param>
    /// <param name="Duty"></param>
    /// <param name="MondayHours"></param>
    /// <param name="TuesdayHours"></param>
    /// <param name="WednesdayHours"></param>
    /// <param name="ThursdayHours"></param>
    /// <param name="FridayHours"></param>
    /// <param name="SaturdayHours"></param>
    /// <param name="SundayHours"></param>
    public sealed record TimesheetEntryDTO(
        int TimesheetEntryId,
        int InnateCodeTaskId,
        string InnateCode,
        string InnateCodeName,
        string TaskName,
        string Duty,
        double MondayHours,
        double TuesdayHours,
        double WednesdayHours,
        double ThursdayHours,
        double FridayHours,
        double SaturdayHours,
        double SundayHours
    );

    /// <summary>
    /// A weekly timesheet with metadata and nested entries.
    /// </summary>
    /// <param name="TimesheetId"></param>
    /// <param name="OwnerId"></param>
    /// <param name="OwnerName"></param>
    /// <param name="CreatedDate"></param>
    /// <param name="StartDate"></param>
    /// <param name="Status"></param>
    /// <param name="DateStatusChanged"></param>
    /// <param name="Info"></param>
    /// <param name="Entries"></param>
    public sealed record TimesheetsDTO(
        int TimesheetId,
        int OwnerId,
        string OwnerName,
        DateTime CreatedDate,
        DateTime StartDate,
        string Status,
        DateTime DateStatusChanged,
        string? Info,
        IReadOnlyList<TimesheetEntryDTO> Entries
    );

    /// <summary>
    /// Aggregated summary of hours booked by a person for a specific code/task.
    /// </summary>
    /// <param name="PersonName"></param>
    /// <param name="TotalHours"></param>
    public sealed record PersonHoursSummaryDTO(
        string PersonName,
        double TotalHours
    );

    /// <summary>
    /// Response for timesheet bookings by code/task query.
    /// Reuses existing TimesheetsDTO structure and adds aggregated summary for capacity analysis.
    /// </summary>
    /// <param name="Timesheets"></param>
    /// <param name="Summary"></param>
    /// <param name="GrandTotalHours"></param>
    public sealed record TimesheetsByCodeTaskResponseDTO(
        IReadOnlyList<TimesheetsDTO> Timesheets,
        IReadOnlyList<PersonHoursSummaryDTO> Summary,
        double GrandTotalHours
    );

    /// <summary>
    /// Represents a row of the CSV file export
    /// </summary>
    /// <param name="PersonName"></param>
    /// <param name="TimesheetWeekStart"></param>
    /// <param name="TimesheetStatus"></param>
    /// <param name="TimesheetInfo"></param>
    /// <param name="InnateCode"></param>
    /// <param name="InnateCodeName"></param>
    /// <param name="TaskName"></param>
    /// <param name="Duty"></param>
    /// <param name="MondayHours"></param>
    /// <param name="TuesdayHours"></param>
    /// <param name="WednesdayHours"></param>
    /// <param name="ThursdayHours"></param>
    /// <param name="FridayHours"></param>
    /// <param name="SaturdayHours"></param>
    /// <param name="SundayHours"></param>
    /// <param name="TotalHoursForWeek"></param>
    public sealed record TimesheetCSVRowData(
        string PersonName,
        string TimesheetWeekStart,
        string TimesheetStatus,
        string TimesheetInfo,
        string InnateCode,
        string InnateCodeName,
        string TaskName,
        string Duty,
        double MondayHours,
        double TuesdayHours,
        double WednesdayHours,
        double ThursdayHours,
        double FridayHours,
        double SaturdayHours,
        double SundayHours,
        double TotalHoursForWeek
    );

    /// <summary>
    /// CSV row data for grouped timesheet bookings export.
    /// </summary>
    /// <param name="ProjectCode">The project/activity code</param>
    /// <param name="TaskName">Name of the task</param>
    /// <param name="PersonName">Name of the person</param>
    /// <param name="Date">Date in formatted string</param>
    /// <param name="Hours">Hours worked on that date</param>
    public sealed record GroupedTimesheetCSVRowData(
        string ProjectCode,
        string TaskName,
        string PersonName,
        string Date,
        double Hours
    );

    /// <summary>
    /// Person allocation for a specific task, with daily hours breakdown.
    /// </summary>
    /// <param name="Person">Name of the person</param>
    /// <param name="BookedHours">Dictionary of date (formatted string) to hours worked</param>
    /// <param name="Total">Total hours for this person on this task</param>
    public sealed record PersonAllocationDTO(
        string Person,
        IReadOnlyDictionary<string, double> BookedHours,
        double Total
    );

    /// <summary>
    /// Task allocation containing all person allocations for a specific task.
    /// </summary>
    /// <param name="TaskName">Name of the task</param>
    /// <param name="Allocations">List of person allocations for this task</param>
    /// <param name="TaskTotal">Total hours for this task across all people</param>
    public sealed record TaskAllocationDTO(
        string TaskName,
        IReadOnlyList<PersonAllocationDTO> Allocations,
        double TaskTotal
    );

    /// <summary>
    /// Response for grouped timesheet bookings by code and task.
    /// Data is grouped by task, then by person, with weekly hours breakdown.
    /// </summary>
    /// <param name="ProjectCode">The project/activity code</param>
    /// <param name="Tasks">List of task allocations</param>
    public sealed record TimesheetBookingsByCodeTaskResponseDTO(
        string ProjectCode,
        IReadOnlyList<TaskAllocationDTO> Tasks
    );

    /// <summary>
    /// Request body for POST /api/timesheets/add. One person's hours for one
    /// week on one task of a Project's timesheet code. If the person has no
    /// timesheet for that week, one is created, already Approved. If they
    /// have one, the hours go into it and its status is left alone, unless
    /// ApproveExistingTimesheet is set. Only Approved timesheets appear in a
    /// Project's Actuals Report. Sending the same person, week and task again
    /// replaces that entry's hours. Name the person by exactly one of
    /// Username or PersonId.
    /// Kept separate from <see cref="TimesheetEntryDTO"/>: a request carries only what the caller
    /// supplies, and the read record adds the ids and fields CapX derives or links.
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
    public sealed record WriteTimesheetEntryDTO(
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
    public sealed record WriteTimesheetEntryResponseDTO(
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