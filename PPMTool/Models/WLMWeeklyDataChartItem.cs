// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data.Enums;

namespace PPMTool.Models
{
    public class WLMWeeklyDataChartItem
    {
        public DateTime WeekStart { get; set; }

        /// <summary>
        /// Raw hours booked by duty. This is the source data and is not modified
        /// when the display options change.
        /// </summary>
        public Dictionary<Duty, float> WeeklyHoursByDuty { get; set; }

        /// <summary>
        /// Values prepared for raw chart display. These are either FTE values
        /// or fractions of all booked hours, depending on the selected mode.
        /// </summary>
        public Dictionary<Duty, float> WeeklyValuesByDuty { get; set; }

        public Dictionary<Duty, float> WLMWeeklyTargetsByDuty { get; set; } = null!;

        public Dictionary<Duty, float> WLMNetByDuty { get; set; }

        public float MinNet { get; private set; } = 0;
        public float MaxNet { get; private set; } = 0;

        /// <summary>
        /// Total hours booked on timesheets this week (excludes time spent in Duty.Other category inc. leave and sickness)
        /// </summary>
        public float TotalHoursBookedForWeekExcludingOther { get; set; } = 0;

        /// <summary>
        /// Total hours booked on timesheets this week
        /// </summary>
        public float TotalHoursBookedForWeek { get; set; } = 0;

        public WLMWeeklyDataChartItem()
        {
            // Initialise
            WeeklyHoursByDuty = new Dictionary<Duty, float>();
            WeeklyValuesByDuty = new Dictionary<Duty, float>();
            WLMNetByDuty = new Dictionary<Duty, float>();

            foreach (Duty duty in Enum.GetValues(typeof(Duty)))
            {
                WeeklyHoursByDuty.Add(duty, 0f);
                WeeklyValuesByDuty.Add(duty, 0f);
                WLMNetByDuty.Add(duty, 0f);
            }
        }

        /// <summary>
        /// Rebuilds the values used for raw chart display from the original
        /// booked hours.
        /// </summary>
        /// <param name="toTotalHours">
        /// If true, values are expressed as a fraction of all booked hours,
        /// including Duty.Other. Otherwise, values are expressed as FTE based
        /// on a standard 35-hour week.
        /// </param>
        public void UpdateWeeklyValues(bool toTotalHours = false)
        {
            foreach (var duty in WeeklyHoursByDuty.Keys)
            {
                // Undo normalisation
                if (toTotalHours)
                {
                    WeeklyValuesByDuty[duty] =
                        TotalHoursBookedForWeek == 0
                            ? 0f
                            : WeeklyHoursByDuty[duty] / TotalHoursBookedForWeek;
                }

                // Normalise by 35 hours to get FTE values
                else
                {
                    WeeklyValuesByDuty[duty] =
                        WeeklyHoursByDuty[duty] / 35f;
                }
            }
        }

        /// <summary>
        /// Updates the difference between booked time and the weekly targets
        /// defined by the WLM.
        /// </summary>
        /// <param name="compareProportion">
        /// If true, compares the proportion of working time booked against each
        /// duty with the expected WLM proportion. Duty.Other is excluded from
        /// both the actual and expected proportions.
        /// </param>
        public void UpdateWLMNetValues(bool compareProportion)
        {
            var wlmDuties = WLMWeeklyTargetsByDuty
                .Where(x => x.Key != Duty.Other);

            var totalExpectedFromWLM = wlmDuties.Sum(x => x.Value);

            foreach (var duty in WeeklyHoursByDuty.Keys)
            {
                // Other is not part of a WLM and must not contribute to the
                // comparison chart or its axis limits.
                if (duty == Duty.Other)
                {
                    WLMNetByDuty[duty] = 0f;
                    continue;
                }

                // Data to display is a comparison of proportions of booked time against the WLM targets
                if (compareProportion)
                {
                    // If no working time was booked, there is no meaningful
                    // proportional comparison to make for this week.
                    if (TotalHoursBookedForWeekExcludingOther == 0 ||
                        totalExpectedFromWLM == 0)
                    {
                        WLMNetByDuty[duty] = 0f;
                        continue;
                    }

                    var actualProportion =
                        WeeklyHoursByDuty[duty] /
                        TotalHoursBookedForWeekExcludingOther;

                    var expectedProportion =
                        WLMWeeklyTargetsByDuty[duty] /
                        totalExpectedFromWLM;

                    // Comparison of the proportions
                    WLMNetByDuty[duty] =
                        actualProportion - expectedProportion;
                }

                // Data to display is a comparison of FTE values against the WLM targets
                else
                {
                    WLMNetByDuty[duty] =
                        WeeklyValuesByDuty[duty] -
                        WLMWeeklyTargetsByDuty[duty];
                }
            }

            var comparisonValues = WLMNetByDuty
                .Where(x => x.Key != Duty.Other)
                .Select(x => x.Value);

            MinNet = comparisonValues
                .Where(value => value < 0)
                .Sum();

            MaxNet = comparisonValues
                .Where(value => value > 0)
                .Sum();
        }
    }
}