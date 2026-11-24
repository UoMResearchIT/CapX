// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.API.Services;
using PPMTool.Data.Context;
using PPMTool.Services;

namespace PPMTool.API.Endpoints
{
    /// <summary>
    /// Resource endpoint methods.
    /// </summary>
    public static class Resources
    {
        /// <summary>
        /// Get all resources for a sub-task.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <param name="http"></param>
        /// <param name="subTaskId">The sub-task ID.</param>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ResourceDTO>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static async Task<IResult> GetAllResourcesAsync(
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
                        "API: GetAllResources: Invalid subTaskId {SubTaskId}",
                        subTaskId);

                    return Results.BadRequest("Sub-task ID must be greater than zero.");
                }

                var caller = GeneralHelpers.GetCurrentUser(http);
                var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
                if (!canAccess)
                {
                    logger.LogWarning(
                        "API: GetAllResources: Caller does not have permission to access resource data.");

                    return Results.Unauthorized();
                }

                var resources = await context.Resources
                    .AsNoTracking()
                    .Include(x => x.SubTask)
                    .Include(x => x.Person)
                    .Include(x => x.FundedFrom)
                    .Where(x => x.SubTask.SubTaskId == subTaskId.Value)
                    .OrderBy(x => x.ResourceId)
                    .ToListAsync();
                var usernames = await GetUsernamesByPersonIdAsync(context, resources.Select(x => x.Person.PersonId));

                var resourceDtos = resources.Select(x => new ResourceDTO(
                    ResourceId: x.ResourceId,
                    SubTaskId: x.SubTask.SubTaskId,
                    PersonId: x.Person.PersonId,
                    FundedFromFundingSourceId: x.FundedFrom?.FundingSourceId,
                    AssignmentFTE: x.AssignmentFTE,
                    BilledFTE: x.BilledFTE,
                    PlannedCost: x.PlannedCost,
                    PersonName: x.Person.Name,
                    Usernames: usernames.GetValueOrDefault(x.Person.PersonId) ?? [],
                    IsProvisional: x.IsProvisional
                )).ToList();

                logger.LogInformation(
                    "API: GetAllResources: Returned {Count} resource records for subTaskId {SubTaskId}.",
                    resourceDtos.Count,
                    subTaskId);

                return Results.Json(resourceDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API: GetAllResources: error");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get one resource by resource ID.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <param name="http"></param>
        /// <param name="resourceId">The resource ID.</param>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResourceDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static async Task<IResult> GetResourceByIdAsync(
            PPMToolContext context,
            ILogger logger,
            HttpContext http,
            [FromQuery] int? resourceId)
        {
            try
            {
                if (!resourceId.HasValue || resourceId.Value <= 0)
                {
                    logger.LogWarning(
                        "API: GetResourceById: Invalid resourceId {ResourceId}",
                        resourceId);

                    return Results.BadRequest("Resource ID must be greater than zero.");
                }

                var caller = GeneralHelpers.GetCurrentUser(http);
                var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
                if (!canAccess)
                {
                    logger.LogWarning(
                        "API: GetResourceById: Caller does not have permission to access resource data.");

                    return Results.Unauthorized();
                }

                var resource = await context.Resources
                    .AsNoTracking()
                    .Include(x => x.SubTask)
                    .Include(x => x.Person)
                    .Include(x => x.FundedFrom)
                    .FirstOrDefaultAsync(x => x.ResourceId == resourceId.Value);

                if (resource == null)
                {
                    logger.LogWarning(
                        "API: GetResourceById: No resource found for resourceId {ResourceId}",
                        resourceId);

                    return Results.NotFound();
                }

                var usernames = await GetUsernamesByPersonIdAsync(context, [resource.Person.PersonId]);
                var dto = new ResourceDTO(
                    ResourceId: resource.ResourceId,
                    SubTaskId: resource.SubTask.SubTaskId,
                    PersonId: resource.Person.PersonId,
                    FundedFromFundingSourceId: resource.FundedFrom?.FundingSourceId,
                    AssignmentFTE: resource.AssignmentFTE,
                    BilledFTE: resource.BilledFTE,
                    PlannedCost: resource.PlannedCost,
                    PersonName: resource.Person.Name,
                    Usernames: usernames.GetValueOrDefault(resource.Person.PersonId) ?? [],
                    IsProvisional: resource.IsProvisional
                );

                logger.LogInformation(
                    "API: GetResourceById: Returned resource record for resourceId {ResourceId}",
                    resourceId);

                return Results.Json(dto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API: GetResourceById: error");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get all resources on a project's sub-tasks, in one call.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <param name="http"></param>
        /// <param name="projectId">The project ID (the database id, not the RTP).</param>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ResourceDTO>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static async Task<IResult> GetAllResourcesForProjectAsync(
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
                        "API: GetAllResourcesForProject: Invalid projectId {ProjectId}",
                        projectId);

                    return Results.BadRequest("Project ID must be greater than zero.");
                }

                var caller = GeneralHelpers.GetCurrentUser(http);
                var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
                if (!canAccess)
                {
                    logger.LogWarning(
                        "API: GetAllResourcesForProject: Caller does not have permission to access resource data.");

                    return Results.Unauthorized();
                }

                var resources = await context.Resources
                    .AsNoTracking()
                    .Include(x => x.SubTask)
                    .Include(x => x.Person)
                    .Include(x => x.FundedFrom)
                    .Where(x => x.SubTask.OwningProject.ProjectId == projectId.Value)
                    .OrderBy(x => x.SubTask.SubTaskId)
                    .ThenBy(x => x.ResourceId)
                    .ToListAsync();
                var usernames = await GetUsernamesByPersonIdAsync(context, resources.Select(x => x.Person.PersonId));

                var resourceDtos = resources.Select(x => new ResourceDTO(
                    ResourceId: x.ResourceId,
                    SubTaskId: x.SubTask.SubTaskId,
                    PersonId: x.Person.PersonId,
                    FundedFromFundingSourceId: x.FundedFrom?.FundingSourceId,
                    AssignmentFTE: x.AssignmentFTE,
                    BilledFTE: x.BilledFTE,
                    PlannedCost: x.PlannedCost,
                    PersonName: x.Person.Name,
                    Usernames: usernames.GetValueOrDefault(x.Person.PersonId) ?? [],
                    IsProvisional: x.IsProvisional
                )).ToList();

                logger.LogInformation(
                    "API: GetAllResourcesForProject: Returned {Count} resource records for projectId {ProjectId}.",
                    resourceDtos.Count,
                    projectId);

                return Results.Json(resourceDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API: GetAllResourcesForProject: error");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Add assignments to an existing task.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WriteResourcesResponseDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(WriteErrorDTO))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static IResult AddResources(
            PPMToolContext context,
            TaskWriteService writeService,
            SettingsService settingsService,
            ILogger logger,
            HttpContext http,
            [FromBody] WriteResourcesDTO request)
        {
            return GeneralHelpers.ExecuteWrite(
                settingsService,
                http,
                logger,
                $"{nameof(Resources)}.{nameof(AddResources)}",
                validate: _ => writeService.ValidateTaskResourcingAdd(context, request),
                logValidationFailure: errors => logger.LogWarning("API: Resources: resourcing add validation failed for SubTaskId {SubTaskId}: {Errors}", request.SubTaskId, string.Join("; ", errors)),
                execute: caller =>
                {
                    var result = writeService.AddTaskResourcing(context, request);
                    logger.LogInformation("API: Resources: added {Count} resourcing assignment(s) to SubTask {SubTaskId} by {User}", result.ResourcesCreated, result.SubTaskId, caller.Name);
                    return Results.Created($"/api/resources/getAll?subTaskId={result.SubTaskId}", result);
                },
                logException: ex => logger.LogError(ex, "API: Resources: error adding resourcing to SubTaskId {SubTaskId}", request.SubTaskId));
        }

        /// <summary>
        /// Update an assignment's FTE, provisional flag or task, identified by
        /// ResourceId.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateResourceResponseDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(WriteErrorDTO))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static IResult UpdateResource(
            PPMToolContext context,
            TaskWriteService writeService,
            SettingsService settingsService,
            ILogger logger,
            HttpContext http,
            [FromBody] UpdateResourceRequestDTO request)
        {
            return GeneralHelpers.ExecuteWrite(
                settingsService,
                http,
                logger,
                $"{nameof(Resources)}.{nameof(UpdateResource)}",
                validate: _ => writeService.ValidateTaskResourcingUpdate(context, request),
                logValidationFailure: errors => logger.LogWarning("API: Resources: resourcing update validation failed for ResourceId {ResourceId}: {Errors}", request.ResourceId, string.Join("; ", errors)),
                execute: caller =>
                {
                    var result = writeService.UpdateTaskResourcing(context, request);
                    logger.LogInformation("API: Resources: updated Resource {ResourceId} (now on SubTask {SubTaskId}) by {User}", result.ResourceId, result.SubTaskId, caller.Name);
                    return Results.Ok(result);
                },
                logException: ex => logger.LogError(ex, "API: Resources: error updating Resource {ResourceId}", request.ResourceId));
        }

        /// <summary>
        /// Usernames of every User linked to each of the given people, in UserId order. A Person
        /// has no navigation to its Users, so they are looked up from the Users table.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="personIds"></param>
        /// <returns></returns>
        private static async Task<Dictionary<int, IReadOnlyList<string>>> GetUsernamesByPersonIdAsync(PPMToolContext context, IEnumerable<int> personIds)
        {
            var ids = personIds.Distinct().ToList();
            return (await context.Users
                .Where(u => u.Person != null && ids.Contains(u.Person.PersonId))
                .OrderBy(u => u.UserId)
                .Select(u => new { u.Person!.PersonId, u.CASUserName })
                .ToListAsync())
                .GroupBy(x => x.PersonId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.CASUserName).ToList());
        }
    }
}
