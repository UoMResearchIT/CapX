// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>One School to create with a new Faculty.</summary>
    /// <param name="Name"></param>
    /// <param name="Code">Must not be used by any other School, in any Faculty</param>
    public sealed record ImportSchoolDTO(
        string Name,
        string Code
    );

    /// <summary>
    /// Request body for POST /api/faculties/add. Creates a new Faculty with
    /// its Schools. To add a School to an existing Faculty, use
    /// POST /api/schools/add.
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Code">Must be unique across all Faculties</param>
    /// <param name="Schools">Schools to create in the Faculty, if any</param>
    public sealed record ImportFacultyRequestDTO(
        string Name,
        string Code,
        IReadOnlyList<ImportSchoolDTO>? Schools
    );

    /// <summary>Response for a created Faculty.</summary>
    /// <param name="FacultyId"></param>
    /// <param name="SchoolIds">In the same order as the request's Schools</param>
    public sealed record ImportFacultyResponseDTO(
        int FacultyId,
        IReadOnlyList<int> SchoolIds
    );

    /// <summary>Request body for POST /api/schools/add. Adds one School to an existing Faculty.</summary>
    /// <param name="Name"></param>
    /// <param name="Code">Must not be used by any other School, in any Faculty</param>
    /// <param name="FacultyCode">Code of the Faculty to add the School to</param>
    public sealed record ImportSchoolRequestDTO(
        string Name,
        string Code,
        string FacultyCode
    );

    /// <summary>Response for a created School.</summary>
    /// <param name="SchoolId"></param>
    /// <param name="FacultyId"></param>
    public sealed record ImportSchoolResponseDTO(
        int SchoolId,
        int FacultyId
    );

    /// <summary>
    /// Request body for PUT /api/faculties/update. Identifies the Faculty by
    /// its current Code and changes the fields supplied. Doesn't change its
    /// Schools: see UpdateSchoolRequestDTO.
    /// </summary>
    /// <param name="Code">Current Code of the Faculty</param>
    /// <param name="Name">New Name, if changing</param>
    /// <param name="NewCode">New Code, if changing</param>
    public sealed record UpdateFacultyRequestDTO(
        string Code,
        string? Name,
        string? NewCode
    );

    /// <summary>Response for an updated Faculty.</summary>
    /// <param name="FacultyId"></param>
    public sealed record UpdateFacultyResponseDTO(
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
