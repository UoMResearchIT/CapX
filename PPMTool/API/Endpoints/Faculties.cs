// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;

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
    }
}