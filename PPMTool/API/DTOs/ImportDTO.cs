// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>Response for a failed import -- nothing was written.</summary>
    /// <param name="Errors"></param>
    public sealed record ImportErrorDTO(
        IReadOnlyList<string> Errors
    );

    /// <summary>One School to create under the imported Faculty.</summary>
    /// <param name="Name"></param>
    /// <param name="Code">Must be unique within the Faculty</param>
    public sealed record ImportSchoolDTO(
        string Name,
        string Code
    );

    /// <summary>
    /// Request body for POST /api/import/faculty. There's no other bulk way
    /// to populate an institution's own faculty/school list -- /manageorgunits
    /// is one row at a time, and SeedHelper.SeedOrganisationalUnits() is
    /// hardcoded to Manchester's own list (see discussion on
    /// UoMResearchIT/CapX#1310).
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Code">Must be unique across all Faculties</param>
    /// <param name="Schools"></param>
    public sealed record ImportFacultyRequestDTO(
        string Name,
        string Code,
        IReadOnlyList<ImportSchoolDTO>? Schools
    );

    /// <summary>Response for a successful Faculty (+ Schools) import.</summary>
    /// <param name="FacultyId"></param>
    /// <param name="SchoolIds">In the same order as the request's Schools list</param>
    public sealed record ImportFacultyResponseDTO(
        int FacultyId,
        IReadOnlyList<int> SchoolIds
    );
}
