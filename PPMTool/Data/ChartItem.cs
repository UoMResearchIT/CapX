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

        public string Colour { get; }

        public ChartItem(string colour, string label, DateTime start, DateTime end, double value1, double value2)
        {
            StartDate = start;
            EndDate = end;
            Value1 = value1;
            Value2 = value2;
            Label = label;
            Colour = colour;
        }

        public static string GetColourStringFTE(double value, double capacity)
        {
            var percent = (int)Math.Round(value / capacity);
            return GetColourStringPercentage(percent);
        }

        public static string GetColourStringPercentage(double percent)
        {
            if (percent < 50) return "#488f31";
            if (percent < 75) return "#76a263";
            if (percent < 100) return "#9fb494";
            if (percent == 100) return "#c6c6c6";
            if (percent < 125) return "#d69fa1";
            if (percent < 150) return "#dd757d";
            return "#de425b";
        }
    }
}
