// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

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
            if (commitChanges) CommitChanges(context);
            return entity.InnateCodeId;
        }

        /// <summary>
        /// Duplicate detected if the name or the code are the same as another 
        /// or if any of the tasks within the code have the same name as another task on the code
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, InnateCode entity)
        {
            var all = GetAll(context);
            var duplicatesNameOfAnother = all
                .Any(x =>
                (x.ActivityName.Trim().ToLower() == entity.ActivityName.Trim().ToLower() ||
                x.ActivityCode.Trim().ToLower() == entity.ActivityCode.Trim().ToLower()) &&
                x.InnateCodeId != entity.InnateCodeId);
            var duplicatesTasks = entity.Tasks.DistinctBy(x => x.TaskName.Trim().ToLower()).Count() != entity.Tasks.Count;
            return duplicatesNameOfAnother || duplicatesTasks;
        }

        public override void Delete(PPMToolContext context, InnateCode entity, bool commitChanges = true)
        {
            // Remove tasks so they are not orphaned
            var tasks = context.InnateCodeTasks.Where(x => x.InnateCode.InnateCodeId == entity.InnateCodeId);
            context.InnateCodeTasks.RemoveRange(tasks);
            context.InnateCodes.Remove(entity);
            if (commitChanges) CommitChanges(context);
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
            if (commitChanges) CommitChanges(context);
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
        public async Task<IEnumerable<CodeToDeactivate>> GetCodesToDeactivateAsync(PPMToolContext context)
        {
            // Get codes which are active, contain "S-RES-" (i.e. project codes) and 
            // which are not associated with any currently active projects. If finished, then 
            // only include if the end date of the associated project is more than 4 weeks ago.
            // This is to allow people to submit final timesheets against recently finished projects.
            var codes = await context.InnateCodes
                .Where(ic => ic.IsActive && ic.ActivityCode.ToLower().Contains("s-res-") &&
                    context.Projects.Any(p =>

                        // Project timesheet code matches
                        p.InnateActivity.InnateCodeId == ic.InnateCodeId &&

                        // Project is cancelled or finished more than 28 days ago
                        ((int)p.ProjectStatus > (int)ProjectStatus.Finished ||
                        (p.ProjectStatus == ProjectStatus.Finished && p.EndDate <= DateTime.UtcNow.AddDays(-28)))
                    )
                )
                .ToListAsync();

            // Map the codes to CodeToDeactivate objects
            IList<CodeToDeactivate> codesToDeactivate = new List<CodeToDeactivate>();
            foreach (var code in codes)
            {
                var projects = await context.Projects
                    .Include(p => p.ProjectManager)
                    .Include(p => p.InnateActivity)
                    .Where(p => p.InnateActivity.InnateCodeId == code.InnateCodeId)
                    .ToListAsync();
                var pmNames = projects.Select(x => x.ProjectManager == null ? "Not Set" : x.ProjectManager.Name);
                var obj = new CodeToDeactivate(code, pmNames, projects.Select(x => x.RTP));
                codesToDeactivate.Add(obj);
            }
            return codesToDeactivate;
        }

        /// <summary>
        /// Object to represent information about a code to be deactivated
        /// </summary>
        public class CodeToDeactivate
        {
            public int InnateCodeId { get; }
            public string ActivityCode { get; }
            public string ActivityName { get; }
            public IEnumerable<string> ProjectManagerNames { get; }
            public IEnumerable<int> ProjectRTP { get; }

            public CodeToDeactivate(InnateCode code, IEnumerable<string> projectManagerNames, IEnumerable<int> projectRTP)
            {
                InnateCodeId = code.InnateCodeId;
                ActivityCode = code.ActivityCode;
                ActivityName = code.ActivityName;
                ProjectManagerNames = projectManagerNames;
                ProjectRTP = projectRTP;
            }

            /// <summary>
            /// Method to return the strings as links to the projects
            /// </summary>
            /// <param name="configuration"></param>
            /// <returns></returns>
            public MarkupString GetRTPAsMarkup(IConfiguration configuration)
            {
                if (ProjectRTP != null && ProjectRTP.Count() > 0)
                {
                    var temp = new List<string>();
                    foreach (var rtp in ProjectRTP)
                    {
                        temp.Add($"<a href=\"{configuration["Authentication:HostUrl"]}/projects/projectdetails?rtp={rtp}\">RTP-{rtp}</a>");
                    }
                    return (MarkupString)string.Join("<br />", temp);
                }
                return (MarkupString)"<span>None</span>";
            }

            /// <summary>
            /// Method to format the project manager names
            /// </summary>
            /// <returns></returns>
            public MarkupString GetProjectManagerNamesAsMarkup()
            {
                if (ProjectManagerNames != null && ProjectManagerNames.Count() > 0)
                {
                    var temp = new List<string>();
                    foreach (var name in ProjectManagerNames)
                    {
                        temp.Add($"<span>{name ?? "Not Set"}</span>");
                    }
                    return (MarkupString)string.Join("<br />", temp);
                }
                return (MarkupString)"<span>None</span>";
            }
        }
    }
}
