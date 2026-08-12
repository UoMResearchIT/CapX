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

    /// <summary>
    /// One resourcing/FTE allocation to create alongside the imported project,
    /// on a single "Delivery" SubTask. Matched against an existing Person via
    /// Access Control username.
    /// </summary>
    /// <param name="Username">CapX Access Control username (e.g. "jrhq77") of an existing Person</param>
    /// <param name="AssignmentFTE"></param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate"></param>
    public sealed record ImportResourcingDTO(
        string Username,
        double AssignmentFTE,
        DateTime StartDate,
        DateTime EndDate
    );

    /// <summary>
    /// One comment to create as a Note on the imported project.
    /// </summary>
    /// <param name="AuthorUsername">CapX Access Control username of the author, if resolved; null/empty attributes to the fallback migration-import User</param>
    /// <param name="AuthorDisplayName">Original author's display name, preserved in the Note text when AuthorUsername can't be resolved</param>
    /// <param name="ContentHtml"></param>
    /// <param name="CreatedDate"></param>
    public sealed record ImportCommentDTO(
        string? AuthorUsername,
        string AuthorDisplayName,
        string ContentHtml,
        DateTime CreatedDate
    );

    /// <summary>
    /// Request body for POST /api/import/project. One project, with its
    /// resourcing and comments created in the same call. See MIGRATION.md
    /// (rse-project-scrape, Durham ARC) for the source-side worksheet this
    /// is generated from -- this DTO shape is intentionally source-agnostic,
    /// any institution's own extraction tooling can target it.
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="RTP"></param>
    /// <param name="PI"></param>
    /// <param name="SchoolCode">Access Control School code (e.g. "SMS")</param>
    /// <param name="ProjectManagerUsername">Access Control username of the Project Manager, if known</param>
    /// <param name="Budget"></param>
    /// <param name="CostModel">Must parse as a PPMTool.Data.Enums.CostModel value</param>
    /// <param name="DayRate">Required (must be greater than zero) only when CostModel is "DayRate"</param>
    /// <param name="ProjectStatus">Must parse as a PPMTool.Data.Enums.ProjectStatus value</param>
    /// <param name="Description">HTML</param>
    /// <param name="RequestDocLink"></param>
    /// <param name="ScrumProjectLink"></param>
    /// <param name="ManagementStartDate">Start date for the auto-created project-management SubTask (Duty.ProjectAndServiceMgmt) -- every Project needs one, see ProjectStatusEvaluator</param>
    /// <param name="ManagementEndDate">End date for the auto-created project-management SubTask</param>
    /// <param name="Resourcing"></param>
    /// <param name="Comments"></param>
    public sealed record ImportProjectRequestDTO(
        string Name,
        int RTP,
        string PI,
        string SchoolCode,
        string? ProjectManagerUsername,
        double Budget,
        string CostModel,
        double DayRate,
        string ProjectStatus,
        string Description,
        string RequestDocLink,
        string? ScrumProjectLink,
        DateTime ManagementStartDate,
        DateTime ManagementEndDate,
        IReadOnlyList<ImportResourcingDTO>? Resourcing,
        IReadOnlyList<ImportCommentDTO>? Comments
    );

    /// <summary>Response for a successful import.</summary>
    /// <param name="ProjectId"></param>
    /// <param name="ResourcesCreated"></param>
    /// <param name="NotesCreated"></param>
    public sealed record ImportProjectResponseDTO(
        int ProjectId,
        int ResourcesCreated,
        int NotesCreated
    );
}
