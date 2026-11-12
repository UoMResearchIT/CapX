// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Data.Helpers;
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
        /// Finds the first resource assigned to the supplied subtask whose overlapping
        /// BilledFTE assignments exceed their applicable contractual FTE.
        /// </summary>
        /// <remarks>
        /// The supplied subtask is evaluated using its current editable dates and resources.
        /// Persisted assignments belonging to the same subtask are excluded and replaced by
        /// the supplied assignments to avoid double-counting when editing an existing task.
        /// All positive BilledFTE assignments are considered, irrespective of funding source
        /// or recharge eligibility.
        /// </remarks>
        /// <param name="context">Database context used to retrieve assignment and workload data.</param>
        /// <param name="subTask">Subtask whose current dates and assigned resources should be checked.</param>
        /// <returns>The first overallocation found, or <see langword="null"/> when all resources fit within capacity.</returns>
        public ResourceOverallocation GetFirstResourceBillableOverallocation(
            PPMToolContext context,
            SubTask subTask)
        {
            var resourceAssignments = subTask.AssignedResources
                .Where(resource => resource.Person != null && resource.BilledFTE > 0)

                // This is to ensure that any multiple resource rows are summed but this shouldn't happen in practice
                // There should only be a single resource row per person on a task.
                .GroupBy(resource => resource.Person.PersonId)
                .Select(group => new
                {
                    PersonId = group.Key,
                    BilledFTE = group.Sum(resource => resource.BilledFTE)
                })
                .ToList();

            // Loop over the resource assignments on this task
            foreach (var assignment in resourceAssignments)
            {
                var overallocation = GetResourceBillableOverallocation(
                    context,
                    assignment.PersonId,
                    subTask.StartDate,
                    subTask.EndDate,
                    subTask.SubTaskId,
                    assignment.BilledFTE);

                if (overallocation != null)
                {
                    return overallocation;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the first resource assigned to any subtask in the specified project whose
        /// overlapping BilledFTE assignments exceed their applicable contractual FTE while
        /// they are assigned to this project.
        /// </summary>
        /// <remarks>
        /// An overallocation is relevant to this project only when it occurs during the
        /// lifetime of a subtask on this project to which the person is assigned.
        /// The relevant people and assignments are loaded in batches and reused across
        /// merged assignment windows.
        /// </remarks>
        /// <param name="context">Database context used to retrieve assignment and workload data.</param>
        /// <param name="projectId">Project whose assigned resources should be checked.</param>
        /// <returns>The first relevant overallocation found, or <see langword="null"/> when none is found.</returns>
        public ResourceOverallocation GetFirstProjectResourceBillableOverallocation(
            PPMToolContext context,
            int projectId)
        {
            // Get the distinct periods during which each person is actually assigned
            // to a subtask on this project.
            var assignmentWindows = context.Resources
                .AsNoTracking()
                .Where(resource =>
                    resource.BilledFTE > 0 &&
                    resource.SubTask!.OwningProject.ProjectId == projectId)
                .Select(resource => new
                {
                    PersonId = resource.Person.PersonId,
                    resource.SubTask.StartDate,
                    resource.SubTask.EndDate
                })
                .Distinct()
                .ToList();

            if (assignmentWindows.Count == 0)
            {
                return null;
            }

            // Get the list of the distinct person IDs
            var personIds = assignmentWindows
                .Select(window => window.PersonId)
                .Distinct()
                .ToList();

            // Get the first and last dates across all assignment windows to limit the query range
            var firstDate = assignmentWindows.Min(window => window.StartDate);
            var lastDate = assignmentWindows.Max(window => window.EndDate);

            // Load each person and their workload model changes once.
            var people = context.People
                .AsNoTracking()
                .Include(person => person.WorkloadModelChanges)
                .Where(person => personIds.Contains(person.PersonId))
                .ToDictionary(person => person.PersonId);

            // Load all relevant overlapping assignments once.
            var assignments = context.Resources
                .AsNoTracking()
                .Include(resource => resource.Person)
                .Include(resource => resource.SubTask)
                .Where(resource =>
                    personIds.Contains(resource.Person.PersonId) &&
                    resource.BilledFTE > 0 &&
                    resource.SubTask!.StartDate <= lastDate &&
                    resource.SubTask.EndDate >= firstDate)
                .ToList();

            var overallocationResults = new List<ResourceOverallocation>();

            // Group the assignment windows by person and merge overlapping windows for each person.
            foreach (var personWindows in assignmentWindows.GroupBy(window => window.PersonId))
            {
                if (!people.TryGetValue(personWindows.Key, out var person))
                {
                    continue;
                }

                // Merge the assignment windows for this person to avoid redundant checks on overlapping periods.
                var mergedWindows = AssignmentHelper.MergeAssignmentWindows(
                    personWindows.Select(window => (
                        window.StartDate,
                        window.EndDate)));

                // Get the previously loaded assignments for this person.
                var personAssignments = assignments
                    .Where(resource => resource.Person.PersonId == person.PersonId)
                    .ToList();

                // Check each merged window for overallocation
                foreach (var window in mergedWindows)
                {
                    var overallocation = GetResourceBillableOverallocation(
                        person,
                        personAssignments,
                        window.StartDate,
                        window.EndDate);

                    if (overallocation != null)
                    {
                        overallocationResults.Add(overallocation);
                        break;
                    }
                }
            }

            return overallocationResults
                .OrderBy(overallocation => overallocation.Date)
                .FirstOrDefault();
        }

        /// <summary>
        /// Finds the first day in the specified date range where the supplied assignments
        /// exceed the person's applicable contractual FTE.
        /// </summary>
        /// <param name="person">Person being evaluated, including workload model changes.</param>
        /// <param name="assignments">The person's relevant assignments.</param>
        /// <param name="startDate">Start of the evaluation period, inclusive.</param>
        /// <param name="endDate">End of the evaluation period, inclusive.</param>
        /// <param name="additionalBilledFTE">Additional editable BilledFTE to include throughout the period.</param>
        /// <returns>A description of the first overallocation found, or <see langword="null"/>.</returns>
        private static ResourceOverallocation GetResourceBillableOverallocation(
            Person person,
            IReadOnlyCollection<Resource> assignments,
            DateTime startDate,
            DateTime endDate,
            double additionalBilledFTE = 0)
        {
            // Some tolerance to allow for FP inaccuracy
            const double tolerance = 0.00001;

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                // Get the WLM active on the day
                var workloadModel = person.GetWorkloadModelOnDate(date);

                // If no WLM then can't do anything
                if (workloadModel == null)
                {
                    continue;
                }

                // Sum persisted assignments and any supplied editable assignment
                var assignedFte = assignments
                    .Where(resource =>
                        resource.SubTask!.StartDate.Date <= date &&
                        resource.SubTask.EndDate.Date >= date)
                    .Sum(resource => resource.BilledFTE) + additionalBilledFTE;

                // Get the contractual FTE from the WLM
                var contractualFte = workloadModel.Total();

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

        /// <summary>
        /// Calculates billable overallocation for a person over a specified period from persisted resource assignments
        /// and optional additional billed FTE.
        /// </summary>
        /// <remarks>Includes only assignments with billed FTE greater than zero that overlap the
        /// specified date range.</remarks>
        /// <param name="context">Database context used to load the person and matching resource assignments.</param>
        /// <param name="personId">Identifier of the person whose billable overallocation is evaluated.</param>
        /// <param name="startDate">Inclusive start date of the evaluation period.</param>
        /// <param name="endDate">Inclusive end date of the evaluation period.</param>
        /// <param name="excludedSubTaskId">Subtask identifier to exclude from the assignment set, or 0 to include all subtasks.</param>
        /// <param name="additionalBilledFTE">Additional billed FTE applied to the result alongside stored assignments.</param>
        /// <returns>A <see cref="ResourceOverallocation"/> describing the first detected overallocation, or <see langword="null"/> when none is found.</returns>
        private static ResourceOverallocation GetResourceBillableOverallocation(
            PPMToolContext context,
            int personId,
            DateTime startDate,
            DateTime endDate,
            int excludedSubTaskId = 0,
            double additionalBilledFTE = 0)
        {
            // Get the person corresponding to the ID passed and their WLM changes
            var person = context.People
                .AsNoTracking()
                .Include(x => x.WorkloadModelChanges)
                .Single(x => x.PersonId == personId);

            var assignments = context.Resources
                .AsNoTracking()
                .Include(x => x.SubTask)
                .Where(x =>
                    x.Person.PersonId == personId &&
                    x.BilledFTE > 0 &&
                    (excludedSubTaskId <= 0 || x.SubTask!.SubTaskId != excludedSubTaskId) &&
                    x.SubTask!.StartDate <= endDate &&
                    x.SubTask.EndDate >= startDate)
                .ToList();

            return GetResourceBillableOverallocation(
                person,
                assignments,
                startDate,
                endDate,
                additionalBilledFTE);
        }

        /// <summary>
        /// Asynchronously retrieves distinct names of resources assigned to subtasks in the specified project.
        /// </summary>
        /// <param name="context">The data context used to query subtasks and assigned resources.</param>
        /// <param name="projectId">The identifier of the project whose assigned resource names to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of unique resource
        /// names for the project.</returns>
        internal async Task<IEnumerable<string>> GetUniqueResourcesAsync(PPMToolContext context, int projectId)
        {
            return await context.SubTasks
                .Include(x => x.AssignedResources)
                    .ThenInclude(x => x.Person)
                .Where(x => x.OwningProject.ProjectId == projectId)
                .SelectMany(x => x.AssignedResources)
                .Select(x => x.Person.Name)
                .Distinct()
                .ToListAsync();
        }
    }
}
