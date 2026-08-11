// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

using Microsoft.AspNetCore.Mvc;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Superuser-only bulk-import (write) endpoints, gated behind
/// SettingType.ImportApiEnabled (defaults to disabled). See
/// UoMResearchIT/CapX#1310.
/// </summary>
public static class Import
{
    /// <summary>
    /// Create a Faculty (+ Schools) -- there's no other bulk way to
    /// populate an institution's own org-unit list today (see #1310).
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ImportFacultyResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult CreateFaculty(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportFacultyRequestDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = CheckGate(settingsService, http, logger, "CreateFaculty");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateFaculty(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Import: faculty validation failed for '{Name}': {Errors}", request.Name, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.CreateFaculty(context, request);
            logger.LogInformation(
                "API: Import: created Faculty {FacultyId} '{Name}' ({SchoolCount} schools) by {User}",
                result.FacultyId, request.Name, result.SchoolIds.Count, caller!.Name);
            return Results.Created($"/api/import/faculty/{result.FacultyId}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Import error for faculty '{Name}'", request.Name);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Shared gate for the import endpoints: ImportApiEnabled setting, then Superuser role.
    /// </summary>
    private static (bool allowed, User? caller, IResult? result) CheckGate(
        SettingsService settingsService, HttpContext http, ILogger logger, string endpointName)
    {
        if (!settingsService.GetSetting(SettingType.ImportApiEnabled, false))
        {
            logger.LogWarning("API: Import.{Endpoint}: rejected, ImportApiEnabled setting is off", endpointName);
            return (false, null, Results.StatusCode(StatusCodes.Status403Forbidden));
        }

        var caller = GeneralHelpers.GetCurrentUser(http);
        if (!GeneralHelpers.IsSuperUser(caller))
        {
            logger.LogWarning("API: Import.{Endpoint}: caller {User} is not a Superuser", endpointName, caller.Name);
            return (false, null, Results.StatusCode(StatusCodes.Status403Forbidden));
        }

        return (true, caller, null);
    }
}
