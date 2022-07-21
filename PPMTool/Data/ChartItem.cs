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

        public double Value1 { get; }

        public double Value2 { get; }

        public string Label { get; }

        public ChartItem(string label, DateTime start, DateTime end, double value1, double value2)
        {
            StartDate = start;
            EndDate = end;
            Value1 = value1;
            Value2 = value2;
            Label = label;
        }

        public string GetColourStringFTE(double value)
        {
            if (value == 100) return "#00783c";
            if (value > 100) return "#e3001b";
            if (value > 50) return "#fc9803";
            return "#ffd500";
        }

        public string GetColourStringWork(double value)
        {
            if (value == 35) return "#00783c";
            if (value > 35) return "#e3001b";
            if (value > 17.5) return "#fc9803";
            return "#ffd500";
        }
    }
}
