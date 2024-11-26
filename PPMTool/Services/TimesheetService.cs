using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class TimesheetService : BaseEntityService<Timesheet>
    {
        /// <summary>
        /// Adds a timesheet. If duplicate found does not add but returns false.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timesheetModel"></param>
        /// <returns>-1 if a duplicate</returns>
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
        /// Duplicate determined by owner and timesheet start date
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timesheetModel"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, Timesheet timesheetModel)
        {
            return context.Timesheets.Any(t => t.Owner == timesheetModel.Owner && timesheetModel.StartDate != t.StartDate);
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
        /// Update an existing Timesheet
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
        /// Gets all the Timesheets with related data
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<Timesheet> GetMyTimesheets(PPMToolContext context, Person user)
        {
            return context.Timesheets
                .Where(t => t.Owner.PersonId == user.PersonId)
                .ToList();
        }

        /// <summary>
        /// Gets all the Timesheets for direct reports
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<Timesheet> GetMyStaffTimesheets(PPMToolContext context, Person user)
        {
            return context.Timesheets
                .Where(t => user.PeopleManaged.Any(p => p.PersonId == t.Owner.PersonId))
                .ToList();
        }

        /// <summary>
        /// Gets all the Timesheets with related data
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<Timesheet> GetAll(PPMToolContext context)
        {
            return context.Timesheets
                .Include(t => t.TimesheetEntries)
                .ToList();
        }

        /// <summary>
        /// Gets just the Timesheet table entities
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<Timesheet> GetAllShallow(PPMToolContext context)
        {
            return context.Timesheets;
        }


        /// <summary>
        /// Delete the Timesheet from the database.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timesheetModel"></param>
        public override void Delete(PPMToolContext context, Timesheet timesheetModel, bool commitChanges = true)
        {
            context.Timesheets.Remove(timesheetModel);
            if (commitChanges) context.SaveChanges();
        }
    }
}
