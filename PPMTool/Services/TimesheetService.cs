// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using FluentDateTime;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class TimesheetService : BaseEntityService<Timesheet>
    {
        /// <summary>
        /// Whether the active user has any timesheet actions to take
        /// </summary>
        public bool HasOwnTimesheetActions { get; private set; }

        /// <summary>
        /// Whether the active user has any staff timesheet actions to take
        /// </summary>
        public bool HasStaffTimesheetActions { get; private set; }

        /// <summary>
        /// Adds a timesheet. If duplicate found does not add but returns -1 otherwise returns ID of added timesheet.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timesheetModel"></param>
        /// <returns>-1 if a duplicate or the id of the timesheet</returns>
        public override int Add(PPMToolContext context, Timesheet timesheetmodel, bool commitChanges = true)
        {
            if (DuplicateDetected(context, timesheetmodel))
            {
                return -1;
            }

            context.Timesheets.Add(timesheetmodel);
            if (commitChanges) CommitChanges(context);
            return timesheetmodel.TimesheetId;
        }

        /// <summary>
        /// Duplicate if same owner and timesheet start date but not itself
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timesheetModel"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, Timesheet timesheetModel)
        {
            return context.Timesheets.Any(t => t.Owner.PersonId == timesheetModel.Owner.PersonId && timesheetModel.StartDate == t.StartDate && timesheetModel.TimesheetId != t.TimesheetId);
        }

        /// <summary>
        /// Get timesheet by its ID
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timesheetID"></param>
        /// <returns></returns>
        internal Timesheet GetById(PPMToolContext context, int? timesheetId)
        {
            return GetAll(context)
                .FirstOrDefault(t => t.TimesheetId == timesheetId);
        }

        /// <summary>
        /// Update an existing timesheet and returns the ID of the updated timesheet
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timesheetModel"></param>
        /// <returns>-1 if a duplicate</returns>
        public override int Update(PPMToolContext context, Timesheet timesheetModel, bool commitChanges = true)
        {
            if (DuplicateDetected(context, timesheetModel))
            {
                return -1;
            }

            context.Timesheets.Update(timesheetModel);
            if (commitChanges) CommitChanges(context);
            return timesheetModel.TimesheetId;
        }

        /// <summary>
        /// Gets all the timesheets with related data
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<Timesheet> GetMyTimesheets(PPMToolContext context, Person user)
        {
            return context.Timesheets
                .Where(t => t.Owner.PersonId == user.PersonId);
        }

        /// <summary>
        /// Gets all the timesheets with related data
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<Timesheet> GetAll(PPMToolContext context)
        {
            return context.Timesheets
                .Include(t => t.Owner)
                .Include(t => t.TimesheetEntries)
                .ThenInclude(x => x.InnateCodeTask)
                .ThenInclude(x => x.InnateCode);
        }

        /// <summary>
        /// Delete the timesheet from the database
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timesheetModel"></param>
        public override void Delete(PPMToolContext context, Timesheet timesheetModel, bool commitChanges = true)
        {
            context.Timesheets.Remove(timesheetModel);
            if (commitChanges) CommitChanges(context);
        }

        /// <summary>
        /// Returns the start date of the next timesheet for the user.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public DateTime GetNextTimesheetStartDateForUser(PPMToolContext context, Person owner)
        {
            var lastTimesheet = context.Timesheets
                .Where(t => t.Owner.PersonId == owner.PersonId)
                .OrderByDescending(t => t.StartDate)
                .FirstOrDefault();
            return lastTimesheet?.StartDate.AddDays(7).Date ?? owner.StartDate.Date.FirstDayOfWeek();
        }

        /// <summary>
        /// Method to add a timesheet entry
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entry"></param>
        /// <param name="commitChanges"></param>
        /// <returns></returns>
        public int AddEntry(PPMToolContext context, TimesheetEntry entry, bool commitChanges = true)
        {
            context.TimesheetEntries.Add(entry);
            if (commitChanges) CommitChanges(context);
            return entry.TimesheetEntryId;
        }

        /// <summary>
        /// Method to update an existing timesheet entry
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entry"></param>
        /// <param name="commitChanges"></param>
        /// <returns></returns>
        public int UpdateEntry(PPMToolContext context, TimesheetEntry entry, bool commitChanges = true)
        {
            context.TimesheetEntries.Update(entry);
            if (commitChanges) CommitChanges(context);
            return entry.TimesheetEntryId;
        }

        /// <summary>
        /// Method to delete a timesheet entry
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entry"></param>
        /// <param name="commitChanges"></param>
        public void DeleteEntry(PPMToolContext context, TimesheetEntry entry, bool commitChanges = true)
        {
            context.TimesheetEntries.Remove(entry);
            if (commitChanges) CommitChanges(context);
        }

        /// <summary>
        /// Gets all the timesheets with entries and tasks owned by person and in range [startRange, endRange)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="person"></param>
        /// <param name="startRange"></param>
        /// <param name="endRange"></param>
        /// <returns></returns>
        public IEnumerable<Timesheet> GetAllTimesheetsForPersonInDateRange(PPMToolContext context, Person person, DateTime startRange, DateTime endRange)
        {
            return context.Timesheets
                .Include(t => t.TimesheetEntries)
                .ThenInclude(x => x.InnateCodeTask)
                .Where(x => x.Owner.PersonId == person.PersonId && x.StartDate >= startRange && x.StartDate <= endRange);
        }

        /// <summary>
        /// Method to remove a task from a person's timesheet template
        /// </summary>
        /// <param name="person"></param>
        public List<int> GetTemplate(Person person)
        {
            var templateData = person.TimesheetTemplateData?.Split('|');
            var templateTimesheetTasks = new List<int>();
            if (templateData != null && templateData.All(x => !string.IsNullOrWhiteSpace(x)))
            {
                templateTimesheetTasks = templateData.Select(int.Parse).ToList();
            }
            return templateTimesheetTasks;
        }

        /// <summary>
        /// Method to add a task to a person's timesheet template
        /// </summary>
        /// <param name="context"></param>
        /// <param name="person"></param>
        /// <param name="task"></param>
        public void AddToTemplate(PPMToolContext context, Person person, InnateCodeTask task)
        {
            var templateTimesheetTasks = GetTemplate(person);

            // If not already in the template then add it to the start and update the person record
            if (!templateTimesheetTasks.Contains(task.InnateCodeTaskId))
            {
                templateTimesheetTasks.Add(task.InnateCodeTaskId);
                string updatedTemplateDetails = string.Join("|", templateTimesheetTasks);
                person.TimesheetTemplateData = updatedTemplateDetails;
                context.People.Update(person);
                CommitChanges(context);
            }
        }

        /// <summary>
        /// Method to removes a task from the person's timesheet template
        /// </summary>
        /// <param name="context"></param>
        /// <param name="person"></param>
        /// <param name="task"></param>
        public void DeleteFromTemplate(PPMToolContext context, Person person, InnateCodeTask task)
        {
            var templateTimesheetTasks = GetTemplate(person);

            // If it is in the list then remove it and update the person record
            if (templateTimesheetTasks.Contains(task.InnateCodeTaskId))
            {
                templateTimesheetTasks.Remove(task.InnateCodeTaskId);
                string updatedTemplateDetails = string.Join("|", templateTimesheetTasks);
                person.TimesheetTemplateData = updatedTemplateDetails;
                context.People.Update(person);
                CommitChanges(context);
            }
        }

        /// <summary>
        /// Sets up a new timesheet using the person's template
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timesheet"></param>
        /// <param name="person"></param>
        /// <param name="tasks"></param>
        public void SetupTimesheetFromTemplate(PPMToolContext context, Timesheet timesheet, Person person, IEnumerable<InnateCodeTask> tasks)
        {
            var templateTimesheetTasks = GetTemplate(person);

            foreach (int taskId in templateTimesheetTasks)
            {
                InnateCodeTask task = tasks.FirstOrDefault(x => x.InnateCodeTaskId == taskId);

                // If task is no-longer in the DB or if the task is no-longer associated with an active code then remove from template
                if (task == null || !task.InnateCode.IsActive)
                {
                    // Remove from template
                    if (task != null) DeleteFromTemplate(context, person, task);

                    Debug.WriteLine($"** Removing task from template as no longer in DB or code is inactive: {task?.GetSensibleObjectName()}");
                }
                else
                {
                    // Add entry to timesheet
                    TimesheetEntry entry = new TimesheetEntry();
                    entry.InnateCodeTask = task;
                    timesheet.TimesheetEntries.Add(entry);

                    Debug.WriteLine($"** Adding new task to the timesheet : {task.InnateCode.GetSensibleObjectName()} : {task.GetSensibleObjectName()}");
                }
            }
            CommitChanges(context);
        }

        /// <summary>
        /// Returns all timesheets (including owner and entries) where activity code for at least one entry matches the one supplied
        /// </summary>
        /// <param name="context"></param>
        /// <param name="innateActivity"></param>
        /// <returns></returns>
        internal IEnumerable<Timesheet> GetAllForInnateCode(PPMToolContext context, InnateCode innateActivity)
        {
            if (innateActivity == null) return new List<Timesheet>();

            return context.Timesheets
                .Include(x => x.TimesheetEntries)
                .ThenInclude(x => x.InnateCodeTask)
                .ThenInclude(x => x.InnateCode)
                .Where(x => x.TimesheetEntries
                    .Any(x => x.InnateCodeTask.InnateCode.InnateCodeId == innateActivity.InnateCodeId)
                );
        }

        /// <summary>
        /// Gets details of the total number of rejected timesheet number (for self)
        /// and submitted timesheets (for direct reports).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="activeUserId"></param>
        /// <returns></returns>
        public async Task<int> GetIssueCountAsync(PPMToolContext context, int activeUserId)
        {
            Debug.WriteLine("** Updating timesheet notification count");
            HasOwnTimesheetActions = false;
            HasStaffTimesheetActions = false;

            int selfNotificationsCount = 0;
            int staffNotificationsCount = 0;

            // Get user's rejected timesheet numbers
            selfNotificationsCount += await context.Timesheets
                .Include(x => x.Owner)
                .Where(x => x.Owner.PersonId == activeUserId && x.Status == Enums.TimesheetStatus.Rejected)
                .CountAsync();
            HasOwnTimesheetActions = selfNotificationsCount > 0;

            // Get line managed staff numbers (submitted timesheets)
            var peopleManaged = context.People
                .Where(x => x.LineManager != null && x.LineManager.PersonId == activeUserId);
            if (await peopleManaged.CountAsync() > 0)
            {
                foreach (Person p in peopleManaged)
                {
                    staffNotificationsCount += await context.Timesheets
                        .Include(x => x.Owner)
                        .Where(x => x.Owner.PersonId == p.PersonId && x.Status == Enums.TimesheetStatus.Submitted)
                        .CountAsync();
                }
            }
            HasStaffTimesheetActions = staffNotificationsCount > 0;

            return selfNotificationsCount + staffNotificationsCount;
        }

        /// <summary>
        /// Gets a timesheet for a specific person and week start date
        /// </summary>
        /// <param name="context"></param>
        /// <param name="personId"></param>
        /// <param name="weekStart"></param>
        /// <returns></returns>
        internal async Task<Timesheet> GetTimesheetForPersonAndWeekAsync(PPMToolContext context, int? personId, DateTime weekStart)
        {
            return await context.Timesheets.FirstOrDefaultAsync(x => x.OwnerId == personId && x.StartDate.Date == weekStart.Date);
        }

        /// <summary>
        /// Gets the entries for a specific timesheet
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timesheetId"></param>
        /// <returns></returns>
        internal async Task<List<TimesheetEntry>> GetEntriesForTimesheetAsync(PPMToolContext context, int timesheetId)
        {
            return await context.TimesheetEntries
                .Where(x => x.TimesheetId == timesheetId)
                .Include(x => x.InnateCodeTask)
                .ThenInclude(x => x.InnateCode)
                .ToListAsync();
        }
    }
}
