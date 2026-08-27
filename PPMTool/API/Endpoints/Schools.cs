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
/// School endpoint methods. Superuser-only write access, gated behind
/// SettingType.ImportApiEnabled -- see UoMResearchIT/CapX#1310.
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
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportSchoolRequestDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Schools.CreateSchool");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateSchool(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Schools: school validation failed for '{Name}': {Errors}", request.Name, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.CreateSchool(context, request);
            logger.LogInformation(
                "API: Schools: created School {SchoolId} '{Name}' under Faculty {FacultyId} by {User}",
                result.SchoolId, request.Name, result.FacultyId, caller!.Name);
            return Results.Created($"/api/schools/{result.SchoolId}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Schools: error creating school '{Name}'", request.Name);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
