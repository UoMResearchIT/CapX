// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Read only transfer of weekly timesheets and nested entries.
/// Access: superuser, the person, or their line manager.
/// </summary>
public static class Assignments
{
    /// <summary>
    /// Get assignment data for a given period of time.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="http"></param>
    /// <param name="startDate">Optional start date of the query window in the format yyyy-MM-dd.</param>
    /// <param name="endDate">Optional end date of the query window in the format yyyy-MM-dd.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AssignmentDTO>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetAssignmentDataAsync(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        [FromQuery] string startDate = null,
        [FromQuery] string endDate = null)
    {
        try
        {
            DateTime? start = null;
            DateTime? end = null;

            if (!string.IsNullOrWhiteSpace(startDate))
            {
                var success = GeneralHelpers.ParseDateTime(startDate, out DateTime parsedStart);
                if (!success)
                {
                    logger.LogWarning($"API: GetAssignmentData: Invalid start date {startDate}");
                    return Results.BadRequest($"Invalid start date {startDate}. Must be in the format yyyy-MM-dd.");
                }

                start = parsedStart.Date;
            }

            if (!string.IsNullOrWhiteSpace(endDate))
            {
                var success = GeneralHelpers.ParseDateTime(endDate, out DateTime parsedEnd);
                if (!success)
                {
                    logger.LogWarning($"API: GetAssignmentData: Invalid end date {endDate}");
                    return Results.BadRequest($"Invalid end date {endDate}. Must be in the format yyyy-MM-dd.");
                }

                end = parsedEnd.Date;
            }

            if (start.HasValue && end.HasValue && end.Value < start.Value)
            {
                logger.LogWarning($"API: GetAssignmentData: End date {endDate} is before start date {startDate}");
                return Results.BadRequest("End date must be on or after start date.");
            }

            // Authorisation check -- only managers can pull the data for now.
            var caller = GeneralHelpers.GetCurrentUser(http);
            var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
            if (!canAccess)
            {
                logger.LogWarning("API: GetAssignmentData: Caller does not have permission to access the data.");
                return Results.Unauthorized();
            }

            // Get the assignment chunks for the given date range
            var assignmentChunkDTOs = await AssignmentsHelper.GetAssignmentChunksAsync(
                context,
                start,
                end,
                logger);

            logger.LogInformation($"API: GetAssignmentData: Returned {assignmentChunkDTOs.Count} assignment records.");

            return Results.Json(assignmentChunkDTOs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetAssignmentData: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}