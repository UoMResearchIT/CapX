using System;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents an aggregation of the assignments of a particular person for the purposes of plotting.
    /// Can hold a couple of values that can vary in meaning depending on the chart being used.
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

        public string TooltipMessages { get; }

        public ChartItem(
            string colour,
            string label,
            DateTime start,
            DateTime end,
            double value1,
            double value2,
            bool isHatched,
            string tooltipMessages = null)
        {
            StartDate = start;
            EndDate = end;
            Value1 = value1;
            Value2 = value2;
            Label = label;
            Colour = colour;
            IsHatched = isHatched;
            TooltipMessages = tooltipMessages;
        }

        /// <summary>
        /// Helper method to get the colour string from an occpuancy
        /// </summary>
        /// <param name="value"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public static string GetColourStringFTE(double value, double capacity)
        {
            // If someone has zero capacity
            int percent = 0;
            if (capacity == 0)
            {
                // If value is zero then it can be coloured as fully allocated
                // Otherwise should show definitely red
                percent = value == 0 ? 100 : 1000;
            }
            else
            {
                percent = (int)Math.Round(value * 100 / capacity);
            }
            return GetColourStringPercentage(percent);
        }

        /// <summary>
        /// Helper method to get the colour string from a percentage
        /// </summary>
        /// <param name="percent"></param>
        /// <returns></returns>
        public static string GetColourStringPercentage(int percent)
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
