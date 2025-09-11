using System.Diagnostics;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Data.Helpers
{
    public abstract class ExportHelper
    {

        /// <summary>
        /// Given a person, prepare data from database with a weekly granularity
        /// </summary>
        /// <param name="person">Person who is being exported</param>
        /// <param name="projects">All projects as retrieved from the project service</param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="finrefs">All the financial references</param>
        /// <returns>List of data items</returns>
        public static IEnumerable<AssignmentChunk> GetExportDataForPerson(Person person, IEnumerable<Project> projects, DateTime startDate, DateTime endDate, IEnumerable<FinancialReference> finrefs)
        {
            // New list
            var data = new List<AssignmentChunk>();

            Debug.WriteLine($"** Building data for {person.Name}...");

            // Filter list of tasks to those running during the window
            var projectsInWindow = projects
                .Where(x => !x.ProjectStatus.IsCancelled())
                .Where(x => x.IsWithin(startDate, endDate));
            var tasksInWindow = projectsInWindow
                .SelectMany(x => x.SubTasks)
                .Where(x => x.AssignedResources
                    .Any(x => x.Person.PersonId == person.PersonId)
                ).ToList();
            Debug.WriteLine($"** {projectsInWindow.Count()} projects and {tasksInWindow.Count()} tasks within window for {person.Name}");

            // Get WLM changes for this person that take place during the window
            var wlms = person.WorkloadModelChanges
                .Where(x => x.ChangeDate >= startDate && x.ChangeDate <= endDate)
                .OrderByDescending(x => x.ChangeDate).ToList();

            // Get WLM in force on the first day of the window or set to default G6
            WorkloadModelChange defaultWLM = person.GetWorkloadModelOnDateOrDefault(startDate);

            // If there isn't a WLM change on the first day of the window then add the default
            if (wlms.FirstOrDefault(x => x.ChangeDate == person.StartDate) == null)
            {
                // Add the start WLM to the list of WLMs active in the window
                wlms.Add(defaultWLM);
            }

            // Are there any changes in grade for this person?
            var changesInGrade = wlms.DistinctBy(x => x.Grade).Count() > 1;

            // Are there any changes in financial year in the window?
            var startFY = FinancialReference.GetFinancialYear(startDate);
            var endFY = FinancialReference.GetFinancialYear(endDate);
            var changesInFinancialYear = startFY != endFY;

            // Insert leadership assignments as subtasks with a special subtaskId so we can identify them later
            foreach (var project in projectsInWindow
                .Where(x => x.CostModel == CostModel.TechAndLeadership && x.ProjectManager?.PersonId == person.PersonId))
            {
                // Find leadership tasks and convert to task
                var dateRanges = project.GetLeadershipTaskRanges();
                foreach (var dateRange in dateRanges)
                {
                    // Add leadership subtask based on the date range
                    var daysOfLeadershipForChunk = (dateRange.EndDate - dateRange.StartDate).TotalDays + 1;
                    var leadershipTask = new SubTask
                    {
                        AssignedResources = new List<Resource>
                        {
                            new Resource
                            {
                                Person = person,
                                AssignmentFTE = project.LeadershipFTE,
                                FundedFrom = project.LeadershipFundingSource,
                                PlannedCost = project.PlannedLeadershipCosts
                            }
                        },
                        Name = "Leadership",
                        SubTaskId = -1,
                        OwningProject = project,
                        StartDate = dateRange.StartDate,
                        EndDate = dateRange.EndDate,
                        RequiresLeadership = false,
                        TaskType = TaskType.FixedDuration,
                        Demand = project.LeadershipFTE,
                        OriginalDemand = project.LeadershipFTE
                    };
                    tasksInWindow.Add(leadershipTask);
                }
            }

            // Each assignment is at least one row of the report
            foreach (var task in tasksInWindow)
            {
                // Get project
                var project = projects.First(x => x.ProjectId == task.OwningProject?.ProjectId);
                Debug.WriteLine($"** {project.GetFullName()} => {task.Name} being examined...");

                // Get funding source info
                var fundingSource = task.AssignedResources.FirstOrDefault(x => x.Person.PersonId == person.PersonId).FundedFrom;

                // Create a line
                var initialChunk = new AssignmentChunk
                {
                    EmployeeName = person.Name,
                    Grade = defaultWLM.Grade,
                    FTE = Math.Round(task.AssignedResources.FirstOrDefault(x => x.Person.PersonId == person.PersonId).AssignmentFTE, 3),
                    Project = project.GetFullName(),
                    LeadRSE = project.ProjectManager?.Name ?? "Unknown",
                    Faculty = project.Faculty.GetDescription(),
                    School = project.School.GetDescription(),
                    PI = project.PI,
                    Task = task.Name,
                    StartDate = task.StartDate,
                    EndDate = task.EndDate,
                    FinancialYear = FinancialReference.GetFinancialYear(task.StartDate),
                    PlannedCost = Math.Round(task.AssignedResources.FirstOrDefault(x => x.Person.PersonId == person.PersonId).PlannedCost, 2),
                    AccountCode = string.IsNullOrWhiteSpace(fundingSource?.AccountCode) ? "Unknown" : fundingSource?.AccountCode,
                    FundingSourceType = string.IsNullOrWhiteSpace(fundingSource?.FundingSourceType.GetDescription()) ? "Unknown" : fundingSource?.FundingSourceType.GetDescription(),
                    FundingSourceDescription = string.IsNullOrWhiteSpace(fundingSource?.Description) ? "None" : fundingSource?.Description,
                    FundingSourceAmount = Math.Round(fundingSource?.AmountAvailable ?? 0, 2)
                };
                IList<AssignmentChunk> taskChunks = new List<AssignmentChunk>()
                {
                    initialChunk
                };

                // Are there any changes to grade for this person at all -- ignore grade changes for leadership task resources
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
                    // Truncate dates if extends beyond window
                    if (chunk.StartDate < startDate)
                    {
                        chunk.StartDate = startDate;
                    }
                    if (chunk.EndDate > endDate)
                    {
                        chunk.EndDate = endDate;
                    }

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
