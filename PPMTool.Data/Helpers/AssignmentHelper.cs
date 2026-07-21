// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Data.Helpers
{
    /// <summary>
    /// Helper methods for manipulating tasks into assignment DTOs for reporting purposes. This includes chunking tasks based on changes in grade or financial year and proportioning costs accordingly.
    /// </summary>
    public class AssignmentHelper
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
        /// <param name="shouldCalculateCosts">If false the chunks will use the cost values already attached to the resources. If true, the mid-grade cost calculator will be used to estimate the cost of the chunk and overwrite anything stored.</param>
        /// <param name="budgetDetails">An optional dictionary of information about the budget status of each resource assignment that can be added to the data if supplied and matched.</param>
        /// <returns></returns>
        public static IEnumerable<AssignmentChunk> GetAssignmentChunks(
            Person person,
            IEnumerable<Project> projectsInWindow,
            IEnumerable<FinancialReference> finrefs,
            DateTime? startDate = null,
            DateTime? endDate = null,
            IEnumerable<SubTask>? tasksInWindow = null,
            bool shouldCalculateCosts = false,
            IDictionary<string, AssignmentBudgetDetail>? budgetDetails = null)
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
            List<SubTask>? tempTasksInWindow = null;
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
                Debug.WriteLine($"** Project {project.RTP} => {task.Name} being examined...");

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
                AssignmentBudgetDetail? budgetLine = null;
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
                    ProjectId = project.RTP,
                    ProjectName = project.Name,
                    LeadRSE = project.ProjectManager?.Name ?? "Unknown",
                    UpperOrgUnit = project.School?.Faculty?.Name ?? "Unknown",
                    LowerOrgUnit = project.School?.Name ?? "Unknown",
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
                    AssignmentType = task.TaskDuty.GetDescription()
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

                Debug.WriteLine($"** Project {project.RTP} => {task.Name} | {taskChunks.Count} chunks after Grade splitting");

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

                Debug.WriteLine($"** Project {project.RTP} => {task.Name} | {taskChunks.Count} chunks after FY splitting");

                // Filter task chunk list to just those that intersect the window
                taskChunks = taskChunks.Where(x => x.StartDate <= endDate && x.EndDate >= startDate).ToList();

                Debug.WriteLine($"** Project {project.RTP} => {task.Name} | {taskChunks.Count} chunks run during the window");

                // Add task to master list
                data.AddRange(taskChunks);
            }

            // Add the mid-grade salary estimates and overwrite the planned costs if necessary
            foreach (var chunk in data)
            {
                // Cost estimate based on mid-grade salaries
                chunk.RecomputeChunkCosts(finrefs, shouldCalculateCosts);
            }

            return data;
        }
    }
}
