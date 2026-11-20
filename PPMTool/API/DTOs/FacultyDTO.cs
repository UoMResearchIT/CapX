// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a faculty suitable for API data exports.
    /// </summary>
    /// <param name="FacultyId">The unique faculty ID.</param>
    /// <param name="Name">The name of the faculty.</param>
    public sealed record FacultyDTO(
        int FacultyId,
        string Name
    );

    /// <summary>
    /// Request body for POST /api/faculties/add. Creates a new Faculty with
    /// its Schools. To add a School to an existing Faculty, use
    /// POST /api/schools/add.
    /// Kept separate from <see cref="FacultyDTO"/>: a request carries only what the caller
    /// supplies, and the read record adds the ids and fields CapX derives or links.
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Code">Must be unique across all Faculties</param>
    /// <param name="Schools">Schools to create in the Faculty, if any</param>
    public sealed record WriteFacultyDTO(
        string Name,
        string Code,
        IReadOnlyList<WriteFacultySchoolDTO>? Schools
    );

    /// <summary>One School to create with a new Faculty.</summary>
    /// <param name="Name"></param>
    /// <param name="Code">Must not be used by any other School, in any Faculty</param>
    public sealed record WriteFacultySchoolDTO(
        string Name,
        string Code
    );

    /// <summary>Response for a created Faculty.</summary>
    /// <param name="FacultyId"></param>
    /// <param name="SchoolIds">In the same order as the request's Schools</param>
    public sealed record WriteFacultyResponseDTO(
        int FacultyId,
        IReadOnlyList<int> SchoolIds
    );

    /// <summary>
    /// Request body for PUT /api/faculties/update. Identifies the Faculty by
    /// its current Code and changes the fields supplied. Doesn't change its
    /// Schools: see <see cref="UpdateSchoolRequestDTO"/>.
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
}