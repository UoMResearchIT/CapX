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

        public float TotalHoursForWeek { get; set; }

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

        public void UpdateWLMNetValues()
        {
            // Loop over all the duties but not including the other category
            foreach (var duty in WeeklyValuesByDuty.Keys.Where(x => x != Duty.Other))
            {
                // For each weekly value compute the net against the WLM
                for (int week = 0; week < WeeklyValuesByDuty.Count; week++)
                {
                    WLMNetByDuty[duty] = TotalHoursForWeek == 0 ? 0 : WeeklyValuesByDuty[duty] - WLMWeeklyTargetsByDuty[duty];
                }
            }

            // Update the min from the size of the aggregates
            IEnumerable<float> flattenedData = WLMNetByDuty.Select(x => x.Value < 0 ? x.Value : 0);
            MinNet = flattenedData.Sum();
            flattenedData = WLMNetByDuty.Select(x => x.Value > 0 ? x.Value : 0);
            MaxNet = flattenedData.Sum();
        }
    }
}