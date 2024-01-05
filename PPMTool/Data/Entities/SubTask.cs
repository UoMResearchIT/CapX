using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentDateTime;
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
            StartDate = DateTime.Now.Date;
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
        /// Method to determine whether a date [startDate endDate].
        /// If end date and start date are the same evaluates against start date.
        /// The end date is assumed to be a working day for the task so it included in the test.
        /// </summary>
        /// <param name="testDate">Date to test</param>
        /// <returns></returns>
        internal bool IsWithin(DateTime testDate)
        {
            return StartDate == EndDate ? testDate == StartDate : testDate >= StartDate && testDate <= EndDate;
        }


        /// <summary>
        /// Used to drive the end date from the start date assuming 7 hour days. This is includes weekends.
        /// </summary>
        public int DurationDays { get; set; }

        /// <summary>
        /// Used to drive the work assuming 7 hour days. This is excludes weekends.
        /// </summary>
        public int DurationBusinessDays { get; set; }

        private bool isWorkDriven;
        /// <summary>
        /// For fixed unit tasks indicates whether the work should be used to drive the duration or the other way round
        /// </summary>
        public bool IsWorkDriven
        {
            get => isWorkDriven;
            set
            {
                if (isWorkDriven != value)
                {
                    isWorkDriven = value;
                    OnWorkDrivenChanged(new EventArgs());
                }
            }
        }

        private bool isEndDateDriven = true;
        /// <summary>
        /// For fixed duration tasks indicates whether the end date should be driven by the duration or the other way round
        /// </summary>
        public bool IsEndDateDriven
        {
            get => isEndDateDriven;
            set
            {
                if (isEndDateDriven != value)
                {
                    isEndDateDriven = value;

                    // Update the end date to match the start date plus a day
                    if (!isEndDateDriven && EndDate <= StartDate)
                    {
                        EndDate = StartDate.AddDays(1);
                    }
                    OnEndDateDrivenChanged(new EventArgs());
                }
            }
        }

        /// <summary>
        /// Update the work, duration (and end date) or units based on the configuration of the task
        /// Work = Duration * Units / 100
        /// Units = Sum of Resource Percentage
        /// </summary>
        /// <param name="permitUndrivenEndToMove">Whether we can move the end date to maintain 
        /// the duration if the end date is not driven. Only applies to fixed duration tasks.</param>
        /// <returns>Returns null if successful otherwise error message</returns>
        public string Schedule(bool permitUndrivenEndToMove)
        {
            try
            {
                // Sum up assigned resources and determine latest start date of assigned resources
                double units = 0d;
                DateTime latestStart = default;
                string latestStarter = string.Empty;
                foreach (var r in AssignedResources)
                {
                    units += r.Percentage;
                    if (r.Person.StartDate > latestStart)
                    {
                        latestStarter = r.Person.Name;
                        latestStart = r.Person.StartDate;
                    }
                }

                // Start date is fixed
                if (HasFixedStart)
                {
                    // If we assign someone who doesn't start until after the date then error
                    if (units > 0d && latestStart > StartDate)
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
                        // Correct the end date to start on the next working day if necessary
                        StartDate = TaskType == TaskType.FixedWork ? GetNextWorkingDay(Predecessor.EndDate) : Predecessor.EndDate.AddDays(1);
                    }

                    // Check whether we need to drive from resources
                    if (units > 0d && latestStart > StartDate)
                    {
                        latestStart = TaskType == TaskType.FixedWork ? GetNextWorkingDay(latestStart) : Predecessor.EndDate.AddDays(1);
                        Debug.WriteLine($"** Start date being changed to {latestStart.Date.ToShortDateString()}, driven by resource {latestStarter}");
                        StartDate = latestStart.Date;
                    }
                }

                // Update core parameters
                if (TaskType == TaskType.FixedUnits)
                {
                    // End date must be driven
                    IsEndDateDriven = true;

                    // Which one is updated based on preference
                    if (IsWorkDriven)
                    {
                        UpdateDuration(units);
                    }
                    else
                    {
                        UpdateWork(units);
                    }

                }
                else if (TaskType == TaskType.FixedWork)
                {
                    // End Date must be driven
                    IsEndDateDriven = true;

                    // Always updates duration and leaves units fixed
                    UpdateDuration(units);
                }
                else
                {
                    // Make sure the duration is at least zero or greater
                    if (EndDate < StartDate) EndDate = StartDate.Date;

                    // If we are allowed to move the end date to maintain the current duration then set the end date now
                    if (!IsEndDateDriven && permitUndrivenEndToMove) EndDate = StartDate.Date.AddDays(DurationDays - 1).Date;

                    // If the end date is not driven then set duration here from the start and end dates
                    if (!IsEndDateDriven) UpdateDurationFromEndDate();

                    // Always updates the work and leaves units fixed
                    UpdateWork(units);
                }

                // Update cost
                PlannedCost = 0d;
                foreach (var res in AssignedResources)
                {
                    // Assume 7 hours in a day; fallback on default day rate if resource day rate is null
                    PlannedCost += (res.Percentage / units) * PlannedWorkHours * ((res.DayRate ?? res.Person.DayRate) / 7f);
                }

                // Set end date from the duration
                if (IsEndDateDriven) EndDate = StartDate.Date.AddDays(DurationDays - 1).Date;

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
            DurationBusinessDays = GetNumberOfBusinessDays(StartDate, EndDate);
        }

        private void UpdateDuration(double units)
        {
            if (units == 0)
            {
                DurationDays = 0;
                DurationBusinessDays = 0;
            }
            else
            {
                // Correct for annual leave etc. with the 0.84
                DurationBusinessDays = (int)Math.Ceiling(PlannedWorkHours / (7 * units * .84));
                var estimatedEndDate = StartDate.AddBusinessDays(DurationBusinessDays);

                // Tasks that start and end on the same day should still have a duration of 1 day so add a day here
                DurationDays = (int)Math.Round(estimatedEndDate.Date.Subtract(StartDate.Date).TotalDays) + 1;
            }
        }

        private void UpdateWork(double units)
        {
            // Duration input is calendar days so need to compute business days to get work
            var endDate = StartDate.AddDays(DurationDays);
            DurationBusinessDays = GetNumberOfBusinessDays(StartDate, endDate);

            // Correct for annual leave etc. with the 0.84 and truncate to 1 DP
            PlannedWorkHours = Math.Ceiling(10 * DurationBusinessDays * 7 * units * 0.84) / 10;
        }

        private DateTime GetNextWorkingDay(DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday) return date.AddDays(2);
            else if (date.DayOfWeek == DayOfWeek.Friday) return date.AddDays(3);
            return date.AddDays(1);
        }


        private int GetNumberOfBusinessDays(DateTime startDate, DateTime endDate)
        {
            // Same day returns zero
            if (startDate.Date == endDate.Date)
            {
                return 0;
            }

            // Cannot start a fixed work task on a weekend
            if (TaskType == TaskType.FixedWork && (startDate.DayOfWeek == DayOfWeek.Saturday || startDate.DayOfWeek == DayOfWeek.Sunday))
            {
                throw new Exception("Cannot start a task on a weekend!");
            }

            // If end date is a weekend day then move on to the following Monday
            if (endDate.DayOfWeek == DayOfWeek.Saturday || endDate.DayOfWeek == DayOfWeek.Sunday) endDate = GetNextWorkingDay(endDate);

            // Work out the number of normal days
            int normalDays = (int)Math.Round(endDate.Date.Subtract(startDate.Date).TotalDays);

            // Best guess at business days is to take 2 days off for every week
            int guess = normalDays - (normalDays / 7) * 2;
            int lastGuess = guess;

            // Iterate
            int error = int.MaxValue;
            while (error > 0 && guess != 0)
            {
                // Compute error
                var guessedEndDate = startDate.Date.AddBusinessDays(guess);
                error = (int)Math.Round(guessedEndDate.Date.Subtract(endDate.Date).TotalDays);

                // Break out early if found the answer
                if (error == 0) return guess;

                // Update guess by 1 day in the correct direction
                lastGuess = guess;
                guess = lastGuess - (error / Math.Abs(error));
            }

            // Shouldn't end up here
            return lastGuess;
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
        /// Event invoked when the work driven setting is changed
        /// </summary>
        public event EventHandler WorkDrivenChanged;
        protected virtual void OnWorkDrivenChanged(EventArgs e)
        {
            EventHandler handler = WorkDrivenChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Event invoked when the end date driven setting is changed
        /// </summary>
        public event EventHandler EndDateDrivenChanged;
        protected virtual void OnEndDateDrivenChanged(EventArgs e)
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
        /// Method to update the budget and schedule status flags for this task
        /// </summary>
        public void UpdateStatusFlags()
        {
            // Update the schedule status and set the flag based on a tolerance of 10% either way
            var endDate = DateTime.Now.Date > EndDate ? EndDate : DateTime.Now.Date;
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
    }
}
