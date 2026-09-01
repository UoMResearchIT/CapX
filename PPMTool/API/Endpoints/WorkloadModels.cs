// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

using Microsoft.AspNetCore.Mvc;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;
using PPMTool.Services;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Workload model change endpoint methods. Superuser-only write access,
/// gated behind SettingType.ImportApiEnabled -- see UoMResearchIT/CapX#1310.
/// </summary>
public static class WorkloadModels
{
    /// <summary>
    /// Create or update one staff member's workload model change (duty/role
    /// FTE split, effective from ChangeDate) -- see
    /// ImportWorkloadModelChangeDTO.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ImportWorkloadModelChangeResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult CreateWorkloadModelChange(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportWorkloadModelChangeDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "WorkloadModels.CreateWorkloadModelChange");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateWorkloadModelChange(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: WorkloadModels: validation failed for '{Username}'/{ChangeDate}: {Errors}", request.Username, request.ChangeDate, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.CreateOrUpdateWorkloadModelChange(context, request);
            logger.LogInformation(
                "API: WorkloadModels: workload model change {WorkloadModelChangeId} for {Username}, effective {ChangeDate} ({Action}) by {User}",
                result.WorkloadModelChangeId, request.Username, request.ChangeDate, result.Created ? "created" : "updated", caller!.Name);
            return Results.Created($"/api/workloadmodels/{result.WorkloadModelChangeId}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: WorkloadModels: error creating workload model change for '{Username}'/{ChangeDate}", request.Username, request.ChangeDate);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
