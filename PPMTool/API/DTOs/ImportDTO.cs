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
    /// Request body for PUT /api/faculties/update. Identifies an existing
    /// Faculty by its current Code; Name and/or Code (rename) are updated
    /// when supplied. Doesn't touch Schools -- see UpdateSchoolRequestDTO.
    /// </summary>
    /// <param name="Code">Current Code of the Faculty to update</param>
    /// <param name="Name">New Name, if changing</param>
    /// <param name="NewCode">New Code, if renaming</param>
    public sealed record UpdateFacultyRequestDTO(
        string Code,
        string? Name,
        string? NewCode
    );

    /// <summary>Response for a successful Faculty update.</summary>
    /// <param name="FacultyId"></param>
    public sealed record UpdateFacultyResponseDTO(
        int FacultyId
    );

    /// <summary>
    /// Request body for PUT /api/schools/update. Identifies an existing
    /// School by its current Code; Name, Code (rename), and/or
    /// NewFacultyCode (re-parent under a different existing Faculty) are
    /// updated when supplied.
    /// </summary>
    /// <param name="Code">Current Code of the School to update</param>
    /// <param name="Name">New Name, if changing</param>
    /// <param name="NewCode">New Code, if renaming</param>
    /// <param name="NewFacultyCode">Code of a different existing Faculty to move this School under, if re-parenting</param>
    public sealed record UpdateSchoolRequestDTO(
        string Code,
        string? Name,
        string? NewCode,
        string? NewFacultyCode
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
    /// Request body for POST /api/projects/notes/add. Adds Comments as
    /// Notes to an existing Project, identified by RTP -- the counterpart
    /// to ImportProjectRequestDTO.Comments for a project that's already
    /// been created (that field only ever gets processed at creation
    /// time; POST /api/projects/add's own Validate() rejects the whole
    /// call outright once the RTP/Name already exists, so there was no
    /// way to add Comments after the fact until this endpoint).
    /// </summary>
    /// <param name="RTP">RTP of the existing Project to add Notes to</param>
    /// <param name="Comments"></param>
    public sealed record ImportNotesRequestDTO(
        int RTP,
        IReadOnlyList<ImportCommentDTO> Comments
    );

    /// <summary>Response for a successful POST /api/projects/notes/add.</summary>
    /// <param name="ProjectId"></param>
    /// <param name="NotesCreated"></param>
    public sealed record ImportNotesResponseDTO(
        int ProjectId,
        int NotesCreated
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
    /// Request body for PUT /api/projects/update. Identifies an existing
    /// Project by its RTP; only the core scalar fields are updatable here
    /// -- Resourcing and Comments are additive actions with their own
    /// semantics via POST /api/projects/add, not something a partial
    /// update should touch. Any field left null is left unchanged.
    /// </summary>
    /// <param name="RTP">RTP of the Project to update</param>
    /// <param name="Name"></param>
    /// <param name="PI"></param>
    /// <param name="SchoolCode">Access Control School code</param>
    /// <param name="ProjectManagerUsername">Pass an empty string to clear the Project Manager, null to leave unchanged</param>
    /// <param name="Budget"></param>
    /// <param name="CostModel">Must parse as a PPMTool.Data.Enums.CostModel value, if changing</param>
    /// <param name="DayRate">Must be greater than zero if the resulting CostModel is "DayRate"</param>
    /// <param name="ProjectStatus">Must parse as a PPMTool.Data.Enums.ProjectStatus value, if changing</param>
    /// <param name="Description">HTML</param>
    /// <param name="RequestDocLink"></param>
    /// <param name="ScrumProjectLink">Pass an empty string to clear, null to leave unchanged</param>
    public sealed record UpdateProjectRequestDTO(
        int RTP,
        string? Name,
        string? PI,
        string? SchoolCode,
        string? ProjectManagerUsername,
        double? Budget,
        string? CostModel,
        double? DayRate,
        string? ProjectStatus,
        string? Description,
        string? RequestDocLink,
        string? ScrumProjectLink
    );

    /// <summary>Response for a successful Project update.</summary>
    /// <param name="ProjectId"></param>
    public sealed record UpdateProjectResponseDTO(
        int ProjectId
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
    /// Request body for PUT /api/timesheets/update. Corrects an existing
    /// TimesheetEntry -- identified by TimesheetEntryId (already exposed by
    /// GET /api/timesheets' TimesheetEntryDTO, so no separate discovery
    /// endpoint is needed). Unlike POST /api/timesheets/add, which always
    /// overwrites all seven days at once (identified by the natural
    /// Username/ProjectId/WeekStartDate/TaskName key), this only touches
    /// fields actually supplied -- correct a single day, or move the entry
    /// to a different task, without resending the rest of the week.
    /// </summary>
    /// <param name="TimesheetEntryId">TimesheetEntryId of the entry to update (see GET /api/timesheets)</param>
    /// <param name="NewTaskName">Move this entry to a different InnateCodeTask under the same project's InnateActivity, if changing. Must not already have its own separate entry on this Timesheet.</param>
    /// <param name="MondayHours"></param>
    /// <param name="TuesdayHours"></param>
    /// <param name="WednesdayHours"></param>
    /// <param name="ThursdayHours"></param>
    /// <param name="FridayHours"></param>
    /// <param name="SaturdayHours"></param>
    /// <param name="SundayHours"></param>
    public sealed record UpdateTimesheetEntryRequestDTO(
        int TimesheetEntryId,
        string? NewTaskName,
        double? MondayHours,
        double? TuesdayHours,
        double? WednesdayHours,
        double? ThursdayHours,
        double? FridayHours,
        double? SaturdayHours,
        double? SundayHours
    );

    /// <summary>Response for a successful TimesheetEntry update.</summary>
    /// <param name="TimesheetEntryId"></param>
    /// <param name="TotalHours">Sum of the week's hours for this entry, after the update</param>
    public sealed record UpdateTimesheetEntryResponseDTO(
        int TimesheetEntryId,
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

    /// <summary>
    /// Request body for POST /api/people/add. Creates a bare Person record --
    /// no linked User/Access-Control account, unlike the Username the other
    /// import endpoints (Resourcing, Timesheets, WorkloadModels) require an
    /// existing one for. For bootstrapping people who never need to log in
    /// (e.g. departed staff, needed only so historical cost data can be
    /// matched against a real Person) -- not for provisioning active staff,
    /// which should go through normal onboarding instead. Rejects a
    /// duplicate Name/initials the same way Pages/AddPerson.razor does
    /// (PersonService.Add) rather than upserting -- unlike
    /// WorkloadModelChange/Timesheet imports, there's no natural
    /// (key, date) pair to upsert on here.
    /// </summary>
    /// <param name="Name">Full name -- ShortName (initials) is auto-derived the same way Person.Name's setter does for the UI</param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate">Null if still in post</param>
    /// <param name="FTE">FTE of the post (see Person.FTE) -- 0.0-1.0, same range Pages/AddPerson.razor enforces</param>
    public sealed record ImportPersonDTO(
        string Name,
        DateTime StartDate,
        DateTime? EndDate,
        double FTE
    );

    /// <summary>Response for a successful Person import.</summary>
    /// <param name="PersonId"></param>
    /// <param name="ShortName">Auto-derived initials</param>
    public sealed record ImportPersonResponseDTO(
        int PersonId,
        string ShortName
    );

    /// <summary>
    /// Request body for PUT /api/people/update. Identifies an existing bare
    /// Person by PersonId (returned from POST /api/people/add); Name,
    /// StartDate, EndDate, and/or FTE are updated when supplied. Supersedes
    /// the original 2026-09-01 decision to leave this endpoint create-only
    /// -- manual corrections (e.g. reconciling a placeholder StartDate)
    /// turned out common enough to be worth a real endpoint rather than
    /// always going through the UI.
    ///
    /// EndDate follows plain nullable-field semantics: omitted/null in the
    /// request leaves the existing EndDate unchanged. There's no way to
    /// clear an already-set EndDate back to null via this endpoint -- do
    /// that directly in the UI.
    /// </summary>
    /// <param name="PersonId">PersonId of the Person to update</param>
    /// <param name="Name">New full name, if changing -- ShortName is re-derived automatically</param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate">See remarks -- can only be set, not cleared, via this endpoint</param>
    /// <param name="FTE">0.0-1.0</param>
    public sealed record UpdatePersonRequestDTO(
        int PersonId,
        string? Name,
        DateTime? StartDate,
        DateTime? EndDate,
        double? FTE
    );

    /// <summary>
    /// Request body for POST /api/users/add. Creates an Access Control
    /// User directly, general-purpose: a bare User (e.g. a synthetic
    /// system account), or one linked to an existing Person via PersonId
    /// (e.g. provisioning a second account -- Manager/Superuser/API -- on
    /// a Person who already has one, the same way that's already
    /// possible in the data model; see People.GetAllPeopleAsync's own
    /// usernamesByPersonId comment). Not a substitute for normal SSO
    /// first-login (which is how someone gets their own first CapX
    /// account) -- for direct provisioning instead. One caller: the
    /// migration tooling's own use of this to create
    /// ImportService.FallbackAuthorUsername ("migration-import", the User
    /// Notes attribute to when a comment's original author can't be
    /// resolved) -- not what this endpoint is *for*, just one thing it's
    /// used for.
    /// </summary>
    /// <param name="CASUserName">Must be unique (case/whitespace-insensitive, same check as every other User)</param>
    /// <param name="EmailAddress">Required (matches User.EmailAddress -- non-nullable in the data model, used for SSO claim matching via User.MatchesClaim); semicolon-separated if more than one</param>
    /// <param name="Name">Display name -- required only when PersonId is omitted; when PersonId is given, the linked Person's own Name is used instead (same as the entity's own User.Person setter behaviour) and this is ignored</param>
    /// <param name="PersonId">Existing Person to link, if any -- omit for a bare User with no linked Person</param>
    /// <param name="RoleType">Must parse as a PPMTool.Data.Enums.RoleType value; defaults to "None" (no real permissions) if omitted</param>
    public sealed record ImportUserDTO(
        string CASUserName,
        string EmailAddress,
        string? Name,
        int? PersonId,
        string? RoleType
    );

    /// <summary>Response for a successful User import.</summary>
    /// <param name="UserId"></param>
    public sealed record ImportUserResponseDTO(
        int UserId
    );

    /// <summary>
    /// Request body for PUT /api/projects/notes/update. Corrects an
    /// existing Note's content -- identified by NoteId (from
    /// GET /api/projects/notes/getAll), not by any natural key, since
    /// unlike Timesheets/WorkloadModelChanges a Note has no unique
    /// (RTP, date) pair to upsert on (a project can have several Notes
    /// with the same CreatedDate). Doesn't touch Author -- an edit
    /// corrects the text, it doesn't reassign who posted it.
    /// </summary>
    /// <param name="NoteId">NoteId of the Note to update</param>
    /// <param name="HtmlContent">Replacement content (required, whole-value replace, not a diff/patch)</param>
    public sealed record UpdateNoteRequestDTO(
        int NoteId,
        string HtmlContent
    );

    /// <summary>Response for a successful Note update.</summary>
    /// <param name="NoteId"></param>
    public sealed record UpdateNoteResponseDTO(
        int NoteId
    );
}
