using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class SubTaskService : BaseEntityService<SubTask>
    {
        /// <summary>
        /// Adds a subtask
        /// </summary>
        /// <param name="context"></param>
        /// <param name="taskModel"></param>
        /// <returns></returns>
        public override int Add(PPMToolContext context, SubTask taskModel, bool commitChanges = true)
        {
            context.SubTasks.Add(taskModel);
            if (commitChanges) CommitChanges(context);
            return taskModel.SubTaskId;
        }

        /// <summary>
        /// Update an existing subtask
        /// </summary>
        /// <param name="context"></param>
        /// <param name="taskModel"></param>
        public override int Update(PPMToolContext context, SubTask taskModel, bool commitChanges = true)
        {
            context.SubTasks.Update(taskModel);
            if (commitChanges) CommitChanges(context);
            return taskModel.SubTaskId;
        }

        /// <summary>
        /// Gets all the subtasks
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<SubTask> GetAll(PPMToolContext context)
        {
            return context.SubTasks
                .Include(s => s.AssignedResources);
        }

        /// <summary>
        /// Returns a flattened list of resources working on a specific project and includes their funding sources
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public IEnumerable<Resource> GetResourcesForProject(PPMToolContext context, int projectId)
        {
            return context.Projects
                .Where(x => x.ProjectId == projectId)
                .Include(x => x.SubTasks)
                    .ThenInclude(x => x.AssignedResources)
                        .ThenInclude(x => x.FundedFrom)
                .SelectMany(x => x.SubTasks)
                .SelectMany(x => x.AssignedResources);
        }

        /// <summary>
        /// Method to take a list of tasks for a project, and a predecessor subtask and forward propagate scheduling.
        /// Returns null if successful otherwise returns the name of the failed task and its error.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="project"></param>
        /// <returns></returns>
        internal Tuple<string, string> ScheduleFollowerTasks(SubTask task, Project project)
        {
            string error;
            var followerTasks = project.SubTasks.Where(x => x.Predecessor == task);
            foreach (var followerTask in followerTasks)
            {
                // Call schedule and have it tracked in the context
                error = followerTask.Schedule(true);

                // If error then abandon forward propagation
                if (error != null) return new Tuple<string, string>(followerTask.Name, error);

                // Recurse into the next layer
                var result = ScheduleFollowerTasks(followerTask, project);

                // If next layer throws an error then pass it back out
                if (result != null) return result;
            }

            // Reached the end so all scheduled OK
            return null;
        }

        /// <summary>
        /// Delete a sub task from the database removing all the resources at the same time.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="subTask"></param>
        public override void Delete(PPMToolContext context, SubTask subTask, bool commitChanges = true)
        {
            foreach (var res in subTask.AssignedResources)
            {
                context.Resources.Remove(res);
            }
            context.SubTasks.Remove(subTask);
            if (commitChanges) CommitChanges(context);
        }

        /// <summary>
        /// Determine whether any existing subtasks in the project model have the same name
        /// </summary>
        /// <param name="projectModel"></param>
        /// <param name="taskModel"></param>
        /// <returns></returns>
        internal bool IsUniqueTaskNameInProject(Project projectModel, SubTask taskModel)
        {
            var subSet = projectModel.SubTasks.Where(x => x.SubTaskId != taskModel.SubTaskId);
            return !subSet.Any(x => x.Name == taskModel.Name);
        }

        /// <summary>
        /// Method to clone an existing task without tracking so it is returned as a new task unknown to EF Core
        /// </summary>
        /// <param name="context"></param>
        /// <param name="taskToClone"></param>
        /// <returns></returns>
        internal SubTask Clone(PPMToolContext context, SubTask taskToClone)
        {
            // Get a copy of the task you want to clone from the DB without tracking
            var clone = context.SubTasks
                .Include(s => s.AssignedResources)
                .Include(s => s.SkillsRequired)
                .AsNoTracking()
                .FirstOrDefault(x => x.SubTaskId == taskToClone.SubTaskId);

            Debug.WriteLine($"** Cloning task {taskToClone.Name}...");

            // Reset ID
            clone.SubTaskId = 0;

            // Create new resources
            clone.AssignedResources.Clear();
            foreach (var res in taskToClone.AssignedResources)
            {
                clone.AssignedResources.Add(new Resource
                {
                    AssignmentFTE = res.AssignmentFTE,
                    DayRate = res.DayRate,
                    IsProvisional = res.IsProvisional,
                    Person = res.Person,
                    UseProjectDayRate = res.UseProjectDayRate,
                    FundedFrom = res.FundedFrom,
                    Rate = res.Rate
                });
            }

            // Refresh the skill tags
            clone.SkillsRequired.Clear();
            foreach (var tag in taskToClone.SkillsRequired)
            {
                clone.SkillsRequired.Add(context.SkillTags.First(x => x.SkillTagId == tag.SkillTagId));
            }

            // Change name
            clone.Name = $"{taskToClone.Name} (Copy)";

            Debug.WriteLine($"** Original task ID: {taskToClone.SubTaskId} | Cloned task ID: {clone.SubTaskId}");

            return clone;
        }

        /// <summary>
        /// Returns a shallow object representing the subtask
        /// </summary>
        /// <param name="context"></param>
        /// <param name="subTaskId"></param>
        /// <returns></returns>
        internal SubTask GetShallowById(PPMToolContext context, int? subTaskId)
        {
            return context.SubTasks.FirstOrDefault(x => x.SubTaskId == subTaskId);
        }

        /// <summary>
        /// Returns the actual work in hours summed over all the subtasks for the given project
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        internal double GetActuals(PPMToolContext context, int projectId)
        {
            return context.Projects
                .Include(x => x.SubTasks)
                .FirstOrDefault(x => x.ProjectId == projectId)
                .SubTasks?
                .RoundedSum(x => x.ActualWorkHours) ?? 0;
        }
    }
}
