// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

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
    /// Faculty endpoint methods.
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
        /// Create a Faculty with its Schools. To add a School to an existing
        /// Faculty, see <see cref="Schools.CreateSchool"/>.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WriteFacultyResponseDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(WriteErrorDTO))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static IResult CreateFaculty(
            PPMToolContext context,
            OrgUnitWriteService writeService,
            SettingsService settingsService,
            ILogger logger,
            HttpContext http,
            [FromBody] WriteFacultyDTO request)
        {
            return GeneralHelpers.ExecuteWrite(
                settingsService,
                http,
                logger,
                $"{nameof(Faculties)}.{nameof(CreateFaculty)}",
                validate: _ => writeService.ValidateFaculty(context, request),
                logValidationFailure: errors => logger.LogWarning("API: Faculties: faculty validation failed for '{Name}': {Errors}", request.Name, string.Join("; ", errors)),
                execute: caller =>
                {
                    var result = writeService.CreateFaculty(context, request);

                    logger.LogInformation(
                        "API: Faculties: created Faculty {FacultyId} '{Name}' ({SchoolCount} schools) by {User}",
                        result.FacultyId, request.Name, result.SchoolIds.Count, caller.Name);
                    return Results.Created($"/api/faculties/{result.FacultyId}", result);
                },
                logException: ex => logger.LogError(ex, "API: Faculties: error creating faculty '{Name}'", request.Name));
        }

        /// <summary>
        /// Update a Faculty's Name or Code, identified by its current Code.
        /// Doesn't change its Schools: see <see cref="Schools.UpdateSchool"/>.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateFacultyResponseDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(WriteErrorDTO))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static IResult UpdateFaculty(
            PPMToolContext context,
            OrgUnitWriteService writeService,
            SettingsService settingsService,
            ILogger logger,
            HttpContext http,
            [FromBody] UpdateFacultyRequestDTO request)
        {
            return GeneralHelpers.ExecuteWrite(
                settingsService,
                http,
                logger,
                $"{nameof(Faculties)}.{nameof(UpdateFaculty)}",
                validate: _ => writeService.ValidateFacultyUpdate(context, request),
                logValidationFailure: errors => logger.LogWarning("API: Faculties: faculty update validation failed for '{Code}': {Errors}", request.Code, string.Join("; ", errors)),
                execute: caller =>
                {
                    var result = writeService.UpdateFaculty(context, request);
                    logger.LogInformation(
                        "API: Faculties: updated Faculty {FacultyId} (was '{Code}') by {User}",
                        result.FacultyId, request.Code, caller.Name);
                    return Results.Ok(result);
                },
                logException: ex => logger.LogError(ex, "API: Faculties: error updating faculty '{Code}'", request.Code));
        }
    }
}