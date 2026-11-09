// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Models;

namespace PPMTool.Services
{
    public class SubTaskService : BaseEntityService<SubTask>
    {
        public SubTaskService(ILogger<SubTaskService> logger) : base(logger)
        {
        }

        /// <inheritdoc />
        public override int Add(PPMToolContext context, SubTask taskModel, bool commitChanges = true)
        {
            context.SubTasks.Add(taskModel);
            if (commitChanges) CommitChanges(context);
            return taskModel.SubTaskId;
        }

        /// <inheritdoc />
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
                error = followerTask.Schedule();

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
        /// <param name="commitChanges"></param>
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
            // Leadership tasks have a fixed name
            if (taskModel.TaskDuty == Duty.ProjectAndServiceMgmt) return true;

            // Check other tasks
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

            // Add the project
            clone.OwningProject = taskToClone.OwningProject;

            // Create new resources
            clone.AssignedResources.Clear();
            foreach (var res in taskToClone.AssignedResources)
            {
                clone.AssignedResources.Add(new Resource
                {
                    AssignmentFTE = res.AssignmentFTE,
                    BilledFTE = res.BilledFTE,
                    DayRate = res.DayRate,
                    IsProvisional = res.IsProvisional,
                    Person = res.Person,
                    UseProjectDayRate = res.UseProjectDayRate,
                    FundedFrom = res.FundedFrom
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

        /// <summary>
        /// Filters the given list of people to just those with assignments that match the duties given
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cachedPeople"></param>
        /// <param name="duties"></param>
        /// <returns></returns>
        internal IEnumerable<Person> GetPeopleWithAssignmentsWithDuty(PPMToolContext context, IEnumerable<Person> cachedPeople, Duty[] duties)
        {
            // Find all subtasks that belong to that duty
            var subtasks = context.SubTasks
                .Include(x => x.AssignedResources)
                    .ThenInclude(x => x.Person)
                .Where(x => duties.Contains(x.TaskDuty))
                .ToList();

            // Get the resources people on them
            var people = subtasks
                .SelectMany(st => st.AssignedResources)
                .Select(ar => ar.Person.PersonId)
                .Distinct();

            // Filter the list
            return cachedPeople.Where(p => people.Contains(p.PersonId));
        }

        /// <summary>
        /// Finds the first resource assigned to the specified subtask whose overlapping
        /// billable assignments exceed their applicable contractual FTE.
        /// </summary>
        /// <param name="context">Context to use for the operation.</param>
        /// <param name="subTaskId">Subtask whose assigned resources should be checked.</param>
        /// <returns>The first overallocation found, or <see langword="null"/> when all resources fit within capacity.</returns>
        public ResourceOverallocation GetFirstResourceBillableOverallocation(PPMToolContext context, int subTaskId)
        {
            // Get the task that matches the passed ID and its resources
            var task = context.SubTasks
                .AsNoTracking()
                .Include(x => x.AssignedResources)
                    .ThenInclude(x => x.Person)
                .SingleOrDefault(x => x.SubTaskId == subTaskId);

            // No task to query
            if (task == null)
            {
                return null;
            }

            // Loop over the resources and construct
            foreach (var resource in task.AssignedResources.OrderBy(x => x.Person.Name))
            {
                // See if this resource has any billable overallocation vs contractual FTE within the task's start and end dates
                var overallocation = GetResourceBillableOverallocation(
                    context,
                    resource.Person.PersonId,
                    task.StartDate,
                    task.EndDate);

                if (overallocation != null)
                {
                    // Just return the first one found
                    return overallocation;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the first day in the specified date range where a person's billable assigned FTE exceeds their
        /// contractual FTE.
        /// </summary>
        /// <remarks>Only assignments with a positive billed FTE are considered. Comparison uses a small
        /// tolerance to avoid false positives from floating-point precision.</remarks>
        /// <param name="context">Database context used to load the person, workload model changes, and billable resource assignments.</param>
        /// <param name="personId">Identifier of the person to evaluate.</param>
        /// <param name="startDate">Start of the evaluation period, inclusive.</param>
        /// <param name="endDate">End of the evaluation period, inclusive.</param>
        /// <returns>A <c>ResourceOverallocation</c> describing the first detected billable overallocation; otherwise, <see langword="null"/>.</returns>
        private static ResourceOverallocation GetResourceBillableOverallocation(
            PPMToolContext context,
            int personId,
            DateTime startDate,
            DateTime endDate)
        {
            // Some tolerance to allow for FP inaccuracy
            const double tolerance = 0.00001;

            // Get the person corresponding to the ID passed and their WLM changes
            var person = context.People
                .AsNoTracking()
                .Include(x => x.WorkloadModelChanges)
                .Single(x => x.PersonId == personId);

            var assignments = context.Resources
                .AsNoTracking()
                .Include(x => x.SubTask)
                    .ThenInclude(x => x.OwningProject)
                .Where(x =>
                    x.Person.PersonId == personId &&
                    x.BilledFTE > 0 &&
                    x.SubTask.StartDate <= endDate &&
                    x.SubTask.EndDate >= startDate)
                .ToList();

            var firstDate = startDate.Date;
            var lastDate = endDate.Date;

            // March through the date range
            for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
            {
                // Get the WLM active on the day
                var workloadModel = person.GetWorkloadModelOnDate(date);

                // If no WLM then can't do anything
                if (workloadModel == null)
                {
                    continue;
                }

                // Sum up the assignments
                var assignedFte = assignments
                    .Where(x =>
                        x.SubTask.StartDate.Date <= date &&
                        x.SubTask.EndDate.Date >= date)
                    .Sum(x => x.BilledFTE);

                // Get the contractual FTE from the WLM
                var contractualFte = workloadModel.Total();

                // If a genuine overallocation (i.e. inability to recharge this much work) then return model
                if (assignedFte > contractualFte + tolerance)
                {
                    return new ResourceOverallocation(
                        person.PersonId,
                        person.Name,
                        date,
                        assignedFte,
                        contractualFte);
                }
            }

            return null;
        }
    }
}
