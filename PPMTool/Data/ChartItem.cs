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
        public static string GetColourStringFTE(double value, double capacity, bool useHotColdScale = false)
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
            return GetColourStringPercentage(percent, useHotColdScale);
        }

        /// <summary>
        /// Helper method to get the colour string from a percentage
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="useHotColdScale"></param>
        /// <returns></returns>
        public static string GetColourStringPercentage(int percent, bool useHotColdScale = false)
        {
            if (useHotColdScale)
            {
                return InterpolateColor(280, 0.1, 1, 1, 0.3, percent);
            }
            else
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

        public static string InterpolateColor(double H, double startS, double startV, double endS, double endV, double percentage)
        {
            // Clamp percentage between 0 and 1
            percentage = Math.Max(0, Math.Min(1, percentage / 100));

            // Interpolate Saturation and Value
            double s = startS + (endS - startS) * percentage;
            double v = startV + (endV - startV) * percentage;

            // Convert HSV to RGB
            (int r, int g, int b) = HSVtoRGB(H, s, v);

            // Return the color as a hex string
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private static (int, int, int) HSVtoRGB(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;

            double rPrime, gPrime, bPrime;

            if (h >= 0 && h < 60)
            {
                rPrime = c;
                gPrime = x;
                bPrime = 0;
            }
            else if (h >= 60 && h < 120)
            {
                rPrime = x;
                gPrime = c;
                bPrime = 0;
            }
            else if (h >= 120 && h < 180)
            {
                rPrime = 0;
                gPrime = c;
                bPrime = x;
            }
            else if (h >= 180 && h < 240)
            {
                rPrime = 0;
                gPrime = x;
                bPrime = c;
            }
            else if (h >= 240 && h < 300)
            {
                rPrime = x;
                gPrime = 0;
                bPrime = c;
            }
            else
            {
                rPrime = c;
                gPrime = 0;
                bPrime = x;
            }

            int r = (int)((rPrime + m) * 255);
            int g = (int)((gPrime + m) * 255);
            int b = (int)((bPrime + m) * 255);

            return (r, g, b);
        }
    }
}
