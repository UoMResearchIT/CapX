using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
            statusMessages = new List<StatusMessage>
            {
                new StatusMessage("Task will start soon.", StatusMessage.MessageType.Info, () => WillStartWithinAMonth()),
                new StatusMessage("Task has recently started.", StatusMessage.MessageType.Info, () => HasStartedInTheLastWeek()),
                new StatusMessage("Task has absent resources and has started or will start soon!", StatusMessage.MessageType.Info, () => HasAbsentResourcesAndStartsWithinAWeek()),
                new StatusMessage("Task has resources with absence during or near the start of this task.", StatusMessage.MessageType.Info, () => IsAffectedByAbsence()),
                new StatusMessage("Task has zero demand.", StatusMessage.MessageType.Info, () => HasZeroDemandAndNoResources()),
                new StatusMessage("Task has provisional resources!", StatusMessage.MessageType.Warning, () => HasProvisionalResources()),
                new StatusMessage("Task is under-resourced!", StatusMessage.MessageType.Warning, () => HasUnmetDemand()),
                new StatusMessage("Task has zero demand but assigned resources!", StatusMessage.MessageType.Warning, () => HasZeroDemandButResourced()),
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

        public virtual IList<Resource> AssignedResources { get; set; } = new List<Resource>();

        /// <summary>
        /// For now, restricted to a single predecessor task and an "finish-to-start" contraint
        /// </summary>
        public SubTask Predecessor { get; set; }

        /// <summary>
        /// Represents the list of tasks for which this task is a predecessor
        /// </summary>
        public ICollection<SubTask> Successors { get; set; } = new List<SubTask>();

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
                        EndDate = StartDate.Date.AddDays(1);
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
        /// This is the original demand of the task when we took the request. The current demand (if the requirement has changed) is recorded in the <see cref="Demand"/> property.
        /// </summary>
        [Required]
        public double OriginalDemand { get; set; }

        /// <summary>
        /// The difference between the demand and the sum of the assigned resources.
        /// </summary>
        public double UnmetDemand { get; set; }

        /// <summary>
        /// The amount the start date of this task lags its predecessor. Only used if a predecessor is set.
        /// </summary>
        public int Lag { get; set; }

        /// <summary>
        /// If using a cost model that charges leadership, should it be charged on this task.
        /// Typically disabled for maintenance tasks.
        /// </summary>
        public bool RequiresLeadership { get; set; } = true;

        /// <summary>
        /// Project which owns the subtask
        /// </summary>
        public Project OwningProject { get; set; }

        /// <summary>
        /// Skills that this task requires
        /// </summary>
        public IList<SkillTag> SkillsRequired { get; set; } = new List<SkillTag>();

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
                // Start is driven by predecessor
                if (Predecessor != null)
                {
                    StartDate = Predecessor.EndDate.Date.AddDays(Lag + 1);
                }

                // Sum up assigned resources as units and determine latest start date of assigned resources
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
                    // Cannot schedule using zero demand so schedule according to original demand if necessary
                    units = Demand == 0 ? OriginalDemand : Demand;
                }

                // If we assign someone who doesn't start until after the date then error
                if (AssignedResources.Count > 0 && latestStart > StartDate)
                {
                    return $"This task has a fixed start date of {StartDate}. " +
                        $"{latestStarter} is assigned to this task but they do not start until {latestStart.Date.ToShortDateString()}";
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

                // Update hours on the resources
                foreach (var res in AssignedResources)
                {
                    res.PlannedWorkHours = (res.AssignmentFTE / units) * PlannedWorkHours;
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
                var billableDays = PlannedWorkHours / (7 * units);
                DurationDays = (int)Math.Ceiling(GetNumberOfCalendarDays(billableDays));
                DurationBillableDays = (int)Math.Ceiling(billableDays);
            }
        }

        private void UpdateWork(double units)
        {
            // Duration input is calendar days so need to compute billable days to get work
            var endDate = StartDate.AddDays(DurationDays);
            var billableDays = GetNumberOfBillableDays(StartDate, endDate);
            PlannedWorkHours = (int)Math.Floor(billableDays * 7 * units);
            DurationBillableDays = (int)Math.Ceiling(billableDays);
        }

        /// <summary>
        /// Use 220 billable days per year to estimate the number of billable days between two dates.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        internal static double GetNumberOfBillableDays(DateTime startDate, DateTime endDate)
        {
            var calendarDays = endDate.Date.Subtract(startDate.Date).Days;
            return (calendarDays / 365f) * 220f;
        }

        /// <summary>
        /// Converts the number of billable days into a duration of calendar days assuming 220 billable days per 365 day year.
        /// </summary>
        /// <param name="billableDays"></param>
        /// <returns></returns>
        private double GetNumberOfCalendarDays(double billableDays)
        {
            return (billableDays / 220f) * 365f;
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
        /// <param name="absence"></param>
        /// <param name="id">Id of the person with whom the absence is associated (deleted absences have no person to get this from)</param>
        /// <returns></returns>
        public bool IsAffectedByAbsence(Absence absence, int? id = null)
        {
            if (id == null)
            {
                id = absence.Person.PersonId;
            }
            return AssignedResources.Any(r => r.Person.PersonId == id) && absence.StartDate.Date.AddDays(7) >= StartDate.Date && absence.StartDate.Date <= EndDate.Date;
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
        /// Determines whether the task has zero demand and no resources assigned.
        /// </summary>
        /// <returns></returns>
        public bool HasZeroDemandAndNoResources()
        {
            return Demand == 0 && AssignedResources.Count == 0;
        }

        /// <summary>
        /// Determines whether the task has zero demand but still has resources assigned.
        /// </summary>
        /// <returns></returns>
        public bool HasZeroDemandButResourced()
        {
            return Demand == 0 && AssignedResources.Count > 0;
        }

        /// <summary>
        /// Gets the unmet demand of a task within the window given.
        /// </summary>
        /// <param name="startDate">If null, assumed to be now</param>
        /// <param name="endDate">If null, window just considered to be the future</param>
        /// <returns></returns>
        public double GetUnmetDemandInWindow(DateTime? startDate = null, DateTime? endDate = null)
        {
            // If no end date then include tasks where they end after the start of the window
            // as this is any task that runs now or in the future
            if (endDate == null && EndDate.Date >= (startDate ?? DateTime.Today))
            {
                return UnmetDemand;
            }

            // If we have a defined window then include tasks where any part of the task runs in the window
            return IsWithin(startDate ?? DateTime.Today, endDate ?? DateTime.Today) ? UnmetDemand : 0;
        }

        /// <summary>
        /// Method to get the amount of work planned for this task from its start to the end of the week
        /// assuming the date time provided is a Monday.
        /// </summary>
        /// <param name="currentWeek"></param>
        /// <returns></returns>
        public double GetPlannedWorkWithinCurrentWeek(DateTime currentWeek)
        {
            // Current week DateTime needs to be a Monday
            if (currentWeek.DayOfWeek != DayOfWeek.Monday)
                throw new Exception("This method requires the day to be a Monday!");

            // Daily work is average planned work
            var workPerDay = PlannedWorkHours / DurationDays;

            // Compute the duration of the task in days in this week
            // Assume runs for full week initially (i.e. starts before the week and ends after the week)
            var daysUpToEndOfWeek = 7d;
            if (StartDate.Date >= currentWeek.Date && StartDate.Date < currentWeek.Date.AddDays(7) &&
                EndDate.Date >= currentWeek.Date && EndDate.Date < currentWeek.Date.AddDays(7))
            {
                // Starts and finishes in the week
                daysUpToEndOfWeek = EndDate.Date.Subtract(StartDate.Date).TotalDays;
            }
            else if (StartDate.Date >= currentWeek.Date && StartDate.Date < currentWeek.Date.AddDays(7))
            {
                // Starts in the week
                daysUpToEndOfWeek = currentWeek.Date.AddDays(7).Subtract(StartDate.Date).TotalDays;
            }
            else if (EndDate.Date >= currentWeek.Date && EndDate.Date < currentWeek.Date.AddDays(7))
            {
                // Ends in the week (end date inclusive)
                daysUpToEndOfWeek = EndDate.Date.Subtract(currentWeek.Date).TotalDays + 1;
            }

            return daysUpToEndOfWeek * workPerDay;
        }

        /// <summary>
        /// Updates the actual or planned technical costs of the task based on the resources, model and financial references provided
        /// </summary>
        /// <param name="costModel"></param>
        /// <param name="financialReference"></param>
        /// <param name="projectDayRate"></param>
        /// <returns></returns>
        internal void UpdateSubTaskCosts(CostModel costModel, double? projectDayRate, FinancialReference financialReference)
        {
            // Reset the totals for this sub task
            ActualCost = 0;
            PlannedCost = 0;

            // For each resource assigned, update the costs
            foreach (var res in AssignedResources)
            {
                res.UpdateResourceCosts(costModel, StartDate, EndDate, projectDayRate, financialReference);

                // Sum up the result post-update
                ActualCost += res.ActualCost;
                PlannedCost += res.PlannedCost;
            }
        }
    }
}
