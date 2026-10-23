using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;

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

                var resourceDtos = resources.Select(x => new ResourceDTO(
                    ResourceId: x.ResourceId,
                    SubTaskId: x.SubTask.SubTaskId,
                    PersonId: x.Person.PersonId,
                    FundedFromFundingSourceId: x.FundedFrom?.FundingSourceId,
                    AssignmentFTE: x.AssignmentFTE,
                    BilledFTE: x.BilledFTE,
                    PlannedCost: x.PlannedCost
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

                var dto = new ResourceDTO(
                    ResourceId: resource.ResourceId,
                    SubTaskId: resource.SubTask.SubTaskId,
                    PersonId: resource.Person.PersonId,
                    FundedFromFundingSourceId: resource.FundedFrom?.FundingSourceId,
                    AssignmentFTE: resource.AssignmentFTE,
                    BilledFTE: resource.BilledFTE,
                    PlannedCost: resource.PlannedCost
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
    }
}
