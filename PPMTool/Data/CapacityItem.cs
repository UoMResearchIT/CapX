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

        /// <summary>
        /// This is not used when this capacity item represents a sum of projects. Otherwise the project the capacity item corresponds to.
        /// </summary>
        public string ProjectName { get; }

        public CapacityItem(string personName, DateTime start, DateTime end, double fte, string projectName = null)
        {
            PersonName = personName;
            StartDate = start;
            EndDate = end;
            FTE = fte;
            ProjectName = projectName;
        }

        public string GetColourString()
        {
            if (FTE == 100) return "#00783c";
            if (FTE > 100) return "#e3001b";
            if (FTE > 50) return "#fc9803";
            return "#ffd500";
        }
    }
}
