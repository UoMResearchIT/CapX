using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Data
{
    public abstract class BaseTask
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public double PlannedWorkHours { get; set; }

        public double ActualWorkHours { get; set; }

        public double PlannedCost { get; set; }

        public double ActualCost { get; set; }

        public bool IsDone { get; set; }
    }
}
