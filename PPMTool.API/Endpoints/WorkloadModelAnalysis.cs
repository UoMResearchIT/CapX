using Microsoft.AspNetCore.Mvc;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;

namespace PPMTool.API.Endpoints
{
    /// <summary>
    /// Provides endpoints for Workload Model Analysis data.
    /// </summary>
    public static class WorkloadModelAnalysis
    {
        /// <summary>
        /// Provides Workload Model Analysis data for a set of people across a date range.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <param name="http"></param>
        /// <param name="personNames">Comma separated list of names, with spaces replaced with underscores</param>
        /// <param name="startDate">In format yyyy-MM-dd</param>
        /// <param name="endDate">In format yyyy-MM-dd</param>
        /// <param name="compareToWLM">Whether data should be net with respect to workload model</param>
        /// <param name="normalisedByTotalHours">Whether data should be a fraction of FTE worked rather than absolute FTE</param>
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
                var success = Helpers.ParseDateTime(startDate, out DateTime start);
                if (!success)
                {
                    logger.LogWarning($"API: GetWorkloadAnalysisDataDateRange: Invalid start date {startDate}");
                    return Results.BadRequest($"Invalid start date {startDate}. Must be in the format yyyy-MM-dd.");
                }
                success = Helpers.ParseDateTime(endDate, out DateTime end);
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
                    var person = await Helpers.FindPersonWithLineManagerByNameAsync(context, name);
                    if (person == null) return Results.NotFound($"Person '{name}' not found.");

                    if (!Helpers.IsSuperUserOrLineManagerOrSelf(context, http, person)) return Results.Unauthorized();

                    selectedPeople.Add(person);
                }

                var results = await Helpers.GenerateWlmAnalysisDataAsync(
                    context,
                    selectedPeople,
                    start,
                    end,
                    compareToWLM,
                    normalisedByTotalHours
                );

                logger.LogInformation($"API: Returned WLM Analysis data as JSON for {personNames}.");
                return Results.Json(results);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WorkloadModelAnalysis Endpoint Error");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
