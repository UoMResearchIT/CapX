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
            Debug.WriteLine($"** Building data for {person.Name}...");

            // Filter list of projects to those running during the window
            var projectsInWindow = projects
                .Where(x => !x.ProjectStatus.IsCancelled())
                .Where(x => x.IsWithin(startDate, endDate));

            // Filter list of tasks for those projects that just run during the window and are assigned to this person
            var tasksInWindow = projectsInWindow
                .SelectMany(x => x.SubTasks)
                .Where(x => x.AssignedResources
                    .Any(x => x.Person.PersonId == person.PersonId)
                )
                .Where(x => x.IsWithin(startDate, endDate));

            Debug.WriteLine($"** {projectsInWindow.Count()} projects and {tasksInWindow.Count()} tasks within window for {person.Name}");

            // Represent the assignments in the window as chunks
            var data = GetAssignmentChunks(person, projectsInWindow, finrefs, startDate, endDate, tasksInWindow);
            Debug.WriteLine($"** Built {data.Count()} rows for {person.Name}");

            return data;
        }

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
        /// <returns></returns>
        internal static IEnumerable<AssignmentChunk> GetAssignmentChunks(
            Person person,
            IEnumerable<Project> projectsInWindow,
            IEnumerable<FinancialReference> finrefs,
            DateTime? startDate = null,
            DateTime? endDate = null,
            IEnumerable<SubTask> tasksInWindow = null,
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
            List<SubTask> tempTasksInWindow = null;
            if (tasksInWindow == null)
            {
                tempTasksInWindow = projectsInWindow
                .SelectMany(x => x.SubTasks)
                .Where(x => x.AssignedResources
                    .Any(x => x.Person.PersonId == person.PersonId)
                )
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

            // Insert leadership assignments as subtasks with a special subtaskId so we can identify them later
            foreach (var project in projectsInWindow
                .Where(x => x.CostModel == CostModel.TechAndLeadership && x.ProjectManager?.PersonId == person.PersonId))
            {
                // Find leadership tasks within the window and convert to actual tasks
                var dateRanges = project.GetLeadershipTaskRanges();
                foreach (var dateRange in dateRanges.Where(x => x.IsWithin(startDate ?? default, endDate ?? default)))
                {
                    // Add leadership subtask based on the date range
                    var leadershipStart = dateRange.StartDate.Date < startDate ? startDate ?? default : dateRange.StartDate.Date;
                    var leadershipEnd = dateRange.EndDate.Date > endDate ? endDate ?? default : dateRange.EndDate.Date;
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
                    tempTasksInWindow.Add(leadershipTask);
                }
            }

            // Each assignment is at least one row of the report
            foreach (var task in tempTasksInWindow)
            {
                // Get project
                var project = projectsInWindow.First(x => x.ProjectId == task.OwningProject?.ProjectId);
                Debug.WriteLine($"** {project.GetFullName()} => {task.Name} being examined...");

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

                // Add task to master list
                data.AddRange(taskChunks);
            }

            // Add the mid-grade salary estimates and ovewrite the planned costs if necessary
            foreach (var chunk in data)
            {
                // Cost estimate based on mid-grade salaries
                chunk.UpdateEstimatedSalaryCost(finrefs, shouldCalculateCosts);
            }

            return data;
        }

        /// <summary>
        /// Represents a collection of data related to the recovery report
        /// </summary>
        internal class RecoveryDataForDay
        {
            /// <summary>
            /// Day of the data
            /// </summary>
            public DateTime Date { get; }

            /// <summary>
            /// Person-Value data based on what the time recovered should be for that day based on WLMs
            /// </summary>
            public IDictionary<string, float> TargetRecovery { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data based on what the sum of the person's assignments say they have on that day
            /// </summary>
            public IDictionary<string, float> RecoveredTime { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data which subtracts the target and recovered values and permits values over 100%
            /// </summary>
            public IDictionary<string, float> Net { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data which subtracts the target and recovered values but caps off values over 100%
            /// </summary>
            public IDictionary<string, float> NetCapped { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data based on what the sum of the person's assignments say they have on that day including leadership
            /// </summary>
            public IDictionary<string, float> RecoveredTimeIncLeadership { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data which subtracts the target and recovered values including leadership and permits values over 100%
            /// </summary>
            public IDictionary<string, float> NetIncLeadership { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data which subtracts the target and recovered values including leadership but caps off values over 100%
            /// </summary>
            public IDictionary<string, float> NetCappedIncLeadership { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data which holds the costs for a person on that day
            /// </summary>
            public IDictionary<string, float> PersonCosts { get; } = new Dictionary<string, float>();

            public RecoveryDataForDay(DateTime date)
            {
                Date = date;
            }
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
            public void Update(
                float targetFTE,
                float assignedFTE,
                float assignedIncLeadFTE,
                float maxCap,
                float actualCosts,
                float costs)
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

            // Create a context to be accesed on this thread
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

                    // Create a new item
                    var currentDayData = new RecoveryDataForDay(currentDate);

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
                            .Sum(x => x.FTE);
                        var projectAssignmentsFTEIncLeadership = chunks
                            .Sum(x => x.FTE);

                        // Net value
                        var netValue = projectAssignmentsFTE - projectWorkTargetFTE;
                        var netValueIncLeadership = projectAssignmentsFTEIncLeadership - projectWorkTargetFTE;

                        // Net value capped
                        var maxOverAllocation = wlmTotal - projectWorkTargetFTE;
                        if (maxOverAllocation < 0) maxOverAllocation = 0;
                        var netValueCapped = netValue > maxOverAllocation ? maxOverAllocation : netValue;
                        var netValueCappedIncLeadership = netValueIncLeadership > maxOverAllocation ? maxOverAllocation : netValueIncLeadership;

                        // Add to the data dictionary (this is mainly for troubleshooting)
                        currentDayData.TargetRecovery.Add(person.Name, (float)projectWorkTargetFTE);
                        currentDayData.RecoveredTime.Add(person.Name, (float)projectAssignmentsFTE);
                        currentDayData.NetCapped.Add(person.Name, (float)netValueCapped);
                        currentDayData.RecoveredTimeIncLeadership.Add(person.Name, (float)projectAssignmentsFTEIncLeadership);
                        currentDayData.NetCappedIncLeadership.Add(person.Name, (float)netValueCappedIncLeadership);
                        currentDayData.PersonCosts.Add(person.Name, (float)actualCostsOnDay);

                        // Update the totals based on this day
                        windowRecoveryData
                            .First(x => x.Name == person.Name)
                            .Update(
                                (float)projectWorkTargetFTE,
                                (float)projectAssignmentsFTE,
                                (float)projectAssignmentsFTEIncLeadership,
                                (float)maxOverAllocation,
                                (float)actualCostsOnDay,
                                (float)referenceCostsForADay
                            );
                    }

                    // Advance the day
                    currentDate = currentDate.AddDays(1);
                }
            }

            return windowRecoveryData;
        }
    }
}
