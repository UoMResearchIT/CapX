// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Data.Helpers
{
    public abstract class ExportHelper
    {
        /// <summary>
        /// Converts the subtasks of the projects provided, or the subtasks provided, into assingment chunk representation.
        /// </summary>
        /// <param name="person">The person whose assignments should be considered</param>
        /// <param name="projectsInWindow">The projects in the window to be considered</param>
        /// <param name="finrefs">Financial references to use</param>
        /// <param name="startDate">Window start date. If not provided, uses earliest project start.</param>
        /// <param name="endDate">Window end date. If not provided, uses latest project end.</param>
        /// <param name="tasksInWindow">The tasks in the window for assginments to be extract. If not provided, extracts subtasks from the projects in the window.</param>
        /// <param name="shouldCalculateCosts">If false the chunks will use the cost values already attached to the resources. If true, the mid-grade cost calculator will be used.</param>
        /// <param name="budgetDetails">An optional dictionary of information about the budget status of each resource assignment that can be added to the data if supplied and matched.</param>
        /// <param name="generateLeadershipTasks">Should the process generate leadership tasks for projects</param>
        /// <returns></returns>
        internal static IEnumerable<AssignmentChunk> GetAssignmentChunks(
            Person person,
            IEnumerable<Project> projectsInWindow,
            IEnumerable<FinancialReference> finrefs,
            DateTime? startDate = null,
            DateTime? endDate = null,
            IEnumerable<SubTask> tasksInWindow = null,
            bool shouldCalculateCosts = false,
            IDictionary<string, AssignmentBudgetDetail> budgetDetails = null,
            GenerateLeadershipTaskLogic generateLeadershipTasks = GenerateLeadershipTaskLogic.CostModel)
        {
            // New list
            var data = new List<AssignmentChunk>();

            // Check dates and infer from projects if not specified
            if (startDate == null)
            {
                startDate = projectsInWindow.Min(x => x.StartDate);
            }
            if (endDate == null)
            {
                endDate = projectsInWindow.Max(x => x.EndDate);
            }

            // Check tasks and infer from projects if not specified
            List<SubTask> tempTasksInWindow = null;
            if (tasksInWindow == null)
            {
                // If there are no subtasks or resources then just replace with empty enumerables
                tempTasksInWindow = projectsInWindow
                    .SelectMany(x => x.SubTasks ?? Enumerable.Empty<SubTask>())
                    .Where(x => (x.AssignedResources ?? Enumerable.Empty<Resource>())
                        .Any(r => r.Person.PersonId == person.PersonId))
                    .Where(x => x.IsWithin(startDate ?? default, endDate ?? default))
                    .ToList();
            }
            else
            {
                tempTasksInWindow = tasksInWindow.ToList();
            }

            // Insert leadership assignments as subtasks with a special subtaskId so we can identify them later if required
            if (generateLeadershipTasks != GenerateLeadershipTaskLogic.None)
            {
                foreach (var project in projectsInWindow.Where(x => x.ProjectManager?.PersonId == person.PersonId))
                {
                    if (generateLeadershipTasks == GenerateLeadershipTaskLogic.Always ||
                        project.CostModel.HasLeadership())
                    {
                        tempTasksInWindow.AddRange(project.GenerateLeadershipTasks()
                            .Where(x => x.IsWithin(startDate ?? default, endDate ?? default)));
                    }
                }
            }

            // Get WLM changes for this person that take place during the window
            var wlms = person.WorkloadModelChanges
                .Where(x => x.ChangeDate >= startDate && x.ChangeDate <= endDate)
                .OrderByDescending(x => x.ChangeDate).ToList();

            // Get WLM in force on the first day of the window or set to default G6
            WorkloadModelChange defaultWLM = person.GetWorkloadModelOnDateOrDefault(startDate ?? default);

            // If there isn't a WLM change on the first day of the window then add the default to the list to complete it
            if (wlms.FirstOrDefault(x => x.ChangeDate == person.StartDate) == null)
            {
                // Add the start WLM to the list of WLMs active in the window
                wlms.Add(defaultWLM);
            }

            // Are there any changes in grade for this person?
            var changesInGrade = wlms.DistinctBy(x => x.Grade).Count() > 1;

            // Are there any changes in financial year in the window?
            var startFY = FinancialReference.GetFinancialYear(startDate ?? default);
            var endFY = FinancialReference.GetFinancialYear(endDate ?? default);
            var changesInFinancialYear = startFY != endFY;

            // Each assignment is at least one row of the report
            foreach (var task in tempTasksInWindow)
            {
                // Get project
                var project = projectsInWindow.First(x => x.ProjectId == task.OwningProject?.ProjectId);
                Debug.WriteLine($"** {project.GetFullName()} => {task.Name} being examined...");

                // Get resource that matches the person
                var resource = task.AssignedResources.First(x => x.Person.PersonId == person.PersonId);

                // Generate the resource key
                var resKey = resource.GenerateUniqueResourceKey();

                // Get funding source info
                var fundingSource = resource.FundedFrom;

                // Proportion the data for the task based on the window
                var adjustedTaskStart = task.StartDate.Date < startDate ? startDate ?? default : task.StartDate;
                var adjustedTaskEnd = task.EndDate.Date > endDate ? endDate ?? default : task.EndDate;
                var daysOfTaskForChunk = adjustedTaskEnd.Subtract(adjustedTaskStart).TotalDays + 1;
                var lengthOfTask = task.EndDate.Subtract(task.StartDate).TotalDays + 1;
                var proportionOfTask = lengthOfTask <= 0 ? 0 : daysOfTaskForChunk / lengthOfTask;

                // If budget infomation provided then use to populate chunk
                var amountCovered = 0d;
                var budgetStatus = BudgetStatus.NotInBudget.GetDescription();
                AssignmentBudgetDetail budgetLine = null;
                if (budgetDetails != null)
                {
                    // Find the budget line associated with this chunk
                    budgetLine = budgetDetails.ContainsKey(resKey) ? budgetDetails[resKey] : null;

                    // Update the values to be assigned to the initial chunk
                    budgetLine?.GetBudgetDetailsForWindow(adjustedTaskStart, adjustedTaskEnd, out budgetStatus, out amountCovered);
                }

                // Create a line representing the full, un-chunked task to start off with
                var initialChunk = new AssignmentChunk(resKey)
                {
                    EmployeeName = person.Name,
                    Grade = defaultWLM.Grade,
                    FTE = resource.AssignmentFTE,
                    BilledFTE = resource.BilledFTE,
                    ProjectName = project.GetFullName(),
                    LeadRSE = project.ProjectManager?.Name ?? "Unknown",
                    Faculty = project.Faculty.GetDescription(),
                    School = project.School.GetDescription(),
                    PI = project.PI,
                    TaskName = task.Name,
                    StartDate = adjustedTaskStart,
                    EndDate = adjustedTaskEnd,
                    FinancialYear = FinancialReference.GetFinancialYear(adjustedTaskStart),
                    PlannedCost = resource.PlannedCost * proportionOfTask,
                    AccountCode = string.IsNullOrWhiteSpace(fundingSource?.AccountCode) ? "Unknown" : fundingSource?.AccountCode,
                    FundingSourceType = string.IsNullOrWhiteSpace(fundingSource?.FundingSourceType.GetDescription()) ? "Unknown" : fundingSource?.FundingSourceType.GetDescription(),
                    FundingSourceDescription = string.IsNullOrWhiteSpace(fundingSource?.Description) ? "None" : fundingSource?.Description,
                    AmountCovered = amountCovered,
                    BudgetStatus = budgetStatus,
                    IsLeadershipAssignment = task.SubTaskId < 0
                };
                IList<AssignmentChunk> taskChunks = new List<AssignmentChunk>()
                {
                    initialChunk
                };

                // Are there any changes to grade for this person
                // Ignore grade changes for leadership task resources
                if (changesInGrade && task.SubTaskId > 0)
                {
                    var tempChunks = new List<AssignmentChunk>();

                    // Find changes that are within the chunk
                    var changes = wlms.Where(x => x.ChangeDate > initialChunk.StartDate && x.ChangeDate <= initialChunk.EndDate).OrderBy(x => x.ChangeDate);
                    var lengthOfInitialChunk = initialChunk.EndDate.Subtract(initialChunk.StartDate).TotalDays + 1;

                    foreach (var change in changes)
                    {
                        // Get previous WLM by looking at day before change
                        var wlmBefore = person.GetWorkloadModelOnDateOrDefault(change.ChangeDate.AddDays(-1));

                        // Define a new task chunk for before period if necessary
                        if (wlmBefore.Grade != change.Grade)
                        {
                            var startDateOfNewChunk = tempChunks.Count > 0 ?
                                new DateTime(tempChunks.Last().EndDate.AddDays(1).Ticks) :
                                new DateTime(initialChunk.StartDate.Ticks);

                            var endDateOfNewChunk = change.ChangeDate.AddDays(-1);

                            var lengthOfNewChunk = endDateOfNewChunk.Subtract(startDateOfNewChunk).TotalDays + 1;
                            var proportionOfInitialChunk = lengthOfNewChunk / lengthOfInitialChunk;
                            if (proportionOfInitialChunk > 1)
                            {
                                proportionOfInitialChunk = 1;
                            }

                            // Update the budget details
                            budgetLine?.GetBudgetDetailsForWindow(startDateOfNewChunk, endDateOfNewChunk, out budgetStatus, out amountCovered);

                            // Add chunk
                            tempChunks.Add(new AssignmentChunk(initialChunk)
                            {
                                StartDate = startDateOfNewChunk,
                                EndDate = endDateOfNewChunk,
                                PlannedCost = initialChunk.PlannedCost * proportionOfInitialChunk,
                                AmountCovered = amountCovered,
                                BudgetStatus = budgetStatus
                            });
                        }
                    }

                    // If we did a split then need to add the final task chunk
                    var remainingCosts = initialChunk.PlannedCost - tempChunks.Sum(x => x.PlannedCost);
                    if (tempChunks.Count > 0)
                    {
                        // Dates
                        var finalChunkStart = new DateTime(tempChunks.Last().EndDate.AddDays(1).Ticks).Date;
                        var finalChunkEnd = new DateTime(initialChunk.EndDate.Ticks).Date;
                        var lengthOfFinalChunk = finalChunkEnd.Subtract(finalChunkStart).TotalDays + 1;

                        // Update the budget details
                        budgetLine?.GetBudgetDetailsForWindow(finalChunkStart, finalChunkEnd, out budgetStatus, out amountCovered);

                        // Add chunk
                        tempChunks.Add(new AssignmentChunk(initialChunk)
                        {
                            StartDate = finalChunkStart,
                            EndDate = finalChunkEnd,
                            PlannedCost = remainingCosts > 0 ? remainingCosts : 0,
                            AmountCovered = amountCovered,
                            BudgetStatus = budgetStatus
                        });
                    }

                    // Replace the list with the new list of chunks
                    if (tempChunks.Count > 0) taskChunks = tempChunks;
                }

                Debug.WriteLine($"** {project.GetFullName()} => {task.Name} | {taskChunks.Count} chunks after Grade splitting");

                // Are there any financial year changes within the window
                if (changesInFinancialYear)
                {
                    var tempChunks = new List<AssignmentChunk>();

                    // Loop over chunks and see if a financial year change lands in the middle
                    foreach (var chunk in taskChunks)
                    {
                        // Get financial year the chunk starts and finishes in
                        var fyStart = FinancialReference.GetFinancialYear(chunk.StartDate);
                        var fyEnd = FinancialReference.GetFinancialYear(chunk.EndDate);

                        // If the task crosses financial years then we need to split it
                        if (fyStart != fyEnd)
                        {
                            var lengthOfInitialChunk = chunk.EndDate.Subtract(chunk.StartDate).TotalDays + 1;

                            // For each financial year falling within the task, add chunks
                            for (var i = fyStart; i <= fyEnd; i++)
                            {
                                // Get start and end dates of the chunk
                                var startDateOfNewChunk =
                                    fyStart == i ?
                                    new DateTime(chunk.StartDate.Ticks) :
                                    new DateTime(i, 8, 1);

                                var endDateOfNewChunk =
                                    fyEnd == i ?
                                    new DateTime(chunk.EndDate.Ticks) :
                                    new DateTime(i + 1, 7, 31);

                                var lengthOfNewChunk = endDateOfNewChunk.Subtract(startDateOfNewChunk).TotalDays + 1;
                                var proportionOfInitialChunk = lengthOfNewChunk / lengthOfInitialChunk;
                                if (proportionOfInitialChunk > 1)
                                {
                                    proportionOfInitialChunk = 1;
                                }

                                // Update the budget details
                                budgetLine?.GetBudgetDetailsForWindow(startDateOfNewChunk, endDateOfNewChunk, out budgetStatus, out amountCovered);

                                tempChunks.Add(new AssignmentChunk(chunk)
                                {
                                    StartDate = startDateOfNewChunk,
                                    EndDate = endDateOfNewChunk,
                                    FinancialYear = i,
                                    PlannedCost = chunk.PlannedCost * proportionOfInitialChunk,
                                    AmountCovered = amountCovered,
                                    BudgetStatus = budgetStatus
                                });
                            }
                        }
                        else
                        {
                            tempChunks.Add(chunk);
                        }
                    }

                    // Replace the list with the new list of chunks
                    if (tempChunks.Count > 0) taskChunks = tempChunks;
                }

                Debug.WriteLine($"** {project.GetFullName()} => {task.Name} | {taskChunks.Count} chunks after FY splitting");

                // Filter task chunk list to just those that intersect the window
                taskChunks = taskChunks.Where(x => x.StartDate <= endDate && x.EndDate >= startDate).ToList();

                Debug.WriteLine($"** {project.GetFullName()} => {task.Name} | {taskChunks.Count} chunks run during the window");

                // Add task to master list
                data.AddRange(taskChunks);
            }

            // Add the mid-grade salary estimates and overwrite the planned costs if necessary
            foreach (var chunk in data)
            {
                // Cost estimate based on mid-grade salaries
                chunk.UpdateEstimatedSalaryCost(finrefs, shouldCalculateCosts);
            }

            return data;
        }

        /// <summary>
        /// Represents the summary over the window of the target and recovery for a person
        /// </summary>
        internal class RecoveryDataOverWindow
        {
            public string Name { get; }

            public float Target { get; private set; }

            public float TargetCosts { get; private set; }

            public float Recovered { get; private set; }

            public float RecoveredCosts { get; private set; }

            public float RecoveredIncLeadership { get; private set; }

            public float RecoveredIncLeadershipCosts { get; private set; }

            public float NetCapped { get; private set; }

            public float NetCappedCosts { get; private set; }

            public float NetCappedIncLead { get; private set; }

            public float NetCappedIncLeadCosts { get; private set; }

            public float PersonCosts { get; private set; }

            public float InBudgetCosts { get; private set; }

            public RecoveryDataOverWindow(string name)
            {
                Name = name;
            }

            /// <summary>
            /// Update the values based on the FTE for the day
            /// </summary>
            /// <param name="targetFTE"></param>
            /// <param name="assignedFTE"></param>
            /// <param name="assignedIncLeadFTE"></param>
            /// <param name="maxCap"></param>
            /// <param name="actualCosts">Costs of the person on the day (zero if left or not start)</param>
            /// <param name="costs">Annual cost / 365 (non-zero even if left or not started)</param>
            /// <param name="inBudget">Amount considered in budget for recovery</param>
            public void Update(
                float targetFTE,
                float assignedFTE,
                float assignedIncLeadFTE,
                float maxCap,
                float actualCosts,
                float costs,
                float inBudget)
            {
                Target += targetFTE;
                TargetCosts += targetFTE * costs;
                Recovered += assignedFTE;
                RecoveredCosts += assignedFTE * costs;
                RecoveredIncLeadership += assignedIncLeadFTE;
                RecoveredIncLeadershipCosts += assignedIncLeadFTE * costs;

                var net = assignedFTE - targetFTE;
                var netCapped = net > maxCap ? maxCap : net;
                NetCapped += netCapped;
                NetCappedCosts += netCapped * costs;

                net = assignedIncLeadFTE - targetFTE;
                netCapped = net > maxCap ? maxCap : net;
                NetCappedIncLead += netCapped;
                NetCappedIncLeadCosts += netCapped * costs;

                PersonCosts += actualCosts;
                InBudgetCosts += inBudget;
            }

            /// <summary>
            /// Returns the FTE of the target for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageTarget(int daysInWindow)
            {
                return Target / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the target FTE for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageTargetCosts()
            {
                return TargetCosts;
            }

            /// <summary>
            /// Returns the FTE of the recovered for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageRecovered(int daysInWindow)
            {
                return Recovered / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the recovered FTE for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageRecoveredCosts()
            {
                return RecoveredCosts;
            }

            /// <summary>
            /// Returns the FTE of the recovered including leadership for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageRecoveredIncLeadership(int daysInWindow)
            {
                return RecoveredIncLeadership / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the recovered including leadership FTE for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageRecoveredIncLeadershipCosts()
            {
                return RecoveredIncLeadershipCosts;
            }

            /// <summary>
            /// Returns the FTE of the capped net for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageNetCapped(int daysInWindow)
            {
                return NetCapped / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the capped net FTE for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageNetCappedCosts()
            {
                return NetCappedCosts;
            }

            /// <summary>
            /// Returns the FTE of the capped net including leadership for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageNetCappedIncLeadership(int daysInWindow)
            {
                return NetCappedIncLead / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the capped net including leadership FTE for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageNetCappedIncLeadershipCosts()
            {
                return NetCappedIncLeadCosts;
            }

            /// <summary>
            /// Gets the costs over the window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetEstimatedCosts()
            {
                return PersonCosts;
            }

            /// <summary>
            /// The total budget amounts as accumlated over the days of the window
            /// </summary>
            /// <returns></returns>
            public float GetInBudgetTotals()
            {
                return InBudgetCosts;
            }
        }

        /// <summary>
        /// Method to generate the recovery information for the report
        /// </summary>
        /// <param name="peopleActive">List of people to be considered</param>
        /// <param name="assignmentChunks">Set of assignment chunks for active people split by WLM change and FY change</param>
        /// <param name="contextFactory"></param>
        /// <param name="personService"></param>
        /// <param name="projectService"></param>
        /// <param name="financialReferenceService"></param>
        /// <returns></returns>
        internal static IEnumerable<RecoveryDataOverWindow> GetRecoveryData(
            IEnumerable<Person> peopleActive,
            IList<AssignmentChunk> assignmentChunks,
            IDbContextFactory<PPMToolContext> contextFactory,
            PersonService personService,
            ProjectService projectService,
            FinancialReferenceService financialReferenceService)
        {
            // Set the report length
            var startDate = assignmentChunks.Min(x => x.StartDate.Date).Date;
            var endDate = assignmentChunks.Max(x => x.EndDate.Date).Date;
            var currentDate = startDate;
            var windowRecoveryData = new List<RecoveryDataOverWindow>();

            // Create a context to be accessed on this thread
            using (var context = contextFactory.CreateDbContext())
            {
                // Get data for each person active in the window
                // Get projects active in the window with their subtasks and resources
                var projectsInWindow = projectService.GetAll(context)
                    .Where(x => !x.ProjectStatus.IsCancelled())
                    .Where(x => x.IsWithin(startDate, endDate));

                // Get fin refs
                var currentFY = FinancialReference.GetFinancialYear(startDate);
                var finref = financialReferenceService.GetFinancialReferenceForDate(context, startDate);

                // Initialise the totals
                foreach (var person in peopleActive)
                {
                    windowRecoveryData.Add(new RecoveryDataOverWindow(person.Name));
                }

                // Loop over the days to get day by day data
                while (currentDate <= endDate)
                {
                    // If the FY has changed then update the finref
                    if (FinancialReference.GetFinancialYear(currentDate) != currentFY)
                    {
                        finref = financialReferenceService.GetFinancialReferenceForDate(context, currentDate);
                    }

                    // Loop over each person employed in the window
                    foreach (var person in peopleActive)
                    {
                        // Get chunks belonging to this person and running on the day
                        var chunks = assignmentChunks
                            .Where(x => x.EmployeeName == person.Name && DateRange.IsWithin(currentDate, x.StartDate, x.EndDate));

                        // Get the project work amount on the day
                        var projectWorkTargetFTE = person.GetProjectWorkAvailabilityOnDate(currentDate);
                        var wlmTotal = person.GetWorkloadModelTotalOnDate(currentDate);
                        var gradeOnDay = person.GetGradeOnDate(currentDate);

                        // Get day costs for person based on mid-grade and scaled by any part-time arrangement
                        var actualCostsOnDay = gradeOnDay == null ? 0 : finref.GetMidGradeCosts(gradeOnDay ?? 6);
                        actualCostsOnDay /= 365.0;
                        actualCostsOnDay *= wlmTotal;

                        // If we don't have any costs for the day then need to compute them from first or last grade we know about
                        var referenceCostsForADay = actualCostsOnDay;
                        if (actualCostsOnDay == 0)
                        {
                            WorkloadModelChange wlm = null;
                            // If the grade is null then find the last WLM before the date
                            if (person.StartDate > currentDate)
                            {
                                // Get first WLM after the date
                                wlm = person.GetFirstWorkloadModelAfter(currentDate);

                            }
                            else if (person.EndDate != null && person.EndDate < currentDate)
                            {
                                // Get last WLM before the date
                                wlm = person.GetLastWorkloadModelBefore(currentDate);
                            }
                            referenceCostsForADay = finref.GetMidGradeCosts(wlm?.Grade ?? 6) / 365.0;
                        }

                        // Get the sum of their assignments on the day with and without leadership
                        var projectAssignmentsFTE = chunks
                            .Where(x => !x.IsLeadershipAssignment)
                            .Sum(x => x.BilledFTE);
                        var projectAssignmentsFTEIncLeadership = chunks
                            .Sum(x => x.BilledFTE);

                        // Net value
                        var netValue = projectAssignmentsFTE - projectWorkTargetFTE;
                        var netValueIncLeadership = projectAssignmentsFTEIncLeadership - projectWorkTargetFTE;

                        // Net value capped
                        var maxOverAllocation = wlmTotal - projectWorkTargetFTE;
                        if (maxOverAllocation < 0) maxOverAllocation = 0;
                        var netValueCapped = netValue > maxOverAllocation ? maxOverAllocation : netValue;
                        var netValueCappedIncLeadership = netValueIncLeadership > maxOverAllocation ? maxOverAllocation : netValueIncLeadership;

                        // Amount in budget for the day across all chunks
                        var inBudget = chunks.Sum(x => x.AmountCovered / (x.EndDate.Subtract(x.StartDate).TotalDays + 1));

                        // Update the totals based on this day
                        windowRecoveryData
                            .First(x => x.Name == person.Name)
                            .Update(
                                (float)projectWorkTargetFTE,
                                (float)projectAssignmentsFTE,
                                (float)projectAssignmentsFTEIncLeadership,
                                (float)maxOverAllocation,
                                (float)actualCostsOnDay,
                                (float)referenceCostsForADay,
                                (float)inBudget
                            );
                    }

                    // Advance the day
                    currentDate = currentDate.AddDays(1);
                }
            }

            return windowRecoveryData;
        }

        /// <summary>
        /// Represents the summary of the financial state of a project over an export period
        /// </summary>
        internal class ProjectBudgetSummary
        {
            public string ProjectName { get; set; }

            public double PlannedCosts { get; set; }

            public double RecoveredCosts { get; set; }
        }
    }
}
