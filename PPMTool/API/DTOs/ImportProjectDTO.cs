// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
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
    /// <param name="SchoolCode">Code of an active School. Give this or SchoolId.</param>
    /// <param name="ProjectManagerUsername">Username of the Project Manager, if any; must have a linked Person</param>
    /// <param name="Budget"></param>
    /// <param name="CostModel">A <see cref="PPMTool.Data.Enums.CostModel"/> name</param>
    /// <param name="DayRate">Greater than zero when CostModel is "DayRate"; ignored otherwise</param>
    /// <param name="ProjectStatus">A <see cref="PPMTool.Data.Enums.ProjectStatus"/> name</param>
    /// <param name="Description">HTML; sanitised on save</param>
    /// <param name="RequestDocLink">Required; need not be a URL</param>
    /// <param name="ScrumProjectLink"></param>
    /// <param name="ManagementStartDate">Start of the Leadership (ProjectAndServiceMgmt) task every Project needs</param>
    /// <param name="ManagementEndDate">End of the Leadership task; on or after ManagementStartDate</param>
    /// <param name="Resourcing">Assignments to create on the Delivery task, if any</param>
    /// <param name="Comments">Comments to add as Notes, if any</param>
    /// <param name="SchoolId">Id of an active School, as an alternative to SchoolCode. If both are given, they must name the same School.</param>
    /// <param name="TimesheetActivityCode">Timesheet code to link the Project to. An existing code is linked as it is, provided no other Project uses it; otherwise a code is created with this activity code, named after the Project, with tasks Development, Management and Maintenance. Without one, the Project has no code and can't receive timesheet entries until one is linked.</param>
    public sealed record ImportProjectRequestDTO(
        string Name,
        int RTP,
        string PI,
        string? SchoolCode,
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
        string? TimesheetActivityCode = null,
        int? SchoolId = null
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
    /// <param name="CostModel">A <see cref="PPMTool.Data.Enums.CostModel"/> name</param>
    /// <param name="DayRate">Greater than zero if the resulting CostModel is "DayRate"</param>
    /// <param name="ProjectStatus">A <see cref="PPMTool.Data.Enums.ProjectStatus"/> name</param>
    /// <param name="Description">HTML; sanitised on save</param>
    /// <param name="RequestDocLink"></param>
    /// <param name="ScrumProjectLink">An empty string clears it</param>
    /// <param name="SchoolId">Id of an active School, as an alternative to SchoolCode. If both are given, they must name the same School.</param>
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
        string? TimesheetActivityCode = null,
        int? SchoolId = null
    );

    /// <summary>Response for an updated Project.</summary>
    /// <param name="ProjectId"></param>
    public sealed record UpdateProjectResponseDTO(
        int ProjectId
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
