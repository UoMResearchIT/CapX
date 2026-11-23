// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a school suitable for API data exports.
    /// </summary>
    /// <param name="SchoolId">The unique school ID.</param>
    /// <param name="FacultyId">The ID of the faculty that the school belongs to.</param>
    /// <param name="Name">The name of the school.</param>
    /// <param name="Code">The school's code, as the write endpoints accept it.</param>
    public sealed record SchoolDTO(
        int SchoolId,
        int FacultyId,
        string Name,
        string Code
    );

    /// <summary>
    ///Request body for POST /api/schools/add. Adds one School to an existing Faculty.
    /// Kept separate from <see cref="SchoolDTO"/>: a request carries only what the caller
    /// supplies, and the read record adds the ids and fields CapX derives or links.
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Code">Must not be used by any other School, in any Faculty</param>
    /// <param name="FacultyCode">Code of the Faculty to add the School to</param>
    public sealed record WriteSchoolDTO(
        string Name,
        string Code,
        string FacultyCode
    );

    /// <summary>Response for a created School.</summary>
    /// <param name="SchoolId"></param>
    /// <param name="FacultyId"></param>
    public sealed record WriteSchoolResponseDTO(
        int SchoolId,
        int FacultyId
    );

    /// <summary>
    /// Request body for PUT /api/schools/update. Identifies the School by its
    /// current Code and changes the fields supplied, including moving it to
    /// another Faculty.
    /// </summary>
    /// <param name="Code">Current Code of the School</param>
    /// <param name="Name">New Name, if changing</param>
    /// <param name="NewCode">New Code, if changing; must not be used by any other School</param>
    /// <param name="NewFacultyCode">Code of the Faculty to move the School to, if moving</param>
    public sealed record UpdateSchoolRequestDTO(
        string Code,
        string? Name,
        string? NewCode,
        string? NewFacultyCode
    );
}