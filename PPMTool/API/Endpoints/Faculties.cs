// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    /// Get all faculties.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FacultyDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetAllFacultiesAsync(
        PPMToolContext context,
        ILogger logger,
        HttpContext http)
    {
        try
        {
            var caller = GeneralHelpers.GetCurrentUser(http);
            var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
            if (!canAccess)
            {
                logger.LogWarning("API: GetAllFaculties: Caller does not have permission to access faculty data.");

                return Results.Unauthorized();
            }

            var faculties = await context.Faculties
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            var facultyDtos = faculties.Select(x => new FacultyDTO(
                FacultyId: x.FacultyId,
                Name: x.Name
            )).ToList();

            logger.LogInformation(
                "API: GetAllFaculties: Returned {Count} faculty records.",
                facultyDtos.Count);

            return Results.Json(facultyDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetAllFaculties: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get one faculty by faculty ID.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="http"></param>
    /// <param name="facultyId">The faculty ID.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FacultyDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetFacultyByIdAsync(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        [FromQuery] int? facultyId)
    {
        try
        {
            if (!facultyId.HasValue || facultyId.Value <= 0)
            {
                logger.LogWarning(
                    "API: GetFacultyById: Invalid facultyId {FacultyId}",
                    facultyId);

                return Results.BadRequest("Faculty ID must be greater than zero.");
            }

            var caller = GeneralHelpers.GetCurrentUser(http);
            var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
            if (!canAccess)
            {
                logger.LogWarning("API: GetFacultyById: Caller does not have permission to access faculty data.");

                return Results.Unauthorized();
            }

            var faculty = await context.Faculties
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FacultyId == facultyId.Value);

            if (faculty == null)
            {
                logger.LogWarning(
                    "API: GetFacultyById: No faculty found for facultyId {FacultyId}",
                    facultyId);

                return Results.NotFound();
            }

            var dto = new FacultyDTO(
                FacultyId: faculty.FacultyId,
                Name: faculty.Name
            );

            logger.LogInformation(
                "API: GetFacultyById: Returned faculty record for facultyId {FacultyId}",
                facultyId);

            return Results.Json(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetFacultyById: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

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
}
