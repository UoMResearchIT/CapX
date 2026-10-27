// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;
using PPMTool.Data.Enums;

namespace PPMTool.API.Endpoints
{
    /// <summary>
    /// Sub-task endpoint methods.
    /// </summary>
    public static class SubTasks
    {
        /// <summary>
        /// Get all sub-tasks for a project.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <param name="http"></param>
        /// <param name="projectId">The project ID.</param>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SubTaskDTO>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static async Task<IResult> GetAllSubTasksAsync(
            PPMToolContext context,
            ILogger logger,
            HttpContext http,
            [FromQuery] int? projectId)
        {
            try
            {
                if (!projectId.HasValue || projectId.Value <= 0)
                {
                    logger.LogWarning(
                        "API: GetAllSubTasks: Invalid projectId {ProjectId}",
                        projectId);

                    return Results.BadRequest("Project ID must be greater than zero.");
                }

                var caller = GeneralHelpers.GetCurrentUser(http);
                var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
                if (!canAccess)
                {
                    logger.LogWarning(
                        "API: GetAllSubTasks: Caller does not have permission to access sub-task data.");

                    return Results.Unauthorized();
                }

                var subTasks = await context.SubTasks
                    .AsNoTracking()
                    .Include(x => x.OwningProject)
                    .Where(x => x.OwningProject.ProjectId == projectId.Value)
                    .OrderBy(x => x.StartDate)
                    .ThenBy(x => x.Name)
                    .ToListAsync();

                var subTaskDtos = subTasks.Select(x => new SubTaskDTO(
                    SubTaskId: x.SubTaskId,
                    OwningProjectProjectId: x.OwningProject.ProjectId,
                    Name: x.Name,
                    StartDate: DateOnly.FromDateTime(x.StartDate),
                    EndDate: DateOnly.FromDateTime(x.EndDate),
                    TaskDuty: x.TaskDuty.GetDescription()
                )).ToList();

                logger.LogInformation(
                    "API: GetAllSubTasks: Returned {Count} sub-task records for projectId {ProjectId}.",
                    subTaskDtos.Count,
                    projectId);

                return Results.Json(subTaskDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API: GetAllSubTasks: error");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get one sub-task by sub-task ID.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <param name="http"></param>
        /// <param name="subTaskId">The sub-task ID.</param>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SubTaskDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static async Task<IResult> GetSubTaskByIdAsync(
            PPMToolContext context,
            ILogger logger,
            HttpContext http,
            [FromQuery] int? subTaskId)
        {
            try
            {
                if (!subTaskId.HasValue || subTaskId.Value <= 0)
                {
                    logger.LogWarning(
                        "API: GetSubTaskById: Invalid subTaskId {SubTaskId}",
                        subTaskId);

                    return Results.BadRequest("Sub-task ID must be greater than zero.");
                }

                var caller = GeneralHelpers.GetCurrentUser(http);
                var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
                if (!canAccess)
                {
                    logger.LogWarning(
                        "API: GetSubTaskById: Caller does not have permission to access sub-task data.");

                    return Results.Unauthorized();
                }

                var subTask = await context.SubTasks
                    .AsNoTracking()
                    .Include(x => x.OwningProject)
                    .FirstOrDefaultAsync(x => x.SubTaskId == subTaskId.Value);

                if (subTask == null)
                {
                    logger.LogWarning(
                        "API: GetSubTaskById: No sub-task found for subTaskId {SubTaskId}",
                        subTaskId);

                    return Results.NotFound();
                }

                var dto = new SubTaskDTO(
                    SubTaskId: subTask.SubTaskId,
                    OwningProjectProjectId: subTask.OwningProject.ProjectId,
                    Name: subTask.Name,
                    StartDate: DateOnly.FromDateTime(subTask.StartDate),
                    EndDate: DateOnly.FromDateTime(subTask.EndDate),
                    TaskDuty: subTask.TaskDuty.GetDescription()
                );

                logger.LogInformation(
                    "API: GetSubTaskById: Returned sub-task record for subTaskId {SubTaskId}",
                    subTaskId);

                return Results.Json(dto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API: GetSubTaskById: error");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
