// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>Response for a rejected write. Nothing was written.</summary>
    /// <param name="Errors">Every validation error found</param>
    public sealed record ImportErrorDTO(
        IReadOnlyList<string> Errors
    );

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

    /// <summary>
    /// One assignment created with a new Project, or with a new Task, on that
    /// Task. The person is identified by username.
    /// </summary>
    /// <param name="Username">Username of a User with a linked Person</param>
    /// <param name="AssignmentFTE">Greater than zero, at most 3 decimal places</param>
    /// <param name="StartDate">Not used: an assignment's period is its Task's</param>
    /// <param name="EndDate">Not used: an assignment's period is its Task's</param>
    public sealed record ImportResourcingDTO(
        string Username,
        double AssignmentFTE,
        DateTime StartDate,
        DateTime EndDate
    );

    /// <summary>One comment to add as a Note on a Project.</summary>
    /// <param name="AuthorUsername">Username of the comment's author, if known. When it's missing or matches no User, the Note is attributed to the "migration-import" User, which must exist, and AuthorDisplayName is kept in its text.</param>
    /// <param name="AuthorDisplayName">The author's name as the source recorded it</param>
    /// <param name="ContentHtml">The comment, as HTML; sanitised on save</param>
    /// <param name="CreatedDate">When the comment was made</param>
    public sealed record ImportCommentDTO(
        string? AuthorUsername,
        string AuthorDisplayName,
        string ContentHtml,
        DateTime CreatedDate
    );

    /// <summary>
    /// Request body for POST /api/projects/notes/add. Adds comments as Notes to
    /// an existing Project. Notes aren't deduplicated, so sending the same
    /// comments twice adds them twice: check GET /api/projects/notes/getAll first.
    /// </summary>
    /// <param name="RTP">RTP of the Project</param>
    /// <param name="Comments">At least one</param>
    public sealed record ImportNotesRequestDTO(
        int RTP,
        IReadOnlyList<ImportCommentDTO> Comments
    );

    /// <summary>Response for added Notes.</summary>
    /// <param name="ProjectId"></param>
    /// <param name="NotesCreated"></param>
    public sealed record ImportNotesResponseDTO(
        int ProjectId,
        int NotesCreated
    );

    /// <summary>
    /// Request body for POST /api/projects/add. Creates a Project with its
    /// Leadership task over the management dates, a "Delivery" task carrying
    /// any Resourcing, and its Notes. The caller's own Person becomes the
    /// Request Owner, so the API key's owner needs a linked Person.
    /// </summary>
    /// <param name="Name">Must be unique</param>
    /// <param name="RTP">Must be unique and greater than zero</param>
    /// <param name="PI"></param>
    /// <param name="SchoolCode">Code of an active School</param>
    /// <param name="ProjectManagerUsername">Username of the Project Manager, if any; must have a linked Person</param>
    /// <param name="Budget"></param>
    /// <param name="CostModel">A PPMTool.Data.Enums.CostModel name</param>
    /// <param name="DayRate">Greater than zero when CostModel is "DayRate"; ignored otherwise</param>
    /// <param name="ProjectStatus">A PPMTool.Data.Enums.ProjectStatus name</param>
    /// <param name="Description">HTML; sanitised on save</param>
    /// <param name="RequestDocLink">Required; need not be a URL</param>
    /// <param name="ScrumProjectLink"></param>
    /// <param name="ManagementStartDate">Start of the Leadership (ProjectAndServiceMgmt) task every Project needs</param>
    /// <param name="ManagementEndDate">End of the Leadership task; on or after ManagementStartDate</param>
    /// <param name="Resourcing">Assignments to create on the Delivery task, if any</param>
    /// <param name="Comments">Comments to add as Notes, if any</param>
    /// <param name="TimesheetActivityCode">Timesheet code to link the Project to. An existing code is linked as it is, provided no other Project uses it; otherwise a code is created with this activity code, named after the Project, with tasks Development, Management and Maintenance. Without one, the Project has no code and can't receive timesheet entries until one is linked.</param>
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
        IReadOnlyList<ImportCommentDTO>? Comments,
        string? TimesheetActivityCode = null
    );

    /// <summary>Response for a created Project.</summary>
    /// <param name="ProjectId">The Project's database id</param>
    /// <param name="ResourcesCreated"></param>
    /// <param name="NotesCreated"></param>
    public sealed record ImportProjectResponseDTO(
        int ProjectId,
        int ResourcesCreated,
        int NotesCreated
    );

    /// <summary>
    /// Request body for PUT /api/projects/update. Identifies the Project by
    /// RTP and changes the fields supplied; a field left out or null is
    /// unchanged. Doesn't change tasks, assignments or Notes. A status change
    /// has the same side effects as in the UI: leaving New Request records
    /// when the request was completed, a cancelled status removes the
    /// Project's assignments, Finished or cancelled removes its followers,
    /// and becoming Active (other than from Paused) restarts the actuals clock.
    /// </summary>
    /// <param name="RTP">RTP of the Project</param>
    /// <param name="Name">Must be unique</param>
    /// <param name="PI"></param>
    /// <param name="SchoolCode">Code of an active School</param>
    /// <param name="ProjectManagerUsername">Username of the Project Manager, which must have a linked Person; an empty string clears it</param>
    /// <param name="Budget"></param>
    /// <param name="CostModel">A PPMTool.Data.Enums.CostModel name</param>
    /// <param name="DayRate">Greater than zero if the resulting CostModel is "DayRate"</param>
    /// <param name="ProjectStatus">A PPMTool.Data.Enums.ProjectStatus name</param>
    /// <param name="Description">HTML; sanitised on save</param>
    /// <param name="RequestDocLink"></param>
    /// <param name="ScrumProjectLink">An empty string clears it</param>
    /// <param name="TimesheetActivityCode">Timesheet code to link the Project to, as for POST /api/projects/add. The current code can't be replaced while it has timesheet entries, since the Project's actuals come through it; an unused current code that no other Project uses is renamed instead. The link can't be cleared.</param>
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
        string? ScrumProjectLink,
        string? TimesheetActivityCode = null
    );

    /// <summary>Response for an updated Project.</summary>
    /// <param name="ProjectId"></param>
    public sealed record UpdateProjectResponseDTO(
        int ProjectId
    );

    /// <summary>
    /// Request body for POST /api/tasks/add. Creates a fixed-duration Task
    /// with a fixed start and end and no predecessor, the shape CapX schedules
    /// without moving its dates.
    /// </summary>
    /// <param name="RTP">RTP of the Project</param>
    /// <param name="Name">Unique within the Project, except for Leadership (ProjectAndServiceMgmt) tasks, as in the UI</param>
    /// <param name="TaskDuty">A PPMTool.Data.Enums.Duty name</param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate">On or after StartDate</param>
    /// <param name="Demand">FTE needed; sets both Demand and OriginalDemand. Greater than zero, at most 3 decimal places.</param>
    /// <param name="Resourcing">Assignments to create on the Task, if any. Each assignee must start by the Task's start, and only managers can go on a Leadership task.</param>
    public sealed record ImportTaskDTO(
        int RTP,
        string Name,
        string TaskDuty,
        DateTime StartDate,
        DateTime EndDate,
        double Demand,
        IReadOnlyList<ImportResourcingDTO>? Resourcing
    );

    /// <summary>Response for a created Task.</summary>
    /// <param name="SubTaskId"></param>
    /// <param name="ResourcesCreated"></param>
    public sealed record ImportTaskResponseDTO(
        int SubTaskId,
        int ResourcesCreated
    );

    /// <summary>
    /// Request body for PUT /api/tasks/update. Identifies the Task by
    /// SubTaskId and changes the fields supplied. Doesn't change assignments:
    /// see PUT /api/tasks/resourcing/update. Dates can only be changed on the
    /// shape POST /api/tasks/add creates; on any other Task, CapX would
    /// recalculate them, so change them in the UI. A Project's last
    /// Leadership task can't be given another duty.
    /// </summary>
    /// <param name="SubTaskId">From GET /api/tasks/getAll</param>
    /// <param name="Name">Unique within the Project, as on creation</param>
    /// <param name="TaskDuty">A PPMTool.Data.Enums.Duty name</param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate">On or after the resulting StartDate</param>
    /// <param name="Demand">FTE needed, at most 3 decimal places. Changes the current Demand; OriginalDemand keeps what was asked for at creation.</param>
    public sealed record UpdateTaskRequestDTO(
        int SubTaskId,
        string? Name,
        string? TaskDuty,
        DateTime? StartDate,
        DateTime? EndDate,
        double? Demand
    );

    /// <summary>Response for an updated Task.</summary>
    /// <param name="SubTaskId"></param>
    public sealed record UpdateTaskResponseDTO(
        int SubTaskId
    );

    /// <summary>
    /// One assignment to add to an existing Task. Name the person by exactly
    /// one of Username or PersonId; PersonId reaches a Person with no User,
    /// such as one created by POST /api/people/add. An assignment has no dates
    /// of its own: its period is its Task's.
    /// </summary>
    /// <param name="Username">Username of a User with a linked Person</param>
    /// <param name="PersonId">Id of a Person, with or without a User</param>
    /// <param name="AssignmentFTE">Greater than zero, at most 3 decimal places</param>
    /// <param name="IsProvisional">Defaults to true</param>
    public sealed record ImportResourceAssignmentDTO(
        string? Username,
        int? PersonId,
        double AssignmentFTE,
        bool? IsProvisional
    );

    /// <summary>
    /// Request body for POST /api/tasks/resourcing/add. Adds assignments to an
    /// existing Task. A person already assigned to the Task is rejected rather
    /// than duplicated: use PUT /api/tasks/resourcing/update to change their
    /// assignment. Not allowed on a fixed-work Task, whose dates CapX
    /// recalculates from the FTE assigned to it.
    /// </summary>
    /// <param name="SubTaskId">From GET /api/tasks/getAll</param>
    /// <param name="Resourcing">At least one assignment</param>
    public sealed record ImportTaskResourcingRequestDTO(
        int SubTaskId,
        IReadOnlyList<ImportResourceAssignmentDTO> Resourcing
    );

    /// <summary>Response for added assignments.</summary>
    /// <param name="SubTaskId"></param>
    /// <param name="ResourcesCreated"></param>
    public sealed record ImportTaskResourcingResponseDTO(
        int SubTaskId,
        int ResourcesCreated
    );

    /// <summary>
    /// Request body for PUT /api/tasks/resourcing/update. Identifies the
    /// assignment by ResourceId and changes the fields supplied. Setting
    /// AssignmentFTE to zero retracts the assignment; there is no DELETE.
    /// Moving it to another Task (NewSubTaskId) is how its period changes.
    /// Not allowed when either Task is fixed-work.
    /// </summary>
    /// <param name="ResourceId">From GET /api/tasks/resourcing/getAll</param>
    /// <param name="AssignmentFTE">Zero or more, at most 3 decimal places</param>
    /// <param name="NewSubTaskId">Task to move the assignment to</param>
    /// <param name="IsProvisional"></param>
    public sealed record UpdateTaskResourcingRequestDTO(
        int ResourceId,
        double? AssignmentFTE,
        int? NewSubTaskId,
        bool? IsProvisional
    );

    /// <summary>Response for an updated assignment.</summary>
    /// <param name="ResourceId"></param>
    /// <param name="SubTaskId">The Task the assignment is now on</param>
    public sealed record UpdateTaskResourcingResponseDTO(
        int ResourceId,
        int SubTaskId
    );

    /// <summary>
    /// Request body for POST /api/timesheets/add. One person's hours for one
    /// week on one task of a Project's timesheet code. Writes into the
    /// person's existing timesheet for that week whatever its status, or
    /// creates an Approved one. Only Approved timesheets count towards a
    /// Project's actuals. Sending the same person, week and task again
    /// replaces that entry's hours. Name the person by exactly one of
    /// Username or PersonId.
    /// </summary>
    /// <param name="Username">Username of a User with a linked Person</param>
    /// <param name="PersonId">Id of a Person, with or without a User</param>
    /// <param name="ProjectId">The Project's database id, not its RTP. The Project must have a timesheet code.</param>
    /// <param name="WeekStartDate">A Monday, with no time of day</param>
    /// <param name="TaskName">One of the tasks on the Project's timesheet code</param>
    /// <param name="MondayHours"></param>
    /// <param name="TuesdayHours"></param>
    /// <param name="WednesdayHours"></param>
    /// <param name="ThursdayHours"></param>
    /// <param name="FridayHours"></param>
    /// <param name="SaturdayHours"></param>
    /// <param name="SundayHours"></param>
    public sealed record ImportTimesheetEntryDTO(
        string? Username,
        int? PersonId,
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

    /// <summary>Response for a written timesheet entry.</summary>
    /// <param name="TimesheetId"></param>
    /// <param name="TimesheetCreated">False if the person already had a timesheet for that week</param>
    /// <param name="EntryCreated">False if an existing entry for the task was updated</param>
    /// <param name="TotalHours">The entry's hours for the week</param>
    public sealed record ImportTimesheetResponseDTO(
        int TimesheetId,
        bool TimesheetCreated,
        bool EntryCreated,
        double TotalHours
    );

    /// <summary>
    /// Request body for PUT /api/timesheets/update. Identifies the entry by
    /// TimesheetEntryId and changes the fields supplied, so a single day can
    /// be corrected, or the entry moved to another task, without resending
    /// the week.
    /// </summary>
    /// <param name="TimesheetEntryId">From GET /api/timesheets/getEntries</param>
    /// <param name="NewTaskName">Another task on the same timesheet code, which has no entry on this timesheet yet</param>
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

    /// <summary>Response for an updated timesheet entry.</summary>
    /// <param name="TimesheetEntryId"></param>
    /// <param name="TotalHours">The entry's hours for the week, after the update</param>
    public sealed record UpdateTimesheetEntryResponseDTO(
        int TimesheetEntryId,
        double TotalHours
    );

    /// <summary>
    /// Request body for POST /api/workloadmodels/add. A person's duty split,
    /// in effect from ChangeDate until their next change, with the same
    /// ranges the UI enforces. ProjectAndServiceManagementFTE is derived from
    /// ProjectManagementFTE and ServiceManagementFTE. A change on the same
    /// date as an existing one updates it.
    /// </summary>
    /// <param name="Username">Username of a User with a linked Person</param>
    /// <param name="ChangeDate">With no time of day</param>
    /// <param name="Grade">4-9</param>
    /// <param name="ProjectWorkFTE">0.0-1.0</param>
    /// <param name="BusinessAsUsualFTE">0.0-1.0</param>
    /// <param name="PersonalDevelopmentFTE">0.0-1.0</param>
    /// <param name="StaffManagementFTE">0.0-1.0</param>
    /// <param name="ArchitectureFTE">0.0-1.0</param>
    /// <param name="ServiceManagementFTE">0.0-1.0</param>
    /// <param name="ProjectManagementFTE">0.0-1.0</param>
    /// <param name="Notes"></param>
    /// <param name="CostValueKey">Cost key: the name of the financial reference value set that prices the person's time (e.g. "Grade65Costs"), as GET /api/wlm/getAll returns it. Without one, assignments under this change are costed at zero, as in the UI. Null leaves an existing change's key unchanged.</param>
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
        string? Notes,
        string? CostValueKey = null
    );

    /// <summary>Response for a written workload model change.</summary>
    /// <param name="WorkloadModelChangeId"></param>
    /// <param name="Created">False if an existing change on that date was updated</param>
    public sealed record ImportWorkloadModelChangeResponseDTO(
        int WorkloadModelChangeId,
        bool Created
    );

    /// <summary>
    /// Request body for POST /api/people/add. Creates a Person with no User,
    /// for someone who never logs in, such as a former member of staff whose
    /// work is being recorded. A duplicate name or initials is rejected, as
    /// in the UI.
    /// </summary>
    /// <param name="Name">Full name; the initials are derived from it</param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate">Null if still in post</param>
    /// <param name="FTE">The post's FTE (Person.FTE), 0.0-1.0. Part-time working belongs in a workload model change instead.</param>
    public sealed record ImportPersonDTO(
        string Name,
        DateTime StartDate,
        DateTime? EndDate,
        double FTE
    );

    /// <summary>Response for a created Person.</summary>
    /// <param name="PersonId"></param>
    /// <param name="ShortName">The derived initials</param>
    public sealed record ImportPersonResponseDTO(
        int PersonId,
        string ShortName
    );

    /// <summary>
    /// Request body for PUT /api/people/update. Identifies the Person by
    /// PersonId and changes the fields supplied. EndDate and the line manager
    /// can be set but not cleared; clear them in the UI. Name the line manager
    /// by at most one of LineManagerPersonId or LineManagerUsername;
    /// LineManagerPersonId reaches a Person with no User, whom the UI's own
    /// line-manager picker can't offer.
    /// </summary>
    /// <param name="PersonId"></param>
    /// <param name="Name">Full name; the initials are re-derived</param>
    /// <param name="StartDate">Not after the start of any Task the person is assigned to</param>
    /// <param name="EndDate"></param>
    /// <param name="FTE">0.0-1.0</param>
    /// <param name="LineManagerPersonId">Id of the line manager's Person; not the Person being updated</param>
    /// <param name="LineManagerUsername">Username of the line manager, which must have a linked Person</param>
    public sealed record UpdatePersonRequestDTO(
        int PersonId,
        string? Name,
        DateTime? StartDate,
        DateTime? EndDate,
        double? FTE,
        int? LineManagerPersonId = null,
        string? LineManagerUsername = null
    );

    /// <summary>
    /// Request body for POST /api/users/add. Creates a User directly: a bare
    /// account, or one linked to an existing Person (a Person can have more
    /// than one User). People normally get their first User by signing in.
    /// </summary>
    /// <param name="CASUserName">Unique, ignoring case and surrounding whitespace</param>
    /// <param name="EmailAddress">Used to match sign-in claims; semicolon-separated if more than one</param>
    /// <param name="Name">Display name; required without PersonId, and replaced by the Person's name with one</param>
    /// <param name="PersonId">Person to link the User to, if any</param>
    /// <param name="RoleType">A PPMTool.Data.Enums.RoleType name; defaults to "None"</param>
    public sealed record ImportUserDTO(
        string CASUserName,
        string EmailAddress,
        string? Name,
        int? PersonId,
        string? RoleType
    );

    /// <summary>Response for a created User.</summary>
    /// <param name="UserId"></param>
    public sealed record ImportUserResponseDTO(
        int UserId
    );

    /// <summary>
    /// Request body for PUT /api/projects/notes/update. Replaces a Note's
    /// content, identified by NoteId. The author is unchanged.
    /// </summary>
    /// <param name="NoteId">From GET /api/projects/notes/getAll</param>
    /// <param name="HtmlContent">The whole new content, as HTML; sanitised on save</param>
    public sealed record UpdateNoteRequestDTO(
        int NoteId,
        string HtmlContent
    );

    /// <summary>Response for an updated Note.</summary>
    /// <param name="NoteId"></param>
    public sealed record UpdateNoteResponseDTO(
        int NoteId
    );
}
