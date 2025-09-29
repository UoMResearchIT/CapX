using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;
using PPMTool.Data.Helpers;

namespace PPMTool.API.Endpoints
{
    /// <summary>
    /// Provides endpoints for Workload Model Analysis data.
    /// </summary>
    public static class WorkLoadModel
    {
        /// <summary>
        /// Provides Workload Model Analysis data for selected people across a date range. Use , to add more people.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<WLMAnalysisPersonDataDTO>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public static async Task<IResult> GetWorkloadAnalysisData(
            PPMToolContext context,
            ILogger logger,
            HttpContext http,
            [FromQuery] string personNames,
            [FromQuery] string startDate,
            [FromQuery] string endDate,
            [FromQuery] bool compareToWLM = false,
            [FromQuery] bool normalisedByTotalHours = false)
        {
            try
            {
                // Try parse the datetimes
                var success = APIHelper.ParseDateTime(startDate, out DateTime start);
                if (!success)
                {
                    logger.LogWarning($"API: GetWorkloadAnalysisDataDateRange: Invalid start date {startDate}");
                    return Results.BadRequest($"Invalid start date {startDate}. Must be in the format yyyy-MM-dd.");
                }
                success = APIHelper.ParseDateTime(endDate, out DateTime end);
                if (!success)
                {
                    logger.LogWarning($"API: GetWorkloadAnalysisDataDateRange: Invalid end date {endDate}");
                    return Results.BadRequest($"Invalid end date {endDate}. Must be in the format yyyy-MM-dd.");
                }

                // Split the comma-separated names
                var names = personNames.Split(',').Select(n => n.Trim()).Where(n => !string.IsNullOrEmpty(n)).ToList();
                if (!names.Any()) return Results.BadRequest("No person names provided.");

                // Same calculations as the WLM
                start = start.StartOfWeek();
                end = end.StartOfWeek().AddDays(6);

                // Validate each person and check authorisation
                var selectedPeople = new List<Data.Entities.Person>();
                foreach (var name in names)
                {
                    var person = await APIHelper.FindPersonWithLineManagerByNameAsync(context, name);
                    if (person == null) return Results.NotFound($"Person '{name}' not found.");

                    if (!APIHelper.IsSuperUserOrLineManagerOrSelf(context, http, person)) return Results.Unauthorized();

                    selectedPeople.Add(person);
                }

                // Prepare to gather the results for each person
                var results = new List<WLMAnalysisPersonDataDTO>();
                string units = GetUnits(compareToWLM, normalisedByTotalHours);

                // Process data for each selected person
                foreach (var person in selectedPeople)
                {
                    var personWeeklyData = new List<WLMWeeklyAnalysisDTO>();

                    // Fetch all timesheets for the person within the date range
                    var allTimesheets = await context.Timesheets
                        .Include(t => t.TimesheetEntries).ThenInclude(e => e.InnateCodeTask).ThenInclude(tk => tk.InnateCode)
                        .Where(t => t.Owner.PersonId == person.PersonId && t.StartDate >= start && t.StartDate <= end)
                        .ToListAsync();

                    var weekStart = start;

                    // Same logic as WLM
                    while (weekStart <= end)
                    {
                        var wlmDataItem = WorkloadModelChartHelper.GetWorkloadModelChartData(person, weekStart, allTimesheets);

                        if (normalisedByTotalHours)
                        {
                            wlmDataItem.SwitchNormalisation(true);
                        }

                        wlmDataItem.UpdateWLMNetValues(normalisedByTotalHours);

                        var sourceData = compareToWLM ? wlmDataItem.WLMNetByDuty : wlmDataItem.WeeklyValuesByDuty;

                        var dutiesDict = sourceData
                            .ToDictionary(kvp => kvp.Key.GetDescription(), kvp => kvp.Value);

                        personWeeklyData.Add(new WLMWeeklyAnalysisDTO(weekStart, units, dutiesDict));
                        weekStart = weekStart.AddDays(7);
                    }
                    results.Add(new WLMAnalysisPersonDataDTO(person.Name, personWeeklyData));
                }

                logger.LogInformation($"API: Returned WLM Analysis data as JSON for {personNames}.");
                return Results.Json(results);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WorkloadModelAnalysis Endpoint Error");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Generates a descriptive string for the units based on query parameters.
        /// </summary>
        /// <param name="compare">Whether the data is a comparison against the WLM.</param>
        /// <param name="normalise">Whether the data is normalised.</param>
        /// <returns>A string describing the data units.</returns>
        private static string GetUnits(bool compare, bool normalise)
        {
            var title = compare ? "Difference between Time Booked and WLM" : "Time Booked";
            title += normalise ? " (Fraction of Total Hours)" : " (FTE)";
            return title;
        }
    }
}
