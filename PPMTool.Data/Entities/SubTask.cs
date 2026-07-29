// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an individual activity or phase of a project
    /// </summary>
    public class SubTask : BaseScheduledTask
    {
        public SubTask()
        {
            // Set default value
            StartDate = DateTime.Today;

            // List of status messages to check for each task which will drive icons
            statusMessages = new List<StatusMessage>
            {
                // Info
                new StatusMessage("Task will start soon.", StatusMessage.MessageType.Info, () => WillStartWithinAMonth()),
                new StatusMessage("Task has recently started.", StatusMessage.MessageType.Info, () => HasStartedInTheLastWeek()),
                new StatusMessage("Task has absent resources and has started or will start soon!", StatusMessage.MessageType.Info, () => HasAbsentResourcesAndStartsWithinAWeek(), FeatureType.Absences), // Absences
                new StatusMessage("Task has resources with absence during or near the start of this task.", StatusMessage.MessageType.Info, () => IsAffectedByAbsence(), FeatureType.Absences), // Absences
                new StatusMessage("Task has zero demand.", StatusMessage.MessageType.Info, () => HasZeroDemandAndNoResources()),
                
                // Warning
                new StatusMessage("Task has provisional resources!", StatusMessage.MessageType.Warning, () => HasProvisionalResources()),
                new StatusMessage("Task is under-resourced!", StatusMessage.MessageType.Warning, () => HasUnmetDemand()),

                // Error
                new StatusMessage("Task has zero demand but assigned resources!", StatusMessage.MessageType.Error, () => HasZeroDemandButResourced()),
                new StatusMessage("Resource on this task has no associated funding source and task is in progress or ran in the past!", StatusMessage.MessageType.Error, () => HasResourceWithNoFundingSourceAndRunning(), FeatureType.ProjectFinance), // Finance
                new StatusMessage("Task has resource(s) with zero FTE assignment!", StatusMessage.MessageType.Warning, () => HasResourceWithZeroFTE()),
                
                // Success
                new StatusMessage("Everything looks OK!", StatusMessage.MessageType.Success, () => !HasActiveStatusMessages())
            };
        }

        /// <summary>
        /// Primary key
        /// </summary>
        public int SubTaskId { get; set; }

        private TaskType taskType;
        /// <summary>
        /// The type of the task to inform how it is scheduled
        /// </summary>
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

        /// <summary>
        /// A list of the resources assigned to the task
        /// </summary>
        public virtual IList<Resource> AssignedResources { get; set; } = new List<Resource>();

        /// <summary>
        /// For now, restricted to a single predecessor task and an "finish-to-start" contraint
        /// </summary>
        public virtual SubTask? Predecessor { get; set; }

        /// <summary>
        /// Represents the list of tasks for which this task is a predecessor
        /// </summary>
        public virtual ICollection<SubTask> Successors { get; set; } = new List<SubTask>();

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

        /// <summary>
        /// Override the setter to call the unmet demand update
        /// </summary>
        [Required]
        public override double Demand
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
        /// Project which owns the subtask
        /// </summary>
        [Required]
        public virtual Project OwningProject { get; set; } = null!;

        /// <summary>
        /// Skills that this task requires
        /// </summary>
        public virtual IList<SkillTag> SkillsRequired { get; set; } = new List<SkillTag>();

        /// <summary>
        /// Which duty the demand for this task should be reflected in. By default, assumes development tasks which will be reflected in the <see cref="Duty.ProjectWork"/> duty.
        /// </summary>
        public Duty TaskDuty { get; set; } = Duty.ProjectWork;

        /// <summary>
        /// Update the work, duration (and end date) or units based on the configuration of the task
        /// Work = Duration * Units
        /// Units = Sum of Resource Assigned FTE
        /// </summary>
        /// <returns>Returns null if successful otherwise error message</returns>
        public string? Schedule()
        {
            try
            {
                // Start is driven by predecessor but only if the start date is not fixed
                if (Predecessor != null && !HasFixedStart)
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
                    // End Date must be driven as by fixing the end date, you are essentially making it fixed duration
                    HasFixedEndDate = false;

                    // Always updates duration and leaves units fixed
                    UpdateDuration(units);

                    // Set end date from the duration (never has fixed end date)
                    UpdateEndDateFromDuration();
                }

                // Fixed Duration Update
                else
                {
                    // If the task has a fixed end date then we are not permitted to move it, check if the start date has moved past the end date
                    if (HasFixedEndDate && StartDate > EndDate)
                    {
                        return $"Task '{Name}' has a fixed end date of {EndDate.ToShortDateString()} which is before the new start date {StartDate.ToShortDateString()} caused by the predecessor.";
                    }

                    // Make sure the duration is at least zero or greater
                    if (EndDate < StartDate) EndDate = StartDate.Date;

                    // If we are allowed to move the end date to maintain the current duration then set the end date now
                    if (!HasFixedEndDate)
                    {
                        UpdateEndDateFromDuration();
                    }
                    // If the end date is fixed then set duration here from the start and end dates
                    else
                    {
                        UpdateDurationFromDates();
                    }

                    // Always updates the work and leaves units fixed
                    UpdateWork(units);
                }

                // Update hours on the resources
                foreach (var res in AssignedResources)
                {
                    if (units > 0)
                    {
                        res.PlannedWorkHours = (res.AssignmentFTE / units) * PlannedWorkHours;
                    }
                    else
                    {
                        res.PlannedWorkHours = 0;
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        /// <summary>
        /// Publicly expose the protected method to recalculate duration from dates
        /// </summary>
        public void RecalculateDurationFromDates()
        {
            UpdateDurationFromDates();
        }

        /// <summary>
        /// Publicly expose the protected method to recalculate end date from duration
        /// </summary>
        public void RecalculateEndDateFromDuration()
        {
            UpdateEndDateFromDuration();
        }

        /// <summary>
        /// Event invoked when the task type is changed
        /// </summary>
        public event EventHandler? TaskTypeChanged;
        protected virtual void OnTaskTypeChanged(EventArgs e)
        {
            EventHandler? handler = TaskTypeChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Event invoked when the fixed start setting is changed
        /// </summary>
        public event EventHandler? FixedStartChanged;
        protected virtual void OnFixedStartChanged(EventArgs e)
        {
            EventHandler? handler = FixedStartChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Event invoked when the end date fixed setting is changed
        /// </summary>
        public event EventHandler? EndDateDrivenChanged;
        protected virtual void OnHasFixedEndDateChanged(EventArgs e)
        {
            EventHandler? handler = EndDateDrivenChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Event invoked when the is done setting is changed
        /// </summary>
        public event EventHandler? DoneChanged;
        protected virtual void OnDoneChanged(EventArgs e)
        {
            EventHandler? handler = DoneChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Updates the unmet demand value for this task.
        /// </summary>
        /// <param name="assignedResources">List of resources to use in the update. If not supplied will use the resources saved on the entity.</param>
        public void UpdateUnmetDemand(IEnumerable<Resource>? assignedResources = null)
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
            return AssignedResources
                .Any(r => r.Person.PersonId == id) &&
                absence.StartDate.Date.AddDays(7) >= StartDate.Date &&
                absence.StartDate.Date <= EndDate.Date;
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
        /// Checks whether any resources on this task have zero FTE assignment.
        /// </summary>
        /// <returns></returns>
        public bool HasResourceWithZeroFTE()
        {
            return AssignedResources.Any(r => r.AssignmentFTE == 0);
        }

        /// <summary>
        /// Checks whether any resources on this task have no associated funding source and the task is currently running or ran in the past.
        /// </summary>
        /// <returns></returns>
        public bool HasResourceWithNoFundingSourceAndRunning()
        {
            return StartDate.Date <= DateTime.Today && AssignedResources.Any(r => r.FundedFrom == null);
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
        /// <param name="ignoreUnmetDemand">Whether the calculation should be based on the demand figure for the task and ignore whatever resources are assigned to it (unless there are none)</param>
        /// <returns></returns>
        public double GetPlannedWorkWithinCurrentWeek(DateTime currentWeek, bool ignoreUnmetDemand)
        {
            // Current week DateTime needs to be a Monday
            if (currentWeek.DayOfWeek != DayOfWeek.Monday)
                throw new Exception("This method requires the day to be a Monday!");

            // If there is no demand then the planned work is zero regardless of resources?
            if (Demand == 0)
            {
                return 0;
            }

            // If there are no resources then the planned work is zero regardless of demand
            if (AssignedResources.Count == 0)
            {
                return 0;
            }

            // If duration is zero then there's no work to distribute
            if (DurationDays == 0)
            {
                return 0;
            }

            // Daily work is average planned work
            var workPerDay = PlannedWorkHours / DurationDays;

            // If ignoring unmet demand then we need to update the planned work for the task
            // since it will have been computed based on the assigned resources which only
            // partially meet the demand. This only matters for fixed duration tasks since the
            // work is the driven quantity in those cases.
            if (ignoreUnmetDemand && UnmetDemand != 0 && TaskType == TaskType.FixedDuration && DurationDays > 0)
            {
                var billableDays = GetNumberOfBillableDays(StartDate, DurationDays);
                var workHours = (int)Math.Floor(billableDays * 7 * Demand);
                workPerDay = workHours / DurationDays;
            }

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
        /// Updates the actual and planned technical costs of the task based on the resources.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="finrefs"></param>
        /// <param name="indirectsPercentage"></param>
        /// <returns>Assignment chunk representation of the resources on the task</returns>
        public IEnumerable<AssignmentChunk> UpdateSubTaskCosts(Project project, IEnumerable<FinancialReference> finrefs, float indirectsPercentage)
        {
            // Reset the totals for this sub task
            ActualCost = 0;
            PlannedCost = 0;
            ActualIndirectCost = 0;
            PlannedIndirectCost = 0;
            List<AssignmentChunk> chunks = new List<AssignmentChunk>();

            // For each resource assigned, update the costs by generating a chunk from the resource
            foreach (var res in AssignedResources)
            {
                chunks.AddRange(res.UpdateResourceCosts(project, this, finrefs, indirectsPercentage));

                // Sum up the result post-update
                ActualIndirectCost += res.ActualIndirectCost;
                PlannedIndirectCost += res.PlannedIndirectCost;
                ActualCost += res.ActualCost;
                PlannedCost += res.PlannedCost;
            }
            return chunks;
        }

        /// <summary>
        /// Returns the assignment value of the resource assignment matching the person given. Zero if not found.
        /// </summary>
        /// <param name="personAssigned"></param>
        /// <returns></returns>
        public double GetAssignmentValueForPerson(Person personAssigned)
        {
            return AssignedResources.FirstOrDefault(x => x.Person?.PersonId == personAssigned.PersonId)?.AssignmentFTE ?? 0;
        }

        /// <summary>
        /// Returns whether the resource assignment matching the person given is provisional or not. False if not found.
        /// </summary>
        /// <param name="personAssigned"></param>
        /// <returns></returns>
        public bool IsProvisionalResource(Person personAssigned)
        {
            return AssignedResources.FirstOrDefault(x => x.Person?.PersonId == personAssigned.PersonId)?.IsProvisional ?? false;
        }
    }
}
