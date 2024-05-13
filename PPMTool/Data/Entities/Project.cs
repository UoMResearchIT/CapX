using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a group of subtask that form a project
    /// </summary>
    public class Project : BaseTask
    {
        public int ProjectId { get; set; }

        [Required]
        public int RTP { get; set; }

        [Required]
        public string PI { get; set; }

        [Required]
        public Portfolio Portfolio { get; set; }

        public Person ProjectManager { get; set; }

        public IList<SubTask> SubTasks { get; set; }

        /// <summary>
        /// This is the amount of money the PI has requested from the funder
        /// </summary>
        [Required]
        public double Budget { get; set; }

        [Required]
        public double FundsReceived { get; set; }

        [Required]
        public ProjectStatus ProjectStatus { get; set; }

        /// <summary>
        /// The Innate Activity Code to which this work is booked on the timesheeting system
        /// </summary>
        public InnateCode InnateActivity { get; set; }

        /// <summary>
        /// Constructor also adds default status messages
        /// </summary>
        public Project()
        {
            // Generate status messages to be maintained against a project
            statusMessages = new List<StatusMessage>
            {
                new StatusMessage("A task in this project will start soon.", StatusMessage.MessageType.Info, () => SubTasks.Any(x => x.WillStartWithinAMonth())),
                new StatusMessage("A task in this project has recently started.", StatusMessage.MessageType.Info, () => SubTasks.Any(x => x.HasStartedInTheLastWeek())),
                new StatusMessage("A task in this project has absent resources and has started or will start soon!", StatusMessage.MessageType.Warning, () => SubTasks.Any(x => x.HasAbsentResourcesAndStartsWithinAWeek())),
                new StatusMessage("A task in this project has provisional resources!", StatusMessage.MessageType.Warning, () => SubTasks.Any(x => x.HasProvisionalResources())),
                new StatusMessage("A task in this project is under-resourced!", StatusMessage.MessageType.Warning, () => SubTasks.Any(x => x.HasUnmetDemand())),
                new StatusMessage("A task in this project is running but the project is not active!", StatusMessage.MessageType.Error, () => RunningTaskButInactive()),
                new StatusMessage("This project is active but has no currently running tasks!", StatusMessage.MessageType.Error, () => ActiveButNoRunningTask()),
                new StatusMessage("This project has no project manager set!", StatusMessage.MessageType.Error, () => NotFinishedOrCancelledButNoPM()),
                new StatusMessage("This project has no timesheet activity set and project has started or will start soon!", StatusMessage.MessageType.Error, () => NotFinishedOrCancelledButNoInnateCodeAndUpcoming()),
                new StatusMessage("Everything looks OK!", StatusMessage.MessageType.Success, () => !HasActiveStatusMessages())
            };
        }


        /// <summary>
        /// Checks whether this project is inactive, not cancelled but there are tasks that are currently running
        /// </summary>
        /// <returns></returns>
        public bool RunningTaskButInactive()
        {
            return SubTasks.Any(x => x.IsCurrentlyRunning()) && ProjectStatus != ProjectStatus.Active && ProjectStatus != ProjectStatus.Maintenance && !ProjectStatus.IsCancelled();
        }

        /// <summary>
        /// Checks whether this project is active but there are no tasks that are currently running
        /// </summary>
        /// <returns></returns>
        public bool ActiveButNoRunningTask()
        {
            return SubTasks.All(x => !x.IsCurrentlyRunning()) && (ProjectStatus == ProjectStatus.Active || ProjectStatus == ProjectStatus.Maintenance);
        }

        /// <summary>
        /// Checks whether this project has any active status messages trigger by its own state or states of the subtasks
        /// </summary>
        /// <returns></returns>
        public bool HasActiveStatusMessages()
        {
            return statusMessages.Any(x => x.Status && x.Type != StatusMessage.MessageType.Success);
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
        /// Checks whether this project has any error-grade status messages
        /// </summary>
        /// <returns></returns>
        public bool HasActiveErrorMessages()
        {
            return statusMessages.Any(x => x.Status && x.Type == StatusMessage.MessageType.Error);
        }

        /// <summary>
        /// Updates the project summary based on the current state of subtasks and resources then updates the database
        /// </summary>
        public void UpdateProjectSummary()
        {
            // Set initial values
            DateTime startDate = DateTime.MaxValue;
            DateTime endDate = DateTime.MinValue;
            double actualCost = 0d;
            double actualHours = 0d;
            double plannedCost = 0d;

            // Loop over all the subtasks
            foreach (var task in SubTasks)
            {
                // Update the flags on this task
                task.UpdateStatusFlags();

                // Update the project start and end dates
                if (task.StartDate < startDate) startDate = task.StartDate;
                if (task.EndDate > endDate) endDate = task.EndDate;

                // Sum costs and hours
                actualCost += task.ActualCost;
                plannedCost += task.PlannedCost;
                actualHours += task.ActualWorkHours;
            }

            // Update project
            StartDate = startDate;
            EndDate = endDate;

            // Truncate to 1 DP
            ActualWorkHours = Math.Round(10 * actualHours) / 10;

            // Truncate the cost to 2 DP as it is currency
            ActualCost = Math.Round(100 * actualCost) / 100;
            PlannedCost = Math.Round(100 * plannedCost) / 100;

            // Update schedule status from the sub task flags
            if (SubTasks.Any(x => x.ScheduleStatus == ScheduleStatus.Late)) ScheduleStatus = ScheduleStatus.Late;
            else if (SubTasks.Any(x => x.ScheduleStatus == ScheduleStatus.Ahead)) ScheduleStatus = ScheduleStatus.Ahead;
            else ScheduleStatus = ScheduleStatus.OnSchedule;

            // Budget status
            if (ActualCost > Budget) BudgetStatus = BudgetStatus.Overspend;
            else if (SubTasks.Any(x => x.BudgetStatus == BudgetStatus.Underspend)) BudgetStatus = BudgetStatus.Underspend;
            else BudgetStatus = BudgetStatus.OnBudget;
        }

        /// <summary>
        /// Method which returns the project name prefixed by the RTP code
        /// </summary>
        /// <returns></returns>
        internal string GetFullName()
        {
            return $"RTP-{RTP} {Name}";
        }
    }
}
