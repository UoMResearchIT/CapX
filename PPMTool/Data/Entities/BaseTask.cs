using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Data.Entities
{
    public abstract class BaseTask
    {
        [Required]
        public string Name { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now.Date;

        /// <summary>
        /// End date is always a driven quantity in forward scheduling
        /// </summary>
        public DateTime EndDate { get; protected set; }

        public double PlannedWorkHours { get; set; }

        public double ActualWorkHours { get; set; }

        public double PlannedCost { get; set; }

        public double ActualCost { get; set; }

        public bool IsDone { get; set; }
    }
}
