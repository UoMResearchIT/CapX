// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Models
{
    public abstract class BaseDemandChartItem
    {
        /// <summary>
        /// Start of the week to which this data corresponds
        /// </summary>
        private DateTime weekStart;
        public DateTime WeekStart
        {
            get => weekStart;
            set
            {
                if (value != weekStart)
                {
                    weekStart = value;
                    Period = (int)Math.Ceiling(weekStart.Month / 3f);
                    Year = weekStart.Year;
                }
            }
        }

        /// <summary>
        /// Set automatically when the week is set and represents the period of the year
        /// </summary>
        public int? Period { get; private set; }

        public int? Year { get; private set; }
    }
}
