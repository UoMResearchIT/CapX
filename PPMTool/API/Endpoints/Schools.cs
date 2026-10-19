// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;

namespace PPMTool.API.Endpoints
{
    /// <summary>
    /// School endpoint methods.
    /// </summary>
    public static class Schools
    {
        /// <summary>
        /// Get all schools.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SchoolDTO>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static async Task<IResult> GetAllSchoolsAsync(
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
                    logger.LogWarning("API: GetAllSchools: Caller does not have permission to access school data.");

                    return Results.Unauthorized();
                }

                var schools = await context.Schools
                    .Include(x => x.Faculty)
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                var schoolDtos = schools.Select(x => new SchoolDTO(
                    SchoolId: x.SchoolId,
                    FacultyId: x.Faculty.FacultyId,
                    Name: x.Name
                )).ToList();

                logger.LogInformation(
                    "API: GetAllSchools: Returned {Count} school records.",
                    schoolDtos.Count);

                return Results.Json(schoolDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API: GetAllSchools: error");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get one school by school ID.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <param name="http"></param>
        /// <param name="schoolId">The school ID.</param>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SchoolDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static async Task<IResult> GetSchoolByIdAsync(
            PPMToolContext context,
            ILogger logger,
            HttpContext http,
            [FromQuery] int? schoolId)
        {
            try
            {
                if (!schoolId.HasValue || schoolId.Value <= 0)
                {
                    logger.LogWarning(
                        "API: GetSchoolById: Invalid schoolId {SchoolId}",
                        schoolId);

                    return Results.BadRequest("School ID must be greater than zero.");
                }

                var caller = GeneralHelpers.GetCurrentUser(http);
                var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
                if (!canAccess)
                {
                    logger.LogWarning("API: GetSchoolById: Caller does not have permission to access school data.");

                    return Results.Unauthorized();
                }

                var school = await context.Schools
                    .Include(x => x.Faculty)
                    .FirstOrDefaultAsync(x => x.SchoolId == schoolId.Value);

                if (school == null)
                {
                    logger.LogWarning(
                        "API: GetSchoolById: No school found for schoolId {SchoolId}",
                        schoolId);

                    return Results.NotFound();
                }

                var dto = new SchoolDTO(
                    SchoolId: school.SchoolId,
                    FacultyId: school.Faculty.FacultyId,
                    Name: school.Name
                );

                logger.LogInformation(
                    "API: GetSchoolById: Returned school record for schoolId {SchoolId}",
                    schoolId);

                return Results.Json(dto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API: GetSchoolById: error");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}