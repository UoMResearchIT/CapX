using System;
using System.Collections.Generic;
using PPMTool.Enums;

namespace PPMTool.Data
{
    internal class WLMWeeklyDataChartItem
    {
        public string PersonName { get; set; }

        public DateTime WeekStart { get; set; }

        public Dictionary<Duty, float> WeeklyValuesByDuty { get; set; }

        public WLMWeeklyDataChartItem()
        {
            WeeklyValuesByDuty = new Dictionary<Duty, float>();
            foreach (Duty duty in Enum.GetValues(typeof(Duty)))
            {
                WeeklyValuesByDuty.Add(duty, 0f);
            }
        }

    }
}