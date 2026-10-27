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
        /// <param name="finrefs">Financial references to use for cost estimates</param>
        /// <param name="startDate">Window start date. If not provided, uses earliest project start.</param>
        /// <param name="endDate">Window end date. If not provided, uses latest project end.</param>
        /// <param name="tasksInWindow">The tasks in the window for assginments to be extract. If not provided, extracts subtasks from the projects in the window.</param>
        /// <param name="shouldCalculateCosts">If false the chunks will use the cost values already attached to the resources. If true, costs are recomputed from financial reference values using the associated cost key and overwrite anything stored.</param>
        /// <returns></returns>
        public static IEnumerable<AssignmentChunk> GetAssignmentChunks(
            Person person,
            IEnumerable<Project> projectsInWindow,
            IEnumerable<FinancialReference>? finrefs = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            IEnumerable<SubTask>? tasksInWindow = null,
            bool shouldCalculateCosts = false)
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
            List<SubTask> tempTasksInWindow;
            if (tasksInWindow == null)
            {
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
                .OrderBy(x => x.ChangeDate)
                .ToList();

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
                var adjustedTaskStart = task.StartDate.Date < startDate
                        ? startDate ?? default
                        : task.StartDate;

                var adjustedTaskEnd = task.EndDate.Date > endDate
                        ? endDate ?? default
                        : task.EndDate;

                var daysOfTaskForChunk = adjustedTaskEnd.Subtract(adjustedTaskStart).TotalDays + 1;
                var lengthOfTask = task.EndDate.Subtract(task.StartDate).TotalDays + 1;
                var proportionOfTask = lengthOfTask <= 0
                        ? 0
                        : daysOfTaskForChunk / lengthOfTask;

                // Get the workload model in force when this task begins
                var taskStartWLM = person.GetWorkloadModelOnDate(adjustedTaskStart) ?? new WorkloadModelChange();

                // Create a line representing the full, un-chunked task
                var initialChunk = new AssignmentChunk(resKey)
                {
                    EmployeeName = person.Name,
                    Grade = taskStartWLM.Grade,
                    CostValueSetId = taskStartWLM.CostValueSetId,
                    CostValueName = taskStartWLM.CostValueSet?.Name,
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
                    AccountCode = string.IsNullOrWhiteSpace(fundingSource?.AccountCode)
                        ? "Unknown"
                        : fundingSource.AccountCode,
                    FundingSourceType = string.IsNullOrWhiteSpace(fundingSource?.FundingSourceType.GetDescription())
                            ? "Unknown"
                            : fundingSource.FundingSourceType.GetDescription(),
                    FundingSourceDescription = string.IsNullOrWhiteSpace(fundingSource?.Description)
                            ? "None"
                            : fundingSource.Description,
                    AssignmentType = task.TaskDuty.GetDescription()
                };

                IList<AssignmentChunk> taskChunks = new List<AssignmentChunk>
                {
                    initialChunk
                };

                // Split at every WLM change within the assignment
                var changes = wlms
                    .Where(x =>
                        x.ChangeDate > initialChunk.StartDate &&
                        x.ChangeDate <= initialChunk.EndDate)
                    .OrderBy(x => x.ChangeDate)
                    .ToList();

                if (changes.Count > 0)
                {
                    var tempChunks = new List<AssignmentChunk>();

                    var lengthOfInitialChunk = initialChunk.EndDate.Subtract(initialChunk.StartDate).TotalDays + 1;
                    var chunkStart = initialChunk.StartDate.Date;

                    foreach (var change in changes)
                    {
                        var chunkEnd = change.ChangeDate.Date.AddDays(-1);

                        if (chunkEnd >= chunkStart)
                        {
                            var chunkWlm = person.GetWorkloadModelOnDate(chunkStart) ?? new WorkloadModelChange();
                            var lengthOfNewChunk = chunkEnd.Subtract(chunkStart).TotalDays + 1;
                            var proportionOfInitialChunk = lengthOfNewChunk / lengthOfInitialChunk;

                            if (proportionOfInitialChunk > 1)
                            {
                                proportionOfInitialChunk = 1;
                            }

                            tempChunks.Add(new AssignmentChunk(initialChunk)
                            {
                                Grade = chunkWlm.Grade,
                                CostValueSetId = chunkWlm.CostValueSetId,
                                CostValueName = chunkWlm.CostValueSet?.Name,
                                StartDate = chunkStart,
                                EndDate = chunkEnd,
                                PlannedCost = initialChunk.PlannedCost * proportionOfInitialChunk
                            });
                        }

                        // The changed WLM comes into force on this date
                        chunkStart = change.ChangeDate.Date;
                    }

                    // Add the final chunk from the last WLM change to the end
                    if (chunkStart <= initialChunk.EndDate.Date)
                    {
                        var chunkWlm = person.GetWorkloadModelOnDate(chunkStart) ?? new WorkloadModelChange();
                        var remainingCosts = initialChunk.PlannedCost - tempChunks.Sum(x => x.PlannedCost);

                        tempChunks.Add(new AssignmentChunk(initialChunk)
                        {
                            Grade = chunkWlm.Grade,
                            CostValueSetId = chunkWlm.CostValueSetId,
                            CostValueName = chunkWlm.CostValueSet?.Name,
                            StartDate = chunkStart,
                            EndDate = initialChunk.EndDate.Date,
                            PlannedCost = remainingCosts > 0 ? remainingCosts : 0
                        });
                    }

                    taskChunks = tempChunks;
                }

                Debug.WriteLine($"** Project {project.RTP} => {task.Name} | {taskChunks.Count} chunks after WLM splitting");

                // Split chunks at financial year boundaries
                if (changesInFinancialYear)
                {
                    var tempChunks = new List<AssignmentChunk>();

                    foreach (var chunk in taskChunks)
                    {
                        var fyStart = FinancialReference.GetFinancialYear(chunk.StartDate);
                        var fyEnd = FinancialReference.GetFinancialYear(chunk.EndDate);

                        if (fyStart != fyEnd)
                        {
                            var lengthOfInitialChunk = chunk.EndDate.Subtract(chunk.StartDate).TotalDays + 1;

                            for (var i = fyStart; i <= fyEnd; i++)
                            {
                                var startDateOfNewChunk = fyStart == i
                                        ? new DateTime(chunk.StartDate.Ticks)
                                        : new DateTime(i, 8, 1);

                                var endDateOfNewChunk = fyEnd == i
                                        ? new DateTime(chunk.EndDate.Ticks)
                                        : new DateTime(i + 1, 7, 31);

                                var lengthOfNewChunk = endDateOfNewChunk.Subtract(startDateOfNewChunk).TotalDays + 1;
                                var proportionOfInitialChunk = lengthOfNewChunk / lengthOfInitialChunk;
                                if (proportionOfInitialChunk > 1)
                                {
                                    proportionOfInitialChunk = 1;
                                }

                                tempChunks.Add(new AssignmentChunk(chunk)
                                {
                                    StartDate = startDateOfNewChunk,
                                    EndDate = endDateOfNewChunk,
                                    FinancialYear = i,
                                    PlannedCost = chunk.PlannedCost * proportionOfInitialChunk
                                });
                            }
                        }
                        else
                        {
                            tempChunks.Add(chunk);
                        }
                    }

                    if (tempChunks.Count > 0)
                    {
                        taskChunks = tempChunks;
                    }
                }

                Debug.WriteLine($"** Project {project.RTP} => {task.Name} | {taskChunks.Count} chunks after FY splitting");

                // Filter task chunk list to just those that intersect the window
                taskChunks = taskChunks
                    .Where(x => x.StartDate <= endDate && x.EndDate >= startDate)
                    .ToList();

                Debug.WriteLine($"** Project {project.RTP} => {task.Name} | {taskChunks.Count} chunks run during the window");

                data.AddRange(taskChunks);
            }

            // Recompute the planned cost using the selected financial reference value
            if (finrefs != null && shouldCalculateCosts)
            {
                foreach (var chunk in data)
                {
                    chunk.RecomputeChunkCosts(finrefs);
                }
            }

            return data;
        }
    }
}
