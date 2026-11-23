// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;
using PPMTool.Data.Enums;
using PPMTool.Services;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Project endpoint methods.
/// </summary>
public static class Projects
{
    /// <summary>
    /// Get all projects.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProjectDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetAllProjectsAsync(
        PPMToolContext context,
        ILogger logger,
        HttpContext http)
    {
        try
        {
            var caller = GeneralHelpers.GetCurrentUser(http);
            var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
            if (!canAccess)
            {
                logger.LogWarning("API: GetAllProjects: Caller does not have permission to access project data.");
                return Results.Unauthorized();
            }

            var projects = await context.Projects
                .AsNoTracking()
                .Include(x => x.ProjectManager)
                .Include(x => x.InnateActivity)
                .Include(x => x.School)
                .OrderBy(x => x.RTP)
                .ToListAsync();

            var projectDtos = projects.Select(x => new ProjectDTO(
                ProjectId: x.ProjectId,
                CapXProjectId: x.RTP,
                Name: x.Name,
                PrincipalInvestigator: x.PI,
                ProjectManagerId: x.ProjectManager?.PersonId,
                ProjectManagerName: x.ProjectManager?.Name,
                TimesheetActivityCode: x.InnateActivity?.ActivityCode,
                TimesheetActivityName: x.InnateActivity?.ActivityName,
                RequestDocLink: x.RequestDocLink,
                ScrumProjectLink: x.ScrumProjectLink,
                ProjectStatus: x.ProjectStatus.GetDescription(),
                SchoolId: x.School.SchoolId,
                CostModel: x.CostModel.GetDescription(),
                StartDate: DateOnly.FromDateTime(x.StartDate),
                EndDate: DateOnly.FromDateTime(x.EndDate),
                Budget: x.Budget,
                DayRate: x.DayRate,
                Description: x.Description
            )).ToList();

            logger.LogInformation("API: GetAllProjects: Returned {Count} project records.", projectDtos.Count);
            return Results.Json(projectDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetAllProjects: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get one project its database primary key.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="http"></param>
    /// <param name="projectId">The database primary key of the project.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProjectDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetProjectByIdAsync(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        [FromQuery] int? projectId)
    {
        try
        {
            if (!projectId.HasValue || projectId.Value <= 0)
            {
                logger.LogWarning("API: GetProjectById: Invalid projectId {ProjectId}", projectId);
                return Results.BadRequest("Project ID must be greater than zero.");
            }

            var caller = GeneralHelpers.GetCurrentUser(http);
            var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
            if (!canAccess)
            {
                logger.LogWarning("API: GetProjectById: Caller does not have permission to access project data.");
                return Results.Unauthorized();
            }

            var project = await context.Projects
                .AsNoTracking()
                .Include(x => x.ProjectManager)
                .Include(x => x.InnateActivity)
                .Include(x => x.School)
                .FirstOrDefaultAsync(x => x.ProjectId == projectId);

            if (project == null)
            {
                logger.LogWarning("API: GetProjectById: No project found for projectId {ProjectId}", projectId);
                return Results.NotFound();
            }

            var dto = new ProjectDTO(
                ProjectId: project.ProjectId,
                CapXProjectId: project.RTP,
                Name: project.Name,
                PrincipalInvestigator: project.PI,
                ProjectManagerId: project.ProjectManager?.PersonId,
                ProjectManagerName: project.ProjectManager?.Name,
                TimesheetActivityCode: project.InnateActivity?.ActivityCode,
                TimesheetActivityName: project.InnateActivity?.ActivityName,
                RequestDocLink: project.RequestDocLink,
                ScrumProjectLink: project.ScrumProjectLink,
                ProjectStatus: project.ProjectStatus.GetDescription(),
                SchoolId: project.School.SchoolId,
                CostModel: project.CostModel.GetDescription(),
                StartDate: DateOnly.FromDateTime(project.StartDate),
                EndDate: DateOnly.FromDateTime(project.EndDate),
                Budget: project.Budget,
                DayRate: project.DayRate,
                Description: project.Description
            );

            logger.LogInformation("API: GetProjectById: Returned project record for projectId {ProjectId}", projectId);
            return Results.Json(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetProjectById: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get one project its CapX project ID.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="http"></param>
    /// <param name="capxProjectId">The CapX project ID of the project.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProjectDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetProjectByCapXProjectIdAsync(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        [FromQuery] int? capxProjectId)
    {
        try
        {
            if (!capxProjectId.HasValue || capxProjectId.Value <= 0)
            {
                logger.LogWarning("API: GetProjectByCapXProjectId: Invalid ID {ProjectId}", capxProjectId);
                return Results.BadRequest("ID must be greater than zero.");
            }

            var caller = GeneralHelpers.GetCurrentUser(http);
            var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
            if (!canAccess)
            {
                logger.LogWarning("API: GetProjectByCapXProjectId: Caller does not have permission to access project data.");
                return Results.Unauthorized();
            }

            var project = await context.Projects
                .AsNoTracking()
                .Include(x => x.ProjectManager)
                .Include(x => x.InnateActivity)
                .Include(x => x.School)
                .FirstOrDefaultAsync(x => x.RTP == capxProjectId);

            if (project == null)
            {
                logger.LogWarning("API: GetProjectByCapXProjectId: No project found for ID {ProjectId}", capxProjectId);
                return Results.NotFound();
            }

            var dto = new ProjectDTO(
                ProjectId: project.ProjectId,
                CapXProjectId: project.RTP,
                Name: project.Name,
                PrincipalInvestigator: project.PI,
                ProjectManagerId: project.ProjectManager?.PersonId,
                ProjectManagerName: project.ProjectManager?.Name,
                TimesheetActivityCode: project.InnateActivity?.ActivityCode,
                TimesheetActivityName: project.InnateActivity?.ActivityName,
                RequestDocLink: project.RequestDocLink,
                ScrumProjectLink: project.ScrumProjectLink,
                ProjectStatus: project.ProjectStatus.GetDescription(),
                SchoolId: project.School.SchoolId,
                CostModel: project.CostModel.GetDescription(),
                StartDate: DateOnly.FromDateTime(project.StartDate),
                EndDate: DateOnly.FromDateTime(project.EndDate),
                Budget: project.Budget,
                DayRate: project.DayRate,
                Description: project.Description
            );

            logger.LogInformation("API: GetProjectByCapXProjectId: Returned project record for ID {ProjectId}", capxProjectId);
            return Results.Json(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetProjectByCapXProjectId: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Create a Project with its Leadership task, any resourcing and its Notes, in one call.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WriteProjectResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(WriteErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult CreateProject(
        PPMToolContext context,
        ProjectWriteService writeService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] WriteProjectDTO request)
    {
        return GeneralHelpers.ExecuteWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Projects)}.{nameof(CreateProject)}",
            validate: caller => writeService.Validate(context, request, caller),
            logValidationFailure: errors => logger.LogWarning("API: Projects: project validation failed for '{Name}': {Errors}", request.Name, string.Join("; ", errors)),
            execute: caller =>
            {
                // Create saves several times (the Project, its InnateActivity, its tasks,
                // its Notes), so without a transaction a failure part-way through leaves
                // a half-created Project that a retry then rejects as a duplicate RTP.
                using var transaction = context.Database.BeginTransaction();
                var result = writeService.Create(context, request, caller);
                transaction.Commit();
                logger.LogInformation(
                    "API: Projects: created Project {ProjectId} '{Name}' ({ResourceCount} resources, {NoteCount} notes) by {User}",
                    result.ProjectId, request.Name, result.ResourcesCreated, result.NotesCreated, caller.Name);
                return Results.Created($"/api/projects/{result.ProjectId}", result);
            },
            logException: ex => logger.LogError(ex, "API: Projects: error creating project '{Name}'", request.Name));
    }

    /// <summary>
    /// Update a Project's fields, identified by RTP. A status change has the
    /// same side effects as in the UI. Doesn't change tasks, resourcing or Notes.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateProjectResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(WriteErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult UpdateProject(
        PPMToolContext context,
        ProjectWriteService writeService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] UpdateProjectRequestDTO request)
    {
        return GeneralHelpers.ExecuteWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Projects)}.{nameof(UpdateProject)}",
            validate: _ => writeService.ValidateProjectUpdate(context, request),
            logValidationFailure: errors => logger.LogWarning("API: Projects: project update validation failed for RTP {RTP}: {Errors}", request.RTP, string.Join("; ", errors)),
            execute: caller =>
            {
                // ProjectService.Update commits (with any new timesheet code) before the cost
                // recalculation saves again, so a failure in between would leave a partial update.
                using var transaction = context.Database.BeginTransaction();
                var result = writeService.UpdateProject(context, request);
                transaction.Commit();
                logger.LogInformation(
                    "API: Projects: updated Project {ProjectId} (RTP {RTP}) by {User}",
                    result.ProjectId, request.RTP, caller.Name);
                return Results.Ok(result);
            },
            logException: ex => logger.LogError(ex, "API: Projects: error updating project RTP {RTP}", request.RTP));
    }

    /// <summary>
    /// Add comments as Notes to an existing Project, identified by CapXProjectId. Notes
    /// aren't deduplicated.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WriteNotesResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(WriteErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult AddNotes(
        PPMToolContext context,
        ProjectWriteService writeService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] WriteNotesDTO request)
    {
        return GeneralHelpers.ExecuteWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Projects)}.{nameof(AddNotes)}",
            validate: _ => writeService.ValidateNotesAdd(context, request),
            logValidationFailure: errors => logger.LogWarning("API: Projects: notes/add validation failed for CapXProjectId {CapXProjectId}: {Errors}", request.CapXProjectId, string.Join("; ", errors)),
            execute: caller =>
            {
                // NoteService.Add commits each Note, and nothing de-duplicates them, so
                // without a transaction a failure part-way through leaves some Notes
                // behind that a retry of the whole request then adds a second time.
                using var transaction = context.Database.BeginTransaction();
                var result = writeService.AddNotes(context, request);
                transaction.Commit();
                logger.LogInformation(
                    "API: Projects: added {NotesCreated} Note(s) to Project {ProjectId} (CapXProjectId {CapXProjectId}) by {User}",
                    result.NotesCreated, result.ProjectId, request.CapXProjectId, caller.Name);
                return Results.Json(result, statusCode: StatusCodes.Status201Created);
            },
            logException: ex => logger.LogError(ex, "API: Projects: error adding notes to CapXProjectId {CapXProjectId}", request.CapXProjectId));
    }

    /// <summary>
    /// Get a Project's Notes, identified by CapXProjectId. A Project with no Notes gives an empty list.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<NoteDTO>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(WriteErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult GetNotes(
        PPMToolContext context,
        ProjectWriteService writeService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromQuery] int capxProjectId)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckWriteApiAccess(settingsService, http, logger, "Projects.GetNotes");
            if (!allowed) return gateResult!;

            var errors = writeService.ValidateNotesGet(context, capxProjectId);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Projects: get notes validation failed for CapXProjectId {CapXProjectId}: {Errors}", capxProjectId, string.Join("; ", errors));
                return Results.BadRequest(new WriteErrorDTO(errors));
            }

            var notes = writeService.GetNotesForCapXProjectId(context, capxProjectId);
            logger.LogInformation("API: Projects: returned {Count} Note(s) for CapXProjectId {CapXProjectId} to {User}", notes.Count, capxProjectId, caller!.Name);
            return Results.Json(notes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Projects: error getting notes for CapXProjectId {CapXProjectId}", capxProjectId);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Replace a Note's content, identified by NoteId (from GET /api/projects/notes/getAll).
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateNoteResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(WriteErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult UpdateNote(
        PPMToolContext context,
        ProjectWriteService writeService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] UpdateNoteRequestDTO request)
    {
        return GeneralHelpers.ExecuteWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Projects)}.{nameof(UpdateNote)}",
            validate: _ => writeService.ValidateNoteUpdate(context, request),
            logValidationFailure: errors => logger.LogWarning("API: Projects: note update validation failed for NoteId {NoteId}: {Errors}", request.NoteId, string.Join("; ", errors)),
            execute: caller =>
            {
                var result = writeService.UpdateNote(context, request, caller);
                logger.LogInformation("API: Projects: updated Note {NoteId} by {User}", result.NoteId, caller.Name);
                return Results.Ok(result);
            },
            logException: ex => logger.LogError(ex, "API: Projects: error updating Note {NoteId}", request.NoteId));
    }
}
