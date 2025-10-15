using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using PPMTool.Data.Helpers;
using PPMTool.Enums;
using static PPMTool.Data.ValidationAttributes;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a group of subtask that form a project
    /// </summary>
    public class Project : BaseTask, ILoggableClass
    {
        public int ProjectId { get; set; }

        /// <summary>
        /// The reference number of the project
        /// </summary>
        [Required]
        public int RTP { get; set; }

        /// <summary>
        /// The principal investigator of the project (our customer)
        /// </summary>
        [Required]
        public string PI { get; set; }

        /// <summary>
        /// Faculty in which the projet sits
        /// </summary>
        [Required]
        public Faculty Faculty { get; set; }

        /// <summary>
        /// School within the faculty in which the project sits
        /// </summary>
        [Required]
        public School School { get; set; }

        /// <summary>
        /// The project manager of this project
        /// </summary>
        [InverseProperty("ManagedProjects")]
        public virtual Person ProjectManager { get; set; }

        /// <summary>
        /// The tasks that make up this project
        /// </summary>
        public virtual IList<SubTask> SubTasks { get; set; }

        /// <summary>
        /// This is the amount of money the PI has requested from the funder
        /// </summary>
        [Required]
        public double Budget { get; set; }

        /// <summary>
        /// If the relevant cost model is chosen then this becomes a required field
        /// </summary>
        [RequiredForAny(Values = new[] { nameof(CostModel.DayRate) }, PropertyName = nameof(CostModel))]
        public double DayRate { get; set; }

        /// <summary>
        /// The cost model this project uses
        /// </summary>
        [Required]
        public CostModel CostModel { get; set; }

        /// <summary>
        /// The status of the project
        /// </summary>
        [Required]
        public ProjectStatus ProjectStatus { get; set; }

        /// <summary>
        /// The Innate Activity Code to which this work is booked on the timesheeting system
        /// </summary>
        public virtual InnateCode InnateActivity { get; set; }

        /// <summary>
        /// HTML formatted text representing the description of the project
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Link to the scrum project on GitHub Projects
        /// </summary>
        [DataType(DataType.Url)]
        public string ScrumProjectLink { get; set; }

        /// <summary>
        /// Link to the RSE request document on SharePoint
        /// </summary>
        [Required]
        [DataType(DataType.Url)]
        public string RequestDocLink { get; set; }

        /// <summary>
        /// List of people who follow the project updates
        /// </summary>
        [InverseProperty("FollowedProjects")]
        public virtual ICollection<Person> Followers { get; set; } = new List<Person>();

        /// <summary>
        /// If using a cost model that has leadership costs calculated, then the planned cost based on the expected duration of the project is available here
        /// </summary>
        public double PlannedLeadershipCosts { get; set; }

        /// <summary>
        /// If using a cost model that has leadership costs calculated, then the actual cost to date based on the duration of the project so far is available here
        /// </summary>
        public double ActualLeadershipCosts { get; set; }

        /// <summary>
        /// Timestamp recording when actuals were last updated.
        /// </summary>
        public string ActualsLastUpdated { get; set; } = DateTime.Now.ToString("R");

        /// <summary>
        /// The amount of time the management of this project is expected to take in FTE
        /// </summary>
        public float LeadershipFTE { get; set; } = GlobalDefaults.ProjectManagementDefaultFTE;

        /// <summary>
        /// List of Invoices associated with this project
        /// </summary>
        public virtual ICollection<Invoice> Invoices { get; set; }

        /// <summary>
        /// List of payments associate with this project
        /// </summary>
        public virtual ICollection<Payment> Payments { get; set; }

        /// <summary>
        /// List of funding sources for this project
        /// </summary>
        public virtual ICollection<FundingSource> FundingSources { get; set; }

        /// <summary>
        /// A specific funding source that is used to fund the leadership costs of this project
        /// </summary>
        public int? FundingSourceId { get; set; }
        public virtual FundingSource LeadershipFundingSource { get; set; }

        /// <summary>
        /// Constructor also adds default status messages
        /// </summary>
        public Project()
        {
            // Generate status messages to be maintained against a project
            statusMessages = new List<StatusMessage>
            {
                new StatusMessage("A task in this project will start soon.", StatusMessage.MessageType.Info, () => SubTasks?.Any(x => x.WillStartWithinAMonth()) ?? false),
                new StatusMessage("A task in this project has recently started.", StatusMessage.MessageType.Info, () => SubTasks?.Any(x => x.HasStartedInTheLastWeek()) ?? false),
                new StatusMessage("A task in this project has absent resources and has started or will start soon!", StatusMessage.MessageType.Info, () => SubTasks?.Any(x => x.HasAbsentResourcesAndStartsWithinAWeek()) ?? false),
                new StatusMessage("A task in this project has provisional resources!", StatusMessage.MessageType.Warning, () => SubTasks?.Any(x => x.HasProvisionalResources()) ?? false),
                new StatusMessage("A current or future task in this project is under-resourced!", StatusMessage.MessageType.Warning, () => HasUnmetDemandInWindow()),
                new StatusMessage("This project has started but has no link to a Scrum project!", StatusMessage.MessageType.Warning, () => HasStartedButHasNoScrumProjectLink()),
                new StatusMessage("This project has no funding source for its leadership charges!", StatusMessage.MessageType.Error, () => HasNoLeadershipFundingSourcesButRan()),
                new StatusMessage("This project has no agreed budget!", StatusMessage.MessageType.Error, () => HasNoBudget()),
                new StatusMessage("A task in this project is running but the project is not active!", StatusMessage.MessageType.Error, () => RunningTaskButInactive()),
                new StatusMessage("This project is active but has no currently running tasks!", StatusMessage.MessageType.Error, () => ActiveButNoRunningTask()),
                new StatusMessage("This project has no project manager set!", StatusMessage.MessageType.Error, () => NotFinishedOrCancelledButNoPM()),
                new StatusMessage("This project has no timesheet activity set and project has started or will start soon!", StatusMessage.MessageType.Error, () => NotFinishedOrCancelledButNoInnateCodeAndUpcoming()),
                new StatusMessage("This project has no RTP number specified!", StatusMessage.MessageType.Error, () => RTP == 0),
                new StatusMessage("This project has no link to a request document!", StatusMessage.MessageType.Error, () => HasNoRequestDocLink()),
                new StatusMessage("This project has no description!", StatusMessage.MessageType.Error, () => HasNoDescription()),
                new StatusMessage("This project is missing faculty and/or school information!", StatusMessage.MessageType.Error, () => HasNoFacultyOrFacultyButNoSchool()),
                new StatusMessage("This project has no tasks!", StatusMessage.MessageType.Error, () => SubTasks == null || SubTasks.Count == 0),
                new StatusMessage("This project is active but hasn't had its actuals updated for more than a month!", StatusMessage.MessageType.Error, () => ActiveButNotHadActualsUpdatedForAMonth()),
                new StatusMessage("This project has no funding sources but is either finished or is active!", StatusMessage.MessageType.Error, () => HasNoFundingSourcesButRan()),
                new StatusMessage("This project has a task with a resource without a funding source and is currently running or has run in the past!", StatusMessage.MessageType.Error, () => HasResourcesWithNoFundingSourceOnRunningTask()),
                new StatusMessage("Everything looks OK!", StatusMessage.MessageType.Success, () => !HasActiveStatusMessages())
            };
        }

        /// <summary>
        /// Determines whether the project has no budget but is not a new request when it legitimately might not have budget
        /// </summary>
        /// <returns></returns>
        private bool HasNoBudget()
        {
            return Budget == 0 && ProjectStatus != ProjectStatus.NewRequest && !ProjectStatus.IsCancelled();
        }

        /// <summary>
        /// Determines whether the project has no leadership funding sources but has run or is running and is in an active, paused, maintenance or finished state with cost model that includes leadership costs
        /// </summary>
        /// <returns></returns>
        private bool HasNoLeadershipFundingSourcesButRan()
        {
            return CostModel == CostModel.TechAndLeadership && ProjectStatus.DidRun() && LeadershipFundingSource == null;
        }

        /// <summary>
        /// Determine whether the project has any funding sources and is in an active, paused, maintenance or finished state
        /// </summary>
        /// <returns></returns>
        private bool HasNoFundingSourcesButRan()
        {
            return ProjectStatus.DidRun() && !FundingSources.Any();
        }

        /// <summary>
        /// Whether this project is active and the actuals updated timestamp shows it hasn't been updated for a month or more
        /// </summary>
        /// <returns></returns>
        private bool ActiveButNotHadActualsUpdatedForAMonth()
        {
            if (ProjectStatus != ProjectStatus.Active) return false;
            DateTime lastUpdated = string.IsNullOrEmpty(ActualsLastUpdated) ? default : DateTime.ParseExact(ActualsLastUpdated, "R", CultureInfo.InvariantCulture);
            return lastUpdated.AddMonths(1) < DateTime.Now;
        }

        /// <summary>
        /// Whether a project has no faculty or has faculty but no school
        /// </summary>
        /// <returns></returns>
        private bool HasNoFacultyOrFacultyButNoSchool()
        {
            return Faculty == Faculty.None || ((Faculty == Faculty.FBMH || Faculty == Faculty.FHUMS || Faculty == Faculty.FSE) && School == School.None);
        }

        /// <summary>
        /// Whether a project has no description
        /// </summary>
        /// <returns></returns>
        public bool HasNoDescription()
        {
            return string.IsNullOrWhiteSpace(Description);
        }

        /// <summary>
        /// Today is within [startdate enddate] and there is no scrum project link
        /// </summary>
        /// <returns></returns>
        public bool HasStartedButHasNoScrumProjectLink()
        {
            return !ProjectStatus.IsCancelled() && DateTime.Today >= StartDate && DateTime.Today <= EndDate && !HtmlHelper.IsValidLink(ScrumProjectLink);
        }

        /// <summary>
        /// Has no URL in the request doc link field or value is less than 12 characters
        /// </summary>
        /// <returns></returns>
        public bool HasNoRequestDocLink()
        {
            return !HtmlHelper.IsValidLink(RequestDocLink);
        }


        /// <summary>
        /// Checks whether this project is inactive, not cancelled but there are tasks that are currently running
        /// </summary>
        /// <returns></returns>
        public bool RunningTaskButInactive()
        {
            return (SubTasks?.Any(x => x.IsCurrentlyRunning()) ?? false) && ProjectStatus != ProjectStatus.Active && ProjectStatus != ProjectStatus.Maintenance && !ProjectStatus.IsCancelled();
        }

        /// <summary>
        /// Checks whether this project is active but there are no tasks that are currently running
        /// </summary>
        /// <returns></returns>
        public bool ActiveButNoRunningTask()
        {
            return (SubTasks?.All(x => !x.IsCurrentlyRunning()) ?? false) && (ProjectStatus == ProjectStatus.Active || ProjectStatus == ProjectStatus.Maintenance);
        }

        /// <summary>
        /// Checks whether this project is not finished or cancelled but has no project manager assigned
        /// </summary>
        /// <returns></returns>
        public bool NotFinishedOrCancelledButNoPM()
        {
            return !ProjectStatus.IsFinishedOrCancelled() && ProjectManager == null;
        }

        /// <summary>
        /// Checks whether this project is not finished or cancelled but has no Innate Code
        /// </summary>
        /// <returns></returns>
        public bool NotFinishedOrCancelledButNoInnateCodeAndUpcoming()
        {
            return !ProjectStatus.IsFinishedOrCancelled() && InnateActivity == null && DateTime.Today.AddMonths(1) >= StartDate;
        }

        /// <summary>
        /// Check whether this project is not in a cancelled state and has any tasks with unmet demand within the window given.
        /// </summary>
        /// <param name="startDate">If null, assumed to be now</param>
        /// <param name="endDate">If null, window just considered to be the future</param>
        /// <returns></returns>
        public bool HasUnmetDemandInWindow(DateTime? startDate = null, DateTime? endDate = null)
        {
            return !ProjectStatus.IsCancelled() && (SubTasks?.Any(x => x.GetUnmetDemandInWindow(startDate, endDate) > 0) ?? false);
        }

        /// <summary>
        /// Checks whether this project has any tasks that are running, or have run, but have no funding source
        /// </summary>
        /// <returns></returns>
        public bool HasResourcesWithNoFundingSourceOnRunningTask()
        {
            // Check if any of the subtasks have resources with no funding source
            return SubTasks?.Any(x => x.HasResourceWithNoFundingSourceAndRunning()) ?? false;
        }

        /// <summary>
        /// Updates the project meta data based on the current state of subtasks, resources and actuals
        /// </summary>
        /// <param name="updateSubTaskCosts">Whether to update the subtask costs and save to database</param>
        /// <param name="financialReferences">If necessary a set of financial references</param>
        public void UpdateProjectMetaData(bool updateSubTaskCosts, IEnumerable<FinancialReference> financialReferences)
        {
            // Check conditions for cost update
            if (CostModel != CostModel.DayRate && (financialReferences == null || financialReferences.Count() == 0))
            {
                throw new Exception("Cannot compute leadership costs for the project as at least one financial reference is required based on the model chosen!");
            }

            // Set initial values
            DateTime startDate = DateTime.MaxValue;
            DateTime endDate = DateTime.MinValue;
            double actualCost = 0d;
            double actualHours = 0d;
            double plannedCost = 0d;
            List<AssignmentChunk> chunks = new List<AssignmentChunk>();

            // Loop over all the subtasks
            if (SubTasks != null)
            {
                foreach (var task in SubTasks)
                {
                    // Update the project start and end dates
                    if (task.StartDate < startDate) startDate = task.StartDate;
                    if (task.EndDate > endDate) endDate = task.EndDate;

                    // Sum technical costs and hours
                    if (updateSubTaskCosts)
                    {
                        // Update the cost of the tasks (and resources)
                        chunks.AddRange(task.UpdateSubTaskCosts(this, financialReferences));
                    }

                    // Read subtask costs and hours and accumulate
                    actualCost += task.ActualCost;
                    plannedCost += task.PlannedCost;
                    actualHours += task.ActualWorkHours;
                }
            }

            // Update project dates
            StartDate = startDate;
            EndDate = endDate;

            // Add the leadership costs, generating chunks if the model requires it
            var leadershipChunks = ExportHelper.GetAssignmentChunks(
                ProjectManager,
                new List<Project> { this },
                financialReferences,
                shouldCalculateCosts: true)
                .Where(c => c.IsLeadershipAssignment);
            ActualLeadershipCosts = Math.Round(100 * CalculateActualLeadershipCosts(leadershipChunks)) / 100;
            PlannedLeadershipCosts = Math.Round(100 * leadershipChunks.Sum(x => x.PlannedCost)) / 100;

            // Truncate to 1 DP
            var newValue = Math.Round(10 * actualHours) / 10;
            if (newValue != ActualWorkHours)
            {
                // Has been updated so store the timestamp
                ActualsLastUpdated = DateTime.Now.ToString("R");
            }
            ActualWorkHours = newValue;

            // Truncate the cost to 2 DP as it is currency and add on leadership costs
            ActualCost = Math.Round(100 * actualCost) / 100 + ActualLeadershipCosts;
            PlannedCost = Math.Round(100 * plannedCost) / 100 + PlannedLeadershipCosts;


            // TODO: Work out how much of the subtask costs are in budget
        }

        /// <summary>
        /// Method which returns the project name prefixed by the RTP code
        /// </summary>
        /// <returns></returns>
        public string GetFullName()
        {
            return $"RTP-{RTP} {Name}";
        }

        /// <summary>
        /// Method to return the dates in which there is unmet demand.
        /// </summary>
        /// <param name="windowStart">The start of the unmet demand window</param>
        /// <param name="windowEnd">The end of the unmet demand window</param>
        public void GetUnmetDemandWindowDates(out DateTime windowStart, out DateTime windowEnd)
        {
            var tasks = SubTasks.Where(x => x.GetUnmetDemandInWindow() > 0);
            windowStart = tasks.Min(x => x.StartDate);
            windowEnd = tasks.Max(x => x.EndDate);
        }

        /// <summary>
        /// Method to run the calculation of leadership costs planned or actual
        /// </summary>
        /// <param name="leadershipChunks"></param>
        /// <returns></returns>
        private double CalculateActualLeadershipCosts(IEnumerable<AssignmentChunk> leadershipChunks)
        {
            // If not using the leadership cost model then this is zero
            if (CostModel != CostModel.TechAndLeadership)
            {
                return 0;
            }

            // Zero for actuals if project hasn't started yet
            if (DateTime.Today < StartDate)
            {
                return 0d;
            }

            // Find leadership task running today
            var currentChunk = leadershipChunks.FirstOrDefault(x => x.IsWithin(DateTime.Today));

            // All tasks before today count toward the actuals and are the same as the planned
            var costs = leadershipChunks
                .Where(x => x.EndDate < DateTime.Today)
                .Sum(x => x.PlannedCost);

            // If we have a current chunk work out the proportion through the chunk
            if (currentChunk != null)
            {
                var daysInChunk = DateTime.Today.Subtract(currentChunk.StartDate).TotalDays + 1;
                var chunkLength = currentChunk.EndDate.Subtract(currentChunk.StartDate).TotalDays + 1;
                var proportion = daysInChunk / chunkLength;
                costs += proportion * currentChunk.PlannedCost;
            }

            return costs;
        }

        /// <summary>
        /// Computes the fraction of time within the given window where tasks are running
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        private double GetFractionOfTimeWithTasksRunning(DateTime startDate, DateTime endDate)
        {
            // Obviously if no tasks then no fraction
            if (SubTasks.Count == 0)
            {
                return 0;
            }

            // Convert tasks to date ranges
            var dateRanges = GetLeadershipTaskRanges();

            // Get the number of overlapping days in this window
            var days = CalculateOverlappingDays(dateRanges, startDate, endDate);

            // Return fraction of the days in the window
            var windowSize = endDate.Subtract(startDate).TotalDays + 1;
            return days / windowSize;
        }

        /// <summary>
        /// Generates a list of date ranges for the leadership tasks
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DateRange> GetLeadershipTaskRanges()
        {
            // Convert the sub tasks to date ranges
            var dateRanges = SubTasks
                .Where(x => x.RequiresLeadership)
                .Select(x => new DateRange { StartDate = x.StartDate, EndDate = x.EndDate });

            // Merge overlapping date ranges
            var mergedRanges = MergeDateRanges(dateRanges);

            return mergedRanges;
        }

        /// <summary>
        /// Method to generate leadership tasks for a project
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<SubTask> GenerateLeadershipTasks()
        {
            // Find leadership tasks within the window and convert to actual tasks
            var dateRanges = GetLeadershipTaskRanges();
            var leadershipTasks = new List<SubTask>();
            if (ProjectManager == null)
            {
                return leadershipTasks;
            }

            // Build tasks
            var daysOfLeadershipForProject = dateRanges.Sum(x => x.EndDate.Subtract(x.StartDate).TotalDays + 1);
            foreach (var dateRange in dateRanges)
            {
                // Add leadership subtask based on the date range
                var leadershipStart = dateRange.StartDate.Date;
                var leadershipEnd = dateRange.EndDate.Date;

                // Adjust leadership task start and end dates based on the person starting or leaving
                // If calculating costs for planned leadership this is tasken into account later in GetAssignmentChunks
                if (leadershipStart < ProjectManager.StartDate)
                {
                    leadershipStart = ProjectManager.StartDate;
                }
                if (ProjectManager.EndDate != null && leadershipEnd > ProjectManager.EndDate)
                {
                    leadershipEnd = ProjectManager.EndDate!.Value;
                }

                // Work out proportions based on adjusted dates
                var daysOfLeadershipForRange = leadershipEnd.Subtract(leadershipStart).TotalDays + 1;
                var proportionOfLeadershipCosts = daysOfLeadershipForRange / daysOfLeadershipForProject;

                // Now create the leadership task
                var leadershipTask = new SubTask
                {
                    Name = "Leadership",
                    SubTaskId = -1,
                    OwningProject = this,
                    StartDate = leadershipStart,
                    EndDate = leadershipEnd,
                    RequiresLeadership = false,
                    TaskType = TaskType.FixedDuration,
                    Demand = LeadershipFTE,
                    OriginalDemand = LeadershipFTE,
                    DurationDays = (int)(leadershipEnd.Subtract(leadershipStart).TotalDays + 1)
                };
                leadershipTask.AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        Person = ProjectManager,
                        AssignmentFTE = LeadershipFTE,
                        FundedFrom = LeadershipFundingSource,
                        PlannedCost = PlannedLeadershipCosts * proportionOfLeadershipCosts,
                        ActualCost = ActualLeadershipCosts * proportionOfLeadershipCosts,
                        SubTask = leadershipTask
                    }
                };
                leadershipTasks.Add(leadershipTask);
            }

            return leadershipTasks;
        }

        /// <summary>
        /// A method to compute how many days overlap between a list of date range objects (assuming they themselves do not overlap) and a window
        /// </summary>
        /// <param name="dateRanges">A list of non-overlapping date ranges</param>
        /// <param name="windowStartDate"></param>
        /// <param name="windowEndDate"></param>
        /// <returns></returns>
        public static int CalculateOverlappingDays(IEnumerable<DateRange> dateRanges, DateTime windowStartDate, DateTime windowEndDate)
        {
            // Count the days overlapping across all tasks
            int totalDays = 0;
            foreach (var range in dateRanges)
            {
                DateTime overlapStart = range.StartDate > windowStartDate ? range.StartDate : windowStartDate;
                DateTime overlapEnd = range.EndDate < windowEndDate ? range.EndDate : windowEndDate;

                if (overlapStart <= overlapEnd)
                {
                    totalDays += (overlapEnd - overlapStart).Days + 1;
                }
            }

            return totalDays;
        }

        /// <summary>
        /// Method to take a bunch of date ranges and merge them into a set of date ranges that do not overlap with each other
        /// </summary>
        /// <param name="dateRanges"></param>
        /// <returns></returns>
        public static IEnumerable<DateRange> MergeDateRanges(IEnumerable<DateRange> dateRanges)
        {
            if (dateRanges.Count() == 0)
                return new List<DateRange>();

            // Sort the date ranges by start date
            dateRanges = dateRanges.OrderBy(r => r.StartDate).ToList();

            // Select the initial date range
            List<DateRange> mergedRanges = new List<DateRange>();
            DateRange currentRange = dateRanges.First();

            // Loop over remain date ranges and check to see if we extend an existing or create a new block
            foreach (var range in dateRanges.Skip(1))
            {
                // Overlaps
                if (range.StartDate <= currentRange.EndDate)
                {
                    // Extend the current range if overlapping
                    currentRange.EndDate = currentRange.EndDate > range.EndDate ? currentRange.EndDate : range.EndDate;
                }

                // Gap between them
                else
                {
                    // Add the current range to the list and start a new range
                    mergedRanges.Add(currentRange);
                    currentRange = range;
                }
            }

            // Add the last range
            mergedRanges.Add(currentRange);

            return mergedRanges;
        }

        /// <summary>
        /// To identify the project in the logs and on exports
        /// </summary>
        /// <returns></returns>
        public string GetSensibleObjectName()
        {
            return GetFullName();
        }
    }
}
