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
/// School endpoint methods.
/// </summary>
public static class Schools
{
    /// <summary>
    /// Add a single School under a Faculty that already exists. Unlike
    /// Faculties.CreateFaculty, this doesn't create a new Faculty --
    /// use this once an institution's Faculty list is already bootstrapped
    /// and a new School needs adding under one of them.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ImportSchoolResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult CreateSchool(
        PPMToolContext context,
        ImportOrgUnitService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportSchoolRequestDTO request)
    {
        return GeneralHelpers.ExecuteImportWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Schools)}.{nameof(CreateSchool)}",
            request,
            validate: () => importService.ValidateSchool(context, request),
            logValidationFailure: errors => logger.LogWarning("API: Schools: school validation failed for '{Name}': {Errors}", request.Name, string.Join("|", errors)),
            execute: caller =>
            {
                var result = importService.CreateSchool(context, request);
                logger.LogInformation(
                    "API: Schools: created School {SchoolId} '{Name}' under Faculty {FacultyId} by {User}",
                    result.SchoolId, request.Name, result.FacultyId, caller.Name);
                return Results.Created($"/api/schools/{result.SchoolId}", result);
            },
            logException: ex => logger.LogError(ex, "API: Schools: error creating school '{Name}'", request.Name));
    }

    /// <summary>
    /// Update an existing School's Name, Code, and/or parent Faculty.
    /// Identified by its current Code.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ImportSchoolResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult UpdateSchool(
        PPMToolContext context,
        ImportOrgUnitService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] UpdateSchoolRequestDTO request)
    {
        return GeneralHelpers.ExecuteImportWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Schools)}.{nameof(UpdateSchool)}",
            request,
            validate: () => importService.ValidateSchoolUpdate(context, request),
            logValidationFailure: errors => logger.LogWarning("API: Schools: school update validation failed for '{Code}': {Errors}", request.Code, string.Join("|", errors)),
            execute: caller =>
            {
                var result = importService.UpdateSchool(context, request);
                logger.LogInformation(
                    "API: Schools: updated School {SchoolId} (was '{Code}') under Faculty {FacultyId} by {User}",
                    result.SchoolId, request.Code, result.FacultyId, caller.Name);
                return Results.Ok(result);
            },
            logException: ex => logger.LogError(ex, "API: Schools: error updating school '{Code}'", request.Code));
    }
}
