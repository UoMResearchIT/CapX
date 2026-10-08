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
/// Faculty endpoint methods.
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
        ImportOrgUnitService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportFacultyRequestDTO request)
    {
        return GeneralHelpers.ExecuteImportWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Faculties)}.{nameof(CreateFaculty)}",
            request,
            validate: () => importService.ValidateFaculty(context, request),
            logValidationFailure: errors => logger.LogWarning("API: Faculties: faculty validation failed for '{Name}': {Errors}", request.Name, string.Join("|", errors)),
            execute: caller =>
            {
                var result = importService.CreateFaculty(context, request);

                logger.LogInformation(
                    "API: Faculties: created Faculty {FacultyId} '{Name}' ({SchoolCount} schools) by {User}",
                    result.FacultyId, request.Name, result.SchoolIds.Count, caller.Name);
                return Results.Created($"/api/faculties/{result.FacultyId}", result);
            },
            logException: ex => logger.LogError(ex, "API: Faculties: error creating faculty '{Name}'", request.Name));
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
        ImportOrgUnitService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] UpdateFacultyRequestDTO request)
    {
        return GeneralHelpers.ExecuteImportWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Faculties)}.{nameof(UpdateFaculty)}",
            request,
            validate: () => importService.ValidateFacultyUpdate(context, request),
            logValidationFailure: errors => logger.LogWarning("API: Faculties: faculty update validation failed for '{Code}': {Errors}", request.Code, string.Join("|", errors)),
            execute: caller =>
            {
                var result = importService.UpdateFaculty(context, request);
                logger.LogInformation(
                    "API: Faculties: updated Faculty {FacultyId} (was '{Code}') by {User}",
                    result.FacultyId, request.Code, caller.Name);
                return Results.Ok(result);
            },
            logException: ex => logger.LogError(ex, "API: Faculties: error updating faculty '{Code}'", request.Code));
    }
}
