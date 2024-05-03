using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class SubTaskService : BaseService<SubTask>
    {
        /// <summary>
        /// Adds a subtask
        /// </summary>
        /// <param name="context"></param>
        /// <param name="taskModel"></param>
        /// <returns></returns>
        public override int Add(PPMToolContext context, SubTask taskModel)
        {
            context.SubTasks.Add(taskModel);
            context.SaveChanges();
            return taskModel.SubTaskId;
        }

        /// <summary>
        /// Update an existing subtask
        /// </summary>
        /// <param name="context"></param>
        /// <param name="taskModel"></param>
        public override void Update(PPMToolContext context, SubTask taskModel)
        {
            context.SubTasks.Update(taskModel);
            context.SaveChanges();
        }

        /// <summary>
        /// Gets all the subtasks
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<SubTask> GetAll(PPMToolContext context)
        {
            return context.SubTasks
                .Include(s => s.AssignedResources)
                .ToList();
        }

        /// <summary>
        /// Method to take a list of tasks for a project, and a predecessor subtask and forward propagate scheduling.
        /// Returns null if successful otherwise returns the name of the failed task and its error.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="projectTasks"></param>
        /// <returns></returns>
        internal Tuple<string, string> UpdateFollowerTasks(SubTask task, IEnumerable<SubTask> projectTasks)
        {
            string error;
            var followerTasks = projectTasks.Where(x => x.Predecessor == task);
            foreach (var followerTask in followerTasks)
            {
                // Call schedule and have it tracked in the context
                error = followerTask.Schedule(true);

                // If error then abandon forward propagation
                if (error != null) return new Tuple<string, string>(followerTask.Name, error);

                // Recurse into the next layer
                var result = UpdateFollowerTasks(followerTask, projectTasks);

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
        public override void Delete(PPMToolContext context, SubTask subTask)
        {
            // Remove resources
            foreach (var res in subTask.AssignedResources)
            {
                context.Resources.Remove(res);
            }

            // Remove sub task
            context.SubTasks.Remove(subTask);

            // Update database
            context.SaveChanges();
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
    }
}
