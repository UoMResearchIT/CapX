using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentDateTime;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class TimesheetService : BaseEntityService<Timesheet>
    {
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
            if (commitChanges) context.SaveChanges();
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
            if (commitChanges) context.SaveChanges();
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
        /// Gets all the timesheets for direct reports
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<Timesheet> GetMyStaffTimesheets(PPMToolContext context, Person user)
        {
            return context.Timesheets
                .Where(t => user.PeopleManaged.Any(p => p.PersonId == t.Owner.PersonId));
        }

        /// <summary>
        /// Gets all the timesheets with related data
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<Timesheet> GetAll(PPMToolContext context)
        {
            return context.Timesheets
                .Include(t => t.TimesheetEntries)
                .ThenInclude(x => x.InnateCodeTask)
                .ThenInclude(x => x.InnateCode);
        }

        /// <summary>
        /// Gets just the timesheet table entities
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<Timesheet> GetAllShallow(PPMToolContext context)
        {
            return context.Timesheets;
        }

        /// <summary>
        /// Delete the timesheet from the database
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timesheetModel"></param>
        public override void Delete(PPMToolContext context, Timesheet timesheetModel, bool commitChanges = true)
        {
            context.Timesheets.Remove(timesheetModel);
            if (commitChanges) context.SaveChanges();
        }

        /// <summary>
        /// Returns the start date of the next timesheet for the user.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        internal DateTime GetNextTimesheetStartDateForUser(PPMToolContext context, Person owner)
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
            if (commitChanges) context.SaveChanges();
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
            if (commitChanges) context.SaveChanges();
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
            if (commitChanges) context.SaveChanges();
        }

        /// <summary>
        /// Gets all the timesheets with entries and tasks owned by person and in range [startRange, endRange)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="person"></param>
        /// <param name="startRange"></param>
        /// <param name="endRange"></param>
        /// <returns></returns>
        internal IEnumerable<Timesheet> GetAllTimesheetsForPersonInDateRange(PPMToolContext context, Person person, DateTime startRange, DateTime endRange)
        {
            return context.Timesheets
                .Include(t => t.TimesheetEntries)
                .ThenInclude(x => x.InnateCodeTask)
                .Where(x => x.Owner.PersonId == person.PersonId && x.StartDate >= startRange && x.StartDate < endRange);
        }

        /// <summary>
        /// Method to remove a task from a user's timesheet template
        /// </summary>
        /// <param name="context"></param>
        /// <param name="role"></param>
        public List<int> GetTemplate(PPMToolContext context, Role role)
        {
            return role.TimesheetTemplateData?.Split(':')?.Select(Int32.Parse)?.ToList();
        }

        /// <summary>
        /// Method to remove a task from a user's timesheet template
        /// </summary>
        /// <param name="context"></param>
        /// <param name="role"></param>
        /// <param name="task"></param>
        public void UpdateTemplate(PPMToolContext context, Role role, InnateCodeTask task)
        {
            var templateTimesheetTasks = role.TimesheetTemplateData?.Split(':')?.Select(Int32.Parse)?.ToList();

            if(templateTimesheetTasks.Contains(task.InnateCodeTaskId)) 
            {
                templateTimesheetTasks.Remove(task.InnateCodeTaskId);
            }
            else
            {
                templateTimesheetTasks.Add(task.InnateCodeTaskId);
            }

            string updatedTemplateDetails = string.Join(":", templateTimesheetTasks);
            role.TimesheetTemplateData = updatedTemplateDetails;
            context.Roles.Update(role);
            context.SaveChanges();
        }

        /// <summary>
        /// Sets up a new timesheet using the user's template
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timesheet"></param>
        /// <param name="role"></param>
        /// <param name="tasks"></param>
        public void SetupTimesheetFromTemplate(PPMToolContext context, Timesheet timesheet, Role role, IEnumerable<InnateCodeTask> tasks)
        {
            var templateTimesheetTasks = GetTemplate(context, role);

            foreach (int taskId in templateTimesheetTasks)
            {
                try
                {
                    InnateCodeTask task = tasks.Where(x => x.InnateCodeTaskId == taskId).FirstOrDefault();
                    TimesheetEntry entry = new TimesheetEntry();
                    entry.InnateCodeTask = task;

                    timesheet.TimesheetEntries.Add(entry);

                    Debug.WriteLine($"Adding new task to the timesheet : {task.InnateCode.GetSensibleObjectName()} : {task.GetSensibleObjectName()}");
                }
                catch (Exception ex) { Debug.WriteLine(ex); }
            }
            context.SaveChanges();
        }
    }
}
