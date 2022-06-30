using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents an aggregation of the assignments of a particular person for the purposes of plotting
    /// </summary>
    public class ChartItem
    {
        public DateTime StartDate { get; }

        public DateTime EndDate { get; }

        public double Value { get; }

        public string Label { get; }

        public ChartItem(string label, DateTime start, DateTime end, double value)
        {
            StartDate = start;
            EndDate = end;
            Value = value;
            Label = label;
        }

        public string GetColourStringFTE()
        {
            if (Value == 100) return "#00783c";
            if (Value > 100) return "#e3001b";
            if (Value > 50) return "#fc9803";
            return "#ffd500";
        }

        public string GetColourStringWork()
        {
            if (Value == 35) return "#00783c";
            if (Value > 35) return "#e3001b";
            if (Value > 17.5) return "#fc9803";
            return "#ffd500";
        }
    }
}
