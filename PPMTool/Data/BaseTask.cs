using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using NodaMoney;

namespace PPMTool.Data
{
    public abstract class BaseTask
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public double PlannedWorkHours { get; set; }

        public double ActualWorkHours { get; set; }

        public Money PlannedCost { get; set; }

        public Money ActualCost { get; set; }

        public bool IsDone { get; set; }
    }
}
