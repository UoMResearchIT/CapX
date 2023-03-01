using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents an aggregation of the assignments of a particular person for the purposes of plotting.
    /// Can hold a couple of values that can vary in menaing depending on the chart being used.
    /// </summary>
    public class ChartItem
    {
        public DateTime StartDate { get; }

        public DateTime EndDate { get; }

        public double Value1 { get; }

        public double Value2 { get; }

        public string Label { get; }

        public string Colour { get; }

        public bool IsHatched { get; }

        public ChartItem(string colour, string label, DateTime start, DateTime end, double value1, double value2, bool isHatched)
        {
            StartDate = start;
            EndDate = end;
            Value1 = value1;
            Value2 = value2;
            Label = label;
            Colour = colour;
            IsHatched = isHatched;
        }

        /// <summary>
        /// Helper method to get the colour string from an occpuancy
        /// </summary>
        /// <param name="value"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public static string GetColourStringFTE(double value, double capacity)
        {
            // If someon has zero capacity then it should be coloured red!
            var percent = capacity == 0 ? 1000 : (int)Math.Round(value / capacity);
            return GetColourStringPercentage(percent);
        }

        /// <summary>
        /// Helper method to get the colour string from a percentage
        /// </summary>
        /// <param name="percent"></param>
        /// <returns></returns>
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
