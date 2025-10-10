using System.Diagnostics;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Data.Helpers
{
    /// <summary>
    /// Helper to process finance data.sources into a form usable by other components
    /// </summary>
    public abstract class FinanceHelper
    {
        /// <summary>
        /// Calculate the funds requested for a project
        /// </summary>
        /// <param name="context"></param>
        /// <param name="resources"></param>
        /// <param name="leadershipSourceId"></param>
        /// <param name="leadershipCosts"></param>
        /// <param name="fundingSources"></param>
        /// <param name="requestedFromInvoices"></param>
        /// <param name="receivedFromPayments"></param>
        /// <returns></returns>
        internal static TransactionBreakdown ComputeTransactionBreakdown(
            PPMToolContext context,
            int leadershipSourceId,
            double leadershipCosts,
            IEnumerable<Resource> resources,
            IEnumerable<FundingSource> fundingSources,
            double requestedFromInvoices,
            double receivedFromPayments)
        {
            // DA is just the sum of the DA funding sources
            var da = fundingSources
                .Where(x => x.FundingSourceType == FundingSourceType.DA)
                .RoundedSum(x => x.AmountAvailable, 2);

            // DI is based on the salary costs and assignments of the resources
            var di = resources
                .Where(x => x.FundedFrom?.FundingSourceType == FundingSourceType.DI)
                .RoundedSum(x => x.PlannedCost, 2);

            // Add to these totals the leadership costs if DI
            var leadershipSource = fundingSources.FirstOrDefault(x => x.FundingSourceId == leadershipSourceId);
            if (leadershipSource != null && leadershipSource.FundingSourceType == FundingSourceType.DI)
            {
                di += leadershipCosts;
            }

            // Create the item adding in the invoiced amounts and the direct payments
            return new TransactionBreakdown(da, di, requestedFromInvoices, receivedFromPayments, fundingSources);
        }

        /// <summary>
        /// Builds a representation of a person's assignments in a window of time across all projects including costs taking into account changes in grade and financial year
        /// </summary>
        /// <param name="person"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="projectsInWindow"></param>
        /// <param name="finrefs"></param>
        /// <returns></returns>
        internal static IEnumerable<AssignmentChunk> GetAssignmentChunksInWindowFromProjects(Person person, DateTime startDate, DateTime endDate, IEnumerable<Project> projectsInWindow, IEnumerable<FinancialReference> finrefs)
        {
            // New list
            var data = new List<AssignmentChunk>();

            // Filter list of tasks for those projects that just run during the window and are assigned to this person
            var tasksInWindow = projectsInWindow
                .SelectMany(x => x.SubTasks)
                .Where(x => x.AssignedResources
                    .Any(x => x.Person.PersonId == person.PersonId)
                )
                .Where(x => x.IsWithin(startDate, endDate))
                .ToList();

            Debug.WriteLine($"** {tasksInWindow.Count} tasks in window for {person.Name}");

            // For each project, convert into chunk representation
            foreach (var project in projectsInWindow)
            {
                data.AddRange(GetAssignmentChunksForPersonOnProject(person, project, finrefs, startDate, endDate));
            }

            Debug.WriteLine($"** Built {data.Count} rows for {person.Name}");
            return data;
        }

        /// <summary>
        /// Builds a representation of a person's assignments on a single project including costs taking into account changes in grade and financial year
        /// </summary>
        /// <param name="person"></param>
        /// <param name="project"></param>
        /// <param name="finrefs"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        internal static IEnumerable<AssignmentChunk> GetAssignmentChunksForPersonOnProject(Person person, Project project, IEnumerable<FinancialReference> finrefs, DateTime? startDate = null, DateTime? endDate = null)
        {
            // New list
            var data = new List<AssignmentChunk>();

            // Check dates
            if (startDate == null)
            {
                startDate = project.StartDate;
            }
            if (endDate == null)
            {
                endDate = project.EndDate;
            }

            // Filter list of tasks for those projects that just run during the window and are assigned to this person
            var tasks = project.SubTasks
                .Where(x => x.AssignedResources
                    .Any(x => x.Person.PersonId == person.PersonId)
                )
                .Where(x => x.IsWithin(startDate ?? default, endDate ?? default))
                .ToList();

            Debug.WriteLine($"** {tasks.Count} tasks in window for {person.Name}");

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

            // Insert leadership assignments as subtasks with a special subtaskId so we can identify them later
            if (project.CostModel == CostModel.TechAndLeadership && project.ProjectManager?.PersonId == person.PersonId)
            {
                // Find leadership tasks within the window and convert to actual tasks
                var dateRanges = project.GetLeadershipTaskRanges();
                foreach (var dateRange in dateRanges.Where(x => x.IsWithin(startDate ?? default, endDate ?? default)))
                {
                    // Add leadership subtask based on the date range
                    DateTime leadershipStart = dateRange.StartDate.Date < startDate ? startDate ?? default : dateRange.StartDate.Date;
                    DateTime leadershipEnd = dateRange.EndDate.Date > endDate ? endDate ?? default : dateRange.EndDate.Date;
                    var daysOfLeadershipForChunk = leadershipEnd.Subtract(leadershipStart).TotalDays + 1;
                    var fullDateRangeDuration = (dateRange.EndDate.Subtract(dateRange.StartDate).TotalDays + 1);
                    var proportionOfTask = fullDateRangeDuration <= 0 ? 0 : daysOfLeadershipForChunk / fullDateRangeDuration;

                    // Adjust leadership task start and end dates based on the person starting or leaving
                    if (leadershipStart < person.StartDate)
                    {
                        leadershipStart = person.StartDate;
                    }
                    if (person.EndDate != null && leadershipEnd > person.EndDate)
                    {
                        leadershipEnd = person.EndDate!.Value;
                    }

                    // Now create the leadership task
                    var leadershipTask = new SubTask
                    {
                        AssignedResources = new List<Resource>
                        {
                            new Resource
                            {
                                Person = person,
                                AssignmentFTE = project.LeadershipFTE,
                                FundedFrom = project.LeadershipFundingSource,
                                PlannedCost = project.PlannedLeadershipCosts * proportionOfTask
                            }
                        },
                        Name = "Leadership",
                        SubTaskId = -1,
                        OwningProject = project,
                        StartDate = leadershipStart,
                        EndDate = leadershipEnd,
                        RequiresLeadership = false,
                        TaskType = TaskType.FixedDuration,
                        Demand = project.LeadershipFTE,
                        OriginalDemand = project.LeadershipFTE
                    };
                    tasks.Add(leadershipTask);
                }
            }

            // Each assignment is at least one row of the report
            foreach (var task in tasks)
            {
                // Get funding source info
                var fundingSource = task.AssignedResources.FirstOrDefault(x => x.Person.PersonId == person.PersonId).FundedFrom;

                // Proportion the data for the task based on the window
                var taskStart = task.StartDate.Date < startDate ? startDate ?? default : task.StartDate;
                var taskEnd = task.EndDate.Date > endDate ? endDate ?? default : task.EndDate;
                var daysOfTaskForChunk = taskEnd.Subtract(taskStart).TotalDays + 1;
                var fullTaskDuration = task.EndDate.Subtract(task.StartDate).TotalDays + 1;
                var proportionOfTask = fullTaskDuration <= 0 ? 0 : daysOfTaskForChunk / fullTaskDuration;

                // Create a line
                var initialChunk = new AssignmentChunk
                {
                    PostNumber = string.Empty,
                    EmployeeName = person.Name,
                    Grade = defaultWLM.Grade,
                    FTE = Math.Round(task.AssignedResources.FirstOrDefault(x => x.Person.PersonId == person.PersonId).AssignmentFTE, 3),
                    Project = project.GetFullName(),
                    LeadRSE = project.ProjectManager?.Name ?? "Unknown",
                    Faculty = project.Faculty.GetDescription(),
                    School = project.School.GetDescription(),
                    PI = project.PI,
                    Task = task.Name,
                    StartDate = taskStart,
                    EndDate = taskEnd,
                    FinancialYear = FinancialReference.GetFinancialYear(taskStart),
                    PlannedCost = task.AssignedResources.FirstOrDefault(x => x.Person.PersonId == person.PersonId).PlannedCost * proportionOfTask,
                    AccountCode = string.IsNullOrWhiteSpace(fundingSource?.AccountCode) ? "Unknown" : fundingSource?.AccountCode,
                    FundingSourceType = string.IsNullOrWhiteSpace(fundingSource?.FundingSourceType.GetDescription()) ? "Unknown" : fundingSource?.FundingSourceType.GetDescription(),
                    FundingSourceDescription = string.IsNullOrWhiteSpace(fundingSource?.Description) ? "None" : fundingSource?.Description,
                    FundingSourceAmount = fundingSource?.AmountAvailable ?? 0,
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

                            tempChunks.Add(new AssignmentChunk(initialChunk)
                            {
                                StartDate = startDateOfNewChunk,
                                EndDate = endDateOfNewChunk,
                                PlannedCost = initialChunk.PlannedCost * proportionOfInitialChunk
                            });
                        }
                    }

                    // If we did a split then need to add the final task chunk
                    var remainingCosts = initialChunk.PlannedCost - tempChunks.Sum(x => x.PlannedCost);
                    if (tempChunks.Count > 0)
                    {
                        tempChunks.Add(new AssignmentChunk(initialChunk)
                        {
                            StartDate = new DateTime(tempChunks.Last().EndDate.AddDays(1).Ticks),
                            EndDate = new DateTime(initialChunk.EndDate.Ticks),
                            PlannedCost = remainingCosts > 0 ? remainingCosts : 0
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

                    // Replace the list with the new list of chunks
                    if (tempChunks.Count > 0) taskChunks = tempChunks;
                }

                Debug.WriteLine($"** {project.GetFullName()} => {task.Name} | {taskChunks.Count} chunks after FY splitting");

                // Filter task chunk list to just those that intersect the window
                taskChunks = taskChunks.Where(x => x.StartDate <= endDate && x.EndDate >= startDate).ToList();

                Debug.WriteLine($"** {project.GetFullName()} => {task.Name} | {taskChunks.Count} chunks run during the window");

                // Update the data for the filtered chunks
                foreach (var chunk in taskChunks)
                {
                    // Cost estimate based on mid-grade salaries
                    chunk.UpdateEstimatedSalaryCost(finrefs);
                }

                // Add task to master list
                data.AddRange(taskChunks);
            }

            Debug.WriteLine($"** Built {data.Count} rows for {person.Name}");
            return data;
        }
    }
}
