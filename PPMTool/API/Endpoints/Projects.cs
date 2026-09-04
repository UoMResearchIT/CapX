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
/// Project endpoint methods. CreateProject is Superuser-only write access,
/// gated behind SettingType.ImportApiEnabled -- see UoMResearchIT/CapX#1310.
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
                .Include(x => x.ProjectManager)
                .Include(x => x.InnateActivity)
                .OrderBy(x => x.RTP)
                .ToListAsync();

            var projectDtos = projects.Select(x => new ProjectDTO(
                ProjectId: x.RTP,
                CapXProjectId: x.ProjectId,
                Name: x.Name,
                PrincipalInvestigator: x.PI,
                ProjectManagerId: x.ProjectManager?.PersonId,
                ProjectManagerName: x.ProjectManager?.Name,
                TimesheetActivityCode: x.InnateActivity?.ActivityCode,
                TimesheetActivityName: x.InnateActivity?.ActivityName,
                RequestDocLink: x.RequestDocLink,
                ScrumProjectLink: x.ScrumProjectLink,
                ProjectStatus: x.ProjectStatus.GetDescription()
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
    /// Get one project by RTP project ID.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="http"></param>
    /// <param name="projectId">The RTP project ID.</param>
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
                .Include(x => x.ProjectManager)
                .Include(x => x.InnateActivity)
                .FirstOrDefaultAsync(x => x.RTP == projectId);

            if (project == null)
            {
                logger.LogWarning("API: GetProjectById: No project found for projectId {ProjectId}", projectId);
                return Results.NotFound();
            }

            var dto = new ProjectDTO(
                ProjectId: project.RTP,
                CapXProjectId: project.ProjectId,
                Name: project.Name,
                PrincipalInvestigator: project.PI,
                ProjectManagerId: project.ProjectManager?.PersonId,
                ProjectManagerName: project.ProjectManager?.Name,
                TimesheetActivityCode: project.InnateActivity?.ActivityCode,
                TimesheetActivityName: project.InnateActivity?.ActivityName,
                RequestDocLink: project.RequestDocLink,
                ScrumProjectLink: project.ScrumProjectLink,
                ProjectStatus: project.ProjectStatus.GetDescription()
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
    /// Create a Project (+ project-management SubTask, Resourcing, Comments)
    /// in one call. See UoMResearchIT/CapX#1310.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ImportProjectResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult CreateProject(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportProjectRequestDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Projects.CreateProject");
            if (!allowed) return gateResult!;

            var errors = importService.Validate(context, request, caller!);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Projects: project validation failed for '{Name}': {Errors}", request.Name, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.Create(context, request, caller!);
            logger.LogInformation(
                "API: Projects: created Project {ProjectId} '{Name}' ({ResourceCount} resources, {NoteCount} notes) by {User}",
                result.ProjectId, request.Name, result.ResourcesCreated, result.NotesCreated, caller!.Name);
            return Results.Created($"/api/projects/{result.ProjectId}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Projects: error creating project '{Name}'", request.Name);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Update an existing Project's core scalar fields. Identified by RTP.
    /// Doesn't touch Resourcing or Comments -- see Projects.CreateProject.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateProjectResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult UpdateProject(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] UpdateProjectRequestDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Projects.UpdateProject");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateProjectUpdate(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Projects: project update validation failed for RTP {RTP}: {Errors}", request.RTP, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.UpdateProject(context, request);
            logger.LogInformation(
                "API: Projects: updated Project {ProjectId} (RTP {RTP}) by {User}",
                result.ProjectId, request.RTP, caller!.Name);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Projects: error updating project RTP {RTP}", request.RTP);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Add Comments as Notes to an existing Project. Identified by RTP.
    /// The counterpart to CreateProject's own Comments handling for a
    /// project that's already been created -- CreateProject's Validate()
    /// rejects the whole call once the RTP/Name already exists, so there
    /// was previously no way to add Comments after the fact.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ImportNotesResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult AddNotes(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportNotesRequestDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Projects.AddNotes");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateNotesImport(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Projects: notes import validation failed for RTP {RTP}: {Errors}", request.RTP, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.AddNotes(context, request);
            logger.LogInformation(
                "API: Projects: added {NotesCreated} Note(s) to Project {ProjectId} (RTP {RTP}) by {User}",
                result.NotesCreated, result.ProjectId, request.RTP, caller!.Name);
            return Results.Json(result, statusCode: StatusCodes.Status201Created);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Projects: error adding notes to RTP {RTP}", request.RTP);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get all Notes for an existing Project, identified by RTP. An empty
    /// list is a normal result (a Project with no Notes yet), not an error.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<NoteDTO>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult GetNotes(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromQuery] int rtp)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Projects.GetNotes");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateNotesGet(context, rtp);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Projects: get notes validation failed for RTP {RTP}: {Errors}", rtp, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var notes = importService.GetNotesForRTP(context, rtp);
            logger.LogInformation("API: Projects: returned {Count} Note(s) for RTP {RTP} to {User}", notes.Count, rtp, caller!.Name);
            return Results.Json(notes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Projects: error getting notes for RTP {RTP}", rtp);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Correct an existing Note's content. Identified by NoteId (from
    /// GET /api/projects/notes/getAll).
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateNoteResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult UpdateNote(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] UpdateNoteRequestDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Projects.UpdateNote");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateNoteUpdate(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Projects: note update validation failed for NoteId {NoteId}: {Errors}", request.NoteId, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.UpdateNote(context, request, caller!);
            logger.LogInformation("API: Projects: updated Note {NoteId} by {User}", result.NoteId, caller!.Name);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Projects: error updating Note {NoteId}", request.NoteId);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
