using System;
using System.Collections.Generic;
using System.Linq;
using PPMTool.Enums;

namespace PPMTool.Data
{
    public class WLMWeeklyDataChartItem
    {
        public DateTime WeekStart { get; set; }

        public Dictionary<Duty, float> WeeklyValuesByDuty { get; set; }

        public Dictionary<Duty, float> WLMWeeklyTargetsByDuty { get; set; }

        public Dictionary<Duty, float> WLMNetByDuty { get; set; }

        public float? MinNet { get; private set; }
        public float? MaxNet { get; private set; }

        /// <summary>
        /// Total hours spent on work this week (excludes time spent in Duty.Other category inc. leave and sickness)
        /// </summary>
        public float? TotalHoursForWeek { get; set; }

        public WLMWeeklyDataChartItem()
        {
            WeeklyValuesByDuty = new Dictionary<Duty, float>();
            foreach (Duty duty in Enum.GetValues(typeof(Duty)))
            {
                WeeklyValuesByDuty.Add(duty, 0f);
            }

            WLMNetByDuty = new Dictionary<Duty, float>();
            foreach (Duty duty in Enum.GetValues(typeof(Duty)))
            {
                WLMNetByDuty.Add(duty, 0f);
            }
        }

        /// <summary>
        /// Method which updates the difference between the weekly values and the weekly targets based on WLM
        /// </summary>
        public void UpdateWLMNetValues()
        {
            // Loop over all the duties but not including the other category
            foreach (var duty in WeeklyValuesByDuty.Keys.Where(x => x != Duty.Other))
            {
                WLMNetByDuty[duty] = TotalHoursForWeek == 0 ? 0 : WeeklyValuesByDuty[duty] - WLMWeeklyTargetsByDuty[duty];
            }

            // Update the min from the size of the aggregates
            IEnumerable<float> flattenedData = WLMNetByDuty.Where(x => x.Key != Duty.Other).Select(x => x.Value < 0 ? x.Value : 0);
            MinNet = flattenedData.Sum();
            flattenedData = WLMNetByDuty.Where(x => x.Key != Duty.Other).Select(x => x.Value > 0 ? x.Value : 0);
            MaxNet = flattenedData.Sum();
        }

        /// <summary>
        /// Method which loops over the <see cref="WeeklyValuesByDuty"/> and normalises them using one of two approaches.
        /// </summary>
        /// <param name="byTotalHours">If true, will normalise with respect to total hours per week rather than 35 hour standard</param>
        /// <exception cref="Exception"></exception>
        internal void NormaliseHours(bool byTotalHours)
        {
            if (byTotalHours && TotalHoursForWeek == null)
            {
                throw new Exception("The total hours for the week for this item has never been set!");
            }

            // Assume standard 35 hour = 1.0 FTE to begin with
            var normalisingParameter = 35f;

            // If using total hours then convert to TotalHours = 1.0 FTE
            if (byTotalHours)
            {
                normalisingParameter = (TotalHoursForWeek ?? 0) == 0 ? 35 : (TotalHoursForWeek ?? 0);
            }

            foreach (var duty in WeeklyValuesByDuty.Keys)
            {
                WeeklyValuesByDuty[duty] /= normalisingParameter;
            }
        }

        /// <summary>
        /// Method to switch between normalisation approaches
        /// </summary>
        /// <param name="toTotalHours">Assumes data is already normalised to standard 35 and converts to normalising by total hours and vice versa</param>
        /// <exception cref="Exception"></exception>
        internal void SwitchNormalisation(bool toTotalHours)
        {
            if (TotalHoursForWeek == null)
            {
                throw new Exception("The total hours for the week for this item has never been set!");
            }

            // Reverse the normalisation in play and apply new one
            foreach (var duty in WeeklyValuesByDuty.Keys)
            {
                WeeklyValuesByDuty[duty] *= toTotalHours ? 35f : (TotalHoursForWeek ?? 0) == 0 ? 35 : (TotalHoursForWeek ?? 0);
                WeeklyValuesByDuty[duty] /= toTotalHours ? (TotalHoursForWeek ?? 0) == 0 ? 35 : (TotalHoursForWeek ?? 0) : 35f;
            }
        }
    }
}