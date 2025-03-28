using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class InnateCodeService : BaseEntityService<InnateCode>
    {
        /// <summary>
        /// Will not add a duplicate but return -1 instead. If successfully added, will return new ID of saved entity.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override int Add(PPMToolContext context, InnateCode entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                // Duplicate found!
                return -1;
            }

            context.InnateCodes.Add(entity);
            if (commitChanges) context.SaveChanges();
            return entity.InnateCodeId;
        }

        public override bool DuplicateDetected(PPMToolContext context, InnateCode entity)
        {
            // Duplicate detected if the name or the code are the same as another or if any of the tasks within the
            // code have the same name as another
            var all = GetAll(context);
            var duplicatesNameOfAnother = all
            .Any(x => (x.ActivityName.Trim().ToLower() == entity.ActivityName.Trim().ToLower() &&
                x.ActivityCode.Trim().ToLower() == entity.ActivityCode.Trim().ToLower())
                && x.InnateCodeId != entity.InnateCodeId);
            var duplicatesTasks = entity.Tasks.DistinctBy(x => x.TaskName.Trim().ToLower()).Count() != entity.Tasks.Count;
            return duplicatesNameOfAnother || duplicatesTasks;
        }

        public override void Delete(PPMToolContext context, InnateCode entity, bool commitChanges = true)
        {
            // Remove tasks so they are not orphaned
            var tasks = context.InnateCodeTasks.Where(x => x.InnateCode.InnateCodeId == entity.InnateCodeId);
            context.InnateCodeTasks.RemoveRange(tasks);
            context.InnateCodes.Remove(entity);
            if (commitChanges) context.SaveChanges();
        }

        public override IEnumerable<InnateCode> GetAll(PPMToolContext context)
        {
            return context.InnateCodes
                .OrderBy(x => x.ActivityCode)
                .Include(x => x.Tasks);
        }

        public override int Update(PPMToolContext context, InnateCode entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                // Duplicate found!
                return -1;
            }
            context.InnateCodes.Update(entity);
            if (commitChanges) context.SaveChanges();
            return entity.InnateCodeId;
        }

        internal InnateCode GetById(PPMToolContext context, int innateCodeId)
        {
            return GetAll(context).FirstOrDefault(x => x.InnateCodeId == innateCodeId);
        }

        /// <summary>
        /// Looks up a activity and task combination by name and determines the duty it is categorised by.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="activity"></param>
        /// <param name="task"></param>
        /// <returns>Duty as int or -1 if not match found</returns>
        internal int FindDutyForTask(PPMToolContext context, string activity, string task)
        {
            var activityToMatch = activity.Trim().ToLower();
            var taskToMatch = task.Trim().ToLower();
            var splitActivityParams = activityToMatch.Split(" - ", 2);
            if (splitActivityParams.Length < 2) return -1;
            var matchAct = context.InnateCodes.FirstOrDefault(x => x.ActivityCode.Trim().ToLower() == splitActivityParams[0].Trim().ToLower() && x.ActivityName.Trim().ToLower() == splitActivityParams[1].Trim().ToLower());
            if (matchAct != null)
            {
                var matchTask = context.InnateCodeTasks.FirstOrDefault(x => x.InnateCode == matchAct && x.TaskName.Trim().ToLower() == task.Trim().ToLower());
                if (matchTask != null)
                {
                    return (int)matchTask.Duty;
                }
            }
            return -1;
        }

        /// <summary>
        /// Get the active timesheet codes
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal IEnumerable<InnateCode> GetActive(PPMToolContext context)
        {
            return context.InnateCodes
                .Where(x => x.IsActive)
                .Include(x => x.Tasks)
                .OrderBy(x => x.ActivityCode);
        }

        /// <summary>
        /// Return all tasks including their parent Innate Code entity 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal IEnumerable<InnateCodeTask> GetAllTasks(PPMToolContext context)
        {
            return context.InnateCodeTasks
                .Include(x => x.InnateCode);
        }

        /// <summary>
        /// Gets details of the timesheet codes which could be marked
        /// as inactive as they are not associated with projects that are still active
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<InnateCode> GetCodesToDeactivate(PPMToolContext context)
        {
            // Get codes which are active, contain "rtp" (project codes) and 
            // which are not associated with any currently active projects
            var unusedTimesheetCodes = context.InnateCodes
                .Where(ic => ic.IsActive && ic.ActivityCode.ToLower().Contains("rtp"))
                .Where(ic => !context.Projects.Any(p => p.InnateActivity.InnateCodeId == ic.InnateCodeId && (int)p.ProjectStatus < 7));

            // PHB : Need to possibly do more work to weed out only those which won't have further time logged to them -
            // i.e. the RSEs using them have up-to-date timesheets so shouldn't need them any more

            return unusedTimesheetCodes;
        }
    }
}
