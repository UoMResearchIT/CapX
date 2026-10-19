using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;
using PPMTool.Data.Enums;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Funding source endpoint methods.
/// </summary>
public static class FundingSources
{
    /// <summary>
    /// Get all funding sources for a project.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="http"></param>
    /// <param name="projectId">The project ID.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FundingSourceDTO>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetAllFundingSourcesAsync(
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
                    "API: GetAllFundingSources: Invalid projectId {ProjectId}",
                    projectId);

                return Results.BadRequest("Project ID must be greater than zero.");
            }

            var caller = GeneralHelpers.GetCurrentUser(http);
            var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
            if (!canAccess)
            {
                logger.LogWarning(
                    "API: GetAllFundingSources: Caller does not have permission to access funding source data.");

                return Results.Unauthorized();
            }

            var fundingSources = await context.FundingSources
                .Include(x => x.Project)
                .Where(x => x.Project.ProjectId == projectId.Value)
                .OrderBy(x => x.FundingSourceId)
                .ToListAsync();

            var fundingSourceDtos = fundingSources.Select(x => new FundingSourceDTO(
                FundingSourceId: x.FundingSourceId,
                ProjectId: x.Project.ProjectId,
                AmountAvailable: x.AmountAvailable,
                Description: x.Description,
                AccountCode: x.AccountCode,
                FundingSourceType: x.FundingSourceType.GetDescription()
            )).ToList();

            logger.LogInformation(
                "API: GetAllFundingSources: Returned {Count} funding source records for projectId {ProjectId}.",
                fundingSourceDtos.Count,
                projectId);

            return Results.Json(fundingSourceDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetAllFundingSources: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get one funding source by funding source ID.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="http"></param>
    /// <param name="fundingSourceId">The funding source ID.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FundingSourceDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetFundingSourceByIdAsync(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        [FromQuery] int? fundingSourceId)
    {
        try
        {
            if (!fundingSourceId.HasValue || fundingSourceId.Value <= 0)
            {
                logger.LogWarning(
                    "API: GetFundingSourceById: Invalid fundingSourceId {FundingSourceId}",
                    fundingSourceId);

                return Results.BadRequest("Funding source ID must be greater than zero.");
            }

            var caller = GeneralHelpers.GetCurrentUser(http);
            var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
            if (!canAccess)
            {
                logger.LogWarning(
                    "API: GetFundingSourceById: Caller does not have permission to access funding source data.");

                return Results.Unauthorized();
            }

            var fundingSource = await context.FundingSources
                .Include(x => x.Project)
                .FirstOrDefaultAsync(x => x.FundingSourceId == fundingSourceId.Value);

            if (fundingSource == null)
            {
                logger.LogWarning(
                    "API: GetFundingSourceById: No funding source found for fundingSourceId {FundingSourceId}",
                    fundingSourceId);

                return Results.NotFound();
            }

            var dto = new FundingSourceDTO(
                FundingSourceId: fundingSource.FundingSourceId,
                ProjectId: fundingSource.Project.ProjectId,
                AmountAvailable: fundingSource.AmountAvailable,
                Description: fundingSource.Description,
                AccountCode: fundingSource.AccountCode,
                FundingSourceType: fundingSource.FundingSourceType.GetDescription()
            );

            logger.LogInformation(
                "API: GetFundingSourceById: Returned funding source record for fundingSourceId {FundingSourceId}",
                fundingSourceId);

            return Results.Json(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetFundingSourceById: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}