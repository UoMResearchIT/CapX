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
/// Methods for manipulating subtasks through the API.
/// </summary>
public static class Tasks
{
    /// <summary>
    /// Get a Project's tasks, identified by RTP.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TaskDTO>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult GetTasks(
        PPMToolContext context,
        ImportTaskService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromQuery] int rtp)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckWriteApiAccess(settingsService, http, logger, "Tasks.GetTasks");
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
    /// Create a fixed-duration task on an existing Project. See ImportTaskDTO.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ImportTaskResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult CreateTask(
        PPMToolContext context,
        ImportTaskService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportTaskDTO request)
    {
        return GeneralHelpers.ExecuteImportWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Tasks)}.{nameof(CreateTask)}",
            validate: _ => importService.ValidateTaskCreate(context, request),
            logValidationFailure: errors => logger.LogWarning("API: Tasks: task create validation failed for RTP {RTP}/'{Name}': {Errors}", request.RTP, request.Name, string.Join("; ", errors)),
            execute: caller =>
            {
                // CreateTask saves more than once (SubTaskService.Add commits), so a
                // failure part-way through would otherwise leave a partial write.
                using var transaction = context.Database.BeginTransaction();
                var result = importService.CreateTask(context, request);
                transaction.Commit();
                logger.LogInformation("API: Tasks: created SubTask {SubTaskId} ('{Name}') on RTP {RTP} by {User}", result.SubTaskId, request.Name, request.RTP, caller.Name);
                return Results.Created($"/api/tasks/{result.SubTaskId}", result);
            },
            logException: ex => logger.LogError(ex, "API: Tasks: error creating task for RTP {RTP}/'{Name}'", request.RTP, request.Name));
    }

    /// <summary>
    /// Update a task's name, duty, dates or demand, identified by SubTaskId.
    /// See UpdateTaskRequestDTO.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateTaskResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult UpdateTask(
        PPMToolContext context,
        ImportTaskService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] UpdateTaskRequestDTO request)
    {
        return GeneralHelpers.ExecuteImportWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Tasks)}.{nameof(UpdateTask)}",
            validate: _ => importService.ValidateTaskUpdate(context, request),
            logValidationFailure: errors => logger.LogWarning("API: Tasks: task update validation failed for SubTaskId {SubTaskId}: {Errors}", request.SubTaskId, string.Join("; ", errors)),
            execute: caller =>
            {
                // SubTaskService.Update commits before the cost recalculation saves again.
                using var transaction = context.Database.BeginTransaction();
                var result = importService.UpdateTask(context, request);
                transaction.Commit();
                logger.LogInformation("API: Tasks: updated SubTask {SubTaskId} by {User}", result.SubTaskId, caller.Name);
                return Results.Ok(result);
            },
            logException: ex => logger.LogError(ex, "API: Tasks: error updating SubTask {SubTaskId}", request.SubTaskId));
    }

    /// <summary>
    /// Get every assignment on a Project's tasks, identified by RTP.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TaskResourceDTO>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult GetResourcing(
        PPMToolContext context,
        ImportTaskService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromQuery] int rtp)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckWriteApiAccess(settingsService, http, logger, "Tasks.GetResourcing");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateTaskResourcingGet(context, rtp);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Tasks: get resourcing validation failed for RTP {RTP}: {Errors}", rtp, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var resourcing = importService.GetResourcingForRTP(context, rtp);
            logger.LogInformation("API: Tasks: returned {Count} resourcing assignment(s) for RTP {RTP} to {User}", resourcing.Count, rtp, caller!.Name);
            return Results.Json(resourcing);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Tasks: error getting resourcing for RTP {RTP}", rtp);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Add assignments to an existing task. See ImportTaskResourcingRequestDTO.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ImportTaskResourcingResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult AddResourcing(
        PPMToolContext context,
        ImportTaskService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportTaskResourcingRequestDTO request)
    {
        return GeneralHelpers.ExecuteImportWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Tasks)}.{nameof(AddResourcing)}",
            validate: _ => importService.ValidateTaskResourcingAdd(context, request),
            logValidationFailure: errors => logger.LogWarning("API: Tasks: resourcing add validation failed for SubTaskId {SubTaskId}: {Errors}", request.SubTaskId, string.Join("; ", errors)),
            execute: caller =>
            {
                var result = importService.AddTaskResourcing(context, request);
                logger.LogInformation("API: Tasks: added {Count} resourcing assignment(s) to SubTask {SubTaskId} by {User}", result.ResourcesCreated, result.SubTaskId, caller.Name);
                return Results.Created($"/api/tasks/{result.SubTaskId}", result);
            },
            logException: ex => logger.LogError(ex, "API: Tasks: error adding resourcing to SubTaskId {SubTaskId}", request.SubTaskId));
    }

    /// <summary>
    /// Update an assignment's FTE, provisional flag or task, identified by
    /// ResourceId. See UpdateTaskResourcingRequestDTO.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateTaskResourcingResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult UpdateResourcing(
        PPMToolContext context,
        ImportTaskService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] UpdateTaskResourcingRequestDTO request)
    {
        return GeneralHelpers.ExecuteImportWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Tasks)}.{nameof(UpdateResourcing)}",
            validate: _ => importService.ValidateTaskResourcingUpdate(context, request),
            logValidationFailure: errors => logger.LogWarning("API: Tasks: resourcing update validation failed for ResourceId {ResourceId}: {Errors}", request.ResourceId, string.Join("; ", errors)),
            execute: caller =>
            {
                var result = importService.UpdateTaskResourcing(context, request);
                logger.LogInformation("API: Tasks: updated Resource {ResourceId} (now on SubTask {SubTaskId}) by {User}", result.ResourceId, result.SubTaskId, caller.Name);
                return Results.Ok(result);
            },
            logException: ex => logger.LogError(ex, "API: Tasks: error updating Resource {ResourceId}", request.ResourceId));
    }
}
