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
/// Faculty endpoint methods. Superuser-only write access, gated behind
/// SettingType.ImportApiEnabled -- see UoMResearchIT/CapX#1310.
/// </summary>
public static class Faculties
{
    /// <summary>
    /// Create a Faculty (+ Schools) -- there's no other bulk way to
    /// populate an institution's own org-unit list today (see #1310).
    /// Always creates a brand-new Faculty; to add a School under one
    /// that already exists, see Schools.CreateSchool.
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
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Faculties.CreateFaculty");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateFaculty(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Faculties: faculty validation failed for '{Name}': {Errors}", request.Name, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.CreateFaculty(context, request);
            logger.LogInformation(
                "API: Faculties: created Faculty {FacultyId} '{Name}' ({SchoolCount} schools) by {User}",
                result.FacultyId, request.Name, result.SchoolIds.Count, caller!.Name);
            return Results.Created($"/api/faculties/{result.FacultyId}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Faculties: error creating faculty '{Name}'", request.Name);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Update an existing Faculty's Name and/or Code. Identified by its
    /// current Code. Doesn't touch Schools -- see Schools.UpdateSchool.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateFacultyResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult UpdateFaculty(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] UpdateFacultyRequestDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Faculties.UpdateFaculty");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateFacultyUpdate(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Faculties: faculty update validation failed for '{Code}': {Errors}", request.Code, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.UpdateFaculty(context, request);
            logger.LogInformation(
                "API: Faculties: updated Faculty {FacultyId} (was '{Code}') by {User}",
                result.FacultyId, request.Code, caller!.Name);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Faculties: error updating faculty '{Code}'", request.Code);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
