// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Radzen;

namespace PPMTool.Models
{
    /// <summary>
    /// Model to hold information about the request clock associated with a project.
    /// </summary>
    public class RequestClockDetails
    {
        public RequestClockDetails(TimeSpan timeRemaining, double clockTime)
        {
            DaysRemaining = (int)timeRemaining.TotalDays;
            ClockPercentage = 100 - Math.Round(Math.Max(DaysRemaining, 0) * 100 / clockTime, 0);
            if (ClockPercentage < 80)
            {
                ClockColour = "var(--rz-success)";
                BorderClass = "rz-border-success";
                ProgressBarStyle = ProgressBarStyle.Success;
            }
            else if (ClockPercentage < 100)
            {
                ClockColour = "var(--rz-warning)";
                BorderClass = "rz-border-warning";
                ProgressBarStyle = ProgressBarStyle.Warning;
            }
            else
            {
                ClockColour = "var(--rz-danger)";
                BorderClass = "rz-border-danger";
                ProgressBarStyle = ProgressBarStyle.Danger;
            }
        }

        public int DaysRemaining { get; }
        public double ClockPercentage { get; }
        public string ClockColour { get; }
        public string BorderClass { get; }
        public ProgressBarStyle ProgressBarStyle { get; }

        /// <summary>
        /// Whether or not the request duration should cause an error
        /// </summary>
        /// <returns></returns>
        internal bool ShouldError()
        {
            return ClockPercentage >= 100;
        }

        /// <summary>
        /// Whether or not the request duration should cause a warning
        /// </summary>
        /// <returns></returns>
        internal bool ShouldWarn()
        {
            return ClockPercentage >= 80 && ClockPercentage < 100;
        }
    }
}
