// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;
using PPMTool.Services;

namespace PPMTool.API.Endpoints;

/// <summary>
/// SubTasks on a Project -- Superuser-only writes, gated behind
/// SettingType.ImportApiEnabled -- see UoMResearchIT/CapX#1310. Covers
/// only the fixed-duration, no-predecessor shape ImportService.Create
/// already uses for the auto-created Leadership/Delivery tasks; see
/// ImportTaskDTO remarks for what's deliberately out of scope.
/// </summary>
public static class Tasks
{
    /// <summary>
    /// Get all SubTasks for an existing Project, identified by RTP. An
    /// empty list is a normal result (a Project with only its
    /// auto-created Leadership task, or none at all), not an error.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TaskDTO>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult GetTasks(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromQuery] int rtp)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Tasks.GetTasks");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateTasksGet(context, rtp);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Tasks: get tasks validation failed for RTP {RTP}: {Errors}", rtp, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var tasks = importService.GetTasksForRTP(context, rtp);
            logger.LogInformation("API: Tasks: returned {Count} Task(s) for RTP {RTP} to {User}", tasks.Count, rtp, caller!.Name);
            return Results.Json(tasks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Tasks: error getting tasks for RTP {RTP}", rtp);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Create one fixed-duration SubTask on an existing Project. See
    /// ImportTaskDTO.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ImportTaskResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult CreateTask(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportTaskDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Tasks.CreateTask");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateTaskCreate(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Tasks: task create validation failed for RTP {RTP}/'{Name}': {Errors}", request.RTP, request.Name, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.CreateTask(context, request);
            logger.LogInformation("API: Tasks: created SubTask {SubTaskId} ('{Name}') on RTP {RTP} by {User}", result.SubTaskId, request.Name, request.RTP, caller!.Name);
            return Results.Created($"/api/tasks/{result.SubTaskId}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Tasks: error creating task for RTP {RTP}/'{Name}'", request.RTP, request.Name);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Correct an existing SubTask's Name/TaskDuty/dates/Demand.
    /// Identified by SubTaskId (from GET /api/tasks/getAll). See
    /// UpdateTaskRequestDTO.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateTaskResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult UpdateTask(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] UpdateTaskRequestDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Tasks.UpdateTask");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateTaskUpdate(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Tasks: task update validation failed for SubTaskId {SubTaskId}: {Errors}", request.SubTaskId, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.UpdateTask(context, request);
            logger.LogInformation("API: Tasks: updated SubTask {SubTaskId} by {User}", result.SubTaskId, caller!.Name);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Tasks: error updating SubTask {SubTaskId}", request.SubTaskId);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
