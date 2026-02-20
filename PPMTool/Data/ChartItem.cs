// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents an aggregation of the assignments of a particular person for the purposes of plotting.
    /// Can hold a couple of values that can vary in meaning depending on the chart being used.
    /// </summary>
    public class ChartItem : IChartItem
    {
        public DateTime StartDate { get; }

        public DateTime EndDate { get; }

        public double Value1 { get; }

        public double Value2 { get; }

        public string Label { get; }

        public string Colour { get; }

        public string TooltipMessages { get; }

        private bool isHatched;
        private bool isFake;

        public ChartItem(
            string colour,
            string label,
            DateTime start,
            DateTime end,
            double value1,
            double value2,
            bool isHatched,
            string tooltipMessages = null,
            bool isFake = false)
        {
            StartDate = start;
            EndDate = end;
            Value1 = value1;
            Value2 = value2;
            Label = label;
            Colour = colour;
            this.isHatched = isHatched;
            TooltipMessages = tooltipMessages;
            this.isFake = isFake;
        }

        /// <summary>
        /// Standard colour palette
        /// </summary>
        private static string[] standardColours =
        [
            "#0072B2",
            "#E69F00",
            "#009E73",
            "#AA4499",
            "#56B4E9",
            "#D55E00",
            "#88CCEE",
            "#117733",
            "#CCCCCC",
            "#332288",
            "#F0E442",
            "#DDCC77",
            "#882255",
            "#6699CC",
            "#CC6677",
            "#44AA99",
            "#CC79A7",
            "#999999"
        ];

        /// <summary>
        /// Returns a hex string from the standard colour palette
        /// </summary>
        /// <param name="arrayIndex"></param>
        /// <returns></returns>
        public static string GetColourString(int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                arrayIndex = 0;
            }
            else if (arrayIndex >= standardColours.Length)
            {
                arrayIndex = arrayIndex - standardColours.Length;
            }
            return standardColours[arrayIndex];
        }

        /// <summary>
        /// Helper method to get the colour string from an occpuancy
        /// </summary>
        /// <param name="value"></param>
        /// <param name="capacity"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static string GetColourStringFTE(double value, double capacity, ColourScale scale = ColourScale.Capacity)
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
            return GetColourStringPercentage(percent, scale);
        }

        /// <summary>
        /// Helper method to get the colour string from a percentage
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static string GetColourStringPercentage(int percent, ColourScale scale = ColourScale.Capacity)
        {
            if (scale == ColourScale.Capacity)
            {
                if (percent < 50) return "#488f31";
                if (percent < 75) return "#76a263";
                if (percent < 100) return "#9fb494";
                if (percent == 100) return "#c6c6c6";
                if (percent < 125) return "#d69fa1";
                if (percent < 150) return "#dd757d";
                return "#de425b";

            }
            else if (scale == ColourScale.Load)
            {
                return InterpolateColor(280, 0.1, 1, 1, 0.3, percent);
            }
            else // TrafficLights
            {
                if (percent < 33) return "#de425b";
                if (percent < 66) return "#ff9800";
                return "#488f31";
            }
        }

        /// <summary>
        /// Helper method to generate an interpolated value of colour given a HSV range.
        /// </summary>
        /// <param name="hue"></param>
        /// <param name="startS"></param>
        /// <param name="startV"></param>
        /// <param name="endS"></param>
        /// <param name="endV"></param>
        /// <param name="percentage"></param>
        /// <returns></returns>
        public static string InterpolateColor(double hue, double startS, double startV, double endS, double endV, double percentage)
        {
            // Clamp percentage between 0 and 1
            percentage = Math.Max(0, Math.Min(1, percentage / 100));

            // Interpolate Saturation and Value
            double s = startS + (endS - startS) * percentage;
            double v = startV + (endV - startV) * percentage;

            // Convert HSV to RGB
            (int r, int g, int b) = HSVtoRGB(hue, s, v);

            // Return the color as a hex string
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        /// <summary>
        /// Convert HSV to RGB values for a colour.
        /// </summary>
        /// <param name="h"></param>
        /// <param name="s"></param>
        /// <param name="v"></param>
        /// <returns></returns>
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

        public bool IsHatched()
        {
            return isHatched;
        }

        public bool IsFake()
        {
            return isFake;
        }
    }
}
