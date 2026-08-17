// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

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
    public class Project : BaseTask, ILoggableObject
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
        public CostModel CostModel { get; set; } = CostModel.TechAndLeadershipWithIndirects;

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
        public string? ActualsLastUpdated { get; set; } = DateTime.Now.ToString("R");

        /// <summary>
        /// The date when the project was created
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// The latest date the project was moved out of the New Request status.
        /// </summary>
        public DateTime? RequestCompletedDate { get; set; }

        /// <summary>
        /// The person who created the project request -- automatically set to the logged in user when the project is created but can be changed later if necessary
        /// </summary>
        [Required]
        public int RequestOwnerId { get; set; }

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
        /// Shadow property to allow easy filtering on FundsReceived in datagrids.
        /// Requires additional logic in the service layer to populate this property when retrieving projects from the database.
        /// </summary>
        /// <returns></returns>
        [NotMapped]
        public double FundsReceived { get; set; }

        /// <summary>
        /// Checks whether any funding sources are not linked to any resources in the subtasks
        /// </summary>
        /// <returns></returns>
        public bool HasFundingSourcesNotLinkedToResources()
        {
            // If no tasks or no resources then return false
            if (SubTasks == null || SubTasks?.Count == 0) return false;
            if (SubTasks?.All(x => x.AssignedResources == null || x.AssignedResources?.Count == 0) ?? true) return false;

            // Get the funding source IDs from the project by flattening to a list or returning an empty list
            // if there are no funding sources associated with the project at all
            var fundingSourceIds =
                FundingSources?
                .Select(x => x.FundingSourceId)
                .ToList() ?? new List<int>();

            // Get the funding source IDs that are linked to assigned resources
            var resourceFundingSourceIds =
                SubTasks?.
                SelectMany(t =>
                {
                    // Take the funding sources linked to resources and flatten
                    return t.AssignedResources?

                        // If there is a an assigned resource with no linked funding source then
                        // use 0 as the funding source ID which will not match any real funding
                        // source ID and will be ignored in the comparison later.
                        .Select(r => r.FundedFrom?.FundingSourceId ?? 0)

                        // If no assigned resources then return an empty list for this sub task
                        ?? new List<int>();
                })

                // If no sub tasks then return empty list
                .ToList() ?? new List<int>();

            // Check whether there are any funding source IDs that are not linked to resources
            return fundingSourceIds.Except(resourceFundingSourceIds).Any();
        }

        /// <summary>
        /// Whether the project uses the day rate model and has a DI funding source
        /// </summary>
        /// <returns></returns>
        public bool DayRateWithDIFunding()
        {
            return CostModel == CostModel.DayRate && (FundingSources?.Any(x => x.FundingSourceType == FundingSourceType.DI) ?? false);
        }

        /// <summary>
        /// Determines whether any resource in any subtask has an assignment FTE of zero
        /// </summary>
        /// <returns></returns>
        public bool HasResourceWithZeroFTE()
        {
            return SubTasks?.Any(t => t.AssignedResources?.Any(r => r.AssignmentFTE == 0) ?? false) ?? false;
        }

        /// <summary>
        /// Determines whether the project is over budget based on planned costs.
        /// </summary>
        /// <returns></returns>
        public bool IsOverBudget(double thresholdPercentage = 0)
        {

            // If no thresholdPercentage is zero then do direct comparison
            if (thresholdPercentage == 0)
            {
                // Round to the nearest whole £1
                return Math.Floor(PlannedCost - Budget) > 0;
            }
            else
            {
                // Avoid division by zero
                if (Budget == 0) return false;

                // Otherwise check the percentage overspend against the threshold
                return Math.Floor((PlannedCost - Budget) * 100d / Budget) > thresholdPercentage;
            }
        }

        /// <summary>
        /// Determines whether the project has no budget but is not a new request when it legitimately might not have budget
        /// </summary>
        /// <returns></returns>
        public bool HasNoBudget()
        {
            return Budget == 0 && ProjectStatus != ProjectStatus.NewRequest && !ProjectStatus.IsCancelled();
        }

        /// <summary>
        /// Determine whether the project has any funding sources and is in an active, paused, maintenance or finished state
        /// </summary>
        /// <returns></returns>
        public bool HasNoFundingSourcesButRan()
        {
            return ProjectStatus.DidRun() && !(FundingSources?.Any() ?? false);
        }

        /// <summary>
        /// Whether this project is active and the actuals updated timestamp shows it hasn't been updated for a month or more
        /// </summary>
        /// <returns></returns>
        public bool ActiveButNotHadActualsUpdatedForAMonth()
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
        /// <param name="indirectsPercentage">The percentage of top slice to apply from the settings</param>
        /// <exception cref="Exception">When no financial references are available in the DB with which to update the costs</exception>
        public void UpdateProjectMetaData(bool recomputeSubTaskCosts, IEnumerable<FinancialReference> financialReferences, float indirectsPercentage)
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
                        task.UpdateSubTaskCosts(this, financialReferences, indirectsPercentage);
                    }

                    // Read subtask costs and hours and accumulate into the relevant categories
                    if (task.TaskDuty == Duty.ProjectAndServiceMgmt)
                    {
                        actualLeadership += task.ActualCost;
                        plannedLeadership += task.PlannedCost;
                    }
                    else
                    {
                        // Note that these will have been computed using BilledFTE so include indirects if applicable
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
                budgetIndirects = indirectsPercentage * FundingSources.Sum(x => x.AmountAvailable);
            }
            BudgetedIndirects = Math.Round(100 * budgetIndirects) / 100;

            // If we are using indirects then they will be included for tech so remove
            if (CostModel.HasIndirects())
            {
                plannedTech /= (1d + indirectsPercentage);
                actualTech /= (1d + indirectsPercentage);
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
            ActualLeadershipCosts = Math.Round(100 * actualLeadership) / 100;
            PlannedLeadershipCosts = Math.Round(100 * plannedLeadership) / 100;

            // The planned and actuals for a project are are the total of all the cost categories
            ActualCost = Math.Round(100 * actualTech) / 100;
            ActualCost += ActualLeadershipCosts + ActualIndirectCost;
            PlannedCost = Math.Round(100 * plannedTech) / 100;
            PlannedCost += PlannedLeadershipCosts + PlannedIndirectCost;
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
            return $"Project {RTP} | {Name}";
        }
    }
}
