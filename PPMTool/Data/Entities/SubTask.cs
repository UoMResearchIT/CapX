using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentDateTime;
using Microsoft.VisualBasic.CompilerServices;
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
            StartDate = DateTime.Now;
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

        public virtual IList<Resource> AssignedResources { get; set; }

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
        /// Method to determine whether a date [startDate endDate).
        /// If end date and start date are the same evaluates against start date.
        /// </summary>
        /// <param name="testDate">Date to test</param>
        /// <returns></returns>
        internal bool IsWithin(DateTime testDate)
        {
            return StartDate == EndDate ? testDate == StartDate : testDate >= StartDate && testDate < EndDate;
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

        /// <summary>
        /// Update the work, duration (and end date) or units based on the configuration of the task
        /// Work = Duration * Units / 100
        /// Units = Sum of Resource Percentage
        /// Returns null if successful otherwise error message
        /// </summary>
        public string Schedule()
        {
            try
            {
                // Sum up assigned resources and determine latest start date of assigned resources
                double units = 0d;
                DateTime latestStart = default;
                string latestStarter = string.Empty;
                foreach (var r in AssignedResources)
                {
                    units += r.Percentage / 100;
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
                        StartDate = GetNextWorkingDay(Predecessor.EndDate);
                    }

                    // Check whether we need to drive from resources
                    if (units > 0d && latestStart > StartDate)
                    {
                        latestStart = GetNextWorkingDay(latestStart);
                        Debug.WriteLine($"** Start date being changed to {latestStart.Date.ToShortDateString()}, driven by resource {latestStarter}");
                        StartDate = latestStart.Date;
                    }
                }



                // Update core parameters
                if (TaskType == TaskType.FixedUnits)
                {
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
                    // Always updates duration and leaves units fixed
                    UpdateDuration(units);
                }
                else
                {
                    // Always updates the work and leaves units fixed
                    UpdateWork(units);
                }

                // Update cost
                PlannedCost = 0d;
                foreach (var res in AssignedResources)
                {
                    PlannedCost += (res.Percentage / (100 * units)) * PlannedWorkHours * res.Person.HourlyRate;
                }

                // Set end date
                EndDate = StartDate.Date.AddDays(DurationDays).Date;

                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
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
                DurationBusinessDays = (int)Math.Ceiling(PlannedWorkHours / (7 * units));
                var estimatedEndDate = StartDate.AddBusinessDays(DurationBusinessDays);
                DurationDays = (int)Math.Round(estimatedEndDate.Date.Subtract(StartDate.Date).TotalDays);
            }
        }

        private void UpdateWork(double units)
        {
            // Duration input is calendar days so need to compute business days
            var endDate = StartDate.AddDays(DurationDays);
            DurationBusinessDays = GetNumberOfBusinessDays(StartDate, endDate);
            PlannedWorkHours = DurationBusinessDays * 7 * units;
        }

        private DateTime GetNextWorkingDay(DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday) date = date.AddDays(2);
            else if (date.DayOfWeek == DayOfWeek.Sunday) date = date.AddDays(1);
            return date;
        }


        private int GetNumberOfBusinessDays(DateTime startDate, DateTime endDate)
        {
            // Same day returns zero
            if (startDate.Date == endDate.Date)
            {
                return 0;
            }

            // Cannot start a task on a weekend
            if (startDate.DayOfWeek == DayOfWeek.Saturday || startDate.DayOfWeek == DayOfWeek.Sunday) throw new Exception("Cannot start a task on a weekend!");

            // If end date is a weekend day then move on to the following Monday
            endDate = GetNextWorkingDay(endDate);

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
    }
}
