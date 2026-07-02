// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Helpers;

namespace PPMTool.API.Helpers
{
    /// <summary>
    /// Helper methods for the WorkloadModel API endpoints.
    /// </summary>
    internal class WorkloadModelHelpers
    {
        /// <summary>
        /// Generates WLM analysis data for a collection of people over a specified date range.
        /// This is a helper method specific to the WorkLoadModel API.
        /// </summary>
        /// <param name="context">The database context for data retrieval.</param>
        /// <param name="people">The collection of people for whom to generate the analysis.</param>
        /// <param name="startDate">The start date of the analysis period.</param>
        /// <param name="endDate">The end date of the analysis period.</param>
        /// <param name="compareToWLM">A flag to determine if the data should be the difference against the WLM.</param>
        /// <param name="normalisedByTotalHours">A flag to determine if the data should be normalised as a fraction of total hours.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of DTOs with the structured WLM analysis data.</returns>
        internal static async Task<List<WLMAnalysisPersonDataDTO>> GenerateWlmAnalysisDataAsync(
            PPMToolContext context,
            IEnumerable<Person> people,
            DateTime startDate,
            DateTime endDate,
            bool compareToWLM,
            bool normalisedByTotalHours)
        {
            var results = new List<WLMAnalysisPersonDataDTO>();

            string units = WorkloadModelChartHelper.GetChartYAxisTitle(compareToWLM, normalisedByTotalHours);

            foreach (var person in people)
            {
                var personWeeklyData = new List<WLMWeeklyAnalysisDTO>();

                var allTimesheets = await context.Timesheets
                    .Include(t => t.TimesheetEntries).ThenInclude(e => e.InnateCodeTask).ThenInclude(tk => tk.InnateCode)
                    .Where(t => t.Owner.PersonId == person.PersonId && t.StartDate >= startDate && t.StartDate <= endDate)
                    .ToListAsync();

                var weekStart = startDate;
                while (weekStart <= endDate)
                {
                    var wlmDataItem = WorkloadModelChartHelper.GetWorkloadModelChartData(person, weekStart, allTimesheets);

                    if (normalisedByTotalHours)
                    {
                        wlmDataItem.SwitchNormalisation(true);
                    }
                    wlmDataItem.UpdateWLMNetValues(normalisedByTotalHours);

                    var sourceData = compareToWLM ? wlmDataItem.WLMNetByDuty : wlmDataItem.WeeklyValuesByDuty;
                    var dutiesDict = sourceData.ToDictionary(kvp => kvp.Key.GetDescription(), kvp => kvp.Value);

                    personWeeklyData.Add(new WLMWeeklyAnalysisDTO(weekStart, units, dutiesDict));
                    weekStart = weekStart.AddDays(7);
                }
                results.Add(new WLMAnalysisPersonDataDTO(person.Name, personWeeklyData));
            }
            return results;
        }
    }
}
