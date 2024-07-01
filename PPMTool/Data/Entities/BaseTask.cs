using System;
using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    public abstract class BaseTask : ObjectWithStatusMessages
    {
        [Required]
        public string Name { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Today;

        public DateTime EndDate { get; set; }

        public double PlannedWorkHours { get; set; }

        public double ActualWorkHours { get; set; }

        /// <summary>
        /// The amount of the money this task / project will cost based on the planned work
        /// </summary>
        public double PlannedCost { get; set; }

        public double ActualCost { get; set; }

        /// <summary>
        /// Flag is set by internal processing.
        /// </summary>
        public BudgetStatus BudgetStatus { get; protected set; }

        /// <summary>
        /// Flag is set by internal processing.
        /// </summary>
        public ScheduleStatus ScheduleStatus { get; protected set; }
    }
}
