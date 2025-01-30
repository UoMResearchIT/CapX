using System;
using System.Collections.Generic;
using System.Linq;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Data
{
    public static class WorkloadModelChartHelper
    {
        public static WLMWeeklyDataChartItem GetWorkloadModelChartData(Person person, DateTime startDate, List<Timesheet> timesheets)
        {
            WorkloadModelChange wlm = person.GetWorkloadModelOnDateOrDefault(startDate);

            WLMWeeklyDataChartItem item = new WLMWeeklyDataChartItem()
            {
                WeekStart = startDate,
                WLMWeeklyTargetsByDuty = new Dictionary<Duty, float>
                {
                    { Duty.Other, 0 },
                    { Duty.ProjectWork, (float)wlm.ProjectWorkFTE },
                    { Duty.BAU, (float)wlm.BusinessAsUsualFTE },
                    { Duty.PersonalDevelopment, (float)wlm.PersonalDevelopmentFTE },
                    { Duty.StaffMgmt, (float)wlm.StaffManagementFTE },
                    { Duty.ProjectAndServiceMgmt, (float)wlm.ProjectAndServiceManagementFTE},
                    { Duty.RSA, (float)wlm.ArchitectureFTE },
                }
            };

            // Loop over each task in the current timesheet
            var currentTimesheet = timesheets.FirstOrDefault(x => x.StartDate.Date == startDate);

            if (currentTimesheet != null)
            {
                foreach (var entry in currentTimesheet.TimesheetEntries)
                {
                    // Update values in the entry as not in DB
                    entry.UpdateTotalHours();

                    // Add the hours for the task to the relevant item in the dictionary
                    item.WeeklyValuesByDuty[entry.InnateCodeTask.Duty] += (float)entry.TotalHours;
                }
            }

            // Find total hours worked (excluding leave)
            float totalHours = 0f;
            foreach (var duty in item.WeeklyValuesByDuty.Keys.Where(x => x != Duty.Other))
            {
                totalHours += item.WeeklyValuesByDuty[duty];
            }
            item.TotalHoursForWeek = totalHours;

            // How many hours expected from WLM
            var wlmTargetTotalHours = item.WLMWeeklyTargetsByDuty.Sum(x => x.Value) * 35f;

            // Convert raw hours to FTE based on standard week
            foreach (var duty in item.WeeklyValuesByDuty.Keys)
            {
                item.WeeklyValuesByDuty[duty] /= 35f;
            }

            // If underbooked due to time on leave or we are on a shorter working week then scale WLM targets for the week
            if (totalHours < wlmTargetTotalHours)
            {
                var fractionWorking = totalHours / wlmTargetTotalHours;
                foreach (var duty in item.WeeklyValuesByDuty.Keys)
                {
                    item.WLMWeeklyTargetsByDuty[duty] *= fractionWorking;
                }
            }

            item.UpdateWLMNetValues();
            item.CheckAndAdjustPercentages();

            return item;
        }
    }
}
