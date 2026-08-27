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
                EndDate: DateOnly.FromDateTime(x.EndDate)
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
                EndDate: DateOnly.FromDateTime(project.EndDate)
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
                EndDate: DateOnly.FromDateTime(project.EndDate)
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

            var errors = importService.Validate(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Projects: project validation failed for '{Name}': {Errors}", request.Name, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.Create(context, request);
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
}
