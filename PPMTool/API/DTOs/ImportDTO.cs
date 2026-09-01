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
    /// Request body for POST /api/faculties/add. There's no other bulk way
    /// to populate an institution's own faculty/school list -- /manageorgunits
    /// is one row at a time, and SeedHelper.SeedOrganisationalUnits() is
    /// hardcoded to Manchester's own list (see discussion on
    /// UoMResearchIT/CapX#1310). Always creates a brand-new Faculty --
    /// to add a School under one that already exists, use
    /// POST /api/schools/add instead.
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
    /// Request body for POST /api/schools/add -- adds a single School under
    /// a Faculty that already exists (unlike ImportFacultyRequestDTO's
    /// nested Schools, which only ever create Schools alongside a brand-new
    /// Faculty). See UoMResearchIT/CapX#1310.
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Code">Must be unique within the Faculty</param>
    /// <param name="FacultyCode">Code of an existing Faculty to add this School under</param>
    public sealed record ImportSchoolRequestDTO(
        string Name,
        string Code,
        string FacultyCode
    );

    /// <summary>Response for a successful School import.</summary>
    /// <param name="SchoolId"></param>
    /// <param name="FacultyId"></param>
    public sealed record ImportSchoolResponseDTO(
        int SchoolId,
        int FacultyId
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
    /// Request body for POST /api/projects/add. One project, with its
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

    /// <summary>
    /// Request body for POST /api/timesheets/add. One week's actual
    /// hours for one person on one project, on a single InnateCodeTask
    /// under that project's InnateActivity code (every Project imported
    /// via POST /api/projects/add auto-provisions one, matching the
    /// pattern SeedHelper.EnsureInnateCodeExists already establishes --
    /// see ImportService.Create). CapX computes a Project's actual hours
    /// by querying Approved Timesheets linked through this InnateActivity
    /// code (see AddTask.razor.cs), so this is the real, native path for
    /// historical actuals -- not a bespoke side-channel field.
    ///
    /// Re-importing the same (Username, WeekStartDate, TaskName) is safe:
    /// the existing entry's hours are overwritten, not accumulated, so a
    /// repeated import doesn't double-count.
    /// </summary>
    /// <param name="Username">CapX Access Control username of an existing Person</param>
    /// <param name="ProjectId">Must already have an InnateActivity code (see remarks)</param>
    /// <param name="WeekStartDate">Must be a Monday -- CapX Timesheets are always Monday-start weeks</param>
    /// <param name="TaskName">Must match one of the project's InnateActivity InnateCodeTask names (the default set is "Development", "Management", "Maintenance")</param>
    /// <param name="MondayHours"></param>
    /// <param name="TuesdayHours"></param>
    /// <param name="WednesdayHours"></param>
    /// <param name="ThursdayHours"></param>
    /// <param name="FridayHours"></param>
    /// <param name="SaturdayHours"></param>
    /// <param name="SundayHours"></param>
    public sealed record ImportTimesheetEntryDTO(
        string Username,
        int ProjectId,
        DateTime WeekStartDate,
        string TaskName,
        double MondayHours,
        double TuesdayHours,
        double WednesdayHours,
        double ThursdayHours,
        double FridayHours,
        double SaturdayHours,
        double SundayHours
    );

    /// <summary>Response for a successful timesheet-entry import.</summary>
    /// <param name="TimesheetId"></param>
    /// <param name="TimesheetCreated">False if an existing Timesheet for this person/week was reused</param>
    /// <param name="EntryCreated">False if an existing entry for this task was updated instead</param>
    /// <param name="TotalHours">Sum of the week's hours for this entry</param>
    public sealed record ImportTimesheetResponseDTO(
        int TimesheetId,
        bool TimesheetCreated,
        bool EntryCreated,
        double TotalHours
    );

    /// <summary>
    /// Request body for POST /api/workloadmodels/add. One workload model
    /// change (duty/role FTE split, effective from ChangeDate) for an
    /// existing Person, identified by Access Control username -- mirrors
    /// WorkloadModelChange, with the same Grade (4-9) and per-duty FTE
    /// (0.0-1.0) ranges the UI enforces (Pages/AddWorkloadModelChange.razor).
    /// ProjectAndServiceManagementFTE isn't accepted directly: it's derived
    /// from ProjectManagementFTE + ServiceManagementFTE, the same as the
    /// entity's own UpdatePSM().
    ///
    /// Idempotent: re-importing the same (Username, ChangeDate) overwrites
    /// the existing change's values rather than creating a duplicate --
    /// CapX itself rejects two changes on the same date for one person (see
    /// AddWorkloadModelChange.razor.cs), so upsert is the only sane import
    /// semantics for repeated/corrected runs.
    /// </summary>
    /// <param name="Username">CapX Access Control username of an existing Person</param>
    /// <param name="ChangeDate">Date this workload model takes effect; applies forward until superseded by the next later ChangeDate (see Person.GetWorkloadModelOnDate)</param>
    /// <param name="Grade">Must be 4-9</param>
    /// <param name="ProjectWorkFTE">0.0-1.0</param>
    /// <param name="BusinessAsUsualFTE">0.0-1.0</param>
    /// <param name="PersonalDevelopmentFTE">0.0-1.0</param>
    /// <param name="StaffManagementFTE">0.0-1.0</param>
    /// <param name="ArchitectureFTE">0.0-1.0</param>
    /// <param name="ServiceManagementFTE">0.0-1.0</param>
    /// <param name="ProjectManagementFTE">0.0-1.0</param>
    /// <param name="Notes"></param>
    public sealed record ImportWorkloadModelChangeDTO(
        string Username,
        DateTime ChangeDate,
        int Grade,
        double ProjectWorkFTE,
        double BusinessAsUsualFTE,
        double PersonalDevelopmentFTE,
        double StaffManagementFTE,
        double ArchitectureFTE,
        double ServiceManagementFTE,
        double ProjectManagementFTE,
        string? Notes
    );

    /// <summary>Response for a successful workload-model-change import.</summary>
    /// <param name="WorkloadModelChangeId"></param>
    /// <param name="Created">False if an existing change on this date was updated instead</param>
    public sealed record ImportWorkloadModelChangeResponseDTO(
        int WorkloadModelChangeId,
        bool Created
    );
}
