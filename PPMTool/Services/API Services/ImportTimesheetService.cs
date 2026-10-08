// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services
{
    /// <summary>
    /// Service for API import operations that validate and mutate timesheets and timesheet entries.
    /// </summary>
    public class ImportTimesheetService
    {
        private readonly TimesheetService timesheetService;
        private readonly PersonService personService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportTimesheetService"/> class.
        /// </summary>
        /// <param name="timesheetService">Service used to validate and persist timesheets.</param>
        /// <param name="personService">Service used to resolve people for imported timesheet data.</param>
        public ImportTimesheetService(TimesheetService timesheetService, PersonService personService)
        {
            this.timesheetService = timesheetService;
            this.personService = personService;
        }

        /// <summary>
        /// Validates a create-or-update timesheet-entry request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming timesheet-entry request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateTimesheetEntry(PPMToolContext context, ImportTimesheetEntryDTO request)
        {
            var errors = new List<string>();

            var hasUsername = !string.IsNullOrWhiteSpace(request.Username);
            var hasPersonId = request.PersonId.HasValue;
            if (hasUsername == hasPersonId)
                errors.Add("Exactly one of Username or PersonId must be supplied");
            else if (hasUsername && ImportHelper.FindUserByUsername(context, request.Username!)?.Person == null)
                errors.Add($"Username '{request.Username}' not found, or has no linked Person");
            else if (hasPersonId && personService.GetById(context, request.PersonId!.Value) == null)
                errors.Add($"PersonId {request.PersonId} does not exist");

            if (request.WeekStartDate.DayOfWeek != DayOfWeek.Monday)
                errors.Add($"WeekStartDate '{request.WeekStartDate:yyyy-MM-dd}' is a {request.WeekStartDate.DayOfWeek}, not a Monday -- CapX Timesheets are always Monday-start weeks");
            if (request.WeekStartDate.TimeOfDay != TimeSpan.Zero)
                errors.Add($"WeekStartDate '{request.WeekStartDate:O}' must be a date with no time of day");

            foreach (var (label, hours) in ImportHelper.DayHours(request))
            {
                if (hours < 0) errors.Add($"{label} cannot be negative");
            }

            var hasTaskName = !string.IsNullOrWhiteSpace(request.TaskName);
            if (!hasTaskName)
                errors.Add("TaskName is required");

            var project = FindProjectWithInnateActivity(context, request.ProjectId);
            if (project == null)
                errors.Add($"ProjectId {request.ProjectId} does not exist");
            else if (project.InnateActivity == null)
                errors.Add($"Project {request.ProjectId} ('{project.Name}') has no InnateActivity code -- only projects created via POST /api/projects/add (or otherwise already linked) can receive imported timesheet entries");
            else if (hasTaskName && !project.InnateActivity.Tasks.Any(t => t.TaskName.Trim().Equals(request.TaskName.Trim(), StringComparison.OrdinalIgnoreCase)))
                errors.Add($"TaskName '{request.TaskName}' does not match any InnateCodeTask under project {request.ProjectId}'s InnateActivity ('{project.InnateActivity.ActivityName}'); available: {string.Join(", ", project.InnateActivity.Tasks.Select(t => t.TaskName))}");

            return errors;
        }

        /// <summary>
        /// Creates or updates a timesheet entry for a person, project, task, and week.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming timesheet-entry request.</param>
        /// <returns>Identifier payload for the timesheet and entry creation outcome.</returns>
        public ImportTimesheetResponseDTO CreateOrUpdateTimesheetEntry(PPMToolContext context, ImportTimesheetEntryDTO request)
        {
            var person = !string.IsNullOrWhiteSpace(request.Username)
                ? ImportHelper.FindUserByUsername(context, request.Username)!.Person!
                : personService.GetById(context, request.PersonId!.Value);
            var project = FindProjectWithInnateActivity(context, request.ProjectId)!;
            var task = project.InnateActivity!.Tasks.First(t => t.TaskName.Trim().Equals(request.TaskName.Trim(), StringComparison.OrdinalIgnoreCase));
            var weekStartDate = ImportHelper.AsUnspecifiedKind(request.WeekStartDate);

            var timesheet = context.Timesheets
                .Include(t => t.TimesheetEntries)
                .FirstOrDefault(t => t.Owner.PersonId == person.PersonId && t.StartDate == weekStartDate);

            var timesheetCreated = timesheet == null;
            if (timesheet == null)
            {
                timesheet = new Timesheet
                {
                    Owner = person,
                    StartDate = weekStartDate,
                    Status = TimesheetStatus.Approved,
                    DateStatusChanged = DateTime.Now,
                };
                var timesheetId = timesheetService.Add(context, timesheet);
                if (timesheetId < 0)
                    throw new InvalidOperationException($"TimesheetService.Add returned {timesheetId} (duplicate) despite passing ValidateTimesheetEntry() -- possible race condition");
            }

            var entry = timesheet.TimesheetEntries.FirstOrDefault(e => e.InnateCodeTaskId == task.InnateCodeTaskId);
            var entryCreated = entry == null;
            if (entry == null)
            {
                entry = new TimesheetEntry { Timesheet = timesheet, InnateCodeTask = task };
                timesheetService.AddEntry(context, entry, commitChanges: false);
            }

            entry.MondayHours = request.MondayHours;
            entry.TuesdayHours = request.TuesdayHours;
            entry.WednesdayHours = request.WednesdayHours;
            entry.ThursdayHours = request.ThursdayHours;
            entry.FridayHours = request.FridayHours;
            entry.SaturdayHours = request.SaturdayHours;
            entry.SundayHours = request.SundayHours;
            entry.UpdateTotalHours();
            context.SaveChangesWithRetry();

            return new ImportTimesheetResponseDTO(timesheet.TimesheetId, timesheetCreated, entryCreated, entry.TotalHours);
        }

        /// <summary>
        /// Validates a timesheet-entry update request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming timesheet-entry update request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateTimesheetEntryUpdate(PPMToolContext context, UpdateTimesheetEntryRequestDTO request)
        {
            var errors = new List<string>();

            var entry = FindTimesheetEntryById(context, request.TimesheetEntryId);
            if (entry == null)
            {
                errors.Add($"TimesheetEntryId {request.TimesheetEntryId} does not exist");
                return errors;
            }

            foreach (var (label, hours) in ImportHelper.DayHours(request))
            {
                if (hours.HasValue && hours.Value < 0) errors.Add($"{label} cannot be negative");
            }

            if (request.NewTaskName != null)
            {
                var innateCode = entry.InnateCodeTask.InnateCode;
                var newTask = innateCode.Tasks.FirstOrDefault(t => t.TaskName.Trim().Equals(request.NewTaskName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (newTask == null)
                    errors.Add($"NewTaskName '{request.NewTaskName}' does not match any InnateCodeTask under this entry's InnateActivity ('{innateCode.ActivityName}'); available: {string.Join(", ", innateCode.Tasks.Select(t => t.TaskName))}");
                else if (newTask.InnateCodeTaskId != entry.InnateCodeTaskId
                         && entry.Timesheet.TimesheetEntries.Any(e => e.InnateCodeTaskId == newTask.InnateCodeTaskId && e.TimesheetEntryId != entry.TimesheetEntryId))
                    errors.Add($"Timesheet {entry.TimesheetId} already has a separate entry for task '{newTask.TaskName}' -- can't move this entry there too");
            }

            return errors;
        }

        /// <summary>
        /// Updates an existing timesheet entry.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming timesheet-entry update request.</param>
        /// <returns>Identifier payload for the updated timesheet entry.</returns>
        public UpdateTimesheetEntryResponseDTO UpdateTimesheetEntry(PPMToolContext context, UpdateTimesheetEntryRequestDTO request)
        {
            var entry = FindTimesheetEntryById(context, request.TimesheetEntryId)!;

            if (request.NewTaskName != null)
                entry.InnateCodeTask = entry.InnateCodeTask.InnateCode.Tasks.First(t => t.TaskName.Trim().Equals(request.NewTaskName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (request.MondayHours.HasValue) entry.MondayHours = request.MondayHours.Value;
            if (request.TuesdayHours.HasValue) entry.TuesdayHours = request.TuesdayHours.Value;
            if (request.WednesdayHours.HasValue) entry.WednesdayHours = request.WednesdayHours.Value;
            if (request.ThursdayHours.HasValue) entry.ThursdayHours = request.ThursdayHours.Value;
            if (request.FridayHours.HasValue) entry.FridayHours = request.FridayHours.Value;
            if (request.SaturdayHours.HasValue) entry.SaturdayHours = request.SaturdayHours.Value;
            if (request.SundayHours.HasValue) entry.SundayHours = request.SundayHours.Value;
            entry.UpdateTotalHours();

            timesheetService.UpdateEntry(context, entry, commitChanges: false);
            context.SaveChangesWithRetry();

            return new UpdateTimesheetEntryResponseDTO(entry.TimesheetEntryId, entry.TotalHours);
        }

        /// <summary>
        /// Finds a project by ID with innate activity and tasks loaded.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="projectId">Project identifier.</param>
        /// <returns>The matching project, or <c>null</c> when none exists.</returns>
        private static Project? FindProjectWithInnateActivity(PPMToolContext context, int projectId) =>
            context.Projects
                .Include(p => p.InnateActivity)
                    .ThenInclude(a => a!.Tasks)
                .FirstOrDefault(p => p.ProjectId == projectId);

        /// <summary>
        /// Finds a timesheet entry by ID with related timesheet and task data loaded.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="timesheetEntryId">Timesheet-entry identifier.</param>
        /// <returns>The matching timesheet entry, or <c>null</c> when none exists.</returns>
        private static TimesheetEntry? FindTimesheetEntryById(PPMToolContext context, int timesheetEntryId) =>
            context.TimesheetEntries
                .Include(e => e.Timesheet)
                    .ThenInclude(t => t.TimesheetEntries)
                .Include(e => e.InnateCodeTask)
                    .ThenInclude(t => t.InnateCode)
                        .ThenInclude(c => c.Tasks)
                .FirstOrDefault(e => e.TimesheetEntryId == timesheetEntryId);
    }
}
