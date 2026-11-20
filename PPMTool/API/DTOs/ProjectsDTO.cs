// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a project suitable for API data exports.
    /// </summary>
    /// <param name="ProjectId">The project ID of the entity in the database.</param>
    /// <param name="CapXProjectId">The UI-visible project reference number.</param>
    /// <param name="Name">The name of the project.</param>
    /// <param name="PrincipalInvestigator">The principal investigator on the project.</param>
    /// <param name="ProjectManagerId">The person ID of the project manager, if one is assigned.</param>
    /// <param name="ProjectManagerName">The name of the project manager, if one is assigned.</param>
    /// <param name="TimesheetActivityCode">The timesheet activity code linked to the project, if one is assigned.</param>
    /// <param name="TimesheetActivityName">The timesheet activity name linked to the project, if one is assigned.</param>
    /// <param name="RequestDocLink">The request document link.</param>
    /// <param name="ScrumProjectLink">The scrum project link.</param>
    /// <param name="ProjectStatus">The current project status.</param>
    /// <param name="SchoolId">The ID of the school associated with the project.</param>
    /// <param name="CostModel">The cost model for the project, as its description. The write endpoints accept this or the enum name.</param>
    /// <param name="StartDate">The start date of the project.</param>
    /// <param name="EndDate">The end date of the project.</param>
    /// <param name="Budget">The amount the PI has requested from the funder.</param>
    /// <param name="DayRate">The day rate; zero unless the cost model is DayRate.</param>
    /// <param name="Description">The project description, as HTML.</param>
    public sealed record ProjectDTO(
        int ProjectId,
        int CapXProjectId,
        string Name,
        string PrincipalInvestigator,
        int? ProjectManagerId,
        string? ProjectManagerName,
        string? TimesheetActivityCode,
        string? TimesheetActivityName,
        string RequestDocLink,
        string? ScrumProjectLink,
        string ProjectStatus,
        int SchoolId,
        string CostModel,
        DateOnly StartDate,
        DateOnly EndDate,
        double Budget,
        double DayRate,
        string Description
    );

    /// <summary>
    /// Request body for POST /api/projects/add. Creates a Project with its
    /// Leadership task over the management dates, a "Delivery" task carrying
    /// any Resourcing, and its Notes. The caller's own Person becomes the
    /// Request Owner, so the API key's owner needs a linked Person.
    /// Kept separate from <see cref="ProjectDTO"/>: a request carries only what the caller
    /// supplies, and the read record adds the ids and fields CapX derives or links.
    /// </summary>
    /// <param name="Name">Must be unique</param>
    /// <param name="RTP">Must be unique and greater than zero</param>
    /// <param name="PI"></param>
    /// <param name="SchoolCode">Code of an active School. Give this or SchoolId.</param>
    /// <param name="ProjectManagerUsername">Username of the Project Manager, if any; must have a linked Person</param>
    /// <param name="Budget"></param>
    /// <param name="CostModel">A <see cref="PPMTool.Data.Enums.CostModel"/> name, or its description as project reads return it</param>
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
    public sealed record WriteProjectDTO(
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
        IReadOnlyList<WriteSubTaskResourcingDTO>? Resourcing,
        IReadOnlyList<WriteNoteDTO>? Comments,
        string? TimesheetActivityCode = null,
        int? SchoolId = null
    );

    /// <summary>Response for a created Project.</summary>
    /// <param name="ProjectId">The Project's database id</param>
    /// <param name="ResourcesCreated"></param>
    /// <param name="NotesCreated"></param>
    public sealed record WriteProjectResponseDTO(
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
    /// <param name="CostModel">A <see cref="PPMTool.Data.Enums.CostModel"/> name, or its description as project reads return it</param>
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
}
