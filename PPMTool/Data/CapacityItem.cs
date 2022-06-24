using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents an aggregation of the assignments of a particular person for the purposes of plotting
    /// </summary>
    public class CapacityItem
    {
        public DateTime StartDate { get; }

        public DateTime EndDate { get; }

        public double FTE { get; }

        public string PersonName { get; }

        public CapacityItem(string personName, DateTime start, DateTime end, double fte)
        {
            PersonName = personName;
            StartDate = start;
            EndDate = end;
            FTE = fte;
        }

        public string GetColourString()
        {
            if (FTE == 100) return "#00783c";
            if (FTE > 100) return "#e3001b";
            return "#ffd500";
        }
    }
}
