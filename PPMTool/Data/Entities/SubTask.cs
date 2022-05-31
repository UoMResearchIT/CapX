using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an individual activity or phase of a project
    /// </summary>
    public class SubTask : BaseTask
    {
        public int SubTaskId { get; set; }

        private TaskType taskType;
        public TaskType TaskType { get => taskType; set { if (taskType != value) { taskType = value; OnTaskTypeChanged(new EventArgs()); } } }

        public IList<Resource> AssignedResources { get; set; }

        /// <summary>
        /// For now, restricted to a singel predecessor task and an "finish-to-start" contraint
        /// </summary>
        public SubTask Predecessor { get; set; }

        private bool hasFixedStart;
        /// <summary>
        /// Basically a simplified constraint type of "Start No Earlier Than" otherwise will be "As Soon As Possible" based on the predecessor end dates
        /// </summary>
        public bool HasFixedStart { get => hasFixedStart; set { if (hasFixedStart != value) { hasFixedStart = value; OnFixedStartChanged(new EventArgs()); } } }


        /// <summary>
        /// Used to drive the end date from the start date assuming 7 hour days
        /// </summary>
        public double DurationHours { get; set; }

        private bool isWorkDriven;
        /// <summary>
        /// For fixed unit tasks indicates whether the work should be used to drive the duration or the other way round
        /// </summary>
        public bool IsWorkDriven { get => isWorkDriven; set { if (isWorkDriven != value) { isWorkDriven = value; OnWorkDrivenChanged(new EventArgs()); } } }

        /// <summary>
        /// Update the work, duration (and end date) or units based on the configuration of the task
        /// Work = Duration * Units / 100
        /// Units = Sum of Resource Percentage
        /// Returns false if unable to configure with current data
        /// </summary>
        public bool Schedule()
        {
            try
            {
                // Update start date from predecessor if necessary
                if (!HasFixedStart && Predecessor != null)
                {
                    StartDate = Predecessor.EndDate;
                }
                else
                {
                    StartDate = DateTime.Now.Date;
                }

                // Sum up assigned resources
                double units = 0d;
                foreach (var r in AssignedResources)
                {
                    units += r.Percentage / 100;
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

                // Set end date
                EndDate = StartDate.AddDays(Math.Ceiling(DurationHours / 7));

                return true;
            }
            catch (Exception)
            {
                // TODO: Should log the exception and pass something useful back to the user
            }
            return false;
        }

        private void UpdateDuration(double units)
        {
            if (units == 0)
            {
                DurationHours = 0;
            }
            else
            {
                DurationHours = PlannedWorkHours / units;
            }
        }

        private void UpdateWork(double units)
        {
            PlannedWorkHours = DurationHours * units;
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
