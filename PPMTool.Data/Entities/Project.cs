using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using PPMTool.Data.Enums;
using PPMTool.Data.Interfaces;
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
        public string PI { get; set; } = null!;

        /// <summary>
        /// School within the faculty in which the project sits
        /// </summary>
        [Required]
        public School School { get; set; } = new School();

        /// <summary>
        /// The project manager of this project
        /// </summary>
        [InverseProperty("ManagedProjects")]
        public virtual Person? ProjectManager { get; set; }

        /// <summary>
        /// The tasks that make up this project
        /// </summary>
        public virtual IList<SubTask> SubTasks { get; set; } = new List<SubTask>();

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
        public virtual InnateCode? InnateActivity { get; set; }

        /// <summary>
        /// HTML formatted text representing the description of the project
        /// </summary>
        [Required]
        public string Description { get; set; } = null!;

        /// <summary>
        /// Link to the scrum project on GitHub Projects
        /// </summary>
        [DataType(DataType.Url)]
        public string? ScrumProjectLink { get; set; }

        /// <summary>
        /// Link to the RSE request document on SharePoint
        /// </summary>
        [Required]
        [DataType(DataType.Url)]
        public string RequestDocLink { get; set; } = null!;

        /// <summary>
        /// List of people who follow the project updates
        /// </summary>
        [InverseProperty("FollowedProjects")]
        public virtual ICollection<Person> Followers { get; set; } = new List<Person>();

        /// <summary>
        /// If using a cost model that has leadership costs calculated, then the planned cost of this based on the expected duration of the project is available here
        /// </summary>
        public double PlannedLeadershipCosts { get; set; }

        /// <summary>
        /// If using a cost model that has leadership costs calculated, then the actual cost to date based on the duration of the project so far is available here
        /// </summary>
        public double ActualLeadershipCosts { get; set; }

        /// <summary>
        /// If not using the day rate model, then this is the sum of the indirect BAU costs top sliced off the funding sources
        /// </summary>
        public double BudgetedIndirects { get; set; }

        /// <summary>
        /// Timestamp recording when actuals were last updated.
        /// </summary>
        public string ActualsLastUpdated { get; set; } = DateTime.Now.ToString("R");

        /// <summary>
        /// List of Invoices associated with this project
        /// </summary>
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        /// <summary>
        /// List of payments associate with this project
        /// </summary>
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        /// <summary>
        /// List of funding sources for this project
        /// </summary>
        public virtual ICollection<FundingSource> FundingSources { get; set; } = new List<FundingSource>();

        /// <summary>
        /// Constructor also adds default status messages
        /// </summary>
        public Project()
        {
            // Generate status messages to be maintained against a project
            statusMessages = new List<StatusMessage>
            {
                // Info
                new StatusMessage("A task in this project will start soon.", StatusMessage.MessageType.Info, () => SubTasks?.Any(x => x.WillStartWithinAMonth()) ?? false),
                new StatusMessage("A task in this project has recently started.", StatusMessage.MessageType.Info, () => SubTasks?.Any(x => x.HasStartedInTheLastWeek()) ?? false),
                new StatusMessage("A task in this project has absent resources and has started or will start soon!", StatusMessage.MessageType.Info, () => SubTasks?.Any(x => x.HasAbsentResourcesAndStartsWithinAWeek()) ?? false, FeatureType.Absences),

                // Warning
                new StatusMessage("A task in this project has provisional resources!", StatusMessage.MessageType.Warning, () => SubTasks?.Any(x => x.HasProvisionalResources()) ?? false),
                new StatusMessage("A current or future task in this project is under-resourced!", StatusMessage.MessageType.Warning, () => HasUnmetDemandInWindow()),
                new StatusMessage("This project has started but has no link to a Scrum project!", StatusMessage.MessageType.Warning, () => HasStartedButHasNoScrumProjectLink()),
                new StatusMessage("Task has resource(s) with zero FTE assignment!", StatusMessage.MessageType.Warning, () => HasResourceWithZeroFTE()),

                // Error
                new StatusMessage("This project is active and overbudget!", StatusMessage.MessageType.Error, () => ProjectStatus.IsActive() && IsOverBudget(), FeatureType.ProjectFinance), // Finance
                new StatusMessage("This project has no agreed budget!", StatusMessage.MessageType.Error, () => HasNoBudget(), FeatureType.ProjectFinance), // Finance
                new StatusMessage("A task in this project is running but the project is not active!", StatusMessage.MessageType.Error, () => RunningTaskButInactive()),
                new StatusMessage("This project is active but has no currently running tasks!", StatusMessage.MessageType.Error, () => ActiveButNoRunningTask()),
                new StatusMessage("This project has no project manager set!", StatusMessage.MessageType.Error, () => NotFinishedOrCancelledButNoPM()),
                new StatusMessage("This project has no timesheet activity set and project has started or will start soon!", StatusMessage.MessageType.Error, () => NotFinishedOrCancelledButNoInnateCodeAndUpcoming(), FeatureType.Timesheets), // Timesheets
                new StatusMessage("This project has no RTP number specified!", StatusMessage.MessageType.Error, () => RTP == 0),
                new StatusMessage("This project has no link to a request document!", StatusMessage.MessageType.Error, () => HasNoRequestDocLink()),
                new StatusMessage("This project has no description!", StatusMessage.MessageType.Error, () => HasNoDescription()),
                new StatusMessage("This project has no tasks!", StatusMessage.MessageType.Error, () => SubTasks == null || SubTasks.Count == 0),
                new StatusMessage("This project is active but hasn't had its actuals updated for more than a month!", StatusMessage.MessageType.Error, () => ActiveButNotHadActualsUpdatedForAMonth(), FeatureType.Timesheets), // Timesheets
                new StatusMessage("This project has no funding sources but is either finished or is active!", StatusMessage.MessageType.Error, () => HasNoFundingSourcesButRan(), FeatureType.ProjectFinance), // Finance
                new StatusMessage("This project has a task with a resource without a funding source and is currently running or has run in the past!", StatusMessage.MessageType.Error, () => HasResourcesWithNoFundingSourceOnRunningTask(), FeatureType.ProjectFinance), // Finance
                new StatusMessage("This project uses the Day Rate model but has a DI funding source which is not allowed! DI funding sources must use salary costs for recharge.", StatusMessage.MessageType.Error, () => DayRateWithDIFunding(), FeatureType.ProjectFinance), // Finance
                new StatusMessage("This project does not have a leadership task!", StatusMessage.MessageType.Error, () => !SubTasks?.Any(x => x.IsLeadershipTask) ?? true),
                
                // Success
                new StatusMessage("Everything looks OK!", StatusMessage.MessageType.Success, () => !HasActiveStatusMessages())
            };
        }

        /// <summary>
        /// Whether the project uses the day rate model and has a DI funding source
        /// </summary>
        /// <returns></returns>
        private bool DayRateWithDIFunding()
        {
            return CostModel == CostModel.DayRate && (FundingSources?.Any(x => x.FundingSourceType == FundingSourceType.DI) ?? false);
        }

        /// <summary>
        /// Determines whether any resource in any subtask has an assignment FTE of zero
        /// </summary>
        /// <returns></returns>
        private bool HasResourceWithZeroFTE()
        {
            return SubTasks?.Any(t => t.AssignedResources?.Any(r => r.AssignmentFTE == 0) ?? false) ?? false;
        }

        /// <summary>
        /// Determines whether the project is over budget based on planned costs.
        /// </summary>
        /// <returns></returns>
        private bool IsOverBudget()
        {
            // Has to be more than £1 difference
            return Math.Floor(PlannedCost - Budget) > 0;
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
        /// Determine whether the project has any funding sources and is in an active, paused, maintenance or finished state
        /// </summary>
        /// <returns></returns>
        private bool HasNoFundingSourcesButRan()
        {
            return ProjectStatus.DidRun() && !(FundingSources?.Any() ?? false);
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
            return !ProjectStatus.IsCancelled() && DateTime.Today >= StartDate && DateTime.Today <= EndDate && !(ScrumProjectLink?.IsValidURL() ?? false);
        }

        /// <summary>
        /// Has no URL in the request doc link field or value is less than 12 characters
        /// </summary>
        /// <returns></returns>
        public bool HasNoRequestDocLink()
        {
            return !RequestDocLink.IsValidURL();
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
        /// <param name="recomputeSubTaskCosts">Whether to update the subtask costs and save to database</param>
        /// <param name="financialReferences">If necessary a set of financial references</param>
        public void UpdateProjectMetaData(bool recomputeSubTaskCosts, IEnumerable<FinancialReference> financialReferences)
        {
            // Check conditions for cost update
            if (CostModel != CostModel.DayRate && (financialReferences == null || financialReferences.Count() == 0))
            {
                throw new Exception("Cannot compute leadership costs for the project as at least one financial reference is required based on the model chosen!");
            }

            // Set initial values
            DateTime startDate = DateTime.MaxValue;
            DateTime endDate = DateTime.MinValue;
            double actualHours = 0d;
            double actualTech = 0d;
            double plannedTech = 0d;
            double budgetIndirects = 0d;
            double actualIndirects = 0d;
            double plannedIndirects = 0d;
            double actualLeadership = 0d;
            double plannedLeadership = 0d;

            // Loop over all the subtasks
            if (SubTasks != null)
            {
                foreach (var task in SubTasks)
                {
                    // Update the project start and end dates
                    if (task.StartDate < startDate) startDate = task.StartDate;
                    if (task.EndDate > endDate) endDate = task.EndDate;

                    // If required, update the sub task costs and save to the DB
                    // Used if the cost model for the project has been changed
                    if (recomputeSubTaskCosts)
                    {
                        // Update the cost of the tasks (and resources)
                        task.UpdateSubTaskCosts(this, financialReferences);
                    }

                    // Read subtask costs and hours and accumulate inot the relevant categories
                    if (task.IsLeadershipTask)
                    {
                        actualLeadership += task.ActualCost;
                        plannedLeadership += task.PlannedCost;
                    }
                    else
                    {
                        actualTech += task.ActualCost;
                        plannedTech += task.PlannedCost;
                    }
                    actualHours += task.ActualWorkHours;
                    plannedIndirects += task.PlannedIndirectCost;
                    actualIndirects += task.ActualIndirectCost;
                }
            }

            // Compute the budgeted indirects
            if (FundingSources != null && FundingSources.Count > 0)
            {
                budgetIndirects = GlobalDefaults.BAUTopSliceFractionDefault * FundingSources.Sum(x => x.AmountAvailable);
            }
            BudgetedIndirects = Math.Round(100 * budgetIndirects) / 100;

            // If we are using indirects then they will be included for tech so remove
            if (CostModel.HasIndirects())
            {
                plannedTech /= (1d + GlobalDefaults.BAUTopSliceFractionDefault);
                actualTech /= (1d + GlobalDefaults.BAUTopSliceFractionDefault);
            }

            // Update project dates
            StartDate = startDate;
            EndDate = endDate;

            // Truncate actuals to 1 DP and update
            var newValue = Math.Round(10 * actualHours) / 10;
            if (newValue != ActualWorkHours)
            {
                // Has been updated so store the timestamp
                ActualsLastUpdated = DateTime.Now.ToString("R");
            }
            ActualWorkHours = newValue;

            // Truncate the cost to 2 DP as it is currency
            ActualIndirectCost = Math.Round(100 * actualIndirects) / 100;
            PlannedIndirectCost = Math.Round(100 * plannedIndirects) / 100;
            ActualCost = Math.Round(100 * actualTech) / 100;
            PlannedCost = Math.Round(100 * plannedTech) / 100;
            ActualLeadershipCosts = Math.Round(100 * actualLeadership) / 100;
            PlannedLeadershipCosts = Math.Round(100 * plannedLeadership) / 100;
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
        /// To identify the project in the logs and on exports
        /// </summary>
        /// <returns></returns>
        public string GetSensibleObjectName()
        {
            return GetFullName();
        }

        /// <summary>
        /// Returns the total planned cost of the project (tech + leaderhsip + indirects)
        /// </summary>
        /// <returns></returns>
        public double GetTotalPlannedCosts()
        {
            return PlannedCost + PlannedLeadershipCosts + PlannedIndirectCost;
        }
    }
}
