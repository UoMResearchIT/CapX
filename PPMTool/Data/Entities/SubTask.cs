using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an individual activity or phase of a project
    /// </summary>
    public class SubTask : BaseTask
    {
        public SubTask()
        {
            // Set default value
            StartDate = DateTime.Today;

            // List of status messages to check for each task which will drive icons
            statusMessages = new List<StatusMessage>()
            {
                new StatusMessage("Task will start soon.", StatusMessage.MessageType.Info, () => WillStartWithinAMonth()),
                new StatusMessage("Task has recently started.", StatusMessage.MessageType.Info, () => HasStartedInTheLastWeek()),
                new StatusMessage("Task has resources with absence during or near the start of this task.", StatusMessage.MessageType.Info, () => IsAffectedByAbsence()),
                new StatusMessage("Task has absent resources and has started or will start soon!", StatusMessage.MessageType.Warning, () => HasAbsentResourcesAndStartsWithinAWeek()),
                new StatusMessage("Task has provisional resources!", StatusMessage.MessageType.Warning, () => HasProvisionalResources()),
                new StatusMessage("Task is under-resourced!", StatusMessage.MessageType.Warning, () => HasUnmetDemand()),
                new StatusMessage("Everything looks OK!", StatusMessage.MessageType.Success, () => !HasActiveStatusMessages())
            };
        }

        public int SubTaskId { get; set; }

        private TaskType taskType;
        public TaskType TaskType
        {
            get => taskType;
            set
            {
                if (taskType != value)
                {
                    taskType = value;
                    OnTaskTypeChanged(new EventArgs());
                }
            }
        }

        private bool isDone;
        /// <summary>
        /// Represents whether a task is complete or not. It can be marked as complete any time whether the full budget for 
        /// the task has been used or not. It will then allow tasks to be completed early without it affecting the definition of "Late".
        /// </summary>
        public bool IsDone
        {
            get => isDone;
            set
            {
                if (isDone != value)
                {
                    isDone = value;
                    OnDoneChanged(new EventArgs());
                }
            }
        }

        public virtual IList<Resource> AssignedResources { get; set; } = new List<Resource>();

        /// <summary>
        /// For now, restricted to a single predecessor task and an "finish-to-start" contraint
        /// </summary>
        public SubTask Predecessor { get; set; }

        private bool hasFixedStart;
        /// <summary>
        /// Basically a simplified constraint type of "Start No Earlier Than" otherwise will be "As Soon As Possible" based on the predecessor end dates
        /// </summary>
        public bool HasFixedStart
        {
            get => hasFixedStart;
            set
            {
                if (hasFixedStart != value)
                {
                    hasFixedStart = value;
                    OnFixedStartChanged(new EventArgs());
                }
            }
        }

        /// <summary>
        /// Method to determine whether a date is in the range [task.startDate task.endDate].
        /// If end date and start date are the same evaluates against start date.
        /// </summary>
        /// <param name="testDate">Date to test</param>
        /// <returns></returns>
        internal bool IsWithin(DateTime testDate)
        {
            return StartDate == EndDate ? testDate == StartDate : testDate >= StartDate && testDate <= EndDate;
        }

        /// <summary>
        /// Method to determine whether any part of the task runs within a date range [startDate endDate].
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        internal bool IsWithin(DateTime startDate, DateTime endDate)
        {
            return
                IsWithin(endDate) ||
                IsWithin(startDate) ||
                StartDate <= startDate && EndDate >= endDate;
        }


        /// <summary>
        /// Used to drive the end date from the start date assuming 7 hour days. This is includes weekends.
        /// </summary>
        public int DurationDays { get; set; }

        /// <summary>
        /// Used to drive the work assuming each day is 220 billable days spread over the year of 365 days so roughly 4.22 hours per calendar day.
        /// </summary>
        public int DurationBillableDays { get; set; }

        private bool hasFixedEndDate;
        /// <summary>
        /// For fixed duration tasks indicates whether the end date should be driven by the duration or the other way round (i.e. the end date is fixed)
        /// </summary>
        public bool HasFixedEndDate
        {
            get => hasFixedEndDate;
            set
            {
                if (hasFixedEndDate != value)
                {
                    hasFixedEndDate = value;

                    // Update the end date to match the start date plus a day
                    if (hasFixedEndDate && EndDate <= StartDate)
                    {
                        EndDate = StartDate.AddDays(1);
                    }
                    OnHasFixedEndDateChanged(new EventArgs());
                }
            }
        }

        private double demand;
        /// <summary>
        /// The minimum demand required to complete this task in FTE.
        /// </summary>
        [Required]
        public double Demand
        {
            get => demand;
            set
            {
                if (demand != value)
                {
                    demand = value;
                    UpdateUnmetDemand();
                }
            }
        }

        /// <summary>
        /// The difference between the demand and the sum of the assigned resources.
        /// </summary>
        public double UnmetDemand { get; set; }

        /// <summary>
        /// Update the work, duration (and end date) or units based on the configuration of the task
        /// Work = Duration * Units
        /// Units = Sum of Resource Assigned FTE
        /// </summary>
        /// <param name="permitEndDateToMove">Whether we can move the end date to maintain 
        /// the duration if the end date is fixed. Only applies to fixed duration tasks.</param>
        /// <param name="project">Project owning the subtask</param>
        /// <returns>Returns null if successful otherwise error message</returns>
        public string Schedule(bool permitEndDateToMove, Project project)
        {
            try
            {
                // Sum up assigned resources and determine latest start date of assigned resources
                double units = 0d;
                DateTime latestStart = default;
                string latestStarter = string.Empty;
                foreach (var r in AssignedResources)
                {
                    units += r.AssignmentFTE;
                    if (r.Person.StartDate > latestStart)
                    {
                        latestStarter = r.Person.Name;
                        latestStart = r.Person.StartDate;
                    }
                }

                // If no resources assigned then use the demand to schedule the task
                if (AssignedResources.Count == 0)
                {
                    units = Demand;
                }

                // Start date is fixed
                if (HasFixedStart)
                {
                    // If we assign someone who doesn't start until after the date then error
                    if (AssignedResources.Count > 0 && latestStart > StartDate)
                    {
                        return $"This task has a fixed start date of {StartDate}. " +
                            $"{latestStarter} is assigned to this task but they do not start until {latestStart.Date.ToShortDateString()}";
                    }
                }

                // Start date driven by predecessor, resources or just leave at default
                else
                {
                    // From predecessor
                    if (Predecessor != null)
                    {
                        StartDate = Predecessor.EndDate.AddDays(1);
                    }

                    // Check whether we need to drive from resources
                    if (AssignedResources.Count > 0 && latestStart > StartDate)
                    {
                        Debug.WriteLine($"** Start date being changed to {latestStart.Date.ToShortDateString()}, driven by resource {latestStarter}");
                        StartDate = latestStart.Date;
                    }
                }

                // Fixed Work Update
                if (TaskType == TaskType.FixedWork)
                {
                    // End Date must be driven
                    HasFixedEndDate = false;

                    // Always updates duration and leaves units fixed
                    UpdateDuration(units);
                }

                // Fixed Duration Update
                else
                {
                    // Make sure the duration is at least zero or greater
                    if (EndDate < StartDate) EndDate = StartDate.Date;

                    // If we are allowed to move the end date to maintain the current duration despite being marked as fixed then set the end date now
                    if (HasFixedEndDate && permitEndDateToMove) EndDate = StartDate.Date.AddDays(DurationDays - 1).Date;

                    // If the end date is fixed then set duration here from the start and end dates
                    if (HasFixedEndDate) UpdateDurationFromEndDate();

                    // Always updates the work and leaves units fixed
                    UpdateWork(units);
                }

                // Update cost
                PlannedCost = 0d;
                foreach (var res in AssignedResources)
                {
                    // Assume 7 hours in a billable day; fallback on project day rate if resource day rate is null
                    PlannedCost += (res.AssignmentFTE / units) * PlannedWorkHours * ((res.DayRate ?? project.DayRate) / 7f);
                }

                // Set end date from the duration
                if (!HasFixedEndDate) EndDate = StartDate.Date.AddDays(DurationDays - 1).Date;

                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        private void UpdateDurationFromEndDate()
        {
            // Tasks that start and end on the same day should still have a duration of 1 day so add a day here
            DurationDays = (int)Math.Round(EndDate.Date.Subtract(StartDate.Date).TotalDays) + 1;
            DurationBillableDays = GetNumberOfBillableDays(StartDate, EndDate);
        }

        private void UpdateDuration(double units)
        {
            if (units == 0)
            {
                DurationDays = 0;
                DurationBillableDays = 0;
            }
            else
            {
                // Compute the billable days from the planned work of the task where a billable day is 7 hours of work
                DurationBillableDays = (int)Math.Round(PlannedWorkHours / (7 * units));
                var estimatedEndDate = StartDate.AddDays(GetNumberOfCalendarDays(DurationBillableDays));

                // Tasks that start and end on the same day should still have a duration of 1 day so add a day here
                DurationDays = (int)Math.Round(estimatedEndDate.Date.Subtract(StartDate.Date).TotalDays) + 1;
            }
        }

        private void UpdateWork(double units)
        {
            // Duration input is calendar days so need to compute billable days to get work
            var endDate = StartDate.AddDays(DurationDays);
            DurationBillableDays = GetNumberOfBillableDays(StartDate, endDate);

            // Truncate to 1 DP
            PlannedWorkHours = Math.Ceiling(10 * DurationBillableDays * 7 * units) / 10;
        }

        /// <summary>
        /// Use 220 billable days per year to estimate the number of billable days between two dates.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        private int GetNumberOfBillableDays(DateTime startDate, DateTime endDate)
        {
            var calendarDays = endDate.Subtract(startDate).Days;
            return (int)Math.Round((calendarDays / 365f) * 220f);
        }

        /// <summary>
        /// Converts the number of billable days into a duration of calendar days assuming 220 billable days per 365 day year.
        /// </summary>
        /// <param name="billableDays"></param>
        /// <returns></returns>
        private int GetNumberOfCalendarDays(int billableDays)
        {
            return (int)Math.Round(billableDays / 220f * 365f);
        }

        /// <summary>
        /// Event invoked when the task type is changed
        /// </summary>
        public event EventHandler TaskTypeChanged;
        protected virtual void OnTaskTypeChanged(EventArgs e)
        {
            EventHandler handler = TaskTypeChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Event invoked when the fixed start setting is changed
        /// </summary>
        public event EventHandler FixedStartChanged;
        protected virtual void OnFixedStartChanged(EventArgs e)
        {
            EventHandler handler = FixedStartChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Event invoked when the end date fixed setting is changed
        /// </summary>
        public event EventHandler EndDateDrivenChanged;
        protected virtual void OnHasFixedEndDateChanged(EventArgs e)
        {
            EventHandler handler = EndDateDrivenChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Event invoked when the is done setting is changed
        /// </summary>
        public event EventHandler DoneChanged;
        protected virtual void OnDoneChanged(EventArgs e)
        {
            EventHandler handler = DoneChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Updates the unmet demand value for this task.
        /// </summary>
        /// <param name="assignedResources">List of resources to use in the update. If not supplied will use the resources saved on the entity.</param>
        public void UpdateUnmetDemand(IEnumerable<Resource> assignedResources = null)
        {
            if (assignedResources == null)
            {
                assignedResources = AssignedResources;
            }
            UnmetDemand = Math.Round(Demand - assignedResources.RoundedSum(r => r.AssignmentFTE, 3), 3);
            if (UnmetDemand < 0) UnmetDemand = 0;
        }

        /// <summary>
        /// Method to update the budget and schedule status flags for this task
        /// </summary>
        public void UpdateStatusFlags()
        {
            // Update the schedule status and set the flag based on a tolerance of 10% either way
            var endDate = DateTime.Today > EndDate ? EndDate : DateTime.Today;
            var daysIntoTask = endDate.Subtract(StartDate.Date).TotalDays;
            var expectedWorkToDate = (PlannedWorkHours / DurationDays) * daysIntoTask;
            var maxWork = expectedWorkToDate * 1.1;
            var minWork = expectedWorkToDate * 0.9;

            // If a task is done, it can be regarded as being on schedule regardless on when it was actually completed.
            if (IsDone) ScheduleStatus = ScheduleStatus.OnSchedule;

            // Simple condition for late
            else if (ActualWorkHours < minWork) ScheduleStatus = ScheduleStatus.Late;

            // Can't be ahead if you have done all the planned work already
            else if (ActualWorkHours > maxWork && ActualWorkHours < PlannedWorkHours) ScheduleStatus = ScheduleStatus.Ahead;

            // Within tolerance or already working beyond the planned work which will be reflected in the budget flag
            else ScheduleStatus = ScheduleStatus.OnSchedule;

            // Effort is somewhat related to costs except you can spend more than the planned amount
            // which would not be captured by a schedule flag
            if (ScheduleStatus == ScheduleStatus.Late) BudgetStatus = BudgetStatus.Underspend;
            else if (ActualWorkHours > PlannedWorkHours) BudgetStatus = BudgetStatus.Overspend;
            else BudgetStatus = BudgetStatus.OnBudget;

        }

        /// <summary>
        /// Checks whether the task has any provisional resources assigned to it.
        /// </summary>
        /// <returns></returns>
        public bool HasProvisionalResources()
        {
            return AssignedResources.Any(r => r.IsProvisional);
        }

        /// <summary>
        /// Checks whether the task has any unmet demand.
        /// </summary>
        /// <returns></returns>
        public bool HasUnmetDemand()
        {
            return UnmetDemand > 0;
        }

        /// <summary>
        /// Checks whether the task has any absent resources and the task is running or will start in 7 days.
        /// </summary>
        /// <returns></returns>
        public bool HasAbsentResourcesAndStartsWithinAWeek()
        {
            return AssignedResources.Any(r => r.Person.IsCurrentlyAbsent()) && DateTime.Today.AddDays(7) >= StartDate && DateTime.Today <= EndDate;
        }

        /// <summary>
        /// Returns the percentage of the minimum demand that is unmet.
        /// </summary>
        /// <returns></returns>
        public double GetPercentageUnmetDemand()
        {
            return Math.Round(UnmetDemand / Demand * 100);
        }

        /// <summary>
        /// Checks whether this task will start within the next month
        /// </summary>
        /// <returns></returns>
        public bool WillStartWithinAMonth()
        {
            return StartDate.Date > DateTime.Today && StartDate.Date.AddMonths(-1) <= DateTime.Today;
        }

        /// <summary>
        /// Checks whether this task has started within the last week
        /// </summary>
        /// <returns></returns>
        public bool HasStartedInTheLastWeek()
        {
            return StartDate.Date <= DateTime.Today && StartDate.Date >= DateTime.Today.AddDays(-7);
        }

        /// <summary>
        /// Checks whether this task is currently running
        /// </summary>
        /// <returns></returns>
        public bool IsCurrentlyRunning()
        {
            return StartDate.Date <= DateTime.Today && EndDate.Date >= DateTime.Today;
        }

        /// <summary>
        /// Checks whether the absence record provided encroaches on the scheduled task period and up to 7 days before.
        /// </summary>
        /// <returns></returns>
        public bool IsAffectedByAbsence(Absence absence)
        {
            return AssignedResources.Any(r => r.Person == absence.Person) && absence.StartDate.AddDays(7) >= StartDate && absence.StartDate <= EndDate;
        }

        /// <summary>
        /// Check whether any resources on this subtask have a planned period of absence which affects the task.
        /// </summary>
        /// <returns></returns>
        public bool IsAffectedByAbsence()
        {
            foreach (var r in AssignedResources)
            {
                foreach (var a in r.Person.Absences)
                {
                    if (IsAffectedByAbsence(a))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks whether a task is currently running or will run in the future and returns the unmet demand. Returns zero if in the past even if it has unmet demand.
        /// </summary>
        /// <returns></returns>
        public double GetUnmetDemandNowAndInFuture()
        {
            return IsWithin(DateTime.Today) || StartDate > DateTime.Today ? UnmetDemand : 0;
        }

        /// <summary>
        /// Method to get the amount of work planned for this task from its start to the end of the week
        /// assuming the date time provided is a Monday.
        /// </summary>
        /// <param name="currentWeek"></param>
        /// <returns></returns>
        public double GetPlannedWorkUpToEndOfWeek(DateTime currentWeek)
        {
            // Current week DateTime needs to be a Monday
            if (currentWeek.DayOfWeek != DayOfWeek.Monday)
                throw new Exception("This method requires the day to be a Monday!");

            // Work is average planned work per day of duration
            var workPerDay = PlannedWorkHours / DurationDays;

            // Assume runs for full week initially
            var daysUpToEndOfWeek = 7d;

            // Correct if starts or ends in the week
            if (StartDate >= currentWeek && StartDate < currentWeek.AddDays(7) &&
                EndDate >= currentWeek && EndDate < currentWeek.AddDays(7))
            {
                // Starts and finishes in the week
                daysUpToEndOfWeek = EndDate.Subtract(StartDate).TotalDays;
            }
            if (StartDate >= currentWeek && StartDate < currentWeek.AddDays(7))
            {
                // Start in the week
                daysUpToEndOfWeek = currentWeek.AddDays(7).Subtract(StartDate).TotalDays;
            }
            else if (EndDate >= currentWeek && EndDate < currentWeek.AddDays(7))
            {
                // Ends in the week (end date inclusive)
                daysUpToEndOfWeek = EndDate.Subtract(currentWeek).TotalDays + 1;
            }

            return daysUpToEndOfWeek * workPerDay;
        }

        public override int GetId()
        {
            return SubTaskId;
        }
    }
}
